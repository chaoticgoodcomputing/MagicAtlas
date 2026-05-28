namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;

/// <summary>
/// Discard cost: "Discard a card", "Discard a legendary card", etc.
/// Reuses the shared <see cref="ActivatedRuleHelpers.ParseDiscardPattern"/>.
/// </summary>
[ActivatedCostRule(Priority = 998)]
public sealed class DiscardCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("discard"))
    {
      return null;
    }

    var (quantity, filter) = ActivatedRuleHelpers.ParseDiscardPattern(costText);

    return new DiscardCost { Filter = filter, Quantity = quantity };
  }
}
