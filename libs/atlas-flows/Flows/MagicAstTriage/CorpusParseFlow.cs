using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Flows.MagicAstTriage.Steps;

namespace MagicAtlas.Flows.MagicAstTriage;

/// <summary>
/// The corpus-parse producer chain promoted out of tests/magic-ast-tests's <c>MagicAstTriageFlow</c>
/// (upstream-atlas-data-plan §0/§6 P0) so a fresh clone can regenerate the CardAtlas file-drop inputs
/// from this shippable library instead of the test assembly. Pipeline:
/// <list type="number">
///   <item>FetchScryfallBulk → <c>_01_Raw</c> mast-oracle-cards.json (cached on disk).</item>
///   <item>ProjectToCardInput → <c>_02_Intermediate</c> card-inputs.json (cached; the CardAtlas D1 input).</item>
///   <item>ParseCorpus → <c>_07_ModelOutput</c> parse-records.json (cached; the ComboAnchors input).</item>
/// </list>
/// This stops at ParseCorpus — the terminal <c>AggregateTriageReport</c> step (the mast-tdd-loop's
/// triage-report.json artifact) remains in tests/magic-ast-tests because it depends on the test-only
/// clustering + interaction-value overlay utilities and is NOT a CardAtlas pipeline input. See the
/// upstream plan for that follow-on.
/// </summary>
public static class CorpusParseFlow
{
  public static BuiltFlow Create(Catalog catalog, HttpClient httpClient) =>
    FlowBuilder.CreateFlow(
      "CorpusParse",
      pipeline =>
      {
        pipeline.AddStep<IEnumerable<MastRawScryfallCard>>(
          label: "FetchScryfallBulk",
          transform: FetchScryfallBulkStep.Create(httpClient),
          outputs: catalog.RawScryfallCards
        );

        pipeline.AddStep<IEnumerable<MastRawScryfallCard>, IEnumerable<MastCardInput>>(
          label: "ProjectToCardInput",
          transform: ProjectToCardInputStep.Create(),
          inputs: catalog.RawScryfallCards,
          outputs: catalog.CardInputs
        );

        pipeline.AddStep<IEnumerable<MastCardInput>, IEnumerable<ParseRecord>>(
          label: "ParseCorpus",
          transform: ParseCorpusStep.Create(),
          inputs: catalog.CardInputs,
          outputs: catalog.ParseRecords
        );
      }
    );
}
