namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>mana-emit</b> family. A mana-producing effect (ADR-0002 §3b) rides a
/// <em>scalar</em> resource with no object subject, the color as its qualifier. ADR-2 label
/// <c>emit:mana:&lt;color&gt;</c> ↔ ADR-3 structure <c>mana[color=&lt;color&gt;]</c> on the Emit side.
/// Disjoint from the pay-mana family (same <c>mana</c> stem) purely by Side.
/// </summary>
public sealed class ManaEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 3 || seg[0] != "emit" || seg[1] != "mana")
      return null;
    return PortStructure.Of(PortSide.Emit, "mana", ("color", seg[2]));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Stem != "mana")
      return null;
    var color = structure.Attr("color");
    if (string.IsNullOrEmpty(color))
      return null;
    return "emit:mana:" + color;
  }
}
