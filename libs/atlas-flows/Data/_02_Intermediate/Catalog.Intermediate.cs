using Flowthru.Data.Catalog;
using MagicAtlas.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Intermediate data catalog entries (Layer 2).
/// Contains typed representations of raw source data without structural transformation.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Processed card symbols with strong types.
  /// Persisted to disk as JSON.
  /// </summary>
  public IItem<CardSymbolDictionary> ProcessedCardSymbols =>
    CreateItem(() =>
      Item.Of<CardSymbolDictionary>("ProcessedCardSymbols")
        .Json()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/card-symbols.json")
        .Build()
    );

  /// <summary>
  /// Processed cards with strong types — ~35k card objects with full type safety. Disk-backed
  /// JSON so the 0.18.x cache plan can fingerprint it (in-memory items are deliberately
  /// non-fingerprintable, cascading uncacheable through every downstream step).
  /// </summary>
  public IItem<CardCollection> ProcessedCards =>
    CreateItem(() =>
      Item.Of<CardCollection>("ProcessedCards")
        .Json()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/processed-cards.json")
        .Build()
    );

  /// <summary>
  /// Rules section (numbered rules only). Produced by the standalone <c>mtg-rules</c> project and
  /// vendored into this project's <c>_02_Intermediate</c> layer; consumed by the FineTune
  /// training-pair builder.
  /// </summary>
  public IItem<string> RulesText =>
    CreateItem(() =>
      Item.Of<string>("RulesText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/rules.txt")
        .Build()
    );

  /// <summary>
  /// Glossary section. Produced by the standalone <c>mtg-rules</c> project and vendored into this
  /// project's <c>_02_Intermediate</c> layer; consumed by the FineTune training-pair builder.
  /// </summary>
  public IItem<string> GlossaryText =>
    CreateItem(() =>
      Item.Of<string>("GlossaryText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/glossary.txt")
        .Build()
    );
}
