using Flowthru.Caching;
using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using Flowthru.Validation.Runtime;
using MagicAtlas.Data;
using MagicAtlas.Flows.CardProcessing;
using MagicAtlas.Flows.Clustering;
using MagicAtlas.Flows.FineTune;
using MagicAtlas.Flows.Ingest;
using MagicAtlas.Flows.OracleEmbedding;
using MagicAtlas.Flows.Reporting;
using MagicAtlas.Flows.RulesProcessing;
using MagicAtlas.Flows.TagLabeling;
using MagicAtlas.Flows.Tuning;
using MagicAtlas.Services;
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

    // ── External services ──────────────────────────────────────────────────
    // Ollama for cluster-labeling LLM calls. Endpoint and default model resolved from
    // `Flowthru:Services:Ollama` in appsettings.json. Preflight inspector (below) probes
    // /api/tags before any step runs and fails fast if the model isn't pulled.
    services.Configure<OllamaServiceOptions>(
      configuration.GetSection("Flowthru:Services:Ollama")
    );
    services.AddSingleton<IOllamaService, OllamaService>();

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

      // Enable smart caching (0.18.x). The cache manifest tracks per-step composite identities
      // (source-hash + input fingerprints) and short-circuits steps whose inputs and code
      // haven't changed since the last successful run. Python steps participate iff they're
      // marked `cacheable=True` on the `@step` decorator (see Flows/**/<step>.py).
      flowthru.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
          .Json()
          .AtPath(Path.Combine(basePath, ".flowthru", "cache.json"))
          .Build()
      );

      // Preflight: reach Ollama before any step runs. Surfaces a friendly diagnostic if the
      // endpoint is down or the configured model isn't pulled on the server.
      flowthru.AddFlowServiceInspector<IOllamaService>(async (svc, ct) =>
      {
        var health = await svc.HealthCheckAsync(ct);
        if (!health.EndpointReachable)
          return Inspect.Fail(health.Diagnostic, source: "Ollama");
        if (!health.ModelAvailable)
          return Inspect.Fail(health.Diagnostic, source: "Ollama");
        return Inspect.Pass();
      });

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
          "FineTune",
          FineTuneFlow.Create
        )
        .WithDescription(
          "Owns the embedding-model lifecycle (download base + future fine-tune)"
        );

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
          "TagLabeling",
          TagLabelingFlow.Create
        )
        .WithDescription(
          "Deterministic cluster labeling: exemplar centroids + Scryfall centroids + per-cluster "
            + "candidate ranking. Produces ClusterTagAffinity for downstream consumers. The LLM "
            + "arbitration pass (QwenLabeling) is unregistered by default — re-enable when wanted."
        );

      // QwenLabeling flow is intentionally unregistered. To turn the LLM arbitration step back
      // on, uncomment the block below — IOllamaService is still wired in DI above and the
      // preflight inspector still attaches.
      // flowthru
      //   .RegisterFlow<Catalog, IOllamaService>(
      //     "QwenLabeling",
      //     QwenLabelingFlow.Create
      //   )
      //   .WithDescription("Qwen arbitration of ClusterTagAffinity → TagAnchoredClusterLabels.");

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
          "Tuning",
          TuningFlow.Create
        )
        .WithDescription(
          "UMAP hyperparameter sweep — SweepUmap2D (5D→2D unsupervised) + SweepUmap5D "
            + "(HD→5D supervised + default 2D). Tuning-only; outputs sweep scorecards."
        );
    });
  }
}
