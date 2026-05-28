namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;
using MagicAST.AST.References;

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

    // "Sacrifice a creature of the chosen type" — the sacrificed permanent must
    // match the creature type chosen as this permanent entered. This is the
    // consumer half of a CR 607 linked ability (the producer is the
    // "choose a creature type" effect under StaticAbility.When = AsThisEnters):
    //   CR 607.1: "An object may have two abilities printed on it such that one of
    //   them causes actions to be taken or objects or players to be affected and
    //   the other one directly refers to those actions, objects, or players. If so,
    //   these two abilities are linked: the second refers only to actions that were
    //   taken or objects or players that were affected by the first, and not by any
    //   other ability."
    // Encode it as the structured ObjectFilter.ChosenCharacteristic reference rather
    // than dropping the phrase (the bare helper does the latter). Sacrificing here
    // moves the chosen-type creature to the graveyard as the activation cost
    // (CR 701.21a).
    if (lower.Contains("of the chosen type"))
    {
      filter = filter with { ChosenCharacteristic = ChosenCharacteristicKind.CreatureType };
    }

    return new SacrificeCost { Filter = filter, Quantity = quantity };
  }
}
