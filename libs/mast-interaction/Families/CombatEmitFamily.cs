namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>additional-combat</b> EMIT (an "additional combat phase" effect, CR 500.8 —
/// Aggravated Assault, Breath of Fury). Combat-presence is a bare EVENT verb (ADR-0003 §5, fork F3): stem
/// <c>combat</c>, the <c>phase=additional</c> facet marking the extra-combat driver that re-drives a
/// creature's re-attack (<see cref="AttacksConsumeFamily"/>). The label is the constant
/// <c>emit:additionalcombat</c> (<see cref="PortLabel.AdditionalCombatEmit"/>).
/// </summary>
public sealed class CombatEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit || port.Label != "emit:additionalcombat")
      return null;
    return PortStructure.Of(PortSide.Emit, "combat", ("phase", "additional"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Stem != "combat" || structure.Attr("phase") != "additional")
      return null;
    return "emit:additionalcombat";
  }
}
