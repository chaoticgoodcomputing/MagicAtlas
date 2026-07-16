namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the reflection-discovered registry of <see cref="IPortFamily"/> implementations. It
/// (1) <see cref="Annotate"/>s each projected port with its structured form (dual-emit) and (2) provides
/// the compat-shim <see cref="ToLegacyLabel"/> the Stage-2 round-trip gate checks. Families are discovered
/// once, so adding one is dropping a new file under <c>Families/</c> — no edit here (the fan-out property).
/// </summary>
public static class PortFamilyRegistry
{
  private static readonly IReadOnlyList<IPortFamily> Families = Discover();

  private static IReadOnlyList<IPortFamily> Discover() =>
    typeof(PortFamilyRegistry)
      .Assembly.GetTypes()
      .Where(t => typeof(IPortFamily).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
      .Select(t => (IPortFamily)Activator.CreateInstance(t)!)
      .ToList();

  /// <summary>Attach the ADR-3 structure (dual-emit) if some family recognizes the port; else return the
  /// port unchanged (Structure stays null — an unconverted family). Additive: the Label is untouched.</summary>
  public static PortNode Annotate(PortNode port, TypeOntology ontology)
  {
    foreach (var family in Families)
    {
      var structure = family.Recognize(port, ontology);
      if (structure is not null)
        return port with { Structure = structure };
    }
    return port;
  }

  /// <summary>The compat-shim serialization (Stage-2 gate): the ADR-2 label for a structured port. Throws
  /// if no family owns the structure — Stage 2 converts families incrementally, so a throw means "this
  /// family isn't converted yet," never a silent wrong label.</summary>
  public static string ToLegacyLabel(PortStructure structure, ObjectFilter? subject, TypeOntology ontology)
  {
    foreach (var family in Families)
    {
      var label = family.Serialize(structure, subject, ontology);
      if (label is not null)
        return label;
    }
    throw new InvalidOperationException(
      $"PortFamilyRegistry: no family serializes structured port {structure.Canonical()} "
        + "(Stage 2 converts families incrementally — this family is not converted yet)."
    );
  }
}
