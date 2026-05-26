namespace MagicAST.Parsing.Parsers.Spell;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.References;

/// <summary>
/// Shared utilities used across multiple <see cref="ISpellRule"/> implementations.
/// Lives outside the dispatcher so rules can be added without taking a dependency
/// on <see cref="SpellAbilityParser"/> internals.
/// </summary>
internal static class SpellRuleHelpers
{
  /// <summary>
  /// Maps the small-number oracle vocabulary ("a", "one"…"ten", or a digit run)
  /// to its integer value. Defaults to 1 on an unrecognised token — callers
  /// guard via regex so unrecognised tokens shouldn't reach here.
  /// </summary>
  public static int ParseSmallWord(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower == "a" || lower == "one")
    {
      return 1;
    }
    if (int.TryParse(lower, out var n))
    {
      return n;
    }
    return lower switch
    {
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => 1,
    };
  }

  /// <summary>
  /// Strict variant of <see cref="ParseSmallWord"/> — returns false on an
  /// unrecognised token instead of defaulting to 1.
  /// </summary>
  public static bool TryParseSmallWord(string raw, out int value)
  {
    var lower = raw.ToLowerInvariant();
    if (lower == "a" || lower == "one") { value = 1; return true; }
    if (int.TryParse(lower, out value)) { return true; }
    switch (lower)
    {
      case "two": value = 2; return true;
      case "three": value = 3; return true;
      case "four": value = 4; return true;
      case "five": value = 5; return true;
      case "six": value = 6; return true;
      case "seven": value = 7; return true;
      case "eight": value = 8; return true;
      case "nine": value = 9; return true;
      case "ten": value = 10; return true;
      default: value = 0; return false;
    }
  }

  /// <summary>
  /// Splits a "[type1], [type2], or [typeN]" or "[type1] or [type2]" phrase into
  /// the underlying card-type tokens, lowercased, in source order, with
  /// duplicates removed (preserving first occurrence).
  /// </summary>
  public static List<string> SplitTypeDisjunction(string phrase)
  {
    var withoutOr = Regex.Replace(
      phrase,
      @"\s*,?\s+or\s+",
      ",",
      RegexOptions.IgnoreCase
    );
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var result = new List<string>();
    foreach (var raw in withoutOr.Split(','))
    {
      var token = raw.Trim().ToLowerInvariant();
      if (token.Length == 0)
      {
        continue;
      }
      if (seen.Add(token))
      {
        result.Add(token);
      }
    }
    return result;
  }

  /// <summary>
  /// Maps an oracle-text color word to one of three axes on
  /// <see cref="ObjectFilter"/>: a colored-list, the <c>IsColorless</c>
  /// flag, or the <c>IsMulticolored</c> flag.
  /// </summary>
  public static (IReadOnlyList<string>? Colors, bool? IsColorless, bool? IsMulticolored) MapColorWord(string? colorWord)
  {
    if (string.IsNullOrWhiteSpace(colorWord))
    {
      return (null, null, null);
    }

    return colorWord.ToLowerInvariant() switch
    {
      "white" => (new[] { "W" }, null, null),
      "blue" => (new[] { "U" }, null, null),
      "black" => (new[] { "B" }, null, null),
      "red" => (new[] { "R" }, null, null),
      "green" => (new[] { "G" }, null, null),
      "colorless" => (null, true, null),
      "multicolored" => (null, null, true),
      _ => (null, null, null),
    };
  }

  // ---------------------------------------------------------------------------
  // Destroy-filter helpers
  // ---------------------------------------------------------------------------

  /// <summary>
  /// Card-type oracle tokens accepted in a destroy filter phrase — both singular
  /// and plural forms. Values are the canonical lowercase singular strings stored
  /// in <see cref="ObjectFilter.CardTypes"/>.
  /// </summary>
  private static readonly Dictionary<string, string> DestroyCardTypeMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "land", "land" },
      { "lands", "land" },
      { "creature", "creature" },
      { "creatures", "creature" },
      { "artifact", "artifact" },
      { "artifacts", "artifact" },
      { "enchantment", "enchantment" },
      { "enchantments", "enchantment" },
      { "planeswalker", "planeswalker" },
      { "planeswalkers", "planeswalker" },
      { "permanent", "permanent" },
      { "permanents", "permanent" },
      { "instant", "instant" },
      { "instants", "instant" },
      { "sorcery", "sorcery" },
      { "sorceries", "sorcery" },
    };

  // Regex for a destroy filter phrase:
  //   [non<X> ] [color ] <noun>
  // Examples: "nonbasic lands", "white creatures", "black creature", "Spirit"
  private static readonly Regex DestroyFilterPattern = new(
    @"^(?:non(?<nonnoun>[A-Za-z]+)\s+)?(?:(?<color>white|blue|black|red|green)\s+)?(?<noun>[A-Za-z]+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Parses a destroy-rule filter phrase (the words after "Destroy all" or
  /// "Destroy target") into an <see cref="ObjectFilter"/>.
  /// Supports:
  /// <list type="bullet">
  ///   <item>Bare card type: "creature", "lands"</item>
  ///   <item>Bare subtype: "Spirit", "Human" (any word not in the card-type table)</item>
  ///   <item>Color + card type: "white creatures", "black creature"</item>
  ///   <item>non- prefix + card type: "nonbasic lands", "nonland creatures"</item>
  /// </list>
  /// Returns <c>null</c> if the phrase does not match the expected shape.
  /// </summary>
  public static ObjectFilter? ParseDestroyFilter(string filterPhrase)
  {
    var m = DestroyFilterPattern.Match(filterPhrase.Trim());
    if (!m.Success)
    {
      return null;
    }

    var nonNoun = m.Groups["nonnoun"].Success ? m.Groups["nonnoun"].Value : null;
    var colorWord = m.Groups["color"].Success ? m.Groups["color"].Value : null;
    var noun = m.Groups["noun"].Value;

    // Resolve Colors from the color word.
    IReadOnlyList<string>? colors = null;
    bool? isColorless = null;
    bool? isMulticolored = null;
    if (colorWord is not null)
    {
      var (mappedColors, mappedColorless, mappedMulticolored) = MapColorWord(colorWord);
      colors = mappedColors;
      isColorless = mappedColorless;
      isMulticolored = mappedMulticolored;
    }

    // Build the Characteristics list from the non- prefix (if present).
    List<string>? characteristics = null;
    if (nonNoun is not null)
    {
      characteristics = ["non" + nonNoun.ToLowerInvariant()];
    }

    // Determine whether the main noun is a card type or a subtype.
    if (DestroyCardTypeMap.TryGetValue(noun, out var singularType))
    {
      return new ObjectFilter
      {
        CardTypes = [singularType],
        Colors = colors,
        IsColorless = isColorless,
        IsMulticolored = isMulticolored,
        Characteristics = characteristics,
      };
    }

    // Not a card type → treat as a subtype (e.g., "Spirit", "Human").
    // Color + subtype combinations are theoretically possible but rare; we support
    // them here for completeness.
    return new ObjectFilter
    {
      Subtypes = [noun],
      Colors = colors,
      IsColorless = isColorless,
      IsMulticolored = isMulticolored,
      Characteristics = characteristics,
    };
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> for
  /// "target [color(s)] [card-type] spell" — used by the counter-spell rule.
  /// </summary>
  public static ObjectFilter BuildSpellFilter(string filterText, IReadOnlyList<string> colorWords)
  {
    var characteristics = new List<string>();
    var cardTypes = new List<string> { "spell" };
    foreach (var word in new[] { "instant", "sorcery", "creature", "noncreature" })
    {
      if (
        filterText.Contains(word, StringComparison.OrdinalIgnoreCase)
        && !filterText.Contains("non" + word, StringComparison.OrdinalIgnoreCase)
      )
      {
        characteristics.Add(word);
      }
    }
    if (filterText.Contains("noncreature", StringComparison.OrdinalIgnoreCase))
    {
      characteristics.Add("noncreature");
    }

    List<string>? colors = null;
    bool? isColorless = null;
    bool? isMulticolored = null;
    foreach (var word in colorWords)
    {
      var (mappedColors, mappedColorless, mappedMulticolored) = MapColorWord(word);
      if (mappedColors is not null)
      {
        colors ??= new List<string>();
        foreach (var c in mappedColors)
        {
          if (!colors.Contains(c))
          {
            colors.Add(c);
          }
        }
      }
      isColorless ??= mappedColorless;
      isMulticolored ??= mappedMulticolored;
    }

    return new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors,
      IsColorless = isColorless,
      IsMulticolored = isMulticolored,
    };
  }
}
