using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage;

/// <summary>
/// End-to-end triage flow consumed by the <c>mast-tdd-loop</c> skill. Pipeline:
/// <list type="number">
///   <item>FetchScryfallBulk → <c>_01_Raw</c> oracle-cards.json (cached on disk).</item>
///   <item>ProjectToCardInput → <c>_02_Intermediate</c> card-inputs.json (cached).</item>
///   <item>ParseCorpus → <c>_07_ModelOutput</c> parse-records.json (cached).</item>
///   <item>AggregateTriageReport → <c>_08_Reporting</c> triage-report.json (the agent-facing artifact).</item>
/// </list>
/// </summary>
public static class MagicAstTriageFlow
{
  public static BuiltFlow Create(
    Catalog catalog,
    HttpClient httpClient,
    string ratchetBaselinePath,
    string handParsedFixturesRoot,
    string? interactionTriageReportPath = null
  )
  {
    return FlowBuilder.CreateFlow("MagicAstTriage", pipeline =>
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

      pipeline.AddStep<IEnumerable<ParseRecord>, TriageReport>(
        label: "AggregateTriageReport",
        transform: AggregateTriageReportStep.Create(
          ratchetBaselinePath,
          handParsedFixturesRoot,
          interactionTriageReportPath
        ),
        inputs: catalog.ParseRecords,
        outputs: catalog.TriageReport
      );
    });
  }
}
