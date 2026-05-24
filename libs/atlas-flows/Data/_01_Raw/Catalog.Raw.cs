using Flowthru.Data.Catalog;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// MTG comprehensive rules text. Populated by <c>FetchRulesTextNode</c> in the <c>Ingest</c>
  /// flow, which scrapes the rules index at <c>https://magic.wizards.com/en/rules</c> for the
  /// current dated .txt URL, follows it, and normalises line endings + strips the BOM.
  /// </summary>
  public IItem<string> RawRules =>
    CreateItem(() =>
      Item.Of<string>("RawRules")
        .Text()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/mtg-rules.txt")
        .Build()
    );

  /// <summary>
  /// Scryfall card-symbology payload. Populated by <c>FetchCardSymbolsNode</c> in the
  /// <c>Ingest</c> flow, which GETs <c>https://api.scryfall.com/symbology</c> directly.
  /// </summary>
  public IItem<RawScryfallCardSymbolList> RawCardSymbols =>
    CreateItem(() =>
      Item.Of<RawScryfallCardSymbolList>("RawCardSymbols")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/symbology.json")
        .Build()
    );

  /// <summary>
  /// Scryfall oracle-cards bulk (~165 MB, ~35K cards). Populated by
  /// <c>FetchOracleCardsBulkNode</c> in the <c>Ingest</c> flow, which resolves Scryfall's
  /// daily-rotating <c>download_uri</c> via the metadata endpoint at
  /// <c>https://api.scryfall.com/bulk-data/oracle-cards</c> and downloads the body in one pass.
  /// </summary>
  public IItem<IEnumerable<RawScryfallCard>> RawCards =>
    CreateItem(() =>
      Item.Of<IEnumerable<RawScryfallCard>>("RawCards")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/oracle-cards.json")
        .Build()
    );

  /// <summary>
  /// Hand-authored keyword definitions that override or supplement the parsed CR glossary
  /// during training-pair construction. See <see cref="CuratedDefinition"/>.
  /// </summary>
  public IItem<IEnumerable<CuratedDefinition>> CuratedDefinitions =>
    CreateItem(() =>
      Item.Of<IEnumerable<CuratedDefinition>>("CuratedDefinitions")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/Curated/curated-definitions.json")
        .Build()
    );

  /// <summary>
  /// Hand-authored hard-negative triplets for fine-tuning. See <see cref="CuratedTriplet"/>.
  /// </summary>
  public IItem<IEnumerable<CuratedTriplet>> CuratedTriplets =>
    CreateItem(() =>
      Item.Of<IEnumerable<CuratedTriplet>>("CuratedTriplets")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/Curated/curated-triplets.json")
        .Build()
    );

  /// <summary>
  /// Glossary entries to drop during training-pair construction. See <see cref="GlossaryExclusion"/>.
  /// </summary>
  public IItem<IEnumerable<GlossaryExclusion>> GlossaryExclusions =>
    CreateItem(() =>
      Item.Of<IEnumerable<GlossaryExclusion>>("GlossaryExclusions")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/Curated/glossary-exclusions.json")
        .Build()
    );


  /// <summary>
  /// (oracle_id, tag_slug) rows pulled from Scryfall's Tagger taxonomy. Produced by the
  /// standalone <c>scripts/scrape_scryfall_tags.py</c> helper (not a Flowthru step — manually
  /// refreshed because Scryfall's API rate limits make per-pipeline-run refresh impractical;
  /// see also <c>tag-index.json</c> in the same directory for per-tag metadata).
  /// </summary>
  public IItem<IEnumerable<TagAssignment>> ScryfallTagAssignments =>
    CreateItem(() =>
      Item.Of<IEnumerable<TagAssignment>>("ScryfallTagAssignments")
        .Parquet()
        .AtPath($"{_basePath}/_01_Raw/Datasets/scryfall-tags/assignments.parquet")
        .Build()
    );
}
