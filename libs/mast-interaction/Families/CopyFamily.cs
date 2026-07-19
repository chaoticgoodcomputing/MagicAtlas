namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>copy</b> EMIT family. Two variants share the <c>copy</c> stem:
/// <list type="bullet">
/// <item><b>permanent token-copy</b> (bare <c>emit:copy</c>, CR 707.2) — the copier the copy-inheritance
/// graft reads; a deployment of a copied permanent.</item>
/// <item><b>spell-copy</b> (<c>resource=spell</c>, <c>emit:copy:spell:&lt;subject&gt;</c>, CR 707.10) — a
/// copy of an instant/sorcery on the stack that re-fires the copied spell's effects
/// (<see cref="CastDriverFamily"/>).</item>
/// </list>
/// The copied filter rides as the port Subject, reconstructed as <see cref="PortLabel.SpellCopyEmit"/> mints
/// it. The <c>resource=spell</c> facet is exactly the distinction the permanent graft keys off.
/// </summary>
public sealed class CopyFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "copy")
      return null;
    return seg.Length >= 3 && seg[2] == "spell"
      ? PortStructure.Of(PortSide.Emit, "copy", ("resource", "spell"))
      : PortStructure.Of(PortSide.Emit, "copy");
  }
}
