using Flowthru.Cli;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Http;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Flows.CardProcessing;
using MagicAtlas.Flows.OracleEmbedding;
using MagicAtlas.Flows.RulesProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Harness;

/// <summary>
/// FlowthruCli entry point for the atlas pipeline harness. Wires the in-lib
/// <see cref="Catalog"/> (filesystem + HTTP-backed under this project's <c>Data/</c> folder), the
/// Python runtime (venv + module search path resolved against the <c>libs/atlas-flows</c> source
/// tree), the HTTP-cached storage medium (conditional GETs for upstream raw data), and the three
/// currently-implemented flows: RulesProcessing, CardProcessing, OracleEmbedding.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveHarnessDirectory())
    );

  /// <summary>
  /// Locate this project's directory from either <see cref="AppContext.BaseDirectory"/> (Nx-routed
  /// builds drop outputs under <c>dist/tests/atlas-flow-test/</c>, not next to the csproj) or the
  /// caller's CWD. Walks upward looking for the csproj.
  /// </summary>
  private static string ResolveHarnessDirectory()
  {
    const string csproj = "MagicAtlas.Flows.Harness.csproj";
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, csproj))) return dir.FullName;
      dir = dir.Parent;
    }
    var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (cwd is not null)
    {
      var candidate = Path.Combine(cwd.FullName, "tests", "atlas-flow-test", csproj);
      if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
      cwd = cwd.Parent;
    }
    throw new InvalidOperationException(
      $"Could not locate {csproj}. Run from within the workspace, or set CWD to tests/atlas-flow-test."
    );
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var dataPath = Path.Combine(basePath, "Data");
    var httpCachePath = Path.Combine(basePath, ".http-cache");

    // libs/atlas-flows lives two levels up from this harness; it owns the pyproject.toml,
    // the .venv created from it, and the Python step modules referenced by AddPythonStep.
    var atlasFlowsRoot = Path.GetFullPath(
      Path.Combine(basePath, "..", "..", "libs", "atlas-flows")
    );
    var venvPath = Path.Combine(atlasFlowsRoot, ".venv");

    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    // Plain HttpClient passed into the catalog ctor (Scryfall bulk-metadata resolution) and the
    // FetchCardSymbols / FetchRulesText source steps. Captured by closure rather than registered
    // as a DI singleton — `HttpStorageMediumProvider` has two public constructors (the IOptions
    // one and a test-friendly HttpClient one) and registering HttpClient in DI makes the
    // activator's constructor-disambiguation fail. Flowthru's own HTTP cache is set up below via
    // UseHttp(), which manages its own HttpClient internally for the cached medium.
    var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MagicAtlas/0.1");
    // Scryfall's API (api.scryfall.com) rejects requests that don't carry an explicit Accept
    // header with HTTP 400 — .NET's default GetAsync sends no Accept at all, so set one here.
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
    );
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html")
    );
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain")
    );

    services.AddFlowthru(flowthru =>
    {
      // Enable HTTP-scheme storage-medium dispatch with on-disk conditional-GET caching. Items
      // declared with `.AtPath("https://…").WithResolver(_resolver)` (currently just RawCards) go
      // through CachedHttpStorageMedium — same-day re-runs see a 304 served from disk.
      flowthru.UseHttp(http =>
      {
        http.Cache = new HttpCacheOptions
        {
          Directory = httpCachePath,
          MaxAge = TimeSpan.FromHours(24),
        };
        http.UserAgent = "MagicAtlas/0.1";
        http.Timeout = TimeSpan.FromMinutes(10);
      });

      flowthru.RegisterCatalog(sp => new Catalog(
        dataPath,
        httpClient,
        sp.GetRequiredService<IStorageMediumResolver>()
      ));
      flowthru.RegisterCatalog(sp => new CardProcessingFlowConfig(
        sp.GetRequiredService<IConfiguration>()
      ));

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });

      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(atlasFlowsRoot);
        python.VenvPath = venvPath;
      });

      // HttpClient is closure-captured rather than DI-injected (see comment above), so the two
      // flows that need it use lambda factories that thread it in. OracleEmbedding doesn't need
      // HTTP itself — it gets IPythonExecutor from DI via UsePython().
      flowthru
        .RegisterFlow<Catalog>(
          "RulesProcessing",
          catalog => RulesProcessingFlow.Create(catalog, httpClient)
        )
        .WithDescription("Auto-fetches MTG comprehensive rules and parses into structured JSON");

      flowthru
        .RegisterFlow<Catalog, CardProcessingFlowConfig>(
          "CardProcessing",
          (catalog, config) => CardProcessingFlow.Create(catalog, config, httpClient)
        )
        .WithDescription(
          "Auto-fetches Scryfall card+symbology bulk and preps cards for analysis"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "OracleEmbedding",
          OracleEmbeddingFlow.Create
        )
        .WithDescription("BERT + UMAP (Python): produces atlas-points.json for the API");
    });
  }
}
