namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>life</b> family. A life-total change is a scalar-resource event on the
/// life pool (CR 118); the direction (<c>gain</c> vs <c>loss</c>, CR 119) is the load-bearing facet, so it
/// rides as an attribute rather than nesting in the stem. ADR-2 label
/// <c>emit:life:&lt;gain|loss&gt;[:&lt;scope&gt;]</c> ↔ ADR-3 structure <c>life[direction=…]</c>. The
/// affected player (when present) rides as the port Subject, so the scope facet of the label comes from it
/// (not the stem); a null Subject leaves the scope absent (the broadest, "any player" reading).
/// </summary>
public sealed class LifeFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 3 || seg[0] != "emit" || seg[1] != "life")
      return null;
    return PortStructure.Of(PortSide.Emit, "life", ("direction", seg[2]));
  }
}
