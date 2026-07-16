using System.Text.Json.Nodes;
using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup.Steps;

namespace MagicAtlas.Ast.Tests.Flows.InteractionRollup;

/// <summary>
/// The interaction-rollup generator (ADR-0003 §8, Migration Stage 0b) — the Flowthru flow that supersedes
/// the <c>tools/interaction-rollup</c> Python prototype. Reads the hand-authored interaction golds
/// (<c>Fixtures/Interactions/golds/*.json</c>), validates + unions them with loud conflict detection, and
/// emits the four content rollup artifacts into <c>Fixtures/Interactions/rollup/</c>:
/// <list type="bullet">
///   <item><c>port-topology.json</c> / <c>port-topology.cited.json</c> — the port universe (stems,
///     kinds, attribute axes) lean + verbose.</item>
///   <item><c>port-interactions.json</c> / <c>port-interactions.cited.json</c> — the residual connection
///     rules (polarity / match_policy / guards / bridges) with promotion status, lean + verbose.</item>
/// </list>
/// The lean pair is <c>strip(provenance)</c> of the cited pair (the WhenWritingNull serializer omits the
/// null provenance fields), so they cannot drift. A rule-union conflict or a ladder-coherence violation
/// throws — that IS the "conflicts fail the build" gate. Diagnostics via Flowthru; never an NUnit gate.
/// </summary>
public static class InteractionRollupFlow
{
  public static BuiltFlow Create(Catalog catalog, string goldsDir, string scaffoldPath) =>
    FlowBuilder.CreateFlow(
      "InteractionRollup",
      pipeline =>
      {
        // Source: load + structurally validate the golds (semi-structured JsonNodes).
        pipeline.AddStep<IEnumerable<JsonNode>>(
          label: "LoadInteractionGolds",
          transform: LoadInteractionGoldsStep.Create(goldsDir),
          outputs: catalog.InteractionGolds
        );

        // Source: load the negotiated topology scaffold (the DECLARED half).
        pipeline.AddStep<JsonNode>(
          label: "LoadTopologyScaffold",
          transform: LoadTopologyScaffoldStep.Create(scaffoldPath),
          outputs: catalog.InteractionScaffold
        );

        // Artifact 1 — the port topology (lean + cited): scaffold (declared) ∪ golds (witnessed).
        pipeline.AddStep<IEnumerable<JsonNode>, JsonNode, PortTopology, PortTopology>(
          label: "Topology",
          transform: TopologyStep.Create(),
          inputs: (catalog.InteractionGolds, catalog.InteractionScaffold),
          outputs: (catalog.PortTopology, catalog.PortTopologyCited)
        );

        // Artifact 2 — the residual interaction rules (lean + cited); union + conflict + ladder gate.
        pipeline.AddStep<IEnumerable<JsonNode>, PortInteractions, PortInteractions>(
          label: "Interactions",
          transform: InteractionsStep.Create(),
          inputs: catalog.InteractionGolds,
          outputs: (catalog.PortInteractions, catalog.PortInteractionsCited)
        );
      }
    );
}
