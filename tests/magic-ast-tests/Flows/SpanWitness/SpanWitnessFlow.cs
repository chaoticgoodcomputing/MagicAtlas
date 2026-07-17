using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.SpanWitness.Steps;

namespace MagicAtlas.Ast.Tests.Flows.SpanWitness;

/// <summary>
/// The <b>span-witness error-check</b> flow — the mast-loop Error-check track's entry surface. Reads the D1
/// card↔port index (<c>card-ports.json</c>, ports with their <c>SourceSpan</c> + ADR-3 <c>stem</c>) and the
/// card oracle text (<c>card-inputs.json</c>), and checks each port's claimed span against the anchor its
/// label asserts. Suspects (span text present, anchor absent) are routed to the golds that witness their
/// stem via the committed cited topology. Corpus-gated (both card-ports and card-inputs are corpus
/// artifacts); emits <c>Data/_08_Reporting/span-witness-report.json</c> (gitignored, never committed).
/// Run after a fresh <c>--flow CardAtlas</c> so the ports it checks are current.
/// </summary>
public static class SpanWitnessFlow
{
  public static BuiltFlow Create(Catalog catalog, string citedTopologyPath) =>
    FlowBuilder.CreateFlow(
      "SpanWitness",
      pipeline =>
      {
        pipeline.AddStep<
          IEnumerable<CardPortRow>,
          IEnumerable<MastCardInput>,
          SpanWitnessReport
        >(
          label: "SpanWitness",
          transform: SpanWitnessStep.Create(citedTopologyPath),
          inputs: (catalog.CardPorts, catalog.CardInputs),
          outputs: catalog.SpanWitnessReport
        );
      }
    );
}
