namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create a N/N colorless [Subtype] creature token." — creates a colorless
/// (no color) creature token with fixed power/toughness, a creature subtype,
/// and no abilities (Rule 111.3).
///
/// <para>
/// "Colorless" is not a color (CR 105.1: "Colorless is not a color"); the
/// token's <see cref="MagicAST.AST.Effects.TokenDefinition.Colors"/> list is
/// empty (<c>[]</c>), matching the colorless encoding convention throughout MAST.
/// Distinct from <see cref="CreateTokenEffectRule"/>, which handles colored tokens
/// only (the pattern requires one of white/blue/black/red/green).
/// </para>
///
/// <para>
/// Fully anchored (^…$). Priority 985 — one below <see cref="CreateTokenEffectRule"/>
/// (986) so the colored-token rule is tried first; only colorless-specific text
/// falls through to this rule.
/// </para>
///
/// <para>CR 111.3 (token creation); CR 105.1 (colorless is not a color).</para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class CreateColorlessCreatureTokenEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Create\s+(?<count>a|an|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+"
    + @"(?<power>\d+)/(?<toughness>\d+)\s+colorless\s+(?<subtype>[A-Za-z]+(?:\s+[A-Za-z]+)*)\s+creature\s+tokens?\s*$",
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

    return new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [],
        Types = ["creature"],
        Subtypes = [subtype],
        IsCopy = false,
      },
    };
  }
}
