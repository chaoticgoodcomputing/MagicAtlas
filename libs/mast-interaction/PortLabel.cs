namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0002 §1–3: the deterministic projection of an AST sub-tree onto its canonical colon-label
/// (the <em>leaf</em>). Pure and total — same sub-tree → same label, no heuristics. The query view
/// is this projection's prefix-preimage (ADR-0002 §2): a port matches every prefix of its leaf.
///
/// <para>Canonical facet order is <c>role : subject : [destination] : [scope] : [exclusion]</c>
/// (ADR-0002 §3), with absent facets dropped (so shorter labels stay valid prefixes). This is the
/// S1 POC: it covers the two <em>consume</em>-port roles the Chatterfang × Pitiless gold needs — a
/// "dies" trigger (Pitiless) and a sacrifice cost (Chatterfang). Subsequent vertical slices add the
/// emit/<c>replace</c> roles, the resource-kind axis (§3b), the type-ontology subject-lift
/// (Squirrel ⊂ creature), and quantities (§8).</para>
/// </summary>
public static class PortLabel
{
  /// <summary>
  /// The subject facet — the object a port acts on, card-type first then subtype (ADR-0002 §3a).
  /// <c>{creature}</c> → <c>"creature"</c>; <c>{creature}+{Squirrel}</c> → <c>"creature:squirrel"</c>.
  /// A subtype-only filter is <b>lifted</b> through the same vendored <see cref="TypeOntology"/> the
  /// operator reads: <c>{Squirrel}</c> → <c>"creature:squirrel"</c> (the kindred card-type is dropped
  /// as a non-permanent — a port acts on a permanent). The lift is a <em>coarse over-approximation</em>
  /// for matching; the operator independently decides the precise <c>Squirrel ⊄ creature</c> straddle
  /// (ADR-0002 §6/§7). <c>null</c> when the filter names no type (and no subtype resolves).
  /// </summary>
  public static string? Subject(ObjectFilter f, TypeOntology ontology)
  {
    var cardTypes = Canon(f.CardTypes) ?? LiftCardTypes(f.Subtypes, ontology);
    var subtypes = Canon(f.Subtypes);
    return (cardTypes, subtypes) switch
    {
      (null, null) => null,
      (not null, null) => cardTypes,
      (null, not null) => subtypes,
      _ => $"{cardTypes}:{subtypes}",
    };
  }

  /// <summary>
  /// Lift a subtype-only fodder filter to its <b>permanent</b> card-type(s) via the ontology
  /// (ADR-0002 §3a): <c>Squirrel</c> → <c>creature</c> (its <c>kindred</c> membership is filtered
  /// out — kindred is not a permanent type, and a sac/death port acts on a permanent). <c>null</c>
  /// if no subtype resolves to a permanent type.
  /// </summary>
  private static string? LiftCardTypes(IReadOnlyList<string>? subtypes, TypeOntology ontology)
  {
    if (subtypes is null || subtypes.Count == 0)
      return null;
    var permanents = ontology.PermanentTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var lifted = subtypes
      .SelectMany(s =>
        ontology.SubtypeToCardTypes.TryGetValue(s, out var cardTypes)
          ? cardTypes
          : Enumerable.Empty<string>()
      )
      .Where(permanents.Contains)
      .Select(c => c.ToLowerInvariant())
      .Distinct()
      .OrderBy(c => c, StringComparer.Ordinal)
      .ToList();
    return lifted.Count == 0 ? null : string.Join("+", lifted);
  }

  /// <summary>
  /// The scope facet — the controller axis (ADR-0002 §3): <c>You</c> → <c>"controlled"</c>,
  /// <c>Opponent</c> → <c>"opponent"</c>, <c>Any</c>/unmarked → <c>null</c> (the broadest prefix).
  /// Ownership is the orthogonal axis — <c>Owner = You</c> → <c>"owned"</c> (never conflated with
  /// control, CR 108.3 vs 108.4).
  /// </summary>
  public static string? Scope(ObjectFilter f) =>
    f.Controller switch
    {
      ControllerFilter.You => "controlled",
      ControllerFilter.Opponent => "opponent",
      _ => f.Owner == ControllerFilter.You ? "owned" : null,
    };

  /// <summary>The exclude-self qualifier — the CR "another" (ADR-0002 §3); the self-scope counterpart
  /// (<c>this creature</c> → <c>self</c>) is blocked on the parser self-binding (ADR-0002 §6).</summary>
  public static string? Exclusion(ObjectFilter f) => f.ExcludeSelf == true ? "another" : null;

  /// <summary>
  /// A "dies" trigger — leaves-the-battlefield to graveyard (CR 700.4), the destination carried as a
  /// qualifier of <c>ltb</c> so <c>ltb:…:to-graveyard ⊆ ltb:…</c> (ADR-0002 §3).
  /// </summary>
  public static string DeathTrigger(ObjectFilter dying, TypeOntology ontology) =>
    Join("ltb", Subject(dying, ontology), "to-graveyard", Scope(dying), Exclusion(dying));

  /// <summary>
  /// A sacrifice cost. CR 701.21a: a player only sacrifices a permanent they control, so an unscoped
  /// fodder filter floors to <c>controlled</c> (the rules-invariant lives here, not in the parse).
  /// </summary>
  public static string SacrificeCost(ObjectFilter fodder, TypeOntology ontology) =>
    Join("sac", Subject(fodder, ontology), Scope(fodder) ?? "controlled", Exclusion(fodder));

  /// <summary>Join the facets in canonical order, dropping the absent ones.</summary>
  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));

  /// <summary>Canonicalise a type list to a deterministic, lower-cased, sorted <c>+</c>-join.</summary>
  private static string? Canon(IReadOnlyList<string>? xs) =>
    xs is null || xs.Count == 0
      ? null
      : string.Join("+", xs.Select(x => x.ToLowerInvariant()).OrderBy(x => x, StringComparer.Ordinal));
}
