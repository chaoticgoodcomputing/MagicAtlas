namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;

/// <summary>
/// Sacrifice cost: "Sacrifice another creature", "Sacrifice X Squirrels", etc.
/// Reuses the shared <see cref="ActivatedRuleHelpers.ParseSacrificePattern"/>.
/// </summary>
[ActivatedCostRule(Priority = 999)]
public sealed class SacrificeCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("sacrifice"))
    {
      return null;
    }

    var (quantity, filter) = ActivatedRuleHelpers.ParseSacrificePattern(costText);
    if (filter == null)
    {
      return null;
    }

    return new SacrificeCost { Filter = filter, Quantity = quantity };
  }
}
