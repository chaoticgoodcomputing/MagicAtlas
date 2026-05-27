namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Prevent the next N damage that would be dealt to target [type] this turn." (Rule 615.1)
/// "Prevent the next N damage that would be dealt to any target this turn." (Rule 615.1)
/// </summary>
[SpellRule]
public sealed class PreventDamageThisTurnRule : ISpellRule
{
  // Matches "target creature|planeswalker|player" — named target type.
  private static readonly Regex NamedTargetPattern = new(
    @"^Prevent\s+the\s+next\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+target\s+(?<type>creature|planeswalker|player)\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "any target" — creature, planeswalker, or player (Rule 115.4a).
  private static readonly Regex AnyTargetPattern = new(
    @"^Prevent\s+the\s+next\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+any\s+target\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Named target type: "target creature / planeswalker / player"
    var m = NamedTargetPattern.Match(text);
    if (m.Success)
    {
      var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value));
      effect = new PreventDamageEffect
      {
        Amount = amount,
        Target = ObjectReference.Target(
          new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] }
        ),
        Duration = new UntilEndOfTurnDuration(),
      };
      return true;
    }

    // "any target" — creature, player, or planeswalker (Rule 115.4a).
    var ma = AnyTargetPattern.Match(text);
    if (ma.Success)
    {
      var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(ma.Groups["amount"].Value));
      effect = new PreventDamageEffect
      {
        Amount = amount,
        Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
        Duration = new UntilEndOfTurnDuration(),
      };
      return true;
    }

    return false;
  }
}
