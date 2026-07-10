namespace MagicAST.AST.References;

/// <summary>
/// Pure relational operators over <see cref="ObjectFilter"/> (magic-ast ADR-0008): can two filters
/// denote a common game object (<see cref="Intersects"/>)? No board state, no object
/// materialization. Type-axis decisions read a vendored <see cref="TypeOntology"/>; the operator
/// hard-codes no type names. Axes land one at a time — any axis a filter constrains but the
/// operator does not yet decide floors the verdict to <see cref="FilterRelation.Unknown"/>, so the
/// operator is sound (never a false <c>Overlaps</c>/<c>Disjoint</c>) at every increment.
/// </summary>
public static class ObjectFilterRelations
{
  /// <summary>
  /// Symmetric satisfiability of <c>a ∧ b</c>: a per-axis contradiction scan combined by a Kleene
  /// lattice — any axis provably incompatible ⇒ <see cref="FilterRelation.Disjoint"/>; else any axis
  /// undecidable ⇒ <see cref="FilterRelation.Unknown"/>; else <see cref="FilterRelation.Overlaps"/>.
  /// </summary>
  public static FilterMatch Intersects(ObjectFilter a, ObjectFilter b, TypeOntology ontology)
  {
    (string Axis, FilterRelation Verdict)[] axes =
    [
      ("Controller", ControllerAxis(a.Controller, b.Controller)), // CR 109.4
      ("Owner", ControllerAxis(a.Owner, b.Owner)), // CR 108.3
      ("IsToken", EqualityAxis(a.IsToken, b.IsToken)), // CR 111
      ("Zone", ZoneAxis(a.Zone, b.Zone)),
      ("Power", ComparisonAxis(a.PowerComparison, b.PowerComparison)),
      ("Toughness", ComparisonAxis(a.ToughnessComparison, b.ToughnessComparison)),
      ("ManaValue", ComparisonAxis(a.ManaValueComparison, b.ManaValueComparison)),
      ("Supertypes", SupertypeAxis(a, b)),
      ("Colors", ColorAxis(a, b)),
      ("Characteristics", CharacteristicsAxis(a, b)),
      ("Name", NameAxis(a.Name, b.Name)),
      ("EntityType", EntityTypeAxis(a, b)),
      ("Types", TypeAxis(a, b, ontology)), // CR 110.4 / 205.3, kindred-neutral
    ];

    string? unknownReason = null;
    foreach (var (axis, verdict) in axes)
    {
      if (verdict == FilterRelation.Disjoint)
        return new FilterMatch(FilterRelation.Disjoint, axis);
      if (verdict == FilterRelation.Unknown && unknownReason is null)
        unknownReason = axis;
    }

    // Unknown floor: a relational axis a filter constrains but the operator does not yet decide.
    unknownReason ??= UndecidedAxis(a) ?? UndecidedAxis(b);

    return unknownReason is null
      ? new FilterMatch(FilterRelation.Overlaps)
      : new FilterMatch(FilterRelation.Unknown, unknownReason);
  }

  /// <summary>
  /// <c>You ⊥ Opponent</c>; <c>Any</c> compatible with both; a runtime-chosen controller
  /// (<c>Target</c>/<c>EnchantedPlayer</c>/<c>ThatPlayer</c>) is undecidable here (a target can be
  /// you), so → <see cref="FilterRelation.Unknown"/>. Either side unconstrained ⇒ no contradiction.
  /// </summary>
  private static FilterRelation ControllerAxis(ControllerFilter? a, ControllerFilter? b)
  {
    if (a is null || b is null)
      return FilterRelation.Overlaps;
    if (IsRuntimeChosen(a.Value) || IsRuntimeChosen(b.Value))
      return FilterRelation.Unknown;

    var disjoint =
      (a == ControllerFilter.You && b == ControllerFilter.Opponent)
      || (a == ControllerFilter.Opponent && b == ControllerFilter.You);
    return disjoint ? FilterRelation.Disjoint : FilterRelation.Overlaps;
  }

  private static bool IsRuntimeChosen(ControllerFilter c) =>
    c
      is ControllerFilter.Target
        or ControllerFilter.EnchantedPlayer
        or ControllerFilter.ThatPlayer;

  /// <summary>A boolean predicate axis (token / nontoken, CR 111): two set, differing values contradict.</summary>
  private static FilterRelation EqualityAxis(bool? a, bool? b)
  {
    if (a is null || b is null)
      return FilterRelation.Overlaps;
    return a == b ? FilterRelation.Overlaps : FilterRelation.Disjoint;
  }

  /// <summary>Single-valued zone: two distinct zones contradict; <c>Anywhere</c> is compatible with any.</summary>
  private static FilterRelation ZoneAxis(Zone? a, Zone? b)
  {
    if (a is null || b is null || a == Zone.Anywhere || b == Zone.Anywhere)
      return FilterRelation.Overlaps;
    return a == b ? FilterRelation.Overlaps : FilterRelation.Disjoint;
  }

