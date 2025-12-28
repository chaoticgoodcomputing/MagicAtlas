namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;

/// <summary>
/// Extracts card attributes from a CardInputDTO.
/// Handles mana costs, colors, color identity, creature stats, and loyalty.
/// </summary>
public sealed partial class AttributeExtractor
{
  private readonly ManaCostParser _manaCostParser = new();

  /// <summary>
  /// Extracts all applicable attributes from a card input.
  /// </summary>
  /// <param name="input">The card input DTO.</param>
  /// <returns>A list of card attributes.</returns>
  public IReadOnlyList<CardAttribute> Extract(CardInputDTO input)
  {
    var attributes = new List<CardAttribute>();

    // Mana cost (most cards have this, lands typically don't)
    if (!string.IsNullOrWhiteSpace(input.ManaCost))
    {
      var parsedCost = _manaCostParser.Parse(input.ManaCost);
      attributes.Add(
        new ManaCostAttribute
        {
          Raw = input.ManaCost,
          Symbols = parsedCost.Symbols,
          ManaValue = parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null,
          IsVariable = parsedCost.IsVariable,
        }
      );
    }

    // Colors
    if (input.Colors is { Count: > 0 })
    {
      attributes.Add(new ColorsAttribute { Colors = input.Colors });
    }

    // Color identity (computed from mana cost + mana symbols in rules text)
    var colorIdentity = ComputeColorIdentity(input);
    if (colorIdentity.Count > 0)
    {
      attributes.Add(new ColorIdentityAttribute { ColorIdentity = colorIdentity });
    }

    // Creature stats (power/toughness)
    if (!string.IsNullOrWhiteSpace(input.Power) && !string.IsNullOrWhiteSpace(input.Toughness))
    {
      attributes.Add(
        new CreatureStatsAttribute
        {
          Power = ParsePowerToughness(input.Power),
          Toughness = ParsePowerToughness(input.Toughness),
        }
      );
    }

    // Planeswalker loyalty
    if (!string.IsNullOrWhiteSpace(input.Loyalty))
    {
      attributes.Add(ParseLoyalty(input.Loyalty));
    }

    // Card layout (for multi-faced cards)
    if (!string.IsNullOrWhiteSpace(input.Layout) && input.Layout != "normal")
    {
      attributes.Add(new LayoutAttribute { Layout = input.Layout });
    }

    return attributes;
  }

  /// <summary>
  /// Extracts attributes from a card face DTO.
  /// </summary>
  /// <param name="face">The card face DTO.</param>
  /// <returns>A list of card attributes.</returns>
  public IReadOnlyList<CardAttribute> ExtractFromFace(CardFaceDTO face)
  {
    var attributes = new List<CardAttribute>();

    // Mana cost
    if (!string.IsNullOrWhiteSpace(face.ManaCost))
    {
      var parsedCost = _manaCostParser.Parse(face.ManaCost);
      attributes.Add(
        new ManaCostAttribute
        {
          Raw = face.ManaCost,
          Symbols = parsedCost.Symbols,
          ManaValue = parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null,
          IsVariable = parsedCost.IsVariable,
        }
      );
    }

    // Colors
    if (face.Colors is { Count: > 0 })
    {
      attributes.Add(new ColorsAttribute { Colors = face.Colors });
    }

    // Creature stats
    if (!string.IsNullOrWhiteSpace(face.Power) && !string.IsNullOrWhiteSpace(face.Toughness))
    {
      attributes.Add(
        new CreatureStatsAttribute
        {
          Power = ParsePowerToughness(face.Power),
          Toughness = ParsePowerToughness(face.Toughness),
        }
      );
    }

    // Planeswalker loyalty
    if (!string.IsNullOrWhiteSpace(face.Loyalty))
    {
      attributes.Add(ParseLoyalty(face.Loyalty));
    }

    return attributes;
  }

  /// <summary>
  /// Computes color identity from the card's mana cost and oracle text.
  /// Color identity includes all colored mana symbols anywhere on the card.
  /// </summary>
  private List<string> ComputeColorIdentity(CardInputDTO input)
  {
    var colors = new HashSet<string>();

    // Add colors from mana cost
    if (!string.IsNullOrWhiteSpace(input.ManaCost))
    {
      AddManaSymbolColors(input.ManaCost, colors);
    }

    // Add colors from oracle text (mana symbols in abilities)
    if (!string.IsNullOrWhiteSpace(input.OracleText))
    {
      AddManaSymbolColors(input.OracleText, colors);
    }

    // Return in WUBRG order
    return OrderColors(colors);
  }

  [GeneratedRegex(@"\{([^}]+)\}")]
  private static partial Regex ManaSymbolRegex();

  /// <summary>
  /// Extracts colors from mana symbols in a string.
  /// </summary>
  private static void AddManaSymbolColors(string text, HashSet<string> colors)
  {
    var matches = ManaSymbolRegex().Matches(text);
    foreach (Match match in matches)
    {
      var content = match.Groups[1].Value.ToUpperInvariant();

      // Check each character for color letters
      foreach (var c in content)
      {
        var colorCode = c switch
        {
          'W' => "W",
          'U' => "U",
          'B' => "B",
          'R' => "R",
          'G' => "G",
          _ => null,
        };
        if (colorCode != null)
        {
          colors.Add(colorCode);
        }
      }
    }
  }

  /// <summary>
  /// Orders colors in WUBRG order.
  /// </summary>
  private static List<string> OrderColors(HashSet<string> colors)
  {
    var order = new[] { "W", "U", "B", "R", "G" };
    return order.Where(colors.Contains).ToList();
  }

  /// <summary>
  /// Parses a power or toughness value into the appropriate AST node.
  /// Handles: "3", "*", "1+*", "*+1", etc.
  /// </summary>
  private static PowerToughnessValue ParsePowerToughness(string value)
  {
    var trimmed = value.Trim();

    // Pure numeric
    if (int.TryParse(trimmed, out var numericValue))
    {
      return new FixedPTValue { Raw = value, Value = numericValue };
    }

    // Pure variable (just "*")
    if (trimmed == "*")
    {
      return new VariablePTValue { Raw = value };
    }

    // Derived value like "1+*" or "*+1"
    if (trimmed.Contains('+') || trimmed.Contains('-'))
    {
      // Try to extract the base value
      var numericPart = trimmed.Replace("*", "").Replace("+", "").Replace("-", "").Trim();
      if (int.TryParse(numericPart, out var baseValue))
      {
        return new DerivedPTValue { Raw = value, BaseValue = baseValue };
      }
    }

    // Default to variable if we can't parse it
    return new VariablePTValue { Raw = value };
  }

  /// <summary>
  /// Parses a loyalty value into a LoyaltyAttribute.
  /// </summary>
  private static LoyaltyAttribute ParseLoyalty(string value)
  {
    var trimmed = value.Trim();

    // Variable loyalty (X)
    if (trimmed.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      return new LoyaltyAttribute
      {
        Raw = value,
        StartingLoyalty = null,
        IsVariable = true,
      };
    }

    // Numeric loyalty
    if (int.TryParse(trimmed, out var numericValue))
    {
      return new LoyaltyAttribute
      {
        Raw = value,
        StartingLoyalty = numericValue,
        IsVariable = false,
      };
    }

    // Unknown format
    return new LoyaltyAttribute
    {
      Raw = value,
      StartingLoyalty = null,
      IsVariable = false,
    };
  }
}
