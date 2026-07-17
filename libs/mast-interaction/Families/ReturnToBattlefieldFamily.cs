namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>return-to-battlefield</b> EMIT (a cast-from-graveyard permission, CR 601.3e —
/// Gravecrawler's aristocrat recursion). A <c>recur</c> zone move with <c>to=battlefield</c> and
/// <c>scope=self</c> (the card itself re-enters as a new object, CR 400.7, refueling its own sac). The
/// gating condition rides as the port Subject; the label is the constant <c>emit:returntobattlefield:self</c>
/// (<see cref="PortLabel.ReturnToBattlefieldEmit"/>).
/// </summary>
public sealed class ReturnToBattlefieldFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit || port.Label != "emit:returntobattlefield:self")
      return null;
    return PortStructure.Of(PortSide.Emit, "recur", ("to", "battlefield"), ("scope", "self"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Stem != "recur" || structure.Attr("to") != "battlefield")
      return null;
    return "emit:returntobattlefield:self";
  }
}
