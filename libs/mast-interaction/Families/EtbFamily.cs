namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>enters-the-battlefield</b> (ETB) family. An "enters the battlefield" trigger
/// (CR 603.6a) CONSUMES an entering object, so its structure is a DEPLOYMENT event on the Consume side.
/// ADR-2 label <c>etb:&lt;subject&gt;[:&lt;scope&gt;][:&lt;exclusion&gt;]</c> ↔ ADR-3 structure
/// <c>deployment:creature[event=etb]</c>. The entering object rides as the port Subject, so the
/// subject/scope/exclusion facets of the label come from it (not the stem). The <c>event=etb</c> marker
/// attr distinguishes this Consume-side deployment from other deployment structures.
/// </summary>
public sealed class EtbFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg[0] != "etb")
      return null;
    return PortStructure.Of(PortSide.Consume, "deployment:creature", ("event", "etb"));
  }
}
