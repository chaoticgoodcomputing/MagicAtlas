namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>damage-dealt</b> TRIGGER family (a "whenever [source] deals [combat] damage
/// [to recipient]" trigger, CR 120 general / CR 510 combat). Same bare EVENT stem <c>damage</c> as
/// <see cref="DamageEmitFamily"/> (an emit and a trigger subscribe to the SAME event — the is-a identity
/// the Stage-3 matcher keys on), distinguished only by <see cref="PortSide.Consume"/>. Facets: <c>manner</c>
/// (<c>combat</c>/<c>noncombat</c>/<c>any</c>) and <c>recipient</c>. The watched SOURCE rides as the port
/// Subject — a self-watching trigger (<c>Subject.IsSelf</c>, "whenever THIS deals damage") is what the
/// engine's same-card guard uses. ADR-2 label <c>trigger:damage:&lt;manner&gt;[:&lt;recipient&gt;]</c> ↔
/// ADR-3 <c>consume damage[manner=…, recipient=…]</c>.
/// </summary>
public sealed class DamageTriggerFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 3 || seg[0] != "trigger" || seg[1] != "damage")
      return null;
    return seg.Length > 3
      ? PortStructure.Of(PortSide.Consume, "damage", ("manner", seg[2]), ("recipient", seg[3]))
      : PortStructure.Of(PortSide.Consume, "damage", ("manner", seg[2]));
  }
}
