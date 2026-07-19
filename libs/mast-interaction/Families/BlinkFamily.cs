namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>blink</b> (flicker) family. The exile-then-return-the-just-exiled composite
/// re-enters a permanent as a new object (CR 603.6e/400.7), so its load-bearing half is a DEPLOYMENT event
/// with <c>manner=blink</c> (§5). ADR-2 label <c>emit:blink:&lt;subject&gt;[:self]</c> ↔ ADR-3 structure
/// <c>deployment:creature[manner=blink]</c>. The blinked object rides as the port Subject, so the
/// subject/self facets of the label come from it (not the stem).
/// </summary>
public sealed class BlinkFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "blink")
      return null;
    return PortStructure.Of(PortSide.Emit, "deployment:creature", ("manner", "blink"));
  }
}
