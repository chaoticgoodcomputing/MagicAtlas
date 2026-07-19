namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>return-to-hand</b> EMIT family (a non-play-zone move, ADR-0003 §5 hole 6 —
/// the <c>from→to</c> primitive with <c>to=hand</c>). Two variants share the <c>recur</c> stem:
/// <list type="bullet">
/// <item><b>spell-recursion</b> (<c>resource=spell</c>) — "return target instant/sorcery to hand" so it can
/// be RECAST (CR 601.2), the emit a <see cref="CastDriverFamily"/> refuels;
/// <c>emit:returntohand:spell:&lt;subject&gt;</c>.</item>
/// <item><b>bounce</b> — "return target permanent to hand" (Boomerang), a coarse label no flow arm reads;
/// <c>emit:returntohand[:&lt;subject&gt;]</c>.</item>
/// </list>
/// The returned/bounced filter rides as the port Subject, reconstructed as <see cref="PortLabel.SpellRecursionEmit"/> /
/// <see cref="PortLabel.ReturnToHandEmit"/> mint it.
/// </summary>
public sealed class ReturnToHandFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "returntohand")
      return null;
    return seg.Length >= 3 && seg[2] == "spell"
      ? PortStructure.Of(PortSide.Emit, "recur", ("to", "hand"), ("resource", "spell"))
      : PortStructure.Of(PortSide.Emit, "recur", ("to", "hand"));
  }
}
