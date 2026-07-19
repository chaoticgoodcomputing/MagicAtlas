namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>untap</b> family. An untap event (CR 701.20) renews a tap gate; the §8
/// tap-renewal carve-out discharges a tap gate for a SELF-untap on the same card (or a subsuming
/// target-untap). ADR-2 label <c>emit:untap[:self]</c> ↔ ADR-3 structure <c>structure:untap[scope=self]</c>.
/// The <c>:self</c> facet distinguishes "untap this" (<see cref="ObjectReference"/>.Self) from
/// "untap target X"; it rides as the <c>scope=self</c> attr rather than the port Subject.
/// </summary>
public sealed class UntapFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    if (port.Label != "emit:untap" && port.Label != "emit:untap:self")
      return null;
    return port.Label == "emit:untap:self"
      ? PortStructure.Of(PortSide.Emit, "structure:untap", ("scope", "self"))
      : PortStructure.Of(PortSide.Emit, "structure:untap");
  }
}
