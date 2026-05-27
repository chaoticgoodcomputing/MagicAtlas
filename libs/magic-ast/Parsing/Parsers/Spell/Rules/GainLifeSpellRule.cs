namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life." — spell-resolution life-gain. Recuperate's first modal option.
/// Also handles "You gain life equal to the life lost this way." — derived-quantity
/// gain linked to a preceding LoseLifeEffect (Rule 119.3). Blood Tithe's second clause.
/// </summary>
[SpellRule]
public sealed class GainLifeSpellRule : ISpellRule
{
  private static readonly Regex LiteralPattern = new(
    @"^You\s+gain\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex LifeLostPattern = new(
    @"^You\s+gain\s+life\s+equal\s+to\s+the\s+life\s+lost\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // "You gain life equal to the life lost this way." — DerivedQuantity(LifeLost)
    if (LifeLostPattern.IsMatch(text))
    {
      effect = new GainLifeEffect
      {
        Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
        Player = ObjectReference.You(),
      };
      return true;
    }

    var m = LiteralPattern.Match(text);
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
