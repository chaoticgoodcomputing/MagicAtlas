namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Costs;

/// <summary>
/// Forage keyword-action cost (CR 701.61a): "Exile three cards from your graveyard
/// or sacrifice a Food." Printed as the bare word "Forage" on the cost side of an
/// activated ability, e.g. "{2}, Forage: [effect]".
///
/// <para>
/// CR 701.61a (verbatim): "To forage means 'Exile three cards from your graveyard
/// or sacrifice a Food.'" The keyword action appears as a single cost component:
/// comma-separated from any mana cost in the standard "[cost]: [effect]" pattern
/// (CR 602.1). MAST records the invocation as a <see cref="ForageCost"/> descriptor;
/// the two alternative payment modes are engine territory per the descriptive-not-engine
/// doctrine (the node names the action, not the execution). Rule reference: 701.61.
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 900)]
public sealed class ForageCostRule : IActivatedCostRule
{
  public Cost? TryMatch(string costText)
  {
    return costText.Trim().Equals("Forage", System.StringComparison.OrdinalIgnoreCase)
      ? new ForageCost()
      : null;
  }
}
