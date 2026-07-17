namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>cast</b> DRIVER consume (<c>cast:spell:self</c>, CR 601.2): an instant/sorcery's
/// own on-cast effect trigger — the consume a spell-recast (<see cref="ReturnToHandFamily"/>) or a
/// spell-copy (<see cref="CopyFamily"/>) refuels. Casting is a bare EVENT verb (stem <c>cast</c>), the
/// <c>role=driver</c> facet distinguishing the spell's OWN cast driver from a "whenever you cast" watcher
/// (<see cref="CastTriggerFamily"/>). The spell's <c>{instant,sorcery}</c> self-type is the fixed Subject,
/// so the label is the constant <c>cast:spell:self</c>.
/// </summary>
public sealed class CastDriverFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume || port.Label != "cast:spell:self")
      return null;
    return PortStructure.Of(PortSide.Consume, "cast", ("role", "driver"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "cast" || structure.Attr("role") != "driver")
      return null;
    return "cast:spell:self";
  }
}
