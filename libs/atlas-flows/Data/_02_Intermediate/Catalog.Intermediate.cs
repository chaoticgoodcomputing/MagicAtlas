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
  /// Processed cards with strong types — ~35k card objects with full type safety. Stored
  /// in-memory only: persisting CardCollection to JSON fails because the record uses init-only
  /// properties that System.Text.Json's default deserializer can't round-trip. The
  /// <c>.Memory()</c> adapter is deliberately non-fingerprintable, which cascades uncacheable
  /// through every downstream step in the 0.18.x cache plan — fix would require swapping to a
  /// serializer-friendly schema (or writing custom JsonConverters) so the item can be
  /// disk-persisted and fingerprinted.
  /// </summary>
  public IItem<CardCollection> ProcessedCards =>
    CreateItem(() => Item.Of<CardCollection>("ProcessedCards").Memory().Build());

  /// <summary>
  /// Introduction section.
  /// </summary>
  public IItem<string> Intro =>
    CreateItem(() =>
      Item.Of<string>("Intro")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/intro.txt")
        .Build()
    );

  /// <summary>
  /// Table of contents section.
  /// </summary>
  public IItem<string> TableOfContents =>
    CreateItem(() =>
      Item.Of<string>("TableOfContents")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/toc.txt")
        .Build()
    );

  /// <summary>
  /// Rules section (numbered rules only).
  /// </summary>
  public IItem<string> RulesText =>
    CreateItem(() =>
      Item.Of<string>("RulesText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/rules.txt")
        .Build()
    );

  /// <summary>
  /// Glossary section.
  /// </summary>
  public IItem<string> GlossaryText =>
    CreateItem(() =>
      Item.Of<string>("GlossaryText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/glossary.txt")
        .Build()
    );

  /// <summary>
  /// Credits section.
  /// </summary>
  public IItem<string> Credits =>
    CreateItem(() =>
      Item.Of<string>("Credits")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/RulesSections/credits.txt")
        .Build()
    );
}
