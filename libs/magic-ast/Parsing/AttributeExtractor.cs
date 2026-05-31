namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

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
          // Variable mana costs (containing {X}/{Y}/{Z}) have no determinate
          // mana value outside the stack — the X portion is undefined until
          // the spell is cast (Rule 107.3). Surface IsVariable as the
          // canonical signal and suppress the partial-literal ManaValue so
          // downstream consumers don't treat it as the spell's total cost.
          ManaValue = parsedCost.IsVariable
            ? null
            : (parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null),
          IsVariable = parsedCost.IsVariable,
        }
      );
    }

    // Colors — always emit, even when empty. An empty Colors[] array is the
    // canonical representation of a colorless card (CR 105.1: colorlessness is
    // the absence of color, not a color value). Doctrine: present-with-empty
    // is the descriptive shape, not absent-attribute.
    attributes.Add(new ColorsAttribute { Colors = input.Colors ?? [] });

    // Color identity (computed from mana cost + mana symbols in rules text).
    // Always WUBRG-ordered per CR convention; fixtures must match. Empty list
    // for colorless cards, same doctrine as Colors above.
    var colorIdentity = ComputeColorIdentity(input);
    attributes.Add(new ColorIdentityAttribute { ColorIdentity = colorIdentity });

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

    // Additional costs encoded in oracle text.
    // "As an additional cost to cast this spell, sacrifice a [type]."
    // These lines are skipped by ClauseSplitter (no oracle ability emitted);
    // we surface them here as AdditionalCostsAttribute on the card.
    var additionalCostsAttr = TryExtractAdditionalCosts(input.OracleText);
    if (additionalCostsAttr is not null)
    {
      attributes.Add(additionalCostsAttr);
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
          // Variable mana costs (containing {X}/{Y}/{Z}) have no determinate
          // mana value outside the stack — the X portion is undefined until
          // the spell is cast (Rule 107.3). Surface IsVariable as the
          // canonical signal and suppress the partial-literal ManaValue so
          // downstream consumers don't treat it as the spell's total cost.
          ManaValue = parsedCost.IsVariable
            ? null
            : (parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null),
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

  // "As an additional cost to cast this spell, sacrifice a <type>."
  // or "sacrifice <N> <type>s" — currently only "sacrifice a" (quantity=1) is
  // handled, which covers the entire family-B corpus.
  [GeneratedRegex(
    @"^As an additional cost to cast this spell,\s+sacrifice\s+a\s+(?<type>[a-z]+)\.",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex AdditionalSacrificePrefix();

  // Single-cost Kicker (Rule 702.33a): "Kicker [cost] (reminder)" means "You may pay
  // an additional [cost] as you cast this spell." Captures the kicker mana cost.
  // Anchored on "Kicker " then one-or-more brace-delimited mana symbols, so the
  // deferred "Multikicker [cost]" (Rule 702.33c) does not match (it starts with
  // "Multikicker"). A "Kicker [cost] and/or [cost]" multi-cost printing (Rule 702.33b)
  // is also out of scope; this captures only the single contiguous cost run.
  [GeneratedRegex(
    @"^Kicker\s+(?<cost>(?:\{[^}]+\})+)(?:\s|$)",
    RegexOptions.None
  )]
  private static partial Regex KickerPrefix();

  /// <summary>
  /// Scans oracle text for "As an additional cost to cast this spell, sacrifice a [type]."
  /// prefix lines and returns an AdditionalCostsAttribute when found, or null when absent.
  /// Handles the sacrifice-a-permanent family; other cost shapes (discard, pay life) are
  /// not yet recognised here and are left unparsed until a future batch extends this method.
  /// </summary>
  private AdditionalCostsAttribute? TryExtractAdditionalCosts(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    // Oracle text is newline-separated; additional-cost lines are always the first line.
    var firstLine = oracleText.Split('\n')[0].Trim();

    var sacrificeMatch = AdditionalSacrificePrefix().Match(firstLine);
    if (sacrificeMatch.Success)
    {
      var cardType = sacrificeMatch.Groups["type"].Value.ToLowerInvariant();
      var sacrificeCost = new AdditionalCost
      {
        Cost = new SacrificeCost
        {
          Filter = new ObjectFilter { CardTypes = [cardType] },
          Quantity = LiteralQuantity.Of(1),
        },
        SourceSpan = new TextSpan(0, firstLine.Length),
      };

      return new AdditionalCostsAttribute { Costs = [sacrificeCost] };
    }

    // Kicker (Rule 702.33a): an optional additional cost paid as you cast the spell.
    // "Kicker [cost]" decomposes into AdditionalCost{Cost, IsOptional:true} — the keyword
    // no longer surfaces as an oracle ability (ADR 0003). The keyword line itself carries
    // no meaningful source offset for the cost host, so the span is collapsed to (0, 0).
    // The linked "if this spell was kicked, ..." resolution (Rule 702.33e / 607) is a
    // separate ability and is not modeled here.
    var kickerMatch = KickerPrefix().Match(firstLine);
    if (kickerMatch.Success)
    {
      var parsed = _manaCostParser.Parse(kickerMatch.Groups["cost"].Value);
      var kickerCost = new AdditionalCost
      {
        Cost = new ManaCost { Symbols = parsed.Symbols },
        IsOptional = true,
        SourceSpan = new TextSpan(0, 0),
      };

      return new AdditionalCostsAttribute { Costs = [kickerCost] };
    }

    return null;
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
