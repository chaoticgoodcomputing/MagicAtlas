namespace MagicAST.Interaction.Families;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>cast</b> TRIGGER consume (a "whenever you cast a [noncreature] spell" trigger,
/// CR 603.2). Bare EVENT stem <c>cast</c> (shared with <see cref="CastEmitFamily"/> / <see cref="CastDriverFamily"/>),
/// the <c>role=trigger</c> facet marking it a watcher. The watched-spell filter (the <c>!creature</c>
/// exclusion the operator tiers on) rides as the port Subject. ADR-2 label
/// <c>trigger:cast:&lt;subject|spell&gt;[:&lt;scope&gt;][:&lt;exclusion&gt;]</c> ↔ ADR-3 <c>consume cast[role=trigger]</c>,
/// the Subject facets reconstructed exactly as <see cref="PortLabel.CastTrigger"/> mints them.
/// </summary>
public sealed class CastTriggerFamily : IPortFamily
{
  public PortStructure? Recognize(PortNode port, TypeOntology ontology)
  {
    if (port.Side != PortSide.Consume)
      return null;
    var seg = port.Label.Split(':');
    if (seg.Length < 2 || seg[0] != "trigger" || seg[1] != "cast")
      return null;
    return PortStructure.Of(PortSide.Consume, "cast", ("role", "trigger"));
  }

  public string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    if (structure.Side != PortSide.Consume || structure.Stem != "cast" || structure.Attr("role") != "trigger")
      return null;
    return Join(
      "trigger",
      "cast",
      subject is null ? "spell" : PortLabel.Subject(subject, ontology) ?? "spell",
      subject is null ? null : PortLabel.Scope(subject),
      subject is null ? null : PortLabel.Exclusion(subject)
    );
  }

  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));
}
