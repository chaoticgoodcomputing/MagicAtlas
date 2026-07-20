using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.FreeTextResidualCensus.Steps;

namespace MagicAtlas.Ast.Tests.Flows.FreeTextResidualCensus;

/// <summary>
/// The initiative-05 free-text burn-down census → <c>Data/_08_Reporting/free-text-residual-census.json</c>.
/// Replaces the frozen hand-committed <c>libs/magic-ast/schema/destring-worklist.json</c> (ADR-0004 §1,
/// issue #38): a measurement about the golds belongs in the reporting layer, recomputed, not in a
/// committed snapshot nothing regenerates.
///
/// <para>Hermetic: reads only the working tree's golds + the free-text whitelist. Never a gate — the gate
/// is <c>GoldFreeTextWhitelistTests</c>, which keys on named (card, sink) pairs and is untouched.</para>
/// </summary>
public static class FreeTextResidualCensusFlow
{
  public static BuiltFlow Create(Catalog catalog, string repoRoot) =>
    FlowBuilder.CreateFlow(
      "FreeTextResidualCensus",
      pipeline =>
      {
        pipeline.AddStep<Data._08_Reporting.Schemas.FreeTextResidualCensus>(
          label: "ScanFreeTextSinks",
          transform: CensusStep.Create(repoRoot),
          outputs: catalog.FreeTextResidualCensus
        );
      }
    );
}