  /// <summary>
  /// Numeric comparison axis (power / toughness / mana value): do the two constraints admit a
  /// common integer? Range operators clamp a <c>[lo, hi]</c> window; <c>NotEqual</c> punches out
  /// points. An empty window ⇒ <see cref="FilterRelation.Disjoint"/>.
  /// </summary>
  private static FilterRelation ComparisonAxis(Comparison? a, Comparison? b)
  {
    if (a is null || b is null)
      return FilterRelation.Overlaps;

    // A relative comparison ("power less than this creature's power") has no printed
    // integer to reduce to a numeric window — its right-hand side resolves against
    // another object at runtime (CR 702.134). Co-satisfiability is undecidable here.
    if (a.Value is null || b.Value is null)
      return FilterRelation.Overlaps;

    long lo = long.MinValue,
      hi = long.MaxValue;
    var excluded = new HashSet<long>();
    foreach (var c in new[] { a, b })
    {
      long v = c.Value.Value;
      switch (c.Operator)
      {
        case ComparisonOperator.LessThan:
          hi = Math.Min(hi, v - 1);
          break;
        case ComparisonOperator.LessThanOrEqual:
          hi = Math.Min(hi, v);
          break;
        case ComparisonOperator.GreaterThan:
          lo = Math.Max(lo, v + 1);
          break;
        case ComparisonOperator.GreaterThanOrEqual:
          lo = Math.Max(lo, v);
          break;
        case ComparisonOperator.Equal:
          lo = Math.Max(lo, v);
          hi = Math.Min(hi, v);
          break;
        case ComparisonOperator.NotEqual:
          excluded.Add(v);
          break;
      }
    }

    if (lo > hi)
      return FilterRelation.Disjoint;
    // An unbounded window minus finitely many excluded points still admits a value.
    if (lo == long.MinValue || hi == long.MaxValue)
      return FilterRelation.Overlaps;
    for (long v = lo; v <= hi; v++)
      if (!excluded.Contains(v))
        return FilterRelation.Overlaps;
    return FilterRelation.Disjoint;
  }

