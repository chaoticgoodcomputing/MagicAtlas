namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create a N/N colorless [Subtype] artifact creature token [with keyword]." —
/// creates a colorless artifact creature token with fixed power/toughness, a
/// subtype, and an optional single keyword ability (Rule 111.3).
///
/// <para>
/// "Colorless" is not a color (CR 105.1: "Colorless is not a color"); the
/// token's <see cref="TokenDefinition.Colors"/> list is <c>["C"]</c>,
/// matching the colorless encoding convention for artifact creature tokens
/// throughout MAST (e.g. the 1/1 colorless Thopter artifact creature token from
/// Whirler Rogue, Whirler Virtuoso, Thopter Foundry, etc.).
/// </para>
///
/// <para>
/// Distinct from <see cref="CreateColorlessCreatureTokenEffectRule"/> which
/// handles pure creature tokens only (and explicitly rejects the word "artifact"
/// in the subtype slot via negative lookahead). This rule matches the more
/// specific "artifact creature" multi-type form.
/// </para>
///
/// <para>
/// Fully anchored (^…$). Priority 984 — one below
/// <see cref="CreateColorlessCreatureTokenEffectRule"/> (985) so the plain-creature
/// rule is tried first; only artifact-creature forms fall through to this rule.
/// </para>
///
/// <para>CR 111.3 (token creation); CR 105.1 (colorless); CR 701.7 (Create).</para>
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class CreateColorlessArtifactCreatureTokenEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Create\s+(?<count>a|an|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+"
    + @"(?<power>\d+)/(?<toughness>\d+)\s+colorless\s+"
    + @"(?<subtype>[A-Za-z]+(?:\s+[A-Za-z]+)*?)\s+artifact\s+creature\s+tokens?"
    + @"(?:\s+with\s+(?<keyword>[a-z][a-z\s]*[a-z]|[a-z]+))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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

    // Normalize subtype to title-case words
    var rawSubtype = m.Groups["subtype"].Value.Trim();
    var subtypeParts = rawSubtype.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var subtype = string.Join(
      " ",
      subtypeParts.Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant())
    );

    // Optional keyword ability from "with <keyword>" suffix
    IReadOnlyList<Ability>? tokenAbilities = null;
    if (m.Groups["keyword"].Success)
    {
      var keywordText = m.Groups["keyword"].Value.Trim().ToLowerInvariant();
      var grantedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keywordText);
      if (grantedAbility is null)
      {
        // The text grants a keyword this helper does not model — bail rather than emit a token that
        // SILENTLY DROPS the granted ability (CR 111.3). Falls through to unparsed for honesty.
        return null;
      }
      tokenAbilities = [grantedAbility];
    }

    return new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = ["C"],
        Types = ["artifact", "creature"],
        Subtypes = [subtype],
        Abilities = tokenAbilities,
        IsCopy = false,
      },
    };
  }
}
