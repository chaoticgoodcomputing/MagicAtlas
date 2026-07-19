namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>life</b> TRIGGER consume (a "whenever [a player] gains/loses life" trigger,
/// CR 119). Same scalar-resource stem <c>life</c> as <see cref="LifeFamily"/> (the emit side),
/// distinguished by <see cref="PortSide.Consume"/>; the <c>direction</c> facet (<c>gain</c>/<c>loss</c>) is
/// what the engine matches same-direction. The watched player rides as the port Subject. ADR-2 label
/// <c>trigger:life:&lt;gain|loss&gt;[:&lt;scope&gt;]</c> ↔ ADR-3 <c>consume life[direction=…]</c>.
/// </summary>
public sealed class LifeTriggerFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 3 || seg[0] != "trigger" || seg[1] != "life")
      return null;
    return PortStructure.Of(PortSide.Consume, "life", ("direction", seg[2]));
  }
}
