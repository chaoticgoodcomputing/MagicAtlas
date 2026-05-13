using Flowthru.Cli;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Flows.CardProcessing;
using MagicAtlas.Flows.Clustering;
using MagicAtlas.Flows.Ingest;
using MagicAtlas.Flows.OracleEmbedding;
using MagicAtlas.Flows.Reporting;
using MagicAtlas.Flows.RulesProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Harness;

/// <summary>
/// FlowthruCli entry point for the atlas pipeline harness. Wires the local-filesystem
/// <see cref="Catalog"/> (rooted at this project's <c>Data/</c> folder), the Python runtime
/// (venv + module search path resolved against the <c>libs/atlas-flows</c> source tree), and the
/// four pipeline flows:
/// </summary>
/// <list type="number">
/// <item><b>Ingest</b> — owns the HTTP boundary; fetches MTG rules + Scryfall card/symbology data
/// and persists into the <c>_01_Raw</c> layer.</item>
/// <item><b>CardProcessing</b> — typed Scryfall card parsing + commander-format filter.</item>
/// <item><b>RulesProcessing</b> — section/rule/glossary extraction from the rules text.</item>
/// <item><b>OracleEmbedding</b> — Python BERT + UMAP for the atlas-api's scatter plot.</item>
/// </list>
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

    // HttpClient threaded into Ingest's source steps by closure capture. Not DI-registered —
    // Flowthru's `HttpStorageMediumProvider` has two public constructors (IOptions + a
    // test-friendly HttpClient one), and adding HttpClient to DI would make its
    // constructor-disambiguation fail at activation time.
    //
    // Wrapped in a `FilesystemHttpCacheHandler` so repeated dev runs within 24h reuse the
    // previously-fetched upstream bytes (oracle bulk JSON, symbology, rules .txt) from
    // `{harness}/.http-cache/` instead of re-downloading. The TTL matches Scryfall's daily bulk
    // rotation cadence; an expired entry on the next run triggers a normal refetch.
    var httpCachePath = Path.Combine(basePath, ".http-cache");
    // Factory deliberately not disposed — the HttpClient (and its handler's logger) outlives this
    // method via closure capture into the flow factories, and the process owns both for its
    // lifetime; the OS reclaims on exit.
    var httpCacheLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var httpCacheHandler = new FilesystemHttpCacheHandler(
      new SocketsHttpHandler(),
      httpCachePath,
      TimeSpan.FromHours(24),
      httpCacheLoggerFactory.CreateLogger<FilesystemHttpCacheHandler>()
    );
    var httpClient = new HttpClient(httpCacheHandler) { Timeout = TimeSpan.FromMinutes(5) };
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
      flowthru.RegisterCatalog(_ => new Catalog(dataPath));
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

      // Ingest is the only flow that takes HttpClient — it's the dedicated HTTP boundary.
      // Downstream flows read whatever Ingest persisted into the _01_Raw layer.
      flowthru
        .RegisterFlow<Catalog>(
          "Ingest",
          catalog => IngestFlow.Create(catalog, httpClient)
        )
        .WithDescription(
          "Fetches MTG rules + Scryfall card and symbology data into the _01_Raw layer"
        );

      flowthru
        .RegisterFlow<Catalog>("RulesProcessing", RulesProcessingFlow.Create)
        .WithDescription("Parses the comprehensive rules text into structured JSON");

      flowthru
        .RegisterFlow<Catalog, CardProcessingFlowConfig>(
          "CardProcessing",
          CardProcessingFlow.Create
        )
        .WithDescription("Processes Scryfall card data and preps for analysis");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "OracleEmbedding",
          OracleEmbeddingFlow.Create
        )
        .WithDescription(
          "BERT encode + UMAP→2D (Python): produces bert-embeddings.parquet + atlas-points.json"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "Clustering",
          ClusteringFlow.Create
        )
        .WithDescription(
          "UMAP→5D + HDBSCAN + c-TF-IDF (Python): produces cluster assignments and labels"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "Reporting",
          ReportingFlow.Create
        )
        .WithDescription(
          "Renders the atlas embedding as a standalone Plotly HTML (index.html)"
        );
    });
  }
}
