namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life" — N is a literal number, number word, or variable (X/Y/Z).
/// "You gain X life.", "You gain 2 life.", "You gain three life."
/// </summary>
[ActivatedEffectRule(Priority = 992)]
public sealed class GainLifeEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.');
    var match = Regex.Match(
      text,
      @"^You\s+gain\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var amountText = match.Groups["amount"].Value;
    Quantity amount;
    if (amountText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.X;
    }
    else if (amountText.Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Y;
    }
    else if (amountText.Equals("Z", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Z;
    }
    else
    {
      var count = ActivatedRuleHelpers.ParseNumberWord(amountText) ?? 1;
      amount = LiteralQuantity.Of(count);
    }

    return new GainLifeEffect { Amount = amount, Player = ObjectReference.You() };
  }
}
