namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create a N/N [color] [Subtype] creature token with [keyword]." — creates a
/// colored (not colorless) plain creature token with fixed power/toughness, a
/// single subtype, and one granted keyword ability (CR 111.3).
///
/// <para>
/// White, blue, black, red, and green are colors (CR 105.1: "There are five
/// colors in the Magic game: white, blue, black, red, and green."), so the
/// token's <see cref="TokenDefinition.Colors"/> list carries the mapped color
/// code (e.g. <c>["W"]</c>) — unlike the colorless-token rules which use
/// <c>["C"]</c>.
/// </para>
///
/// <para>
/// Distinct from <see cref="CreateTokenEffectRule"/> (priority 986), which
/// anchors on "…creature tokens?$" and cannot match a trailing "with &lt;keyword&gt;"
/// clause. This rule REQUIRES the with-clause, keeping it strictly disjoint from
/// that sibling: no oracle text can match both. Also distinct from
/// <see cref="CreateColorlessArtifactCreatureTokenEffectRule"/> (colorless,
/// artifact creature) and <see cref="CreateColorlessCreatureTokenEffectRule"/>
/// (colorless, plain creature) — this rule is the colored, plain-creature (no
/// "artifact" type) analogue of those.
/// </para>
///
/// <para>
/// Fully anchored (^…$). Priority 987 — alongside
/// <see cref="CreateTokenWithChangelingEffectRule"/> (987), another
/// "with …"-tailed specific-create-token variant; both sit above the generic
/// <see cref="CreateTokenEffectRule"/> (986) so the more specific with-clause
/// forms are tried first.
/// </para>
///
/// <para>CR 111.1 (tokens); CR 701.7a (Create); CR 105.1 (colors);
/// CR 702.9a/b (Flying is an evasion ability).</para>
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class CreateColoredCreatureTokenWithKeywordEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Create\s+(?<count>a|an|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+"
    + @"(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+"
    + @"(?<subtype>[A-Za-z]+(?:\s+[A-Za-z]+)*?)\s+creature\s+tokens?\s+with\s+"
    + @"(?<keyword>[a-z][a-z\s]*[a-z]|[a-z]+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new VariableQuantity { Name = rawCount.ToUpperInvariant() },
      "a" or "an" or "one" => LiteralQuantity.Of(1),
      "two" => LiteralQuantity.Of(2),
      "three" => LiteralQuantity.Of(3),
      "four" => LiteralQuantity.Of(4),
      "five" => LiteralQuantity.Of(5),
      "six" => LiteralQuantity.Of(6),
      "seven" => LiteralQuantity.Of(7),
      "eight" => LiteralQuantity.Of(8),
      "nine" => LiteralQuantity.Of(9),
      "ten" => LiteralQuantity.Of(10),
      _ => LiteralQuantity.Of(int.TryParse(rawCount, out var n) ? n : 1),
    };

    if (!_colorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    // Normalize subtype to title-case words
    var rawSubtype = m.Groups["subtype"].Value.Trim();
    var subtypeParts = rawSubtype.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var subtype = string.Join(
      " ",
      subtypeParts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant())
    );

    var keywordText = m.Groups["keyword"].Value.Trim().ToLowerInvariant();
    var grantedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keywordText);
    if (grantedAbility is null)
    {
      // The text grants a keyword this helper does not model — bail rather than emit a token
      // that SILENTLY DROPS the granted ability (CR 111.3). Falls through to unparsed for
      // honesty, or to a sibling rule (e.g. changeling) if one models this keyword.
      return null;
    }

    return new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        Abilities = [grantedAbility],
        IsCopy = false,
      },
    };
  }
}
