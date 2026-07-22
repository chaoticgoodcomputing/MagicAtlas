using Flowthru.Cli;
using Flowthru.Data.Storage.Http;
using Flowthru.Diagnostics;
using Flowthru.Hosting;
using Flowthru.Step.Python;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.ArtifactCensus;
using MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;
using MagicAtlas.Ast.Tests.Flows.DerivedBacklog;
using MagicAtlas.Ast.Tests.Flows.DiceComboReport;
using MagicAtlas.Ast.Tests.Flows.DiscriminatorGovernance;
using MagicAtlas.Ast.Tests.Flows.FreeTextResidualCensus;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup;
using MagicAtlas.Ast.Tests.Flows.InteractionTriage;
using MagicAtlas.Ast.Tests.Flows.LabelCensus;
using MagicAtlas.Ast.Tests.Flows.MagicAstSmoke;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage;
using MagicAtlas.Ast.Tests.Flows.CardAtlas;
using MagicAtlas.Ast.Tests.Flows.PortGraphAtlas;
using MagicAtlas.Ast.Tests.Flows.TopologyDemand;
using MagicAtlas.Ast.Tests.Flows.OverApproximation;
using MagicAtlas.Ast.Tests.Flows.SpanWitness;
using MagicAtlas.Ast.Tests.Flows.WidenedAttributes;
using MagicAtlas.Ast.Tests.Infrastructure;
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
  public static Task<int> Main(string[] args)
  {
    // ADR 0004 (#22): make every step's cache key aware of the code that actually performs
    // its transform, not just the step class that declares it. Must run before any flow is
    // built, because FlowBuilder.AddStep snapshots the identity at wire-up time.
    StepCodeIdentity.EnsureAugmented();
    return FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveHarnessDirectory())
    );
  }

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
    var interactionGoldsDir = Path.Combine(basePath, "Fixtures", "Interactions", "golds");
    var topologyScaffoldPath = Path.Combine(
      basePath,
      "Fixtures",
      "Interactions",
      "topology-scaffold.json"
    );
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

      // The interaction-value overlay (allComboBlockingCards) that the triage
      // flow fuses into its yield-cluster ranking. Produced by a prior
      // InteractionTriage run; read by path (loose coupling) so a first run
      // without it simply falls back to the pre-fusion fractional-yield order.
      var interactionTriageReportPath = Path.Combine(
        dataPath,
        "_08_Reporting",
        "interaction-triage-report.json"
      );

      flowthru
        .RegisterFlow<Catalog>(
          "MagicAstTriage",
          catalog =>
            MagicAstTriageFlow.Create(
              catalog,
              httpClient,
              ratchetBaselinePath,
              handParsedFixturesRoot,
              interactionTriageReportPath
            )
        )
        .WithDescription(
          "Fetches the Scryfall oracle-cards bulk, runs MagicAST over every card, and emits "
            + "Data/_08_Reporting/triage-report.json — the input artifact for the mast-tdd-loop skill."
        );

      flowthru
        .RegisterFlow<Catalog>(
          "PortLabelCensus",
          catalog =>
            LabelCensusFlow.Create(catalog, ontologyPath, interactionTriageReportPath)
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

      flowthru
        .RegisterFlow<Catalog>(
          "InteractionRollup",
          catalog =>
            InteractionRollupFlow.Create(catalog, interactionGoldsDir, topologyScaffoldPath)
        )
        .WithDescription(
          "Generates the interaction-rollup artifacts (ADR-0003 §8, Stage 0b) from the hand-authored "
            + "interaction golds: unions declared rules with loud conflict detection + ladder coherence and "
            + "writes Fixtures/Interactions/rollup/{port-topology,port-interactions}{,.cited}.json. Supersedes "
            + "the retired tools/interaction-rollup Python prototype."
        );

      // The value-ranked demand overlay — a SEPARATE, corpus-gated flow (NEVER folded into the hermetic
      // InteractionRollup). Reads the committed topology + golds + the gitignored combo-anchor report.
      var comboAnchorReportPath = Path.Combine(
        dataPath,
        "_08_Reporting",
        "combo-anchor-report.json"
      );
      var portTopologyCitedPath = Path.Combine(
        basePath,
        "Fixtures",
        "Interactions",
        "rollup",
        "port-topology.cited.json"
      );
      flowthru
        .RegisterFlow<Catalog>(
          "TopologyDemand",
          catalog =>
            TopologyDemandFlow.Create(
              catalog,
              interactionGoldsDir,
              portTopologyCitedPath,
              topologyScaffoldPath,
              comboAnchorReportPath
            )
        )
        .WithDescription(
          "Value-ranked demand overlay for the ADR-3 topology (corpus-gated diagnostic): ranks witnessed "
            + "stems (by gold popularity), the six sought holes (by combo-anchor payoff mass, tiebreak hand "
            + "priority), and the supergroups → Data/_08_Reporting/port-topology-demand.json. Degrades "
            + "gracefully when combo-anchor-report.json is absent (corpus=null). Never touches InteractionRollup."
        );

      // The span-witness error-check — the mast-loop Error-check track entry. Reads the D1 card-ports
      // (spans + stems) + card oracle text, flags ports whose claimed span lacks their label's anchor, and
      // routes each suspect to the golds witnessing its stem via the committed cited topology.
      flowthru
        .RegisterFlow<Catalog>(
          "SpanWitness",
          catalog => SpanWitnessFlow.Create(catalog, portTopologyCitedPath)
        )
        .WithDescription(
          "Span-witness error-check (corpus-gated diagnostic; mast-loop Error-check track): a port's "
            + "SourceSpan is a witness — flags parsed ports whose claimed oracle text lacks the anchor their "
            + "label asserts (a false-positive port or a span mis-attribution), and routes each to the golds "
            + "that witness its ADR-3 stem → Data/_08_Reporting/span-witness-report.json. Run after --flow "
            + "CardAtlas so the ports are current."
        );

      // The over-approximation report (ADR-0004 §6, modeled-dependency completeness). Reads the D1
      // card-ports (card scope + Green/Amber tier) + card oracle text, re-parses each card, and derives
      // AST-condition-nodes MINUS conditions-the-projection-consumed by ablation. No hand register.
      flowthru
        .RegisterFlow<Catalog>(
          "OverApproximation",
          catalog => OverApproximationFlow.Create(catalog, ontologyPath)
        )
        .WithDescription(
          "Over-approximation report (corpus-gated diagnostic; ADR-0004 §6): enumerates AST Condition "
            + "nodes the PortWalk projection DROPS — derived by ablation (delete the node, re-project, "
            + "compare), never a hand-maintained register — and joins each to the ports, and the GREENs, "
            + "that consequently rest on an unmodeled condition (Gravecrawler's \"as long as you control a "
            + "Zombie\") → Data/_08_Reporting/over-approximation-report.json. Run after --flow CardAtlas."
        );

      // The widened-attribute report (ADR-0004 §6). The SIBLING class the one above cannot see: not a
      // dropped condition NODE (a lost guard) but a dropped FACET (a lost scope) — a controller/owner/
      // exclusion the AST states and the port does not carry. Same ablation technique, structurally
      // disjoint domain (an attribute site contains no polymorphic node; a Condition is one).
      flowthru
        .RegisterFlow<Catalog>(
          "WidenedAttributes",
          catalog => WidenedAttributesFlow.Create(catalog, ontologyPath)
        )
        .WithDescription(
          "Widened-attribute report (corpus-gated diagnostic; ADR-0004 §6): enumerates narrowing AST "
            + "FACETS the PortWalk projection drops from the ports it produces, so the port names more of "
            + "the game than the card does (Chatterfang's \"under your control\" missing from "
            + "replace:token-creation) — derived by ablation, never a register — and joins each to the "
            + "ports, and the GREENs, that are consequently broader than their card → "
            + "Data/_08_Reporting/widened-attribute-report.json. Complements, never replaces, "
            + "OverApproximation (dropped condition nodes) and known-coarse-projections.json (coarse "
            + "discriminators). Run after --flow CardAtlas."
        );

      // The ADR-0004 §1 artifact census. Hermetic (working tree only) — the repo root is resolved by
      // walking up from this project directory to the workspace marker.
      flowthru
        .RegisterFlow<Catalog>(
          "ArtifactCensus",
          catalog => ArtifactCensusFlow.Create(catalog, ArtifactClassifier.RepoRoot(basePath))
        )
        .WithDescription(
          "ADR-0004 §1 artifact census: enumerates every artifact under tests/**/Fixtures, "
            + "**/Data/_08_Reporting, dumps/, libs/**/*.json (plus the committed snapshot families) and "
            + "classifies each Evidence / Derived / architectural-decision, flagging the genuinely "
            + "ambiguous residue → Data/_08_Reporting/artifact-census.json. The GATE over the "
            + "classification is the NUnit ArtifactClassificationGateTests."
        );

      // The ADR-0004 §4 cross-track joins. Hermetic (every input is a committed artifact), so this runs
      // on a clean checkout with no corpus — a join that only runs when the corpus is present is a join
      // that silently does not run.
      flowthru
        .RegisterFlow<Catalog>(
          "CrossTrackJoins",
          catalog => CrossTrackJoinsFlow.Create(catalog, CrossTrackSources.RepoRoot(basePath))
        )
        .WithDescription(
          "ADR-0004 §4 cross-track joins: (1) quarantined-oracle-text → gold → shipped combo tier → "
            + "Data/_08_Reporting/quarantine-tier-join.json (a GREEN pin resting on quarantined text is the "
            + "Suture Priest shape); (2) gold declares → rollup rule → engine guard → "
            + "Data/_08_Reporting/guard-witness-join.json (the guard→witness map, grouped out of the golds' "
            + "own declares blocks — no registry). The GATES are the NUnit CrossTrackJoins fixtures."
        );

      // ADR-0004 §1, issue #38: the initiative-05 free-text burn-down, recomputed rather than frozen in
      // libs/magic-ast/schema/destring-worklist.json. Hermetic (working tree only).
      flowthru
        .RegisterFlow<Catalog>(
          "FreeTextResidualCensus",
          catalog =>
            FreeTextResidualCensusFlow.Create(catalog, ArtifactClassifier.RepoRoot(basePath))
        )
        .WithDescription(
          "Initiative-05 free-text (de-string) burn-down census: per-sink instance/card counts over "
            + "every committed gold under Fixtures/HandParsedCards/**, joined to the named "
            + "Fixtures/whitelist-freetext.json carve-outs → "
            + "Data/_08_Reporting/free-text-residual-census.json. Replaces the frozen "
            + "destring-worklist.json. Never a gate; the GATE is the NUnit GoldFreeTextWhitelistTests."
        );

      // ADR-0004 §1, issue #38: the near-duplicate discriminator check, demoted from a CORE-ring gate to
      // a report once its JSON whitelist moved to the declaration sites.
      flowthru
        .RegisterFlow<Catalog>(
          "DiscriminatorGovernance",
          DiscriminatorGovernanceFlow.Create
        )
        .WithDescription(
          "Discriminator governance report: every intra-family near-duplicate discriminator pair, split "
            + "into the ones a declaration-site NearDuplicateOf/Reason ruling explains and the ones "
            + "nobody has ruled on → Data/_08_Reporting/discriminator-governance.json. Never a gate; the "
            + "GATE is the NUnit DiscriminatorUniquenessTests (hard per-family collision)."
        );

      // ADR-0004 §2, issue #32: the derived backlog — projected(corpus) − served − asserted-unarmable,
      // computed never stored. Hermetic (reflects the engine + reads committed golds/pins), so it runs on a
      // clean checkout with no corpus. Retires holes{} (#26) and known-coarse-projections.json.
      flowthru
        .RegisterFlow<Catalog>(
          "DerivedBacklog",
          catalog => DerivedBacklogFlow.Create(catalog, ArtifactClassifier.RepoRoot(basePath))
        )
        .WithDescription(
          "ADR-0004 §2 derived backlog (issue #32): projected(corpus) − served(rollup ∪ guards) − "
            + "asserted-unarmable(golds), keyed by dispatch dimension + discriminator, plus the decisions "
            + "subtrahend, the owner attribute-axis backlog, and combo-level unserved demand → "
            + "Data/_08_Reporting/derived-backlog.json. An unserved projection with no gold is BACKLOG; one "
            + "with an asserted-absence gold is a DECISION. Retires holes{} and known-coarse-projections.json. "
            + "Hermetic — no corpus. The GATE is PortWalkExhaustivenessTests, which re-runs the pure derivation."
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
