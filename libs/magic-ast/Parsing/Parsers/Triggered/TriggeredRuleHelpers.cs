namespace MagicAST.Parsing.Parsers.Triggered;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.Parsing;

/// <summary>
/// Shared utilities used across <see cref="ITriggeredRule"/> implementations.
/// </summary>
internal static class TriggeredRuleHelpers
{
  public static ManaCost? TryBuildManaCost(string manaText)
  {
    try
    {
      var parsed = new ManaCostParser().Parse(manaText);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      return new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }
  }

  public static int? ParseWordOrDigitCount(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("two")) return 2;
    if (lower.Contains("three")) return 3;
    if (lower.Contains("four")) return 4;
    if (lower.Contains("five")) return 5;
    if (lower.Contains("six")) return 6;
    if (lower.Contains("seven")) return 7;
    if (lower.Contains("eight")) return 8;
    if (lower.Contains("nine")) return 9;
    if (lower.Contains("ten")) return 10;
    var m = Regex.Match(lower, @"\b(\d+)\b");
    if (m.Success) return int.Parse(m.Groups[1].Value);
    if (Regex.IsMatch(lower, @"\b(a|an|one)\b")) return 1;
    return null;
  }

  public static (string Article, int Count) ParseArticle(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("two ")) return ("two", 2);
    if (lower.Contains("three ")) return ("three", 3);
    if (lower.Contains("four ")) return ("four", 4);
    if (lower.Contains("an ")) return ("an", 1);
    if (lower.Contains("a ")) return ("a", 1);
    return ("", 1);
  }

  public static (string Power, string Toughness)? ParsePowerToughness(string text)
  {
    var match = Regex.Match(text, @"(\d+|X)/(\d+|X)");
    if (!match.Success)
    {
      return null;
    }
    return (match.Groups[1].Value, match.Groups[2].Value);
  }

  public static List<string> ParseColors(string text)
  {
    var colors = new List<string>();
    var lower = text.ToLowerInvariant();
    var colorMappings = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMappings)
    {
      if (lower.Contains(name))
      {
        colors.Add(code);
      }
    }
    if (lower.Contains("colorless"))
    {
      colors.Clear();
      colors.Add("C");
    }
    return colors;
  }

  // Captures the subtype word(s) between a color word and "creature token" in
  // the canonical "P/T color [Subtype] creature token" oracle pattern.
  // Handles optional "you may" prefix and trailing "with [ability]" suffixes.
  // Captures one or two consecutive words so two-word subtypes (e.g. "Spike Drone")
  // could appear, though single-word is the overwhelmingly common case (Rule 205.3m).
  private static readonly Regex _creatureTokenSubtypePattern = new(
    @"\d+/\d+\s+(?:white|blue|black|red|green)\s+(?<sub1>[A-Z][a-z]+)(?:\s+(?<sub2>[A-Z][a-z]+))?\s+creature\s+token",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Extracts creature subtypes from oracle token-creation text.
  /// Uses the structural position (word(s) between color and "creature token") so
  /// arbitrary MTG creature subtypes are handled without a closed enumeration.
  /// Rule 205.3m — creature subtypes are listed after the card's types.
  /// </summary>
  public static List<string> ParseCreatureSubtypes(string text)
  {
    var subtypes = new List<string>();
    var match = _creatureTokenSubtypePattern.Match(text);
    if (!match.Success)
    {
      return subtypes;
    }

    // sub1 is always present on a successful match; capitalize first letter.
    var raw1 = match.Groups["sub1"].Value;
    subtypes.Add(char.ToUpperInvariant(raw1[0]) + raw1[1..]);

    // sub2 is present only for two-word subtypes (uncommon but possible).
    if (match.Groups["sub2"].Success)
    {
      var raw2 = match.Groups["sub2"].Value;
      subtypes.Add(char.ToUpperInvariant(raw2[0]) + raw2[1..]);
    }

    return subtypes;
  }

  public static List<string> ParseTokenAbilities(string text)
  {
    var abilities = new List<string>();
    var lower = text.ToLowerInvariant();
    if (lower.Contains("with flying")) abilities.Add("flying");
    if (lower.Contains("with lifelink")) abilities.Add("lifelink");
    if (lower.Contains("with vigilance")) abilities.Add("vigilance");
    if (lower.Contains("with deathtouch")) abilities.Add("deathtouch");
    if (lower.Contains("with haste")) abilities.Add("haste");
    if (lower.Contains("with trample")) abilities.Add("trample");
    return abilities;
  }

  public static StaticAbility? BuildKeywordStaticAbility(string keywordRaw)
  {
    var lower = keywordRaw.ToLowerInvariant().Trim();
    Effect? effect = lower switch
    {
      "flying" => new MagicAST.AST.Effects.Keyword.EvasionEffect
      {
        CanBeBlockedBy = new MagicAST.AST.References.ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      },
      "vigilance" => new MagicAST.AST.Effects.Keyword.VigilanceEffect(),
      "trample" => new MagicAST.AST.Effects.Keyword.TrampleEffect(),
      "haste" => new MagicAST.AST.Effects.Keyword.HasteEffect(),
      "reach" => new MagicAST.AST.Effects.Keyword.ReachEffect(),
      "lifelink" => new MagicAST.AST.Effects.Damage.LifelinkEffect(),
      "indestructible" => new MagicAST.AST.Effects.Keyword.IndestructibleEffect(),
      "deathtouch" => null,
      _ => null,
    };
    if (effect is null)
    {
      return null;
    }
    var keywordSource = char.ToUpperInvariant(lower[0]) + lower[1..];
    return new StaticAbility { Effects = [effect], KeywordSource = keywordSource };
  }
}
