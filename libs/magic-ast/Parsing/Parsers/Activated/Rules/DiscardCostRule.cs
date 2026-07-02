namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Discard cost: "Discard a card", "Discard a legendary card", "Discard your hand", etc.
/// Reuses the shared <see cref="ActivatedRuleHelpers.ParseDiscardPattern"/> for the card-count
/// shapes. "Discard your hand" (or "discard their hand") is the whole-hand shape: the count is
/// a <see cref="DerivedQuantity"/> with <see cref="DerivedKind.CardsInHand"/> — descriptively,
/// "hand" means "all cards in your hand at the time the cost is paid" (CR 701.9a).
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

    // "Discard your hand" / "Discard their hand" — whole-hand discard cost.
    // CR 701.9a: "To discard a card, move it from its owner's hand to that player's graveyard."
    // The count is derived (all cards currently in hand), not a fixed literal.
    if (lower == "discard your hand" || lower == "discard their hand")
    {
      return new DiscardCost
      {
        Filter = new ObjectFilter { CardTypes = ["card"] },
        Quantity = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
      };
    }

    var (quantity, filter) = ActivatedRuleHelpers.ParseDiscardPattern(costText);

    return new DiscardCost { Filter = filter, Quantity = quantity };
  }
}
