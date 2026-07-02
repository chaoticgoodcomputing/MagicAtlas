using Flowthru.Caching;
using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Flows.CardProcessing;
using MagicAtlas.Flows.FineTune;
using MagicAtlas.Flows.FineTuneEval;
using MagicAtlas.Flows.Ingest;
using MagicAtlas.Flows.OracleEmbedding;
using MagicAtlas.Flows.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Harness;

/// <summary>
/// FlowthruCli entry point for the explorer-mode atlas pipeline. Wires the local-filesystem
/// <see cref="Catalog"/> (rooted at this project's <c>Data/</c> folder), the Python runtime
/// (venv + module search path resolved against the <c>libs/atlas-flows</c> source tree), and the
/// six pipeline flows:
/// </summary>
/// <list type="number">
/// <item><b>Ingest</b> — HTTP boundary; fetches Scryfall card/symbology bytes. (MTG rules text
/// moved to the standalone <c>mtg-rules</c> project.)</item>
/// <item><b>CardProcessing</b> — typed Scryfall card parsing + commander-format filter.</item>
/// <item><b>FineTune</b> — MTG-corpus fine-tune of the base sentence-transformer.</item>
/// <item><b>OracleEmbedding</b> — encode oracle lines → unsupervised UMAP → 2D atlas coordinates.</item>
/// <item><b>Reporting</b> — render the atlas as a standalone Plotly HTML.</item>
/// </list>
/// <remarks>
/// Categorical/cluster machinery (HDBSCAN, supervised UMAP, archetype taxonomy, attribution
/// scorecards) is intentionally absent — exploiter-mode queries are handled by MagicAST
/// (libs/magic-ast/), not by statistical attribution. The atlas here is explorer-mode only:
/// "show me cards near this one" via semantic similarity.
/// </remarks>
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

    // Surface the data root to Python steps via env var. The subprocess Python inherits this,
    // and steps that materialize sidecar artifacts (e.g. the FineTune flow writing model files
    // under _06_Models/) construct absolute paths off it. Avoids each Python step learning
    // the catalog's `_basePath` indirectly.
    Environment.SetEnvironmentVariable("MAGIC_ATLAS_DATA", dataPath);

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
    var httpCachePath = Path.Combine(basePath, "Data/_01_Raw/Datasets/External/.http-cache");

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
      flowthru.RegisterCatalog(sp => new Catalog(
        dataPath, sp.GetRequiredService<IConfiguration>()
      ));
      flowthru.RegisterCatalog(sp => new CardProcessingFlowConfig(
        sp.GetRequiredService<IConfiguration>()
      ));

      // Enable smart caching. The cache manifest tracks per-step composite identities
      // (source-hash + input fingerprints) and short-circuits steps whose inputs and code
      // haven't changed since the last successful run. Python steps participate iff they're
      // marked `cacheable=True` on the `@step` decorator.
      flowthru.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
          .Json()
          .AtPath(Path.Combine(basePath, ".flowthru", "cache.json"))
          .Build()
      );

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
        .RegisterFlow<Catalog, CardProcessingFlowConfig>(
          "CardProcessing",
          CardProcessingFlow.Create
        )
        .WithDescription("Processes Scryfall card data and preps for analysis");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "FineTune",
          FineTuneFlow.Create
        )
        .WithDescription(
          "Owns the embedding-model lifecycle (base model download + MTG-corpus fine-tune)"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "OracleEmbedding",
          OracleEmbeddingFlow.Create
        )
        .WithDescription(
          "Encode oracle lines via the fine-tuned model + unsupervised UMAP → 2D atlas coordinates"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "Reporting",
          ReportingFlow.Create
        )
        .WithDescription(
          "Renders the atlas embedding as a standalone Plotly HTML (index.html)"
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "FineTuneEval",
          FineTuneEvalFlow.Create
        )
        .WithDescription(
          "Diagnostic: encodes the corpus + training-pair set under base AND fine-tuned "
            + "models and emits a base-vs-fine-tuned health scorecard "
            + "(geometry + per-source triplet margins). Run on demand."
        );
    });
  }
}
