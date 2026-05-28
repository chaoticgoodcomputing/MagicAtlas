namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;

/// <summary>
/// Pay-life cost: "Pay 1 life", "Pay 3 life" (Rule 118). The life payment is a
/// cost that reduces the player's life total.
/// </summary>
[ActivatedCostRule(Priority = 997)]
public sealed class PayLifeCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();
    var m = Regex.Match(
      trimmed,
      @"^Pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value;
    var amount = ActivatedRuleHelpers.ParseNumberWord(rawAmount) ?? (int.TryParse(rawAmount, out var n) ? n : (int?)null);
    if (amount is null)
    {
      return null;
    }

    return new PayLifeCost { Amount = LiteralQuantity.Of(amount.Value) };
  }
}
