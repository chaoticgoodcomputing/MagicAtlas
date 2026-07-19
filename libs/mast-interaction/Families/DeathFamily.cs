namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>death</b> ("dies") family. A dies trigger is leaves-the-battlefield to
/// graveyard (CR 700.4 / 603.6b), the destination carried as a qualifier so <c>ltb:…:to-graveyard ⊆
/// ltb:…</c> (ADR-2 §3). ADR-2 label <c>ltb:&lt;subject&gt;:to-graveyard[:scope][:another]</c> ↔ ADR-3
/// structure <c>removal:creature[to=graveyard]</c>. The dying object rides as the port Subject, so the
/// subject/scope/exclusion facets of the label come from it (not the stem).
/// </summary>
public sealed class DeathFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 1 || seg[0] != "ltb")
      return null;
    return PortStructure.Of(PortSide.Consume, "removal:creature", ("to", "graveyard"));
  }
}
