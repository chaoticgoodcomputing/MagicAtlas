namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>return-to-battlefield</b> EMIT (a graveyard→battlefield return, CR 601.3e /
/// 400.7). A <c>recur</c> zone move with <c>to=battlefield</c> in two forms, distinguished by <c>scope</c>:
/// <list type="bullet">
/// <item><c>emit:returntobattlefield:self</c> → <c>recur[to=battlefield, scope=self]</c> — the card itself
/// re-enters as a new object (Gravecrawler's aristocrat self-recast, refueling its own sac).</item>
/// <item><c>emit:returntobattlefield</c> (bare) → <c>recur[to=battlefield]</c> — a reanimation that returns
/// ANOTHER creature (Karmic Guide, Puppeteer Clique) or a persist/undying return (Kitchen Finks), feeding an
/// ETB payoff. Both forms take the reanimate arm (<see cref="PortFlowMatcher.FlowArm.ReanimateToEtb"/>/
/// <c>ReanimateToSac</c>) under the same <c>RecastSatisfies</c> guard, matching the label oracle's
/// <c>("returntobattlefield", …)</c> switch entries exactly.</item>
/// </list>
/// The gating condition rides as the port Subject.
/// </summary>
public sealed class ReturnToBattlefieldFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    return port.Label switch
    {
      "emit:returntobattlefield:self" => PortStructure.Of(
        PortSide.Emit, "recur", ("to", "battlefield"), ("scope", "self")
      ),
      "emit:returntobattlefield" => PortStructure.Of(PortSide.Emit, "recur", ("to", "battlefield")),
      _ => null,
    };
  }
}
