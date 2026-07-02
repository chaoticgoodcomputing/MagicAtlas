using Flowthru.Data.Catalog;

namespace MagicAtlas.Rules.Data;

/// <summary>
/// Raw data catalog (Layer 1): the verbatim MTG comprehensive rules text, fetched from WotC by the
/// <c>Ingest</c> flow's <c>FetchRulesText</c> step. Copyright hygiene — the <c>.txt</c> is a
/// gitignored local build artifact (see <c>.gitignore</c>), never committed; only the derived,
/// copyright-clean facts (the type ontology) leave this project.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// The verbatim comprehensive-rules text (~2 MB), normalized to LF with the UTF-8 BOM stripped
  /// by <c>FetchRulesTextNode</c>. The single raw input the <c>RulesProcessing</c> flow splits.
  /// </summary>
  public IItem<string> RawRules =>
    CreateItem(() =>
      Item.Of<string>("RawRules")
        .Text()
        .AtPath($"{_basePath}/_01_Raw/Datasets/comprehensive-rules.txt")
        .Build()
    );
}
