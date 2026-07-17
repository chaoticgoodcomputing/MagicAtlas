namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>attacks-or-blocks</b> CONSUME (a creature's combat-presence opportunity, CR
/// 508/509). Bare EVENT stem <c>combat</c> (shared with <see cref="CombatEmitFamily"/>) on the
/// <see cref="PortSide.Consume"/> side with <c>scope=self</c> — the extra-combat arm satisfies it to
/// re-drive combat damage / re-fire a card's own "whenever this attacks" trigger. The label is the constant
/// <c>attacksorblocks:self</c> (<see cref="PortLabel.AttacksConsume"/>).
/// </summary>
public sealed class AttacksConsumeFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume || port.Label != "attacksorblocks:self")
      return null;
    return PortStructure.Of(PortSide.Consume, "combat", ("scope", "self"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "combat" || structure.Attr("scope") != "self")
      return null;
    return "attacksorblocks:self";
  }
}
