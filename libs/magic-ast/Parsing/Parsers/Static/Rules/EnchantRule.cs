namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 992)]
public sealed class EnchantRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip trailing reminder text so "Enchant creature (Target a creature as you
    // cast this. This card enters attached to that creature.)" reduces to
    // "Enchant creature" before the descriptor match.
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var match = Regex.Match(
      rawText,
      @"^\s*Enchant\s+(?<descriptor>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var descriptor = match.Groups["descriptor"].Value.Trim().ToLowerInvariant();
    if (descriptor.Length == 0)
    {
      return null;
    }

    var filter = BuildEnchantFilter(descriptor);
    if (filter is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        KeywordSource = "Enchant",
        Effects = [new MagicAST.AST.Effects.Combat.EnchantRestrictionEffect
        {
          LegalTargets = filter,
        }],
      },
    ];
  }

  private static ObjectFilter? BuildEnchantFilter(string descriptor)
  {
    // Strip leading "a "/"an " articles that appear in some printings.
    var d = Regex.Replace(descriptor, @"^(?:a|an)\s+", "", RegexOptions.IgnoreCase).Trim();

    ControllerFilter? controller = null;
    if (d.EndsWith(" you control"))
    {
      controller = ControllerFilter.You;
      d = d[..^" you control".Length].Trim();
    }
    else if (d.EndsWith(" an opponent controls"))
    {
      controller = ControllerFilter.Opponent;
      d = d[..^" an opponent controls".Length].Trim();
    }

    // "Enchant player" (CR 702.5) — a player is not an object/card type
    // (CR 109 vs CR 102), so the legal-target descriptor lands on the
    // EntityType axis rather than CardTypes.
    if (d == "player")
    {
      return new ObjectFilter { EntityType = "player", Controller = controller };
    }

    // Simple-noun shape: "creature", "land", "permanent", "artifact", "enchantment".
    var simpleTypes = new[] { "creature", "land", "permanent", "artifact", "enchantment", "planeswalker" };
    if (simpleTypes.Contains(d))
    {
      return new ObjectFilter { CardTypes = [d], Controller = controller };
    }

    // Disjunctive type shape: "typeA or typeB" (e.g. "artifact or creature").
    // Both halves must be recognised simple types.
    var orMatch = Regex.Match(d, @"^(?<a>[a-z]+)\s+or\s+(?<b>[a-z]+)$", RegexOptions.IgnoreCase);
    if (orMatch.Success)
    {
      var typeA = orMatch.Groups["a"].Value;
      var typeB = orMatch.Groups["b"].Value;
      if (simpleTypes.Contains(typeA) && simpleTypes.Contains(typeB))
      {
        return new ObjectFilter { CardTypes = [typeA, typeB], Controller = controller };
      }
    }

    // Color-disjunctive shape: "colorA or colorB creature"
    // (e.g. "red or green creature", "white or blue creature").
    // Rule 105: multiple entries in Colors[] encode a disjunction — the filter
    // matches any creature that has at least one of the listed colors.
    var colorOrMatch = Regex.Match(
      d,
      @"^(?<colorA>white|blue|black|red|green)\s+or\s+(?<colorB>white|blue|black|red|green)\s+creature$",
      RegexOptions.IgnoreCase
    );
    if (colorOrMatch.Success)
    {
      var colorA = MapColorNameToSymbol(colorOrMatch.Groups["colorA"].Value.ToLowerInvariant());
      var colorB = MapColorNameToSymbol(colorOrMatch.Groups["colorB"].Value.ToLowerInvariant());
      if (colorA is not null && colorB is not null)
      {
        return new ObjectFilter
        {
          CardTypes = ["creature"],
          Colors = [colorA, colorB],
          Controller = controller,
        };
      }
    }

    return null;
  }

  private static string? MapColorNameToSymbol(string colorName) => colorName switch
  {
    "white" => "W",
    "blue"  => "U",
    "black" => "B",
    "red"   => "R",
    "green" => "G",
    _       => null,
  };
}
