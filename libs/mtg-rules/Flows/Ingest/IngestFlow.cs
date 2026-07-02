using Flowthru.Flow;
using MagicAtlas.Rules.Data;
using MagicAtlas.Rules.Flows.Ingest.Nodes;

namespace MagicAtlas.Rules.Flows.Ingest;

/// <summary>
/// The HTTP boundary: fetches the MTG comprehensive rules text into the <c>_01_Raw</c> layer.
/// Scryfall/card ingestion stays in MagicAtlas' atlas-flows project — this project owns only the
/// rules text.
/// </summary>
public static class IngestFlow
{
  public static BuiltFlow Create(Catalog catalog, HttpClient httpClient)
  {
    return FlowBuilder.CreateFlow("Ingest", pipeline =>
    {
      pipeline.AddStep<string>(
        label: "FetchRulesText",
        transform: FetchRulesTextNode.Create(httpClient),
        outputs: catalog.RawRules
      );
    });
  }
}
