namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>attacks-or-blocks</b> CONSUME (a creature's combat-presence opportunity, CR
/// 508/509). Bare EVENT stem <c>combat</c> (shared with <see cref="CombatEmitFamily"/>) on the
/// <see cref="PortSide.Consume"/> side, in two <c>scope</c> forms the projection produces: <c>self</c>
/// ("whenever THIS creature attacks", <see cref="PortLabel.AttacksConsume"/> = <c>attacksorblocks:self</c>)
/// and <c>creature</c> ("whenever A creature attacks", the coarse <c>attacksorblocks:creature</c> trigger
/// path in <see cref="PortGraph"/>.Trigger). Both are satisfied by the extra-combat arm — an additional
/// combat phase re-drives combat damage / re-fires the attacks trigger. The label oracle
/// (<c>("additionalcombat","attacksorblocks")</c>) accepts either scope, so both must carry Structure.
/// </summary>
public sealed class AttacksConsumeFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    return port.Label switch
    {
      "attacksorblocks:self" => PortStructure.Of(PortSide.Consume, "combat", ("scope", "self")),
      "attacksorblocks:creature" => PortStructure.Of(PortSide.Consume, "combat", ("scope", "creature")),
      _ => null,
    };
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "combat")
      return null;
    return structure.Attr("scope") switch
    {
      "self" => "attacksorblocks:self",
      "creature" => "attacksorblocks:creature",
      _ => null,
    };
  }
}
