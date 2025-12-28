using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Helpers;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Transforms raw Scryfall card symbols into processed symbols with strong types.
/// Converts color strings to ManaColor enums.
/// </summary>
public static class ParseCardSymbolsNode
{
  public static Func<RawScryfallCardSymbolList, Task<CardSymbolDictionary>> Create()
  {
    return async (input) =>
    {
      var symbols = input
        .Data.Select(raw => new CardSymbol
        {
          Symbol = raw.Symbol,
          LooseVariant = raw.Loose_Variant,
          English = raw.English,
          SvgUri = raw.Svg_Uri,
          Transposable = raw.Transposable,
          RepresentsMana = raw.Represents_Mana,
          AppearsInManaCosts = raw.Appears_In_Mana_Costs,
          ManaValue = raw.Mana_Value,
          Hybrid = raw.Hybrid,
          Phyrexian = raw.Phyrexian,
          Cmc = raw.Cmc ?? 0, // Default to 0 if null
          Funny = raw.Funny,
          Colors = EnumExtensions.ParseManaColors(raw.Colors),
          GathererAlternates = raw.Gatherer_Alternates,
        })
        .ToDictionary(cardSymbol => cardSymbol.Symbol);

      return await Task.FromResult(new CardSymbolDictionary { Symbols = symbols });
    };
  }
}
