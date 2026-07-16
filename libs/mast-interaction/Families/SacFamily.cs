namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>sacrifice</b> cost family. Sacrificing a permanent is a controller-scoped
/// removal of the fodder object (CR 701.21a: a player only sacrifices a permanent they control), so its
/// structure is the fodder creature with <c>manner=sacrificed</c> (§5). ADR-2 label
/// <c>sac:&lt;subject&gt;:&lt;scope&gt;[:another]</c> ↔ ADR-3 structure <c>creature[manner=sacrificed]</c>.
/// The fodder rides as the port Subject, so the subject/scope/exclusion facets of the label come from it;
/// an unscoped fodder floors to <c>controlled</c> (the rules-invariant, not the parse).
/// </summary>
public sealed class SacFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 1 || seg[0] != "sac")
      return null;
    return PortStructure.Of(PortSide.Consume, "creature", ("manner", "sacrificed"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "creature" || structure.Attr("manner") != "sacrificed")
      return null;
    if (subject is null)
      return null;
    return Join(
      "sac",
      PortLabel.Subject(subject, ontology),
      PortLabel.Scope(subject) ?? "controlled",
      PortLabel.Exclusion(subject)
    );
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
