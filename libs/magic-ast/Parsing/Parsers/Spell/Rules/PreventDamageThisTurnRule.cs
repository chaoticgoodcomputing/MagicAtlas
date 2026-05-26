namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Prevent the next N damage that would be dealt to target [type] this turn."
/// — Recuperate's second modal option. Rule 615.1.
/// </summary>
[SpellRule]
public sealed class PreventDamageThisTurnRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Prevent\s+the\s+next\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+target\s+(?<type>creature|planeswalker|player)\s+this\s+turn$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
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
}
