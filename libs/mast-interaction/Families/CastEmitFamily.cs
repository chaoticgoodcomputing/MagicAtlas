namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>cast</b> EMIT (a RE-CAST of a spell, CR 601): a noncreature permanent that
/// bounced itself to hand and is cast again (Displacer Kitten family), the emit a <see cref="CastTriggerFamily"/>
/// consumes. Bare EVENT stem <c>cast</c> on the <see cref="PortSide.Emit"/> side; the recast-spell filter
/// rides as the port Subject. ADR-2 label <c>emit:cast:&lt;subject|spell&gt;</c> ↔ ADR-3 <c>emit cast</c>,
/// the Subject reconstructed as <see cref="PortLabel.CastEmit"/> mints it.
/// </summary>
public sealed class CastEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "cast")
      return null;
    return PortStructure.Of(PortSide.Emit, "cast");
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Stem != "cast")
      return null;
    return Join("emit", "cast", subject is null ? "spell" : PortLabel.Subject(subject, ontology) ?? "spell");
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
