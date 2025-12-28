namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;

/// <summary>
/// Parses mana cost strings into a list of ManaSymbol objects.
/// Handles all standard MTG mana symbol formats:
/// - Basic: {W}, {U}, {B}, {R}, {G}
/// - Colorless: {C}
/// - Generic: {0}, {1}, {2}, ..., {16}, etc.
/// - Variable: {X}, {Y}, {Z}
/// - Hybrid: {W/U}, {B/G}, etc.
/// - Hybrid generic: {2/W}, {2/U}, etc.
/// - Phyrexian: {W/P}, {U/P}, {B/P}, {R/P}, {G/P}
/// - Phyrexian hybrid: {W/U/P}, {G/W/P}, etc.
/// - Snow: {S}
/// </summary>
public sealed partial class ManaCostParser
{
  // Regex to match mana symbols like {W}, {2}, {W/U}, {2/W}, {G/P}, etc.
  [GeneratedRegex(@"\{([^}]+)\}")]
  private static partial Regex ManaSymbolRegex();

  /// <summary>
  /// Parses a mana cost string into a ParsedManaCost result.
  /// </summary>
  /// <param name="manaCost">The mana cost string (e.g., "{2}{G}{G}").</param>
  /// <returns>A parsed mana cost with symbols and mana value.</returns>
  public ParsedManaCost Parse(string? manaCost)
  {
    if (string.IsNullOrWhiteSpace(manaCost))
    {
      return new ParsedManaCost([], 0, false);
    }

    var symbols = new List<ManaSymbol>();
    var manaValue = 0;
    var isVariable = false;

    var matches = ManaSymbolRegex().Matches(manaCost);

    foreach (Match match in matches)
    {
      var content = match.Groups[1].Value.ToUpperInvariant();
      var (symbol, mv, variable) = ParseSymbol(content);
      symbols.Add(symbol);
      manaValue += mv;
      isVariable |= variable;
    }

    return new ParsedManaCost(symbols, manaValue, isVariable);
  }

  /// <summary>
  /// Parses a single mana symbol from its content string.
  /// </summary>
  private static (ManaSymbol Symbol, int ManaValue, bool IsVariable) ParseSymbol(string content)
  {
    // Check for Phyrexian (contains /P)
    var isPhyrexian = content.Contains("/P");
    if (isPhyrexian)
    {
      content = content.Replace("/P", "");
    }

    // Variable mana (X, Y, Z)
    if (content is "X" or "Y" or "Z")
    {
      return (ManaSymbol.Variable, 0, true);
    }

    // Snow mana
    if (content == "S")
    {
      return (new ManaSymbol { Kind = ManaSymbolKind.Snow }, 1, false);
    }

    // Colorless mana {C}
    if (content == "C")
    {
      return (ManaSymbol.Colorless, 1, false);
    }

    // Pure generic mana {0}, {1}, {2}, etc.
    if (int.TryParse(content, out var genericAmount))
    {
      return (ManaSymbol.Generic(genericAmount), genericAmount, false);
    }

    // Single colored mana
    if (TryParseColor(content, out var singleColor))
    {
      var symbol = isPhyrexian
        ? ManaSymbol.Phyrexian(singleColor)
        : CreateColoredSymbol(singleColor);
      return (symbol, 1, false);
    }

    // Hybrid mana (contains /)
    if (content.Contains('/'))
    {
      var parts = content.Split('/');
      if (parts.Length == 2)
      {
        // Check for hybrid generic (e.g., 2/W)
        if (
          int.TryParse(parts[0], out var hybridGeneric)
          && TryParseColor(parts[1], out var hybridColor)
        )
        {
          var symbol = new ManaSymbol
          {
            Kind = ManaSymbolKind.HybridGeneric,
            Colors = [hybridColor],
            GenericAmount = hybridGeneric,
            IsPhyrexian = isPhyrexian,
          };
          return (symbol, hybridGeneric, false); // MV is the generic portion
        }

        // Pure hybrid (e.g., W/U)
        if (TryParseColor(parts[0], out var colorA) && TryParseColor(parts[1], out var colorB))
        {
          var symbol = new ManaSymbol
          {
            Kind = ManaSymbolKind.Hybrid,
            Colors = [colorA, colorB],
            IsPhyrexian = isPhyrexian,
          };
          return (symbol, 1, false);
        }
      }
    }

    // Unknown symbol - treat as generic 0
    return (ManaSymbol.Generic(0), 0, false);
  }

  /// <summary>
  /// Creates a colored mana symbol for a single color.
  /// </summary>
  private static ManaSymbol CreateColoredSymbol(ManaColor color)
  {
    return color switch
    {
      ManaColor.White => ManaSymbol.White,
      ManaColor.Blue => ManaSymbol.Blue,
      ManaColor.Black => ManaSymbol.Black,
      ManaColor.Red => ManaSymbol.Red,
      ManaColor.Green => ManaSymbol.Green,
      _ => ManaSymbol.Colorless,
    };
  }

  /// <summary>
  /// Tries to parse a single character as a mana color.
  /// </summary>
  private static bool TryParseColor(string value, out ManaColor color)
  {
    color = value.ToUpperInvariant() switch
    {
      "W" => ManaColor.White,
      "U" => ManaColor.Blue,
      "B" => ManaColor.Black,
      "R" => ManaColor.Red,
      "G" => ManaColor.Green,
      _ => default,
    };
    return value.ToUpperInvariant() is "W" or "U" or "B" or "R" or "G";
  }
}

/// <summary>
/// Result of parsing a mana cost string.
/// </summary>
/// <param name="Symbols">The parsed mana symbols.</param>
/// <param name="ManaValue">The total mana value (converted mana cost).</param>
/// <param name="IsVariable">True if the cost contains X, Y, or Z.</param>
public sealed record ParsedManaCost(
  IReadOnlyList<ManaSymbol> Symbols,
  int ManaValue,
  bool IsVariable
);
