namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — a <b>port family</b>: the bidirectional map between a family's ADR-2 colon-label and
/// its ADR-3 <see cref="PortStructure"/>. One self-contained file per family, reflection-discovered by
/// <see cref="PortFamilyRegistry"/>, so families are added conflict-free (the Stage-2 fan-out unit).
///
/// <para>A family is defined by two total, pure methods that must be mutual inverses on the family's own
/// ports (the Stage-2 round-trip gate enforces it): <see cref="Recognize"/> maps a projected port to its
/// structure; <see cref="Serialize"/> maps that structure back to the exact ADR-2 label. Both return
/// <c>null</c> for ports/structures the family does NOT own, so the registry can try each in turn.</para>
/// </summary>
public interface IPortFamily
{
  /// <summary>The ADR-3 structure for <paramref name="port"/> if this family owns it (recognized from its
  /// <see cref="PortNode.Label"/> / <see cref="PortNode.Subject"/> / <see cref="PortNode.Side"/>), else
  /// <c>null</c>. The structure may draw richer facets from the port's Subject than the label shows.</summary>
  PortStructure? Recognize(PortNode port, TypeOntology ontology);

  /// <summary>The ADR-2 legacy label for <paramref name="structure"/> if this family owns it, else
  /// <c>null</c>. The compatibility shim (retired at Stage 4 cutover); must reproduce byte-for-byte the
  /// label the ADR-0002 generator produced for the same port.</summary>
  string? Serialize(PortStructure structure, ObjectFilter? subject, TypeOntology ontology);
}
