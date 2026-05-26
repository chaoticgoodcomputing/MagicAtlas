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

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> for "target [non&lt;color&gt;] [color(s)] [card-type] spell
  /// [with mana value N]" — used by the counter-spell rule.
  /// </summary>
  /// <param name="colorWords">Named color words (white, blue, …, colorless, multicolored).</param>
  /// <param name="nonColorWord">A "non&lt;color&gt;" predicate (e.g. "nonblue"), or null.</param>
  /// <param name="cardTypeWord">A bare card-type qualifier before "spell" (e.g. "artifact"), or null.
  ///   Card-types that are already implied by "spell" semantics (instant, sorcery, creature,
  ///   noncreature) go into <c>Characteristics</c>; true orthogonal card types (artifact, land,
  ///   enchantment, permanent) are appended to <c>CardTypes</c>.</param>
  /// <param name="manaValueComparison">A mana-value comparison, or null.</param>
  public static ObjectFilter BuildSpellFilter(
    IReadOnlyList<string> colorWords,
    string? nonColorWord = null,
    string? cardTypeWord = null,
    Comparison? manaValueComparison = null
  )
  {
    var characteristics = new List<string>();
    var cardTypes = new List<string> { "spell" };

    // non<color> predicates (nonblue, nonred, …) → Characteristics (open-ended set)
    if (!string.IsNullOrWhiteSpace(nonColorWord))
    {
      characteristics.Add(nonColorWord.ToLowerInvariant());
    }

    // Card-type qualifier:
    //   - "instant", "sorcery", "creature" → Characteristics (spell-subset semantics)
    //   - "noncreature" → Characteristics
    //   - "artifact", "land", "enchantment", "permanent" → additional CardType
    if (!string.IsNullOrWhiteSpace(cardTypeWord))
    {
      var lower = cardTypeWord.ToLowerInvariant();
      switch (lower)
      {
        case "instant":
        case "sorcery":
        case "creature":
        case "noncreature":
          characteristics.Add(lower);
          break;
        default:
          // Orthogonal card type: artifact, land, enchantment, permanent, etc.
          cardTypes.Add(lower);
          break;
      }
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
      ManaValueComparison = manaValueComparison,
    };
  }
}
