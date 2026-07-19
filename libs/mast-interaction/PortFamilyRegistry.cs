namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 — the reflection-discovered registry of <see cref="IPortFamily"/> implementations.
/// <see cref="Annotate"/>s each projected port with its structured form (dual-emit). Families are
/// discovered once, so adding one is dropping a new file under <c>Families/</c> — no edit here (the fan-out
/// property). (The structure→label <c>ToLegacyLabel</c> shim + its Stage-2 round-trip gate were retired
/// with ADR-0003 §5's cleanup, now that <see cref="PortStructure"/> is the authoritative matcher input.)
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
}
