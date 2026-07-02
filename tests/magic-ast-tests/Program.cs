using Flowthru.Cli;
using Flowthru.Data.Storage.Http;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.DiceComboReport;
using MagicAtlas.Ast.Tests.Flows.InteractionTriage;
using MagicAtlas.Ast.Tests.Flows.LabelCensus;
using MagicAtlas.Ast.Tests.Flows.MagicAstSmoke;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage;
using MagicAtlas.Ast.Tests.Flows.CardAtlas;
using MagicAtlas.Ast.Tests.Flows.PortGraphAtlas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Ast.Tests;

/// <summary>
/// Entry point for the MagicAST test harness — a self-contained Flowthru project whose flows
/// validate the <c>MagicAST</c> oracle-text parser and surface its current gaps for the TDD
/// loop driven by the <c>mast-tdd-loop</c> skill.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveHarnessDirectory())
    );

  private static string ResolveHarnessDirectory()
  {
    const string csproj = "MagicAtlas.Ast.Tests.csproj";
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, csproj))) return dir.FullName;
      dir = dir.Parent;
    }
    var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (cwd is not null)
    {
      var candidate = Path.Combine(cwd.FullName, "tests", "magic-ast-tests", csproj);
      if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
      cwd = cwd.Parent;
    }
    throw new InvalidOperationException(
      $"Could not locate {csproj}. Run from within the workspace, or set CWD to tests/magic-ast-tests."
    );
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
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

    // HttpClient for the Scryfall bulk fetch. Closure-captured into the triage
    // flow factory below — not DI-registered (atlas-flows' note: registering it
    // would interfere with Flowthru's HttpStorageMediumProvider activation).
    var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MagicAtlas-MAST/0.1");
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
    );

    var dataPath = Path.Combine(basePath, "Data");

    // Ratchet baseline lives at the project root; hand-parsed fixtures live under Fixtures/
    // (committed source-of-truth golds, separated from the Flowthru triage Data/ layers). The
    // triage flow's aggregation step reads the baseline to fill in handParsedCoverage, and scans
    // the fixtures directory to flag candidates as AlreadyHandParsed.
    var ratchetBaselinePath = Path.Combine(basePath, "test-baseline.json");
    var handParsedFixturesRoot = Path.Combine(basePath, "Fixtures", "HandParsedCards");

    // Interaction-graph inputs: the canonical known-families grammar (committed fixture) and the
    // vendored type ontology (Curated). Both feed the InteractionTriage reconstruction + viz.
    var knownFamiliesPath = Path.Combine(basePath, "Fixtures", "Interactions", "known-families.json");
    var ontologyPath = Path.Combine(
      dataPath,
      "_01_Raw",
      "Datasets",
      "Curated",
      "type-ontology.json"
    );

    services.AddFlowthru(flowthru =>
    {
      // Python step host for the interaction-graph Plotly viz — this project's own .venv (deps in
      // pyproject.toml: networkx, pandas, plotly, pyarrow). Module search rooted at the project dir,
      // so "Flows.InteractionTriage.plot_interaction_graph" resolves.
      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.VenvPath = Path.Combine(basePath, ".venv");
      });

      // HTTP storage medium: lets the Commander Spellbook variants.json dump load as a plain https://
      // catalog item (CsbVariantsRaw → FetchCombos). The conditional-GET disk cache means a fresh
      // clone fetches the ~510 MB dump once and reuses it WITHOUT any network round-trip for the MaxAge
      // window; only after that does it revalidate (a cheap conditional GET → 304 unless CSB actually
      // changed). CSB combos churn slowly, so the window is WEEKLY — we do not re-pull (or even
      // re-validate) the 510 MB dump more than once a week. Bump down only if a combo refresh is urgent.
      flowthru.UseHttp(http =>
      {
        http.UserAgent = "MagicAtlas-MAST/0.1";
        http.Cache = new HttpCacheOptions
        {
          Directory = Path.Combine(dataPath, "_01_Raw", "Datasets", "External", ".http-cache"),
          MaxAge = TimeSpan.FromDays(7),
        };
      });

      flowthru.RegisterCatalog(_ => new Catalog(dataPath, ratchetBaselinePath));

      flowthru
        .RegisterFlow<Catalog>("MagicAstSmoke", MagicAstSmokeFlow.Create)
        .WithDescription("Placeholder smoke test that runs MagicAST.OracleParser over a fixed input.");

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "InteractionTriage",
          (catalog, executor) =>
            InteractionTriageFlow.Create(
              catalog,
              executor,
              knownFamiliesPath,
              ontologyPath
            )
        )
        .WithDescription(
          "Streams the curled Commander Spellbook combo dump into the lean work-list and classifies "
            + "each combo by blocking layer (parse vs reconstruction) → Data/_08_Reporting/interaction-triage-report.json."
        );

      flowthru
        .RegisterFlow<Catalog>(
          "MagicAstTriage",
          catalog =>
            MagicAstTriageFlow.Create(
              catalog,
              httpClient,
              ratchetBaselinePath,
              handParsedFixturesRoot
            )
        )
        .WithDescription(
          "Fetches the Scryfall oracle-cards bulk, runs MagicAST over every card, and emits "
            + "Data/_08_Reporting/triage-report.json — the input artifact for the mast-tdd-loop skill."
        );

      flowthru
        .RegisterFlow<Catalog>(
          "PortLabelCensus",
          catalog => LabelCensusFlow.Create(catalog, ontologyPath)
        )
        .WithDescription(
          "Parses + PortWalk-projects every corpus card and aggregates the distinct port-label space → "
            + "Data/_08_Reporting/port-label-census.json (diagnostic: the card:label ratio that sizes the two-layer cycle engine)."
        );

      var diceStubAstsPath = Path.Combine(
        dataPath,
        "_01_Raw",
        "Datasets",
        "Curated",
        "dice-report-stub-asts.json"
      );
      flowthru
        .RegisterFlow<Catalog>(
          "DiceComboReport",
          catalog =>
            DiceComboReportFlow.Create(
              catalog,
              ontologyPath,
              handParsedFixturesRoot,
              diceStubAstsPath
            )
        )
        .WithDescription(
          "Reconstructs every CSB die-roll combo 'as if the support cards were parsed' (gold AST > stub > "
            + "parsed text > inert) → Data/_08_Reporting/dice-combo-report.json: per-combo best dice-cycle "
            + "tier + hops vs product reach + cards-in-cycle + AST provenance, plus engine-derived novel dice loops."
        );

      flowthru
        .RegisterFlow<Catalog, IPythonExecutor>(
          "PortGraphAtlas",
          (catalog, executor) => PortGraphAtlasFlow.Create(catalog, ontologyPath, executor)
        )
        .WithDescription(
          "Materializes the emergent port-LABEL graph over the CSB combo-card union and analyzes its edge "
            + "structure → Data/_08_Reporting/port-graph-atlas.json (SCC decomposition, hub census, the "
            + "data-driven hub-cut fragmentation experiment, and the complete family-collapsed archetype "
            + "catalog), plus the family 'subway map' Plotly viz → Data/_08_Reporting/family-graph.html."
        );

      flowthru
        .RegisterFlow<Catalog>(
          "CardAtlas",
          catalog => CardAtlasFlow.Create(catalog, ontologyPath)
        )
        .WithDescription(
          "The CardAtlas data layer (D1–D4) over the parse-ready CSB combo-card union: D1 card↔port index "
            + "+ metadata (card-ports.json / card-meta.json), D4 per-combo reconstructed loops "
            + "(combo-instances.json), D2 realized-annotated family subway map (resource-graph.json), and "
            + "D3 realized combo-shape catalog (archetype-catalog.json). The 'shape → buildable' bridge."
        );

      flowthru.ConfigureMetadata(meta =>
      {
        var metadataPath = Path.Combine(basePath, "Metadata");
        meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
        meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
      });
    });
  }
}
