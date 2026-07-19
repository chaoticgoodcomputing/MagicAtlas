namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>damage-dealt</b> EMIT family (a "deals N damage" effect, CR 119/120).
/// Damage-dealt is a bare EVENT verb (ADR-0003 §5, fork F3 — no supergroup), so the stem is <c>damage</c>
/// with the discriminating facets as attributes: <c>manner</c> (<c>combat</c> CR 510 / <c>noncombat</c>
/// CR 120) and <c>recipient</c> (the damaged object class — <c>any</c>/<c>player</c>/<c>opponent</c>/…).
/// The SOURCE (who deals it) rides as the port Subject, not the label. ADR-2 label
/// <c>emit:damage:&lt;manner&gt;[:&lt;recipient&gt;]</c> ↔ ADR-3 <c>damage[manner=…, recipient=…]</c>.
/// The <c>manner</c> facet is what makes a non-combat emit never feed a combat-specific trigger — the
/// soundness the frontend over-sensitivity (Barrage Ogre ✗→ Ancient Copper Dragon) hinges on.
/// </summary>
public sealed class DamageEmitFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 3 || seg[0] != "emit" || seg[1] != "damage")
      return null;
    return seg.Length > 3
      ? PortStructure.Of(PortSide.Emit, "damage", ("manner", seg[2]), ("recipient", seg[3]))
      : PortStructure.Of(PortSide.Emit, "damage", ("manner", seg[2]));
  }
}
