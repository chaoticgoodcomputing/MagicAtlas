namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 — a <b>port family</b>: the map from a family's ADR-2 colon-label to its ADR-3
/// <see cref="PortStructure"/>. One self-contained file per family, reflection-discovered by
/// <see cref="PortFamilyRegistry"/>, so families are added conflict-free.
///
/// <para><see cref="Recognize"/> maps a projected port to its structure, returning <c>null</c> for ports
/// the family does NOT own so the registry can try each in turn. (The reverse structure→label
/// <c>Serialize</c> shim was retired with ADR-0003 §5's cleanup — the Stage-2 round-trip gate it powered
/// is gone now that <see cref="PortStructure"/> is the authoritative matcher input; the label is produced
/// by the projection's <see cref="PortLabel"/> builders and read back here.)</para>
/// </summary>
public interface IPortFamily
{
  /// <summary>The ADR-3 structure for <paramref name="port"/> if this family owns it (recognized from its
  /// <see cref="PortNode.Label"/> / <see cref="PortNode.Subject"/> / <see cref="PortNode.Side"/>), else
  /// <c>null</c>. The structure may draw richer facets from the port's Subject than the label shows.</summary>
  PortStructure? Recognize(PortNode port, TypeOntology ontology);
}
