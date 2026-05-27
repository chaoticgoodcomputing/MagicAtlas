namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You lose N life." — spell-resolution life-loss addressed to the controller.
/// Examples: Vampiric Tutor (second sentence), Imperial Seal.
/// Rule 119.3.
/// </summary>
[SpellRule]
public sealed class YouLoseLifeSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^You\s+lose\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
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

    effect = new LoseLifeEffect
    {
      Amount = amount,
      Player = ObjectReference.You(),
    };
    return true;
  }
}
