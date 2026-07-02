using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Flows.Ingest.Nodes;

namespace MagicAtlas.Flows.Ingest;

/// <summary>
/// Owns the upstream Scryfall HTTP boundary for the atlas pipelines. Two independent source steps
/// fetch raw card data from public endpoints and persist it into the catalog's <c>_01_Raw</c>
/// layer. (The MTG comprehensive rules text moved to the standalone <c>mtg-rules</c> project,
/// which publishes the structured rules + glossary + type ontology this project vendors.)
/// </summary>
/// <list type="bullet">
/// <item><b>FetchOracleCardsBulk</b> — Scryfall's daily bulk-cards JSON (~165 MB, ~35K cards) via
/// the metadata-then-download two-step at <c>api.scryfall.com/bulk-data/oracle-cards</c>.</item>
/// <item><b>FetchCardSymbols</b> — Scryfall's card-symbology JSON at
/// <c>api.scryfall.com/symbology</c>.</item>
/// </list>
/// <remarks>
/// Splitting these fetches off into a dedicated flow gives the downstream flows
/// (<c>CardProcessing</c>, <c>OracleEmbedding</c>) a clean separation
/// between "where the data came from" and "how the data is processed." Runs as
/// <c>nx run atlas-flow-test:run -- --flow Ingest</c>, or as part of any wider slice via
/// <c>--to &lt;some downstream item&gt;</c>.
/// <para>
/// The <em>active catalog</em> decides how the raw items persist: in the local/dev catalog they
/// land in <c>_01_Raw/Datasets/</c> as flat files; in a future production catalog they would
/// land in EFCore tables Trax can read directly without a filesystem dependency.
/// </para>
/// </remarks>
public static class IngestFlow
{
  public static BuiltFlow Create(Catalog catalog, HttpClient httpClient)
  {
    return FlowBuilder.CreateFlow("Ingest", pipeline =>
    {
      pipeline.AddStep<IEnumerable<RawScryfallCard>>(
        label: "FetchOracleCardsBulk",
        transform: FetchOracleCardsBulkNode.Create(httpClient),
        outputs: catalog.RawCards
      );

      pipeline.AddStep<RawScryfallCardSymbolList>(
        label: "FetchCardSymbols",
        transform: FetchCardSymbolsNode.Create(httpClient),
        outputs: catalog.RawCardSymbols
      );

    });
  }
}
