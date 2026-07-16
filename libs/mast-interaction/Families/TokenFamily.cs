namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>token</b> (create-token) family. Putting a token onto the battlefield is a
/// DEPLOYMENT event (CR 111.1/608.2n) — a token IS a deployment — so its stem is <c>deployment:creature</c>,
/// distinguished from a hard-cast/blink deployment by the <c>token=true</c> facet (§5). ADR-2 label
/// <c>emit:token:&lt;subject&gt;[:&lt;scope&gt;]</c> ↔ ADR-3 structure <c>deployment:creature[token=true]</c>.
/// The created token rides as the port Subject, so the subject/scope facets of the label come from it (not
/// the stem).
/// </summary>
public sealed class TokenFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Emit)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "emit" || seg[1] != "token")
      return null;
    return PortStructure.Of(PortSide.Emit, "deployment:creature", ("token", "true"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Emit || structure.Attr("token") != "true")
      return null;
    return Join(
      "emit",
      "token",
      subject is null ? null : PortLabel.Subject(subject, ontology),
      subject is null ? null : PortLabel.Scope(subject)
    );
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
