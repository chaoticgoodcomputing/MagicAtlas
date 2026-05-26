namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life." — spell-resolution life-gain. Recuperate's first modal option.
/// </summary>
[SpellRule]
public sealed class GainLifeSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^You\s+gain\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var amountText = m.Groups["amount"].Value;
    Quantity amount;
    var amountLower = amountText.ToLowerInvariant();
    if (amountLower is "x" or "y" or "z")
    {
      amount = new VariableQuantity { Name = amountLower.ToUpperInvariant() };
    }
    else
    {
      amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(amountText));
    }

    effect = new GainLifeEffect
    {
      Amount = amount,
      Player = ObjectReference.You(),
    };
    return true;
  }
}