  /// <summary>
  /// Card-type overlap. The two filters' type lists are conjunctions (an object has every listed
  /// type), so their union must be co-satisfiable. The only provable contradiction is the CR 110.4
  /// permanent / non-permanent partition (a non-permanent card can't be a permanent). Two
  /// non-permanent types (e.g. instant ∧ sorcery) → <see cref="FilterRelation.Unknown"/> (no rule
  /// forbids it; none is printed). <c>kindred</c> is partition-neutral (308.1, always rides another
  /// type) and <c>card</c> is universal — neither derives Disjoint. <c>player</c> is not an object
  /// (CR 102 vs 109) → disjoint from every object type.
  /// </summary>
  /// <summary>An exact card-name pin (CR 201.4): two distinct names contradict; one side unconstrained is fine.</summary>
  private static FilterRelation NameAxis(string? a, string? b)
  {
    if (a is null || b is null)
      return FilterRelation.Overlaps;
    return string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal)
      ? FilterRelation.Overlaps
      : FilterRelation.Disjoint;
  }

  /// <summary>
  /// EntityType (CR 102 player vs 109 object). Two entity kinds differ → Disjoint; a player filter
  /// against an object-constrained filter → Disjoint (a player is not an object).
  /// </summary>
  private static FilterRelation EntityTypeAxis(ObjectFilter a, ObjectFilter b)
  {
    var ea = a.EntityType?.Trim().ToLowerInvariant();
    var eb = b.EntityType?.Trim().ToLowerInvariant();
    if (ea is not null && eb is not null)
      return ea == eb ? FilterRelation.Overlaps : FilterRelation.Disjoint;
    if (ea == "player" && ConstrainsObject(b))
      return FilterRelation.Disjoint;
    if (eb == "player" && ConstrainsObject(a))
      return FilterRelation.Disjoint;
    return FilterRelation.Overlaps;
  }

  private static bool ConstrainsObject(ObjectFilter f) =>
    (
      f.CardTypes?.Any(t =>
      {
        var x = t.Trim().ToLowerInvariant();
        return x != "player" && x != "card";
      }) ?? false
    )
    || f.Subtypes is { Count: > 0 };

  /// <summary>
  /// Characteristics: keyword presence is always combinable; combat states must share a role
  /// (Attacking ⊥ Blocking, CR 508/509); an <c>OtherCharacteristic</c> residual is undecidable.
  /// </summary>
  private static FilterRelation CharacteristicsAxis(ObjectFilter a, ObjectFilter b)
  {
    var chars = new List<Characteristic>();
    if (a.Characteristics is not null)
      chars.AddRange(a.Characteristics);
    if (b.Characteristics is not null)
      chars.AddRange(b.Characteristics);
    if (chars.OfType<OtherCharacteristic>().Any())
      return FilterRelation.Unknown;

    var states = chars.OfType<CombatStateCharacteristic>().Select(c => c.State).ToList();
    for (var i = 0; i < states.Count; i++)
      for (var j = i + 1; j < states.Count; j++)
        if (!CombatCompatible(states[i], states[j]))
          return FilterRelation.Disjoint;
    return FilterRelation.Overlaps;
  }

  /// <summary>The base combat roles (attack / block) a combat-state predicate permits.</summary>
  private static (bool Attack, bool Block) CombatRoles(CombatState s) =>
    s switch
    {
      CombatState.Attacking => (true, false),
      CombatState.Blocking => (false, true),
      CombatState.AttackingOrBlocking => (true, true),
      CombatState.AttackingAlone => (true, false),
      _ => (true, true),
    };

  private static bool CombatCompatible(CombatState a, CombatState b)
  {
    var (aa, ab) = CombatRoles(a);
    var (ba, bb) = CombatRoles(b);
    return (aa && ba) || (ab && bb);
  }

  /// <summary>Supertypes are orthogonal (205.4b) and combinable; only a required-vs-excluded clash contradicts.</summary>
  private static FilterRelation SupertypeAxis(ObjectFilter a, ObjectFilter b)
  {
    var required = Union(a.Supertypes, b.Supertypes);
    var excluded = Union(a.ExcludedSupertypes, b.ExcludedSupertypes);
    return required.Any(excluded.Contains) ? FilterRelation.Disjoint : FilterRelation.Overlaps;
  }

  /// <summary>
  /// Color overlap (CR 105). Colorless has no color, so it excludes any required color and mono/multi;
  /// mono ⊥ multi; a monocolored object must fit every "has one of …" set into a single shared color.
  /// Two plain color sets are always co-satisfiable (a multicolored object can carry both).
  /// </summary>
  private static FilterRelation ColorAxis(ObjectFilter a, ObjectFilter b)
  {
    var colorless = a.IsColorless == true || b.IsColorless == true;
    var mono = a.IsMonocolored == true || b.IsMonocolored == true;
    var multi = a.IsMulticolored == true || b.IsMulticolored == true;
    var requiredSets = new List<HashSet<string>>();
    if (a.Colors is { Count: > 0 })
      requiredSets.Add(ToColorSet(a.Colors));
    if (b.Colors is { Count: > 0 })
      requiredSets.Add(ToColorSet(b.Colors));

    if (colorless && (mono || multi || requiredSets.Count > 0))
      return FilterRelation.Disjoint;
    if (mono && multi)
      return FilterRelation.Disjoint;
    if (mono && requiredSets.Count > 0)
    {
      var intersection = new HashSet<string>(requiredSets[0]);
      foreach (var set in requiredSets.Skip(1))
        intersection.IntersectWith(set);
      if (intersection.Count == 0)
        return FilterRelation.Disjoint; // no single color satisfies every required set
    }
    return FilterRelation.Overlaps;
  }

  private static HashSet<string> ToColorSet(IReadOnlyList<string> colors) =>
    new(colors.Select(c => c.Trim().ToLowerInvariant()), StringComparer.Ordinal);

  /// <summary>
  /// Type-identity overlap across CardTypes / Subtypes / ExcludedCardTypes / ExcludedSubtypes. The
  /// object's card-type set must contain every forced type, exclude every excluded type, and host
  /// each required subtype on one of its owning card types (the ontology's per-pool owners). A
  /// closed-pool subtype forces its single owner (so <c>Forest ⊥ instant</c>); a creature-type
  /// subtype can ride creature OR kindred (308.1), so it never forces Disjoint, and overlap is
  /// asserted only on the ordinary (non-kindred) owner — a kindred-only path yields
  /// <see cref="FilterRelation.Unknown"/>.
  /// </summary>
  private static FilterRelation TypeAxis(ObjectFilter a, ObjectFilter b, TypeOntology ontology)
  {
    var forced = Union(a.CardTypes, b.CardTypes);
    var excludedTypes = Union(a.ExcludedCardTypes, b.ExcludedCardTypes);
    var reqSubs = Union(a.Subtypes, b.Subtypes);
    var exclSubs = Union(a.ExcludedSubtypes, b.ExcludedSubtypes);

    // A required subtype that is also excluded is an outright contradiction.
    if (reqSubs.Any(exclSubs.Contains))
      return FilterRelation.Disjoint;

    // Owner options per required subtype: all owners, plus the ordinary (non-kindred) subset.
    var subOwners = new List<(IReadOnlyList<string> All, IReadOnlyList<string> Primary)>();
    var unknownSubtype = false;
    foreach (var s in reqSubs)
    {
      var owners = Lookup(ontology.SubtypeToCardTypes, s);
      if (owners is null || owners.Count == 0)
      {
        unknownSubtype = true; // subtype not in the ontology — can't place it
        continue;
      }
      var primary = owners
        .Where(o => !string.Equals(o, "kindred", StringComparison.OrdinalIgnoreCase))
        .ToList();
      subOwners.Add((owners, primary.Count > 0 ? primary : owners));
    }

    // Enumerate owner assignments (cartesian product); forced types are always in the set.
    var feasible = false;
    var overlaps = false;
    EnumerateAssignments(
      subOwners,
      0,
      [.. forced],
      primarySoFar: true,
      excludedTypes,
      ontology,
      ref feasible,
      ref overlaps
    );

    if (!feasible)
      return FilterRelation.Disjoint;
    if (unknownSubtype)
      return FilterRelation.Unknown; // an unplaceable-by-us subtype injects doubt
    return overlaps ? FilterRelation.Overlaps : FilterRelation.Unknown;
  }

  private static void EnumerateAssignments(
    List<(IReadOnlyList<string> All, IReadOnlyList<string> Primary)> subOwners,
    int index,
    List<string> chosen,
    bool primarySoFar,
    HashSet<string> excludedTypes,
    TypeOntology ontology,
    ref bool feasible,
    ref bool overlaps
  )
  {
    if (index == subOwners.Count)
    {
      switch (ComboFeasibility(chosen, excludedTypes, ontology))
      {
        case FilterRelation.Overlaps:
          feasible = true;
          if (primarySoFar)
            overlaps = true;
          break;
        case FilterRelation.Unknown:
          feasible = true;
          break;
      }
      return;
    }

    var (all, primary) = subOwners[index];
    foreach (var owner in all)
    {
      chosen.Add(owner);
      var isPrimary = primary.Contains(owner, StringComparer.OrdinalIgnoreCase);
      EnumerateAssignments(
        subOwners,
        index + 1,
        chosen,
        primarySoFar && isPrimary,
        excludedTypes,
        ontology,
        ref feasible,
        ref overlaps
      );
      chosen.RemoveAt(chosen.Count - 1);
    }
  }

  /// <summary>Is a concrete card-type set co-satisfiable (110.4) and free of excluded types?</summary>
  private static FilterRelation ComboFeasibility(
    List<string> tokens,
    HashSet<string> excludedTypes,
    TypeOntology ontology
  )
  {
    var distinct = tokens
      .Select(t => t.ToLowerInvariant())
      .Distinct(StringComparer.Ordinal)
      .ToList();
    if (distinct.Any(excludedTypes.Contains))
      return FilterRelation.Disjoint;

    var classes = distinct.Select(t => Classify(t, ontology)).ToList();
    var unknown = false;
    for (var i = 0; i < classes.Count; i++)
      for (var j = i + 1; j < classes.Count; j++)
        switch (PairVerdict(classes[i], classes[j]))
        {
          case FilterRelation.Disjoint:
            return FilterRelation.Disjoint;
          case FilterRelation.Unknown:
            unknown = true;
            break;
        }
    return unknown ? FilterRelation.Unknown : FilterRelation.Overlaps;
  }

  private static HashSet<string> Union(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
  {
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (a is not null)
      foreach (var x in a)
        set.Add(x.Trim().ToLowerInvariant());
    if (b is not null)
      foreach (var x in b)
        set.Add(x.Trim().ToLowerInvariant());
    return set;
  }

  private static IReadOnlyList<string>? Lookup(
    IReadOnlyDictionary<string, IReadOnlyList<string>> map,
    string key
  )
  {
    foreach (var kv in map)
      if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
        return kv.Value;
    return null;
  }

  /// <summary>Coarse card-type class for the 110.4 partition; the pseudo-types are not in the ontology's real lists.</summary>
  private enum TypeClass
  {
    Card, // "card" — universal
    Player, // "player" — not an object (CR 102)
    PermanentPseudo, // "permanent" — is-a-permanent predicate (CR 110.4a)
    Kindred, // partition-neutral (CR 308.1)
    Permanent, // a real 110.4 permanent type
    NonPermanent, // a real non-permanent type
    Unknown, // a token not in the ontology
  }

  private static TypeClass Classify(string t, TypeOntology ontology) =>
    t switch
    {
      "card" => TypeClass.Card,
      "player" => TypeClass.Player,
      "permanent" => TypeClass.PermanentPseudo,
      "kindred" => TypeClass.Kindred,
      _ when Contains(ontology.PermanentTypes, t) => TypeClass.Permanent,
      _ when Contains(ontology.NonPermanentTypes, t) => TypeClass.NonPermanent,
      _ => TypeClass.Unknown,
    };

  private static bool Contains(IReadOnlyList<string> xs, string t) =>
    xs.Any(x => string.Equals(x, t, StringComparison.OrdinalIgnoreCase));

  /// <summary>Can two card-type classes coexist on one object? Symmetric.</summary>
  private static FilterRelation PairVerdict(TypeClass x, TypeClass y)
  {
    // Player is categorically not an object → disjoint from everything but Player.
    if (x == TypeClass.Player || y == TypeClass.Player)
    {
      if (x == TypeClass.Player && y == TypeClass.Player)
        return FilterRelation.Overlaps;
      if (x == TypeClass.Unknown || y == TypeClass.Unknown)
        return FilterRelation.Unknown;
      return FilterRelation.Disjoint;
    }
    if (x == TypeClass.Unknown || y == TypeClass.Unknown)
      return FilterRelation.Unknown;
    if (x == TypeClass.Card || y == TypeClass.Card)
      return FilterRelation.Overlaps;
    if (x == TypeClass.Kindred || y == TypeClass.Kindred)
      // Kindred always rides another card type (308.1), so it never forces Disjoint — but the
      // specific combo's existence (a Kindred Land, say) is admissible, not provable, so a concrete
      // pairing is Unknown, never a false Overlaps. Mirrors the subtype-straddle treatment.
      return x == y ? FilterRelation.Overlaps : FilterRelation.Unknown;

    // Remaining: Permanent, PermanentPseudo, NonPermanent.
    var xPerm = x is TypeClass.Permanent or TypeClass.PermanentPseudo;
    var yPerm = y is TypeClass.Permanent or TypeClass.PermanentPseudo;
    var xNon = x == TypeClass.NonPermanent;
    var yNon = y == TypeClass.NonPermanent;

    if ((xPerm && yNon) || (xNon && yPerm))
      return FilterRelation.Disjoint; // permanent ⊥ non-permanent (110.4)
    if (xNon && yNon)
      return FilterRelation.Unknown; // two non-permanent types — no rule forbids, none printed
    return FilterRelation.Overlaps; // both permanent-ish (artifact creature, …)
  }

  /// <summary>
  /// True if the filter constrains an axis the operator does not yet decide — the soundness floor.
  /// Each landed axis removes its property from this scan. <b>Decided so far:</b> Controller, Owner,
  /// IsToken, Zone, the three numeric comparisons, the type identity axis (CardTypes, Subtypes,
  /// ExcludedCardTypes, ExcludedSubtypes), Supertypes/ExcludedSupertypes, the color family,
  /// Characteristics (keyword + combat-state), Name, EntityType, <c>ExcludeSelf</c>, and
  /// <c>IsSelf</c>. The residue is the Phase-3 relational axes.
  /// <para>
  /// <c>ExcludeSelf</c> (CR 109.5 "another") is intentionally absent: excluding the source object
  /// from a category never empties the category at the static level, so it is relationally
  /// compatible — it never blocks overlap, and flooring it to Unknown would needlessly demote every
  /// "another …" edge. Whether the only shared object happens to be the source is a runtime question
  /// the analytical operator does not answer. <c>IsSelf</c> ("this object") is absent for the same
  /// reason — the source is a real object, so a self-filter still overlaps a type-compatible filter;
  /// it is the <see cref="Subsumes"/> direction that gates it (a self-only sup is not contained by a
  /// non-self sub).
  /// </para>
  /// </summary>
  private static string? UndecidedAxis(ObjectFilter f) =>
    f.ExiledWith is not null ? "ExiledWith"
    : f.SharesColorWith is not null ? "SharesColorWith"
    : f.SharesCreatureTypeWith is not null ? "SharesCreatureTypeWith"
    : f.SharesNameWith is not null ? "SharesNameWith"
    : f.SharesCardTypeWith is not null ? "SharesCardTypeWith"
    : f.SharesPermanentTypeWith is not null ? "SharesPermanentTypeWith"
    : f.ChosenCharacteristic is not null ? "ChosenCharacteristic"
    : f.History is not null ? "History"
    : f.AttachedTo is not null ? "AttachedTo"
    : null;

  // ----- Subsumes ------------------------------------------------------------------------------

  /// <summary>
  /// Directional containment: is every object matching <paramref name="sub"/> also matched by
  /// <paramref name="sup"/> (sub ⊆ sup)? Grades edge reliability for mast-int — a <see cref="Trilean.Yes"/>
  /// is a reliable refuel, <see cref="Trilean.Unknown"/>/<see cref="Trilean.No"/> a conditional one.
  /// Combined by Kleene AND over the axes: every constraint <paramref name="sup"/> imposes must be
  /// <em>guaranteed</em> by <paramref name="sub"/>. Distinct from <see cref="Intersects"/> — a
  /// subtype only witnesses overlap, it does not guarantee its card type, so a straddling subtype
  /// (creature⇔kindred, 308.1) yields <see cref="Trilean.Unknown"/> here, never <see cref="Trilean.Yes"/>.
  /// </summary>
  public static SubsumeMatch Subsumes(ObjectFilter sub, ObjectFilter sup, TypeOntology ontology)
  {
    // A self-contradictory sub denotes ∅, and ∅ ⊆ everything.
    if (Intersects(sub, sub, ontology).Relation == FilterRelation.Disjoint)
      return new SubsumeMatch(Trilean.Yes);
    // sub is satisfiable: if it cannot share an object with sup, it is not contained in it.
    if (Intersects(sub, sup, ontology).Relation == FilterRelation.Disjoint)
      return new SubsumeMatch(Trilean.No, "Disjoint");

    (string Axis, Trilean Verdict)[] axes =
    [
      ("Controller", ControllerSubsumes(sub.Controller, sup.Controller)),
      ("Owner", ControllerSubsumes(sub.Owner, sup.Owner)),
      ("Zone", ZoneSubsumes(sub.Zone, sup.Zone)),
      ("IsToken", BoolSubsumes(sub.IsToken, sup.IsToken)),
      ("IsSelf", BoolSubsumes(sub.IsSelf, sup.IsSelf)),
      ("Power", ComparisonSubsumes(sub.PowerComparison, sup.PowerComparison)),
      ("Toughness", ComparisonSubsumes(sub.ToughnessComparison, sup.ToughnessComparison)),
      ("ManaValue", ComparisonSubsumes(sub.ManaValueComparison, sup.ManaValueComparison)),
      ("Types", TypeSubsumes(sub, sup, ontology)),
      ("Supertypes", SupertypeSubsumes(sub, sup)),
      ("Colors", ColorSubsumes(sub, sup)),
      ("Name", NameSubsumes(sub.Name, sup.Name)),
      ("EntityType", EntityTypeSubsumes(sub, sup)),
      ("Characteristics", CharacteristicsSubsumes(sub, sup)),
    ];

    string? reason = null;
    foreach (var (axis, verdict) in axes)
    {
      if (verdict == Trilean.No)
        return new SubsumeMatch(Trilean.No, axis); // No absorbs in Kleene AND
      if (verdict == Trilean.Unknown && reason is null)
        reason = axis;
    }

    reason ??= SupUndecidedAxis(sup);
    return reason is null
      ? new SubsumeMatch(Trilean.Yes)
      : new SubsumeMatch(Trilean.Unknown, reason);
  }

  /// <summary>sup pins a controller for all objects; sub must pin the same one. <c>Any</c>/null sup imposes nothing.</summary>
  private static Trilean ControllerSubsumes(ControllerFilter? sub, ControllerFilter? sup)
  {
    if (sup is null || sup == ControllerFilter.Any)
      return Trilean.Yes;
    if (IsRuntimeChosen(sup.Value))
      return Trilean.Unknown;
    if (sub is null || sub == ControllerFilter.Any)
      return Trilean.No;
    if (IsRuntimeChosen(sub.Value))
      return Trilean.Unknown;
    return sub == sup ? Trilean.Yes : Trilean.No;
  }

  private static Trilean ZoneSubsumes(Zone? sub, Zone? sup)
  {
    if (sup is null || sup == Zone.Anywhere)
      return Trilean.Yes;
    if (sub is null || sub == Zone.Anywhere)
      return Trilean.No;
    return sub == sup ? Trilean.Yes : Trilean.No;
  }

  private static Trilean BoolSubsumes(bool? sub, bool? sup)
  {
    if (sup is null)
      return Trilean.Yes;
    if (sub is null)
      return Trilean.No;
    return sub == sup ? Trilean.Yes : Trilean.No;
  }

  /// <summary>sub's admitted-integer interval must sit inside sup's. <c>NotEqual</c> on either side → Unknown.</summary>
  private static Trilean ComparisonSubsumes(Comparison? sub, Comparison? sup)
  {
    if (sup is null)
      return Trilean.Yes;
    if (sub is null)
      return Trilean.No;
    if (sub.Operator == ComparisonOperator.NotEqual || sup.Operator == ComparisonOperator.NotEqual)
      return Trilean.Unknown;
    // A relative comparison ("less than this creature's power") has no printed integer
    // window — its threshold resolves against another object at runtime (CR 702.134), so
    // interval containment is undecidable.
    if (sub.Value is null || sup.Value is null)
      return Trilean.Unknown;
    var (slo, shi) = Range(sub);
    var (plo, phi) = Range(sup);
    return plo <= slo && shi <= phi ? Trilean.Yes : Trilean.No;
  }

  private static (long Lo, long Hi) Range(Comparison c) =>
    c.Operator switch
    {
      ComparisonOperator.LessThan => (long.MinValue, (long)c.Value! - 1),
      ComparisonOperator.LessThanOrEqual => (long.MinValue, c.Value!.Value),
      ComparisonOperator.GreaterThan => ((long)c.Value! + 1, long.MaxValue),
      ComparisonOperator.GreaterThanOrEqual => (c.Value!.Value, long.MaxValue),
      ComparisonOperator.Equal => (c.Value!.Value, c.Value!.Value),
      _ => (long.MinValue, long.MaxValue),
    };

  /// <summary>Type-identity containment: every sup card type / subtype / exclusion must be guaranteed by sub.</summary>
  private static Trilean TypeSubsumes(ObjectFilter sub, ObjectFilter sup, TypeOntology ontology)
  {
    var result = Trilean.Yes;

    if (sup.CardTypes is not null)
      foreach (var c in sup.CardTypes)
        result = Kleene.And(result, TypeGuaranteed(c.Trim().ToLowerInvariant(), sub, ontology));

    if (sup.Subtypes is not null)
    {
      var subSubs = Union(sub.Subtypes, null); // case-insensitive
      foreach (var s in sup.Subtypes)
        // A specific subtype is guaranteed only if sub also requires it.
        result = Kleene.And(result, subSubs.Contains(s.Trim()) ? Trilean.Yes : Trilean.No);
    }

    if (sup.ExcludedCardTypes is not null)
      foreach (var e in sup.ExcludedCardTypes)
        result = Kleene.And(result, ExclusionGuaranteed(e.Trim().ToLowerInvariant(), sub, ontology));

    if (sup.ExcludedSubtypes is not null)
    {
      var subExcl = Union(sub.ExcludedSubtypes, null);
      foreach (var e in sup.ExcludedSubtypes)
        result = Kleene.And(result, subExcl.Contains(e.Trim()) ? Trilean.Yes : Trilean.Unknown);
    }

    return result;
  }

  /// <summary>Does every object satisfying sub provably have card type <paramref name="c"/>?</summary>
  private static Trilean TypeGuaranteed(string c, ObjectFilter sub, TypeOntology ontology)
  {
    var forced = Union(sub.CardTypes, null);
    if (forced.Contains(c) || forced.Any(x => Implies(x, c, ontology)))
      return Trilean.Yes;

    var straddleReachesC = false;
    if (sub.Subtypes is not null)
      foreach (var s in sub.Subtypes)
      {
        var owners = Lookup(ontology.SubtypeToCardTypes, s.Trim());
        if (owners is null || owners.Count == 0)
          continue;
        if (owners.All(o => Implies(o, c, ontology)))
          return Trilean.Yes; // every owner forces c (a closed pool whose owner implies c)
        if (owners.Any(o => Implies(o, c, ontology)))
          straddleReachesC = true; // c reachable via some owner but not all — the straddle
      }
    if (straddleReachesC)
      return Trilean.Unknown;

    // Unrelated forced types — but if any relation to c is itself Unknown, defer.
    var cClass = Classify(c, ontology);
    if (forced.Any(x => PairVerdict(Classify(x, ontology), cClass) == FilterRelation.Unknown))
      return Trilean.Unknown;
    return Trilean.No;
  }

  /// <summary>Does card type <paramref name="x"/> guarantee card type <paramref name="c"/>?</summary>
  private static bool Implies(string x, string c, TypeOntology ontology)
  {
    if (string.Equals(x, c, StringComparison.OrdinalIgnoreCase))
      return true;
    // A real permanent type implies the "permanent" pseudo-type (creature ⊆ permanent).
    if (string.Equals(c, "permanent", StringComparison.OrdinalIgnoreCase))
      return Contains(ontology.PermanentTypes, x);
    return false;
  }

  /// <summary>Does sub guarantee its objects do NOT carry excluded card type <paramref name="e"/>?</summary>
  private static Trilean ExclusionGuaranteed(string e, ObjectFilter sub, TypeOntology ontology)
  {
    if (Union(sub.ExcludedCardTypes, null).Contains(e))
      return Trilean.Yes;
    var eClass = Classify(e, ontology);
    var anyUnknown = false;
    foreach (var x in Union(sub.CardTypes, null))
      switch (PairVerdict(Classify(x, ontology), eClass))
      {
        case FilterRelation.Disjoint:
          return Trilean.Yes; // sub forces a type that can't coexist with e
        case FilterRelation.Unknown:
          anyUnknown = true;
          break;
      }
    return anyUnknown ? Trilean.Unknown : Trilean.No;
  }

  private static Trilean SupertypeSubsumes(ObjectFilter sub, ObjectFilter sup)
  {
    var result = Trilean.Yes;
    var subReq = Union(sub.Supertypes, null);
    if (sup.Supertypes is not null)
      foreach (var s in sup.Supertypes)
        result = Kleene.And(result, subReq.Contains(s.Trim()) ? Trilean.Yes : Trilean.No);
    var subExcl = Union(sub.ExcludedSupertypes, null);
    if (sup.ExcludedSupertypes is not null)
      foreach (var e in sup.ExcludedSupertypes)
        result = Kleene.And(result, subExcl.Contains(e.Trim()) ? Trilean.Yes : Trilean.Unknown);
    return result;
  }

  /// <summary>Color containment (CR 105): every color constraint sup imposes must be guaranteed by sub.</summary>
  private static Trilean ColorSubsumes(ObjectFilter sub, ObjectFilter sup)
  {
    var result = Trilean.Yes;
    if (sup.IsColorless == true)
      result = Kleene.And(result, sub.IsColorless == true ? Trilean.Yes : Trilean.No);
    if (sup.IsMonocolored == true)
      result = Kleene.And(result, sub.IsMonocolored == true ? Trilean.Yes : Trilean.Unknown);
    if (sup.IsMulticolored == true)
      result = Kleene.And(result, sub.IsMulticolored == true ? Trilean.Yes : Trilean.Unknown);
    if (sup.Colors is { Count: > 0 })
    {
      var supSet = ToColorSet(sup.Colors);
      var subColors = sub.Colors is { Count: > 0 } ? ToColorSet(sub.Colors) : null;
      // sub guarantees one of sup's colors iff every color sub may carry lies inside sup's set.
      result = Kleene.And(
        result,
        subColors is not null && subColors.IsSubsetOf(supSet) ? Trilean.Yes : Trilean.No
      );
    }
    return result;
  }

  private static Trilean NameSubsumes(string? sub, string? sup)
  {
    if (sup is null)
      return Trilean.Yes;
    if (sub is null)
      return Trilean.No;
    return string.Equals(sub.Trim(), sup.Trim(), StringComparison.Ordinal)
      ? Trilean.Yes
      : Trilean.No;
  }

  private static Trilean EntityTypeSubsumes(ObjectFilter sub, ObjectFilter sup)
  {
    var supE = sup.EntityType?.Trim().ToLowerInvariant();
    if (supE is null)
      return Trilean.Yes;
    if (supE == "player")
    {
      var subPlayer =
        string.Equals(sub.EntityType, "player", StringComparison.OrdinalIgnoreCase)
        || (
          sub.CardTypes?.Any(t => string.Equals(t.Trim(), "player", StringComparison.OrdinalIgnoreCase))
          ?? false
        );
      return subPlayer ? Trilean.Yes : Trilean.No;
    }
    return string.Equals(sub.EntityType?.Trim(), supE, StringComparison.OrdinalIgnoreCase)
      ? Trilean.Yes
      : Trilean.No;
  }

  private static Trilean CharacteristicsSubsumes(ObjectFilter sub, ObjectFilter sup)
  {
    if (sup.Characteristics is not { Count: > 0 })
      return Trilean.Yes;
    if (sup.Characteristics.OfType<OtherCharacteristic>().Any())
      return Trilean.Unknown;
    if (sub.Characteristics?.OfType<OtherCharacteristic>().Any() ?? false)
      return Trilean.Unknown; // sub's residual clouds any guarantee

    var result = Trilean.Yes;
    foreach (var c in sup.Characteristics)
      switch (c)
      {
        case KeywordCharacteristic kw:
          var hasKeyword =
            sub.Characteristics?.OfType<KeywordCharacteristic>().Any(k => k.Keyword == kw.Keyword)
            ?? false;
          result = Kleene.And(result, hasKeyword ? Trilean.Yes : Trilean.No);
          break;
        case CombatStateCharacteristic cs:
          var implied =
            sub.Characteristics?.OfType<CombatStateCharacteristic>()
              .Any(s => RolesSubset(s.State, cs.State)) ?? false;
          result = Kleene.And(result, implied ? Trilean.Yes : Trilean.No);
          break;
        case TappedStateCharacteristic ts:
          var hasTapState =
            sub.Characteristics?.OfType<TappedStateCharacteristic>().Any(s => s.Tapped == ts.Tapped)
            ?? false;
          result = Kleene.And(result, hasTapState ? Trilean.Yes : Trilean.No);
          break;
        case CounterCharacteristic cc:
          var hasCounter =
            sub.Characteristics?.OfType<CounterCharacteristic>()
              .Any(s => string.Equals(s.CounterType, cc.CounterType, StringComparison.OrdinalIgnoreCase))
            ?? false;
          result = Kleene.And(result, hasCounter ? Trilean.Yes : Trilean.No);
          break;
      }
    return result;
  }

  private static bool RolesSubset(CombatState sub, CombatState sup)
  {
    var (subA, subB) = CombatRoles(sub);
    var (supA, supB) = CombatRoles(sup);
    return (!subA || supA) && (!subB || supB);
  }

  /// <summary>
  /// Remaining Phase-3 relational axes plus <c>ExcludeSelf</c>: if sup constrains one,
  /// sub-containment is undecidable here. (ExcludeSelf is conservative for the stricter containment
  /// question — it can silently exclude an object sub would include — so unlike
  /// <see cref="Intersects"/> it floors to Unknown rather than passing as relationally compatible.)
  /// </summary>
  private static string? SupUndecidedAxis(ObjectFilter sup) =>
    sup.ExiledWith is not null ? "ExiledWith"
    : sup.SharesColorWith is not null ? "SharesColorWith"
    : sup.SharesCreatureTypeWith is not null ? "SharesCreatureTypeWith"
    : sup.SharesNameWith is not null ? "SharesNameWith"
    : sup.SharesCardTypeWith is not null ? "SharesCardTypeWith"
    : sup.SharesPermanentTypeWith is not null ? "SharesPermanentTypeWith"
    : sup.ChosenCharacteristic is not null ? "ChosenCharacteristic"
    : sup.History is not null ? "History"
    : sup.AttachedTo is not null ? "AttachedTo"
    : sup.ExcludeSelf == true ? "ExcludeSelf"
    : null;
}
