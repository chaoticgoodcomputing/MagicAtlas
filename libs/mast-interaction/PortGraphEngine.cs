namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>Who authored an edge (ADR-0002 §5). Card-defined hops are certain; rules-defined are operator-tiered.</summary>
public enum EdgeProvenance
{
  CardDefined,
  RulesDefined,
}

/// <summary>
/// A directed port→port edge over the single-role <see cref="PortNode"/> model (ADR-0002), carrying
/// its provenance (§5) and — for rules-defined edges — the operator's verdicts. A card-defined edge
/// is GREEN by construction (the card's own causality); a rules-defined edge is tiered like the
/// classic <c>InteractionEdge</c>.
/// </summary>
public sealed record PortEdge
{
  public required PortNode From { get; init; }
  public required PortNode To { get; init; }
  public required EdgeProvenance Provenance { get; init; }
  public EdgeFamily Family { get; init; } = EdgeFamily.Flow;
  public FilterRelation Overlap { get; init; } = FilterRelation.Overlaps;
  public Trilean Reliability { get; init; } = Trilean.Yes;
  public string? Reason { get; init; }

  public CertaintyTier Tier =>
    Provenance == EdgeProvenance.CardDefined ? CertaintyTier.Green // certain by construction (§5)
    : Overlap == FilterRelation.Disjoint ? CertaintyTier.Red
    : Overlap == FilterRelation.Overlaps && Reliability == Trilean.Yes ? CertaintyTier.Green
    : CertaintyTier.Amber;
}

/// <summary>A reconstructed loop over the port graph; its tier is the worst hop.</summary>
public sealed record PortCycle
{
  public required IReadOnlyList<PortEdge> Edges { get; init; }

  /// <summary>
  /// Firability (ADR-0002 §8): no hop touches a gated port (a rate limit / intervening-if). A
  /// non-firable cycle cannot be certified infinite — <c>net(R)</c> is blind to gates — so its tier
  /// floors to Amber even when every edge is Green. A <b>tap</b> gate is the dischargeable exception:
  /// it floors unless the loop renews it (<see cref="TapRenewed"/>) by untapping the permanent each
  /// iteration (Blasting Station's "untap when a creature enters", fed by the loop's creature tokens).
  /// </summary>
  public bool Firable =>
    !Edges.Any(e => e.From.Gated || e.To.Gated)
    && (TapRenewed || !Edges.Any(e => e.From.TapGated || e.To.TapGated));

  /// <summary>
  /// All the cycle's tap gates are renewed (ADR-0002 §8): every tap-gated permanent the loop traverses
  /// untaps itself each iteration on an event the loop produces (a self-untap <c>etb:X → emit:untap</c>
  /// fed by a created token whose type triggers it). The engine sets this; default <c>false</c> (a tap
  /// gate floors unless proven renewed — the carve-out is strict, the dual of §8-B's self-return).
  /// </summary>
  public bool TapRenewed { get; init; }

  /// <summary>
  /// Multi-cost conjunction (ADR-0002 §8): an activated ability fires only if <b>all</b> its costs are
  /// paid, so a loop that closes through one cost port of an ability is certifiable only if the
  /// ability's <em>other</em> cost ports are each fed too — by the loop, by a producer, or free (tap).
  /// The engine sets this; an unfed resource co-cost (Chatterfang's <c>{B}</c> with no mana source)
  /// means the ability can't actually fire, so the cycle floors to Amber. Default <c>true</c> for
  /// hand-built cycles that don't carry the card-defined cost structure.
  /// </summary>
  public bool CoCostsSatisfied { get; init; } = true;

  /// <summary>
  /// Balance (ADR-0002 §8): the loop's per-iteration resource production covers its consumption —
  /// <c>net(R) ≥ 0</c>. The engine sets this for mana (the binding combo resource): a cycle whose
  /// <c>pay:mana</c> costs exceed the mana its own producers feed back (Chatterfang × Ruthless Knave —
  /// {2}{B} = 3 mana vs two Treasures = 2) is finite, not infinite, so it floors to Amber. Default
  /// <c>true</c> (and conservative: only floored when the shortfall is provable from known quantities).
  /// </summary>
  public bool Balanced { get; init; } = true;

  /// <summary>
  /// Productivity (ADR-0002 §8): the loop nets an <em>unbounded</em> resource — it doesn't just sustain
  /// itself, it produces something. A <b>pure-mana</b> loop (every emit is mana) that nets exactly zero
  /// (a 1-for-1 filter: Bog Initiate <c>{1}:Add{B}</c> ↔ Farrelite Priest <c>{1}:Add{W}</c>) is a
  /// do-nothing — it cycles the same mana forever, producing no advantage — so it is not an infinite
  /// combo and floors to Amber. A loop with a non-mana output (a created token, a counter, a trigger)
  /// is productive via that output even at net-zero mana. Default <c>true</c>; the engine sets it.
  /// </summary>
  public bool Productive { get; init; } = true;

  /// <summary>The worst hop sets the tier, floored to Amber when the cycle is not firable, has an unfed co-cost, is resource-negative, or nets nothing (§8).</summary>
  public CertaintyTier Tier =>
    Edges.Count == 0
      ? CertaintyTier.Green
      : (CertaintyTier)
        Math.Max(
          (int)Edges.Max(e => e.Tier),
          Firable && CoCostsSatisfied && Balanced && Productive ? 0 : (int)CertaintyTier.Amber
        );

  /// <summary>The hop that limits the tier (and its operator <see cref="PortEdge.Reason"/>).</summary>
  public PortEdge? LimitingHop =>
    Edges
      .OrderByDescending(e => (int)e.Tier)
      .ThenBy(e => e.From.Label, StringComparer.Ordinal)
      .FirstOrDefault();

  /// <summary>Why this cycle isn't a certified-GREEN infinite (for the viz hover): the cycle-level floor
  /// reason takes precedence (a gate / an unfed co-cost), else the worst hop's operator reason; <c>null</c> when GREEN.</summary>
  public string? LimitingReason =>
    Tier == CertaintyTier.Green ? null
    : Edges.Any(e => e.From.Gated || e.To.Gated) ? "gated (rate-limit / intervening-if)"
    : !Firable ? "tap (not renewed by an untapper)"
    : !Balanced ? "mana-negative"
    : !Productive ? "net-zero filter (no surplus)"
    : !CoCostsSatisfied ? "unfed co-cost"
    : LimitingHop?.Reason is { Length: > 0 } reason ? reason
    : "amber hop";
}

/// <summary>
/// The interaction engine over the single-role port model (ADR-0002 §4–6) — the successor to
/// the original <c>InteractionEngine</c>. Combines the walk's <b>card-defined</b> edges (certain, §5) with
/// <b>rules-defined</b> edges it derives: flow (an emitted object refuels a consume; mana refunds a
/// mana cost), the curated <b>sac→death bridge</b> (a sacrificed creature dies, CR 701.21a→700.4),
/// and <b>modifier</b> edges (a replacement intercepts a token emission). Type decisions go through
/// the MAST <c>ObjectFilter</c> operator (§7); <c>Disjoint</c> is the prune. Built alongside the old
/// engine (S3a) until the migration (S3b). The derived flow rules are the minimal grammar the
/// canonical gold needs — the fuller derived role-compatibility (§6) is a follow-on.
/// </summary>
public sealed class PortGraphEngine
{
  /// <summary>
  /// The canonical cycle-reconstruction REACH — the maximum number of hops (edges) in an elementary
  /// cycle the product reconstructs and reports. This is the single source of truth: the recall bench
  /// (<c>ComboRecallRunner</c>), the product viz flow (<c>MaterializeCyclesStep</c>), and the flow-arm
  /// scope tests all reference it so they cannot drift apart (a worker scope test that enumerates
  /// <em>unbounded</em> can pass on a cycle longer than the product reconstructs — a false "will-flip";
  /// see <c>docs/adding-a-flow-arm.md</c> anti-pattern 5).
  /// <para>
  /// Currently <b>6</b>: the longest real reconstructed loop is the 6-hop cast-recursion blink
  /// (Displacer Kitten: emit:cast → trigger:cast → emit:blink → etb → emit:untap → pay:mana); the
  /// 5-hop sac→death→token→doubler→refuel aristocrat loop and the full Ashnod×Ruthless×Chatterfang
  /// 6-hop infinite also fit. Raising it is <b>soundness-safe</b> — longer cycles have more co-costs/§8
  /// gates to clear, so they floor toward Amber, never toward a false GREEN — but at the per-combo
  /// 2–3-card scale it finds nothing new past ~7 (cycle counts saturate; the bound is non-binding on
  /// compute). The two-layer path (<see cref="FindCyclesByLabelGraph"/>) treats reach as a post-
  /// enumeration display filter, not a search bound. (See the cycle-enumeration feasibility memo.)
  /// </para>
  /// </summary>
  public const int DefaultReconstructionReach = 6;

  private readonly TypeOntology _ontology;

  public PortGraphEngine(TypeOntology ontology) => _ontology = ontology;

  public IReadOnlyList<PortEdge> Materialize(IReadOnlyList<PortGraph> graphs)
  {
    // (0) Copy-token inheritance (copy-inheritance-scope.md, Decision 2): the copy effect is the ONLY
    // port whose meaning depends on the OTHER cards in the set, so the graft runs here — where the card
    // set is known — not in PortWalk (which sees one card). It resolves each copy emit's target filter
    // against the materialised set and clones the matching cards' ports under a synthesized copy identity.
    var grafted = GraftCopyInheritance(graphs);
    graphs = grafted.Graphs;

    var ports = graphs.SelectMany(g => g.Ports).ToList();
    var edges = new List<PortEdge>();

    // (1) Card-defined edges — the walk's intra-ability causality, certain by construction (§5).
    foreach (var graph in graphs)
      foreach (var edge in graph.CardDefinedEdges)
        edges.Add(
          new PortEdge { From = edge.From, To = edge.To, Provenance = EdgeProvenance.CardDefined }
        );

    // The graft's synthesized closing edges (an inherited target-untap renewing the copier's tap), tiered
    // by the operator in the pass (Decision 3/4) — added alongside the card-defined edges so FindCycles
    // closes the copy loop with no new arm (the connection layer doing its normal job, per §6 Track B).
    edges.AddRange(grafted.ClosingEdges);

    var emits = ports.Where(p => p.Side == PortSide.Emit).ToList();
    var consumes = ports.Where(p => p.Side == PortSide.Consume).ToList();
    var intercepts = ports.Where(p => p.Side == PortSide.Intercept).ToList();

    // (2) Flow — an emitted object refuels a consume; mana refunds a mana cost.
    foreach (var emit in emits)
      foreach (var consume in consumes)
        if (FlowFeasible(emit, consume))
          AddRulesEdge(edges, emit, consume, EdgeFamily.Flow);

    // (2b) Untap-lands → mana (the mana-untap enabler). An "untap up to N lands" effect (Peregrine Drake)
    // makes those lands available to tap for mana again — a free mana source that refunds a pay:mana cost
    // (recast a flicker spell, activate again). This is the SHARED enabler the blink-etb-refuel and
    // displacer-cast-blink families turn on. NOT routed through AddRulesEdge: a pay:mana consume has a
    // null (scalar) Subject, which would hit the scalar null-default GREEN (adding-a-flow-arm
    // anti-pattern 3) and FALSELY certify it. Instead each edge is tiered explicitly AMBER — the untapped
    // lands' COLOURS and COUNT are unknown (the cards don't say which lands, "up to five" could be zero),
    // so the engine can never certify they cover a specific coloured pip (CR 107.4). Soundly irreducible
    // (NOT GREEN, NOT Red): the loop is structurally feasible but the mana is uncertain. The dual of the
    // blink arm's optional-ETB floor.
    foreach (var untap in emits.Where(p => IsLandUntap(p)))
      foreach (var pay in consumes.Where(p => IsPayMana(p.Label)))
        edges.Add(UntapLandsFeedsMana(untap, pay));

    // (3) Bridge — a sacrificed creature dies (CR 701.21a→700.4), feeding a dies-trigger.
    foreach (var sac in consumes.Where(p => Role(p.Label) == "sac"))
      foreach (var dies in consumes.Where(p => Role(p.Label) == "ltb" && p.Label.Contains(":to-graveyard")))
        AddRulesEdge(edges, sac, dies, EdgeFamily.Flow);

    // (4) Modifier — a replacement intercepts a token emission (ADR-0001 §4).
    foreach (var emit in emits.Where(p => ResourceKind(p.Label) == "token"))
      foreach (var intercept in intercepts)
        AddRulesEdge(edges, emit, intercept, EdgeFamily.Modifier);

    return edges;
  }

  /// <summary>The graft pass's product: the original graphs PLUS a synthesized copy graph per admissible
  /// (copier, copied-card) pair, and the closing edges that renew the copier's tap from an inherited
  /// target-untap.</summary>
  private sealed record GraftResult
  {
    public required IReadOnlyList<PortGraph> Graphs { get; init; }
    public required IReadOnlyList<PortEdge> ClosingEdges { get; init; }
  }

  /// <summary>
  /// Copy-token inheritance (copy-inheritance-scope.md, Decision 2/3) — the combo-aware graft. For each
  /// <c>emit:copy</c> port (a copier), resolve its target filter against the OTHER cards in the set; for
  /// each candidate C the copy filter <b>Subsumes</b> (Decision 3 — never merely <c>Intersects</c>), clone
  /// C's ports + card-defined edges under a synthesized copy identity, applying the copier's
  /// <see cref="PortNode.CopyMods"/> (e.g. Kiki's <c>abilityAdder:haste</c>, a <c>supertypeRemover</c>).
  /// A GREEN card-defined edge <c>copier.emit:copy → copy.&lt;etb&gt;</c> records that the copier definitely
  /// creates that object (CR 707.2). The cloned card-defined edges stay GREEN (C's own causality is
  /// preserved). Finally, if the copy carries an inherited untap that reaches the copier's tap, synthesize
  /// the closing edge (Decision 4) so the loop closes.
  /// <para><b>The false-positive guard (Decision 3), two-layered:</b> (i) admissibility — we graft C only
  /// when the copy filter does not <em>provably</em> exclude C's known type (<see cref="CopyAdmits"/> uses
  /// <c>Subsumes</c>, which prunes a type-incompatible C that <c>Intersects</c> would wrongly admit);
  /// (ii) closure — a grafted copy with no ability that acts back on the copier forms no cycle (dead weight
  /// the cycle finder ignores). A copier + a vanilla creature therefore produces NO combo: a vanilla body
  /// has no ports to clone, so no closing edge exists.</para>
  /// </summary>
  private GraftResult GraftCopyInheritance(IReadOnlyList<PortGraph> graphs)
  {
    // EXACT-match emit:copy (a permanent token-copy) — NOT a prefix: emit:copy:spell is a STACK spell-copy
    // (CR 707.10) with no battlefield ports to graft, and must never enter the permanent graft path. Keep
    // this an equality check; broadening it to StartsWith would wrongly graft a spell-copy as a permanent.
    var copies = graphs
      .SelectMany(g => g.Ports)
      .Where(p => p.Side == PortSide.Emit && p.Label == "emit:copy" && p.Subject is not null)
      .ToList();
    if (copies.Count == 0)
      return new GraftResult { Graphs = graphs, ClosingEdges = [] };

    // The card each port belongs to → its source graph (to read a candidate's full port graph + edges).
    var graphByCard = graphs
      .SelectMany(g => g.Ports.Select(p => (p.Card, g)))
      .GroupBy(x => x.Card, StringComparer.Ordinal)
      .ToDictionary(x => x.Key, x => x.First().g, StringComparer.Ordinal);

    var extraGraphs = new List<PortGraph>();
    var closingEdges = new List<PortEdge>();

    foreach (var copy in copies)
    {
      var copier = copy.Card;
      foreach (var candidate in graphs)
      {
        // The candidate card's identity (its ports all share one Card). Skip the copier copying itself.
        var candidateCard = candidate.Ports.Select(p => p.Card).FirstOrDefault();
        if (candidateCard is null || string.Equals(candidateCard, copier, StringComparison.Ordinal))
          continue;

        var candidateSelf = SelfFilter(candidate);
        if (candidateSelf is null || !CopyAdmits(copy.Subject!, candidateSelf))
          continue; // not a creature we can certify the copy may legally be — don't graft (guard layer i)

        var (graftGraph, copyId) = CloneUnderCopyIdentity(candidate, candidateCard, copier, copy.CopyMods);
        extraGraphs.Add(graftGraph);

        // The copier definitely creates the copy object (CR 707.2): a card-defined GREEN edge from the
        // copy emit to each of the copy's entry/cost consumes (its ETB fires in the copier's loop).
        var graftEdges = graftGraph.CardDefinedEdges.ToList();
        foreach (var entry in graftGraph.Ports.Where(p => p.Side == PortSide.Consume && Role(p.Label) == "etb"))
          graftEdges.Add(new CardDefinedEdge { From = copy, To = entry });
        extraGraphs[^1] = graftGraph with { CardDefinedEdges = graftEdges };

        // The closing hop (Decision 4): an inherited untap on the copy that renews the COPIER's tap.
        foreach (var untap in graftGraph.Ports.Where(p => p.Side == PortSide.Emit && IsUntap(p.Label)))
          if (UntapReachesSource(untap) is { } reliability)
            closingEdges.Add(CloseUntapToTap(untap, copier, graphByCard, reliability));

        // The blink closing hop (blink arm). An inherited BLINK on the copy (Kiki copies Restoration
        // Angel / Felidar Guardian — the copy's ETB blinks a permanent) can blink the COPIER itself, which
        // re-enters UNTAPPED (CR 603.6e/400.7), renewing the copier's tap — the dual of the inherited
        // untap. Feasible iff the blinked filter admits a creature (the copier is a creature). The blink is
        // Gated ("you may"), so the renewal is AMBER, never GREEN — soundly irreducible (the optional ETB).
        foreach (var blink in graftGraph.Ports.Where(p => p.Side == PortSide.Emit && IsBlink(p.Label)))
          if (BlinkReachesSource(blink) is { } reliability)
            closingEdges.Add(CloseUntapToTap(blink, copier, graphByCard, reliability));
      }
    }

    return new GraftResult { Graphs = [.. graphs, .. extraGraphs], ClosingEdges = closingEdges };
  }

  /// <summary>
  /// A card's <b>self characteristics</b> reconstructed from its <c>IsSelf:true</c> ports (an ETB/LTB
  /// trigger's "this creature" filter — Corridor Monitor's <c>etb:creature:self</c> ⇒ <c>{creature}</c>).
  /// The card type line is not threaded into <see cref="PortWalk"/>, so this is the engine's only window
  /// onto "what type is C": its own self-scoped abilities. <c>null</c> when no self-typed port exists (a
  /// vanilla body — nothing to graft, which is exactly the negative-control behaviour).
  /// </summary>
  private static ObjectFilter? SelfFilter(PortGraph graph)
  {
    var cardTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var p in graph.Ports)
      if (p.Subject?.IsSelf == true && p.Subject.CardTypes is { } cts)
        foreach (var t in cts)
          cardTypes.Add(t);
    return cardTypes.Count == 0 ? null : new ObjectFilter { CardTypes = [.. cardTypes] };
  }

  /// <summary>
  /// Decision 3 admissibility, via <c>Subsumes</c> (NOT <c>Intersects</c>). C is graftable iff the copy
  /// filter does not <b>provably</b> exclude C's known self-type — <c>Subsumes(sub=C, sup=copyFilter)</c>
  /// returns <see cref="Trilean.Yes"/> (certified) or <see cref="Trilean.Unknown"/> (unverifiable from
  /// ports — e.g. the copy filter's <c>!Legendary</c> exclusion against a self-type that carries no
  /// supertype; the copy's "creature you control" targeting guarantees control + non-legendarity at copy
  /// time, which the static self-filter cannot witness). A <see cref="Trilean.No"/> is a provable type
  /// mismatch (the copy filter wants <c>{artifact}</c>, C is creature-only) — pruned. This is strictly
  /// stronger than <c>Intersects</c>: a creature-only C is <c>Intersects</c>-compatible with an
  /// artifact-wanting filter (both permanent) but <c>Subsumes</c> rejects it. The GREEN ceiling rides on
  /// the closing ability (Decision 3b), not on this admissibility verdict.
  /// </summary>
  private bool CopyAdmits(ObjectFilter copyFilter, ObjectFilter candidateSelf)
  {
    // Drop the Controller/Owner axes from the copy filter before the containment check: "create a copy of
    // target creature YOU CONTROL" enforces control at copy TIME via targeting (CR 707.2), a board-state
    // guarantee the candidate's static self-filter (reconstructed from its own abilities) can never
    // witness. Leaving Controller=You on the sup would make every copy's Subsumes verdict No (the
    // self-filter has no controller), defeating the graft. The remaining axes — card type, subtype,
    // supertype exclusion — are the type-identity constraints the static self-filter CAN speak to.
    var typeFilter = copyFilter with { Controller = null, Owner = null };
    return ObjectFilterRelations.Subsumes(candidateSelf, typeFilter, _ontology).Value != Trilean.No;
  }

  /// <summary>
  /// Clone a candidate card's ports + card-defined edges under a synthesized copy identity
  /// (<c>"&lt;copier&gt; copy of &lt;C&gt;"</c>), tagging each grafted port with its <see cref="PortNode.Grafter"/>
  /// = the copier (CR 707.2 — the copy is a new object carrying C's copiable abilities). The copier's
  /// <paramref name="mods"/> adjust the cloned ports' type facets — a <c>supertypeRemover</c> strips a
  /// supertype, a <c>typeAdder</c> adds a card-type — but never <em>add</em> an ability C lacks, so they
  /// cannot widen the graft (Decision 1/2 soundness). An <c>abilityAdder</c> (Kiki's haste) is an inert
  /// keyword irrelevant to the untap loop, so it adds no flow port.
  /// </summary>
  private (PortGraph Graph, string CopyId) CloneUnderCopyIdentity(
    PortGraph candidate,
    string candidateCard,
    string copier,
    IReadOnlyList<MagicAST.AST.Effects.TokenCopy.CopyModification>? mods
  )
  {
    var copyId = $"{copier} copy of {candidateCard}";
    var remap = new Dictionary<string, PortNode>(StringComparer.Ordinal);

    PortNode Clone(PortNode p)
    {
      if (remap.TryGetValue(p.Identity, out var existing))
        return existing;
      var subject = ApplyMods(p.Subject, mods);
      var clone = p with
      {
        Card = copyId,
        Identity = $"{copyId}::{p.Label}",
        Subject = subject,
        Grafter = copier,
        CopiedFrom = candidateCard,
      };
      remap[p.Identity] = clone;
      return clone;
    }

    var ports = candidate.Ports.Select(Clone).ToList();
    var edges = candidate
      .CardDefinedEdges.Select(e => new CardDefinedEdge { From = Clone(e.From), To = Clone(e.To) })
      .ToList();
    return (new PortGraph { Ports = ports, CardDefinedEdges = edges }, copyId);
  }

  /// <summary>Apply the copy's modifications to a cloned port's subject (CR 707.2 + the "except" clauses):
  /// strip a removed supertype, add an added card type/subtype. Removals/additions of TYPE never add an
  /// ability, so the graft cannot widen (Decision 1). <c>null</c> subject (a scalar/inert port) is unchanged.</summary>
  private static ObjectFilter? ApplyMods(
    ObjectFilter? subject,
    IReadOnlyList<MagicAST.AST.Effects.TokenCopy.CopyModification>? mods
  )
  {
    if (subject is null || mods is null || mods.Count == 0)
      return subject;
    var result = subject;
    foreach (var mod in mods)
      result = mod switch
      {
        MagicAST.AST.Effects.TokenCopy.SupertypeRemover sr => result with
        {
          ExcludedSupertypes = Concat(result.ExcludedSupertypes, sr.Supertypes),
          Supertypes = result.Supertypes?.Where(s => !sr.Supertypes.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList(),
        },
        MagicAST.AST.Effects.TokenCopy.TypeAdder ta => result with
        {
          CardTypes = Concat(result.CardTypes, ta.CardTypes),
          Subtypes = Concat(result.Subtypes, ta.Subtypes),
        },
        _ => result, // abilityAdder / powerToughnessOverride don't change a port's type identity
      };
    return result;
  }

  private static IReadOnlyList<string>? Concat(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
  {
    if (b is null || b.Count == 0)
      return a;
    var set = new List<string>(a ?? []);
    foreach (var x in b)
      if (!set.Contains(x, StringComparer.OrdinalIgnoreCase))
        set.Add(x);
    return set;
  }

  /// <summary>An untap emit — self (<c>emit:untap:self</c>) or target (<c>emit:untap</c>).</summary>
  private static bool IsUntap(string label) =>
    label == "emit:untap:self" || label == "emit:untap";

  /// <summary>A blink (flicker) emit — <c>emit:blink[:...]</c> (the re-entered permanent's renewal hop).</summary>
  private static bool IsBlink(string label) =>
    label.StartsWith("emit:blink", StringComparison.Ordinal);

  /// <summary>
  /// The blink-arm dual of <see cref="UntapReachesSource"/> — does an inherited blink renew the tap-gated
  /// copier? A blink whose blinked filter admits a creature can pick the creature copier (which then
  /// re-enters UNTAPPED, CR 603.6e/400.7). A self-targeting blink (the copy blinking only ITSELF) renews
  /// the copy, not the copier — excluded. A blink Gated by "you may" (every blink in scope is) yields
  /// AMBER (the optional ETB can't be certified to fire), never GREEN — the soundness-preserving floor.
  /// Returns the closing-edge reliability, or <c>null</c> when the blink can't reach a creature copier.
  /// </summary>
  private static Trilean? BlinkReachesSource(PortNode blink)
  {
    // A blink with no Subject (under-specified) targets any permanent → reaches a creature copier.
    if (blink.Subject is null)
      return blink.Gated ? Trilean.Unknown : Trilean.Yes;
    if (blink.Subject.IsSelf == true)
      return null; // a self-only blink renews the COPY, not the copier
    if (!TargetAdmitsCreature(blink.Subject))
      return null; // "blink target land" can't pick a creature copier — no renewal
    return blink.Gated ? Trilean.Unknown : Trilean.Yes;
  }

  /// <summary>
  /// Decision 4 — does an inherited untap reach the tap-gated copier, and at what reliability? An
  /// <b>unconditional</b> untap (not <see cref="PortNode.Gated"/>) renews the copier when it is either
  /// a SELF-untap (the copy untapping itself doesn't help the copier — excluded) or — the Corridor case —
  /// a TARGET-untap whose target filter admits a creature (the copier is a creature; CR 707.2 makes the
  /// copy's "untap target artifact or creature" able to choose the copier). The target's CardTypes are
  /// read <b>disjunctively</b> ("artifact OR creature" — the card text), so a target naming <c>creature</c>
  /// or <c>permanent</c>, or an unconstrained target, renews; a target naming only <c>land</c> does not
  /// (no false renewal). A <see cref="PortNode.Gated"/> untap (optional / conditional) yields AMBER, not a
  /// renewal — the closing hop is uncertain. Returns the edge reliability (<see cref="Trilean.Yes"/> =
  /// GREEN-eligible), or <c>null</c> when the untap cannot reach the copier at all.
  /// </summary>
  private static Trilean? UntapReachesSource(PortNode untap)
  {
    if (untap.Label == "emit:untap:self")
      return null; // a self-untap renews the COPY, not the copier — needs the inherited-self path (D), not here
    // A bare target-untap (no Subject) targets any permanent → reaches a creature copier (unconstrained).
    if (untap.Subject is null)
      return untap.Gated ? Trilean.Unknown : Trilean.Yes;
    var targetsCreature = TargetAdmitsCreature(untap.Subject);
    if (!targetsCreature)
      return null; // "untap target land" can't choose a creature copier — no renewal
    return untap.Gated ? Trilean.Unknown : Trilean.Yes;
  }

  /// <summary>An untap TARGET filter (its CardTypes read disjunctively as "X or Y", per the card text)
  /// can choose a creature iff it names <c>creature</c> or <c>permanent</c>, or constrains no card type.</summary>
  private static bool TargetAdmitsCreature(ObjectFilter target)
  {
    if (target.CardTypes is not { Count: > 0 } types)
      return true; // unconstrained-by-type target — any permanent, incl. a creature
    return types.Any(t =>
      string.Equals(t, "creature", StringComparison.OrdinalIgnoreCase)
      || string.Equals(t, "permanent", StringComparison.OrdinalIgnoreCase)
    );
  }

  /// <summary>
  /// An untap emit that untaps <b>land(s)</b> — "untap up to five lands" (Peregrine Drake). Those lands
  /// become a free mana source (they can be tapped for mana again, CR 305.4 / 605), which is what feeds
  /// the mana-untap enabler arm (<c>emit:untap</c>(land) → <c>pay:mana</c>). Recognised by the untap
  /// target Subject naming <c>land</c> (or <c>permanent</c>, which admits lands). A self-untap
  /// (<c>emit:untap:self</c>, no Subject) or a creature-only untap is NOT a mana source — excluded, so the
  /// arm never falsely manufactures mana from untapping a non-land. The disjunctive read mirrors
  /// <see cref="TargetAdmitsCreature"/>.
  /// </summary>
  private static bool IsLandUntap(PortNode untap)
  {
    if (!IsUntap(untap.Label) || untap.Label == "emit:untap:self" || untap.Subject is null)
      return false;
    if (untap.Subject.CardTypes is not { Count: > 0 } types)
      return false; // an unconstrained untap could be any permanent, but we won't claim mana without a land
    return types.Any(t =>
      string.Equals(t, "land", StringComparison.OrdinalIgnoreCase)
      || string.Equals(t, "permanent", StringComparison.OrdinalIgnoreCase)
    );
  }

  /// <summary>
  /// The mana-untap enabler edge: an "untap N lands" emit feeds a <c>pay:mana</c> cost (the untapped lands
  /// tap for mana to refund it). Tiered explicitly <b>AMBER</b> (Reliability=Unknown), never GREEN: the
  /// lands' colours and exact count are unknown ("up to five lands" of unstated colour), so the engine
  /// cannot certify they cover a specific coloured pip (CR 107.4) — the honest, soundness-preserving floor
  /// (adding-a-flow-arm anti-pattern 2/3: never fudge a GREEN, never let the scalar null-default certify).
  /// Overlaps (not Disjoint), so the hop participates in a cycle and floors it to AMBER.
  /// </summary>
  private static PortEdge UntapLandsFeedsMana(PortNode untap, PortNode pay) =>
    new()
    {
      From = untap,
      To = pay,
      Provenance = EdgeProvenance.RulesDefined,
      Family = EdgeFamily.Flow,
      Overlap = FilterRelation.Overlaps,
      Reliability = Trilean.Unknown,
      Reason = "untapped lands' colours/count are unknown — can't certify they cover the cost (CR 107.4)",
    };

  /// <summary>Synthesize the closing edge from the inherited untap to the copier's <c>tap:self</c> cost
  /// (Decision 4), tiered by the operator reliability the graft computed (GREEN when the untap is
  /// unconditional and reaches the copier). When the copier has no <c>tap:self</c> port the loop can't
  /// renew — but a copier whose copy is tap-activated always has one (the copy was made by tapping).</summary>
  private PortEdge CloseUntapToTap(
    PortNode untap,
    string copier,
    IReadOnlyDictionary<string, PortGraph> graphByCard,
    Trilean reliability
  )
  {
    var tap =
      (graphByCard.TryGetValue(copier, out var g) ? g.Ports : [])
        .FirstOrDefault(p => p.Side == PortSide.Consume && p.Label == "tap:self")
      ?? Port(copier, "tap:self", PortSide.Consume);
    return new PortEdge
    {
      From = untap,
      To = tap,
      Provenance = EdgeProvenance.RulesDefined,
      Family = EdgeFamily.Flow,
      Overlap = FilterRelation.Overlaps,
      Reliability = reliability,
      Reason = reliability == Trilean.Yes ? null : "inherited untap is optional/conditional",
    };
  }

  /// <summary>A copier-side <c>tap:self</c> for the closing edge when the copier graph lacks one (defensive).</summary>
  private static PortNode Port(string card, string label, PortSide side) =>
    new()
    {
      Card = card,
      Label = label,
      Side = side,
      Identity = $"{card}::{label}",
    };

  /// <summary>The minimal derived flow grammar (§6) the gold needs: a created token refuels a sac; mana refunds a mana cost.</summary>
  private bool FlowFeasible(PortNode emit, PortNode consume) =>
    (ResourceKind(emit.Label), Role(consume.Label)) switch
    {
      ("token", "sac") => TokenSatisfiesAtCreation(emit, consume),
      ("mana", "pay") => ResourceKind(consume.Label) == "mana" // mana refunds a mana cost…
        && ManaColorFeeds(ManaColor(emit.Label), ManaColor(consume.Label)), // …of a colour it can pay
      ("life", "trigger") => LifeFlowFeasible(emit, consume), // a life event feeds a same-direction life trigger (CR 119)
      // Die rolls (CR 706.2). A "roll [N] dice" emit refuels a "whenever you roll one or more dice"
      // trigger, so a self-feeding roll engine closes (Brazen Dwarf, Mr. House). Feasibility only —
      // AddRulesEdge's operator tiers certainty on the player Subjects (You↔You → GREEN; a result
      // threshold on the trigger floors firability via §8, not the arm).
      ("rolldice", "trigger") => ResourceKind(consume.Label) == "rolldice",
      // Cast-recursion (Displacer Kitten family). A RE-CAST spell (emit:cast — a noncreature permanent that
      // bounced itself to hand and is cast again, CR 601) feeds a "whenever you cast a [noncreature] spell"
      // trigger (CR 603.2) whose watched-spell filter is type-compatible. Feasibility only — AddRulesEdge's
      // operator tiers the certainty on the Subjects (a bare "a spell" recast vs a "NONcreature spell"
      // trigger → Intersects-Overlaps but not Subsumes → AMBER), and the recast's pay:mana co-cost floors
      // the loop via §8 when the loop can't refill it. A copy of a spell (emit:copy:spell) is deliberately
      // NOT this arm — CR 707.10 makes a copy uncast, so it never feeds a cast trigger (SpellCopyEmit docs).
      ("cast", "trigger") => CastSatisfiesTrigger(emit, consume),
      // Blink (CR 603.6e/400.7). A blinked permanent re-enters as a NEW object, so its ETB retriggers:
      // emit:blink refuels an Enters-trigger whose entering filter is type-compatible with the blinked
      // permanent (Felidar blinks Resto → Resto's ETB fires again). Feasibility only — AddRulesEdge's
      // operator tiers the certainty on the Subjects (a blink of "a permanent" vs "this creature" enters
      // → Intersects-Overlaps but not Subsumes → AMBER; the "you may" Gated floors it too).
      ("blink", "etb") => BlinkSatisfiesEnter(emit, consume),
      // Aristocrat recursion (aristocrat-recursion-scope.md, Decision 2b). A creature re-entering the
      // battlefield via a cast-from-graveyard permission (emit:returntobattlefield:self) refuels a sac
      // whose fodder Subsumes it (the structural twin of (token, sac)), and feeds an ETB-trigger payoff
      // (Essence Warden / Suture Priest). Feasibility only — AddRulesEdge's operator tiers the certainty.
      ("returntobattlefield", "sac") => RecastSatisfies(emit, consume),
      ("returntobattlefield", "etb") => RecastSatisfies(emit, consume),
      // Spell-recursion → recast (CR 601.2). An instant/sorcery returned to hand
      // (emit:returntohand:spell, Archaeomancer/Izzet Chronarch's ETB returning Ghostly Flicker) can be
      // recast, re-firing its effects: it refuels the spell's cast:spell consume, re-driving the spell's
      // blink/effect emits. Feasibility only — AddRulesEdge's operator tiers GREEN vs AMBER on the
      // Subjects (the returned {instant,sorcery} graveyard filter vs the cast spell's {instant,sorcery}
      // self-type). A returned filter type-incompatible with the cast spell is pruned by the operator.
      ("returntohand", "cast") => SpellRecursionSatisfiesCast(emit, consume),
      _ => false,
    };

  /// <summary>
  /// A spell-recursion emit (emit:returntohand:spell, Subject = the returned instant/sorcery filter)
  /// satisfies a cast:spell consume (Subject = the spell's {instant,sorcery} self-type) when the two are
  /// type-compatible — the returned card COULD be the spell being recast (Archaeomancer returns "an instant
  /// or sorcery"; Ghostly Flicker IS an instant). The structural twin of <see cref="RecastSatisfies"/>:
  /// feasibility only (Intersects ≠ Disjoint); <see cref="AddRulesEdge"/>'s operator sets the tier on the
  /// Subjects. A returned filter that provably can't be the cast spell's type is pruned by the operator's
  /// Disjoint.
  /// </summary>
  private bool SpellRecursionSatisfiesCast(PortNode emit, PortNode consume)
  {
    if (emit.Subject is null || consume.Subject is null)
      return true;
    return ObjectFilterRelations.Intersects(emit.Subject, consume.Subject, _ontology).Relation
      != FilterRelation.Disjoint;
  }

  /// <summary>
  /// A re-entered creature (emit:returntobattlefield:self) satisfies a consume whose fodder/entering
  /// filter <b>Subsumes</b> it — a re-cast Gravecrawler (a creature) satisfies "Sacrifice a creature"
  /// and "another creature you control enters". The recast emit Subject is the card's self-filter
  /// (CardTypes:[creature], IsSelf); the consume Subject is the sac/etb fodder. The actual certainty
  /// (GREEN vs AMBER) is set by <see cref="AddRulesEdge"/>'s Intersects/Subsumes on those Subjects, NOT
  /// here — this is the feasibility gate only (the structural twin of <see cref="TokenSatisfiesAtCreation"/>).
  /// A consume scoped to a DIFFERENT specific object (a non-overlapping type) is pruned by the operator.
  /// </summary>
  private bool RecastSatisfies(PortNode emit, PortNode consume)
  {
    if (emit.Subject is null || consume.Subject is null)
      return true;
    // A sac/etb that requires a specific OTHER object (e.g. an artifact-Treasure sac) can't be the
    // re-entered creature; the operator prunes a Disjoint type in AddRulesEdge. Here we only confirm the
    // re-entered creature could BE the consume's target by type-compatibility (Subsumes/Intersects ≠ No).
    return ObjectFilterRelations.Intersects(emit.Subject, consume.Subject, _ontology).Relation
      != FilterRelation.Disjoint;
  }

  /// <summary>
  /// A blinked permanent (emit:blink, Subject = the blinked filter) re-enters the battlefield as a NEW
  /// object (CR 603.6e/400.7), so its Enters-trigger re-fires. Feasible iff the blinked filter is
  /// type-compatible with the etb consume's entering filter (the blink could pick the permanent whose ETB
  /// this is) — the structural twin of <see cref="TokenSatisfiesAtCreation"/>. Feasibility only:
  /// <see cref="AddRulesEdge"/>'s Intersects/Subsumes on the Subjects sets GREEN vs AMBER (a blink of "a
  /// permanent" only Intersects "this creature" enters ⇒ AMBER; a self-targeting blink could Subsume).
  /// A consume scoped to a provably different type is pruned by the operator's Disjoint.
  /// </summary>
  private bool BlinkSatisfiesEnter(PortNode emit, PortNode consume)
  {
    if (emit.Subject is null || consume.Subject is null)
      return true;
    // A blink on a card never refuels that SAME card's "this creature enters" self-trigger
    // (Subject.IsSelf): a self-only ETB fires only for THE source object, and the source can't legally be
    // the target of its own blink (Felidar/Displacer "exile ANOTHER permanent", CR 109.5; Restoration's
    // "non-Angel" excludes Resto-the-Angel). The operator's Intersects deliberately ignores the
    // ExcludeSelf/IsSelf interplay (a runtime question) and an exclusion stated as a subtype (!Angel)
    // against a bare self-ETB can't be witnessed — but feeding the blinker's OWN self-ETB is a structural
    // self-blink, which the engine can't certify, so prune it (the dual of TokenSatisfiesAtCreation's
    // :self guard; a false negative only on the rare genuine self-blinker, never a false positive). A
    // blink of another permanent still refuels a DIFFERENT card's self-ETB (Felidar blinks Resto) — the
    // real arm, and the inter-card hop the combos turn on.
    if (consume.Subject.IsSelf == true && string.Equals(emit.Card, consume.Card, StringComparison.Ordinal))
      return false;
    return ObjectFilterRelations.Intersects(emit.Subject, consume.Subject, _ontology).Relation
      != FilterRelation.Disjoint;
  }

  /// <summary>
  /// A re-cast spell (<c>emit:cast</c>) feeds a spell-cast trigger (<c>trigger:cast</c>, CR 603.2) iff the
  /// consume is genuinely a CAST trigger (resource kind <c>cast</c> — NOT a same-role life/other trigger
  /// the tuple <c>("cast","trigger")</c> could otherwise reach) and the recast spell's filter is
  /// type-compatible with the watched-spell filter (Intersects ≠ Disjoint). Feasibility only — the operator
  /// tiers GREEN vs AMBER on the Subjects (a bare "a spell" recast against a "NONcreature spell" trigger is
  /// Overlaps-but-not-Subsumes → AMBER), and the recast's <c>pay:mana</c> co-cost floors the loop via §8.
  /// </summary>
  private bool CastSatisfiesTrigger(PortNode emit, PortNode consume)
  {
    if (ResourceKind(consume.Label) != "cast")
      return false; // a non-cast trigger of the same role token — not this arm
    if (emit.Subject is null || consume.Subject is null)
      return true;
    return ObjectFilterRelations.Intersects(emit.Subject, consume.Subject, _ontology).Relation
      != FilterRelation.Disjoint;
  }

  /// <summary>A life-gain/loss event refuels a life trigger of the SAME direction (gain↔gain, loss↔loss,
  /// CR 119). Direction-match only — the player-scope overlap (who gains/loses vs whom the trigger
  /// watches) is the operator's job via the port Subjects (ADR-0002 §7: the label names, the operator
  /// decides), so "you gain → whenever you gain" certifies GREEN while "a player loses → whenever an
  /// opponent loses" is a sound AMBER.</summary>
  private static bool LifeFlowFeasible(PortNode emit, PortNode consume) =>
    LifeDirection(emit.Label) is { } dir && dir == LifeDirection(consume.Label);

  /// <summary>The gain/loss facet of a <c>emit:life:&lt;dir&gt;</c> / <c>trigger:life:&lt;dir&gt;</c> label.</summary>
  private static string? LifeDirection(string label)
  {
    var parts = label.Split(':');
    return parts.Length >= 3 && parts[1] == "life" ? parts[2] : null;
  }

  /// <summary>The colour facet of a mana label (<c>emit:mana:&lt;colour&gt;</c> / <c>pay:mana:&lt;colour&gt;</c>); <c>null</c> for a generic <c>pay:mana</c>.</summary>
  private static string? ManaColor(string label)
  {
    var parts = label.Split(':');
    return parts.Length >= 3 ? parts[2] : null;
  }

  /// <summary>
  /// Does emitted mana of one colour satisfy a mana cost's colour? <c>any</c> satisfies anything (the
  /// producer picks the colour — a Treasure makes any one colour, so it pays <c>{B}</c>, GREEN by
  /// producer choice, ADR-0002 §3b†); a generic <c>{N}</c> cost takes any colour; otherwise the colours
  /// must match — so <c>emit:mana:green</c> does NOT pay <c>pay:mana:black</c> (no false-GREEN).
  /// </summary>
  private static bool ManaColorFeeds(string? emitColor, string? payColor) =>
    string.Equals(emitColor, "any", StringComparison.OrdinalIgnoreCase)
    || payColor is null
    || string.Equals(emitColor, payColor, StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// A created token satisfies a consume's type requirement (a <c>sac</c> cost, or — via the bridge —
  /// a <c>dies</c>-trigger) only if its <b>at-creation</b> type already carries the consume's required
  /// card types. A create-token effect fully specifies the token's type (CR 111.10 — a Treasure is a
  /// non-creature artifact), so the emit's <c>CardTypes</c> are <b>exact</b>: a Treasure cannot feed
  /// "sacrifice a creature" (nor satisfy "a creature dies"), and a creature token cannot feed "sacrifice
  /// an artifact". An
  /// intervening <em>animation</em> that later adds <c>creature</c> is an external enabler outside the
  /// reconstructed loop — a deliberate modeling boundary, <b>not</b> a claim the types are
  /// <see cref="FilterRelation.Disjoint"/> (the <see cref="ObjectFilterRelations"/> operator stays a
  /// pure type relation; a crewed Vehicle genuinely IS a creature). This guard lives at the engine as a
  /// loop-reconstruction policy, accepting that animation false-negative. (MAST judge panel 2026-06-04:
  /// operator subtype→cardtype exclusivity is UNSOUND — Vehicles CR 301.7, Equipment CR 301.5c — so the
  /// fix belongs here, not in the operator.) Removes the corpus's creature-token ↔ artifact-sac junk.
  /// </summary>
  private bool TokenSatisfiesAtCreation(PortNode emit, PortNode consume)
  {
    if (emit.Subject is null || consume.Subject is null)
      return true;
    // A created token can never satisfy a :self consume ("Sacrifice this") — the token is a different
    // object, not the source permanent (CR 400.7; the dual of the §8 one-shot self-death). So a
    // self-sacrificing producer is never refuelled by the tokens a loop makes — its self-sac is
    // consumed once. (A type-based "sacrifice a Treasure", IsSelf null, stays refuellable.)
    if (consume.Subject.IsSelf == true)
      return false;
    // The token's at-creation card types: created-token filters often carry their type as a SUBTYPE
    // with CardTypes null (the card-type in the label is a display-time lift), so lift through the
    // ontology — a Treasure ⇒ artifact, a Saproling ⇒ creature (CR 205.3, a subtype rides its type).
    var tokenTypes = EffectiveCardTypes(emit.Subject);
    if (tokenTypes.Count == 0)
      return true; // under-specified token — no basis to prune

    // Every explicitly-required card type must be present (CR 110.4a — "permanent" is any permanent type).
    foreach (var t in consume.Subject.CardTypes ?? [])
      if (!TokenHasCardType(tokenTypes, t))
        return false;
    // Every required subtype must be bearable: the token must have one of the subtype's (non-kindred)
    // owner card types, else it cannot be that subtype at creation (a Saproling is not a Treasure).
    foreach (var s in consume.Subject.Subtypes ?? [])
    {
      var owners = PrimaryOwners(s);
      if (owners.Count > 0 && !owners.Any(o => tokenTypes.Contains(o, StringComparer.OrdinalIgnoreCase)))
        return false;
    }
    return true;
  }

  /// <summary>The token's at-creation card types: its explicit CardTypes plus the (non-kindred) owner
  /// card types of each of its subtypes (a Treasure ⇒ artifact; a Squirrel ⇒ creature). CR 205.3.</summary>
  private HashSet<string> EffectiveCardTypes(ObjectFilter f)
  {
    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var t in f.CardTypes ?? [])
      set.Add(t);
    foreach (var s in f.Subtypes ?? [])
      foreach (var o in PrimaryOwners(s))
        set.Add(o);
    return set;
  }

  /// <summary>A subtype's ordinary (non-kindred) owner card types, via the ontology (CR 308.1: kindred is partition-neutral). Empty for an unknown subtype — no basis to prune.</summary>
  private List<string> PrimaryOwners(string subtype)
  {
    foreach (var kv in _ontology.SubtypeToCardTypes)
      if (string.Equals(kv.Key, subtype, StringComparison.OrdinalIgnoreCase))
        return
        [
          .. kv.Value.Where(o => !string.Equals(o, "kindred", StringComparison.OrdinalIgnoreCase)),
        ];
    return [];
  }

  /// <summary>Does the created token's effective card-type set carry the required type? "permanent" (CR 110.4a) is satisfied by any permanent type.</summary>
  private bool TokenHasCardType(HashSet<string> tokenTypes, string required) =>
    string.Equals(required, "permanent", StringComparison.OrdinalIgnoreCase)
      ? tokenTypes.Any(t => _ontology.PermanentTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
      : tokenTypes.Contains(required, StringComparer.OrdinalIgnoreCase);

  private void AddRulesEdge(List<PortEdge> edges, PortNode from, PortNode to, EdgeFamily family)
  {
    if (ReferenceEquals(from, to))
      return;

    FilterMatch overlap;
    SubsumeMatch reliability;
    if (from.Subject is null || to.Subject is null)
    {
      // Scalar resource (mana): kind-match alone, no ObjectFilter overlap.
      overlap = new FilterMatch(FilterRelation.Overlaps);
      reliability = new SubsumeMatch(Trilean.Yes);
    }
    else
    {
      overlap = ObjectFilterRelations.Intersects(from.Subject, to.Subject, _ontology);
      reliability = ObjectFilterRelations.Subsumes(from.Subject, to.Subject, _ontology);
    }

    // Cross-card ExcludeSelf carve-out (Slice 6). An "other"/"another" self-exclusion
    // (sup.ExcludeSelf=true) only omits the sup's OWN source object. When `from` and `to` are
    // DIFFERENT cards, the `from` object can never BE that excluded self, so the exclusion imposes
    // no real constraint — promote the lone-ExcludeSelf Unknown to Yes. Same-card stays Unknown (the
    // `from` object could be the excluded self). Surgical: Subsumes returns Reason=="ExcludeSelf" only
    // when every other axis already subsumed, so this fires for nothing else. The operator-tier twin
    // of the same-card guard at BlinkSatisfiesEnter.
    if (
      reliability.Value == Trilean.Unknown
      && reliability.Reason == "ExcludeSelf"
      && !string.Equals(from.Card, to.Card, StringComparison.Ordinal)
    )
    {
      reliability = new SubsumeMatch(Trilean.Yes);
    }

    if (overlap.Relation == FilterRelation.Disjoint)
      return; // the prune — no edge

    edges.Add(
      new PortEdge
      {
        From = from,
        To = to,
        Provenance = EdgeProvenance.RulesDefined,
        Family = family,
        Overlap = overlap.Relation,
        Reliability = reliability.Value,
        Reason = overlap.Relation == FilterRelation.Unknown ? overlap.Reason : reliability.Reason,
      }
    );
  }

  /// <summary>
  /// Elementary cycles over the materialised graph (each rooted at its lowest-identity port, surfaced
  /// once). A cycle is a candidate loop; its tier is the worst hop. (Same discipline as
  /// the original engine's <c>FindCycles</c>, over the new edge type.)
  /// <para><b>The per-instance reference.</b> This enumerates over the per-card <c>Identity</c> graph
  /// (one node per port instance). The two-layer engine (<see cref="FindCyclesByLabelGraph"/>) runs the
  /// expensive enumeration over the distinct-<em>label</em> graph instead and instantiates per candidate
  /// shape; it is gated to produce a <b>byte-identical</b> result to this method (the equivalence test).
  /// This stays as the reference implementation per the two-layer design (next-steps §3 of
  /// <c>docs/two-layer-cycle-engine.md</c>) — the ADR's retire/keep decision is deferred to humans.</para>
  /// </summary>
  public IReadOnlyList<PortCycle> FindCycles(
    IReadOnlyList<PortEdge> edges,
    int maxLength = int.MaxValue
  ) => EnumerateInstanceCycles(edges, edges, maxLength);

  /// <summary>
  /// The shared per-instance elementary-cycle DFS (ADR-0002 §8). Enumerates over the
  /// <paramref name="searchEdges"/> adjacency — the admissible edge set the caller hands it — while every
  /// §8 floor/prune is computed against the FULL <paramref name="allEdges"/> set (co-costs, producers,
  /// renewals can sit off the ring). The reference <see cref="FindCycles"/> passes <c>searchEdges ==
  /// allEdges</c>; the two-layer engine passes the (provably-complete) subset of instance edges that lie on
  /// a candidate label-cycle. Because the label graph is a sound over-approximation (every real instance
  /// cycle projects to a closed label walk, so each of its instance edges' label-hop appears in some
  /// candidate shape), restricting the search to that subset drops <b>no</b> instance cycle — the
  /// equivalence guarantee. Each cycle is rooted at its lowest-identity node and surfaced once.
  /// </summary>
  private List<PortCycle> EnumerateInstanceCycles(
    IReadOnlyList<PortEdge> searchEdges,
    IReadOnlyList<PortEdge> allEdges,
    int maxLength
  )
  {
    var adjacency = searchEdges
      .GroupBy(e => e.From.Identity)
      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

    // §8 conjunction input: each cost's co-costs. Computed over the FULL edge set (a co-cost feeder may
    // live off the ring); the search-edge restriction only narrows enumeration, never the §8 evidence.
    var coCosts = CoCostMap(allEdges);

    var cycles = new List<PortCycle>();
    var path = new List<PortEdge>();
    var onPath = new HashSet<string>(StringComparer.Ordinal);

    // Progress heartbeat for the whole-corpus UNION enumeration (exponential, historically a silent
    // multi-hour stall — see cycle-enumeration-acceleration.md). Gated on graph size so the per-combo
    // bench (tiny 2-3 card graphs, < the threshold) is silent and unaffected. Emitted on stderr so it
    // never pollutes the flow's stdout data stream.
    var bigGraph = adjacency.Count > 1000;
    var sw = bigGraph ? System.Diagnostics.Stopwatch.StartNew() : null;
    long steps = 0;
    long nextStepLog = 5_000_000;
    string currentStart = "";

    // Per-start-node SAFETY BUDGET (union only). A single alphabetically-early high-degree node (a
    // broad token/mana emitter) defeats the canonical-rooting prune — almost every node sorts after it —
    // so its bounded DFS can still explore tens of millions of paths and dominate the whole pass. When one
    // node exceeds PerNodeStepBudget it is abandoned (and logged), so no single node can stall the run.
    // This makes the union pass sound-but-incomplete (it may drop cycles rooted at an abandoned node — a
    // lower-bounding count, never a false cycle). Disabled for the per-combo bench (sw is null).
    const long PerNodeStepBudget = 20_000_000;
    long nodeSteps = 0;
    var nodeAborted = false;
    var truncatedNodes = 0;

    void Dfs(string nodeId, string startId)
    {
      if (nodeAborted || !adjacency.TryGetValue(nodeId, out var outgoing))
        return;
      foreach (var edge in outgoing)
      {
        if (sw is not null)
        {
          if (++steps >= nextStepLog)
          {
            nextStepLog += 5_000_000;
            Console.Error.WriteLine(
              $"[FindCycles] …{steps / 1_000_000}M edge-steps, {cycles.Count} cycles, "
                + $"{sw.Elapsed.TotalSeconds:F0}s (at start node {currentStart})"
            );
          }
          if (++nodeSteps >= PerNodeStepBudget)
          {
            nodeAborted = true;
            truncatedNodes++;
            Console.Error.WriteLine(
              $"[FindCycles] BUDGET: abandoned start node {currentStart} at "
                + $"{nodeSteps / 1_000_000}M node-steps ({cycles.Count} cycles so far)"
            );
            return;
          }
        }
        var toId = edge.To.Identity;
        if (toId == startId)
        {
          path.Add(edge);
          var loop = path.ToList();
          if (
            !IsOneShotSelfRemoval(loop, allEdges) // §8 "B": prune the structurally non-repeatable
            && !BridgeFedByIncompatibleToken(loop) // §8: the loop's token can't satisfy the dies-trigger
            && !CounterGateUnsatisfiable(loop, allEdges) // §8: a "had a counter" gate the loop can't re-satisfy
          )
            cycles.Add(
              new PortCycle
              {
                Edges = loop,
                CoCostsSatisfied = ConjunctionHolds(loop, coCosts, allEdges),
                Balanced = ManaBalanced(loop, coCosts, allEdges),
                Productive = ManaProductive(loop, coCosts, allEdges),
                TapRenewed = TapGatesRenewed(loop, allEdges),
              }
            );
          path.RemoveAt(path.Count - 1);
        }
        else if (
          path.Count < maxLength - 1 // leave room for the closing edge (≤ maxLength edges per cycle)
          && string.CompareOrdinal(toId, startId) > 0
          && !onPath.Contains(toId)
        )
        {
          path.Add(edge);
          onPath.Add(toId);
          Dfs(toId, startId);
          onPath.Remove(toId);
          path.RemoveAt(path.Count - 1);
        }
      }
    }

    var startNodes = adjacency.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    if (sw is not null)
      Console.Error.WriteLine(
        $"[FindCycles] enumerating cycles ≤{maxLength} over {startNodes.Count} start nodes / "
          + $"{searchEdges.Count} edges (heartbeat every 5M edge-steps)…"
      );
    var startsDone = 0;
    foreach (var start in startNodes)
    {
      currentStart = start;
      nodeSteps = 0;
      nodeAborted = false;
      onPath.Clear();
      onPath.Add(start);
      path.Clear();
      Dfs(start, start);
      // Per-start-node heartbeat too, so a single high-degree node that blows up the DFS still shows
      // forward motion between its intra-DFS step-logs (and a stall pins the exact culprit node).
      if (sw is not null && ++startsDone % 1000 == 0)
        Console.Error.WriteLine(
          $"[FindCycles] {startsDone}/{startNodes.Count} start nodes done, "
            + $"{cycles.Count} cycles, {steps / 1_000_000}M steps, {sw.Elapsed.TotalSeconds:F0}s"
        );
    }
    if (sw is not null)
      Console.Error.WriteLine(
        $"[FindCycles] done: {cycles.Count} cycles, {steps / 1_000_000}M edge-steps, "
          + $"{sw.Elapsed.TotalSeconds:F0}s"
          + (truncatedNodes > 0 ? $" — {truncatedNodes} start node(s) hit the step budget (count is a lower bound)" : "")
      );
    return cycles;
  }

  /// <summary>
  /// The <b>two-layer</b> cycle engine (<c>docs/two-layer-cycle-engine.md</c>). The expensive elementary-
  /// cycle enumeration runs over the DISTINCT cycle-relevant LABEL graph (Layer 1 — the bounded "atom"
  /// set, ~hundreds of nodes, not the ~100k port instances), yielding candidate interaction SHAPES. Then
  /// an instantiate-and-tier pass (Layer 2) materialises the concrete ports realising each candidate shape
  /// and runs the operator + §8 to assign each cycle its tier — the precise, instance-dependent truth.
  ///
  /// <para><b>Layer 1 (candidate shapes).</b> Build adjacency over distinct labels (an edge <c>A→B</c>
  /// exists iff some <see cref="PortEdge"/> goes from a label-A port to a label-B port — the kind/role
  /// "could bond" relation), enumerate its elementary cycles. The label graph is bounded by the grammar,
  /// so a <b>generous (unbounded) length</b> is used here — the cards-based bound is a display filter
  /// (<paramref name="displayMaxLengthInCards"/>), applied to the instance cycles after Layer 2.</para>
  ///
  /// <para><b>Layer 2 (instantiate + tier).</b> Collect the admissible instance edges — those whose
  /// <c>(From.Label → To.Label)</c> hop appears in <em>any</em> candidate label-cycle — and run the SAME
  /// per-instance DFS (<see cref="EnumerateInstanceCycles"/>) over them. This is what makes the result
  /// byte-identical to <see cref="FindCycles"/>: every real instance cycle projects to a closed label walk
  /// covered by Layer 1, so its edges are all admissible, so the restricted DFS finds it; conversely no new
  /// cycle appears because the DFS is the identical elementary-cycle finder. The display bound is applied
  /// as a post-filter on the instance cycle's <b>distinct card count</b> (a cards-based filter, the design's
  /// demotion of the old hop-length bound).</para>
  /// </summary>
  public IReadOnlyList<PortCycle> FindCyclesByLabelGraph(
    IReadOnlyList<PortEdge> edges,
    int displayMaxLengthInCards = int.MaxValue,
    int maxLabelCycleLength = int.MaxValue,
    int maxInstanceHops = int.MaxValue
  )
  {
    // Layer 1: candidate label-cycle shapes over the distinct-label graph. UNBOUNDED by default (exact, the
    // per-combo path). The whole-corpus UNION caller passes maxLabelCycleLength because the union label
    // graph is dense enough that unbounded elementary-cycle enumeration over it does not terminate in
    // practice (the blowup the two-layer split moved here from the instance layer) — there, the bound makes
    // it a tractable, observable, SOUND-BUT-INCOMPLETE approximation (it can only DROP candidate hops, never
    // invent them, so Layer 2 still never reports a false cycle; it may miss cycles whose only label-shape
    // is longer than the bound).
    var shapeHops = LabelCycleHops(edges, maxLabelCycleLength);

    // Layer 2a: the admissible instance edges — every edge whose label-hop participates in a candidate
    // shape. A sound over-approximation of "edges that can lie on a real instance cycle" (the label graph
    // is itself a sound over-approximation, two-layer-cycle-engine.md "Layer 1"), so restricting the
    // instance DFS to these drops no instance cycle.
    var admissible = edges.Where(e => shapeHops.Contains((e.From.Label, e.To.Label))).ToList();

    // Layer 2b: instantiate + tier — the identical per-instance enumerator over the admissible subset,
    // with every §8 floor still evidenced against the FULL edge set. maxInstanceHops bounds the per-instance
    // DFS for the union caller (default unbounded = exact).
    var cycles = EnumerateInstanceCycles(admissible, edges, maxInstanceHops);

    // Length-bound is now a cards-based DISPLAY filter (two-layer-cycle-engine.md §Complexity): keep only
    // cycles spanning ≤ K distinct cards. Default int.MaxValue = no filter (the full enumeration).
    if (displayMaxLengthInCards == int.MaxValue)
      return cycles;
    return cycles
      .Where(c =>
        c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
          .Distinct(StringComparer.Ordinal)
          .Count() <= displayMaxLengthInCards
      )
      .ToList();
  }

  /// <summary>
  /// Layer 1 of the two-layer engine — enumerate elementary cycles over the DISTINCT-label graph and
  /// return the set of label HOPS <c>(fromLabel, toLabel)</c> that appear on any candidate cycle. The
  /// label graph groups the per-instance edges by their endpoints' <see cref="PortNode.Label"/> (the
  /// "atoms", two-layer-cycle-engine.md): nodes = distinct labels, an edge <c>A→B</c> iff some instance
  /// edge runs A→B. Bounded by the grammar (hundreds of nodes), so the enumeration runs with no length
  /// cap. A self-loop label (A→A, a same-label hop) is its own one-node cycle and contributes the hop
  /// <c>(A,A)</c>. The returned hop-set is the admissibility gate Layer 2 filters instance edges by.
  /// </summary>
  private static HashSet<(string From, string To)> LabelCycleHops(
    IReadOnlyList<PortEdge> edges,
    int maxLen = int.MaxValue
  )
  {
    // Distinct-label adjacency (deduped — the label graph is the atom graph, one node per label).
    var labelAdj = edges
      .GroupBy(e => e.From.Label, StringComparer.Ordinal)
      .ToDictionary(
        g => g.Key,
        g => g.Select(e => e.To.Label).Distinct(StringComparer.Ordinal).OrderBy(l => l, StringComparer.Ordinal).ToList(),
        StringComparer.Ordinal
      );

    var hops = new HashSet<(string, string)>();

    // Self-loop labels (A→A) are one-node cycles; record their hop directly.
    foreach (var e in edges)
      if (string.Equals(e.From.Label, e.To.Label, StringComparison.Ordinal))
        hops.Add((e.From.Label, e.To.Label));

    // Elementary cycles over the label graph, rooted at each label's lowest-ordinal node (the same
    // canonical-rooting discipline as the instance DFS). UNBOUNDED by default (the per-combo path, exact);
    // the whole-corpus UNION caller passes a maxLen because the union label graph is DENSE (hub resources
    // like death/returntobattlefield/mana interconnect heavily) and unbounded elementary-cycle enumeration
    // over it is itself intractable — the blowup the two-layer split moved here from the instance layer.
    var path = new List<string>();
    var onPath = new HashSet<string>(StringComparer.Ordinal);
    var bigGraph = labelAdj.Count > 200;
    var sw = bigGraph ? System.Diagnostics.Stopwatch.StartNew() : null;

    void Dfs(string node, string start)
    {
      if (!labelAdj.TryGetValue(node, out var outs))
        return;
      foreach (var to in outs)
      {
        if (string.Equals(to, start, StringComparison.Ordinal))
        {
          // Closed an elementary label-cycle: record every hop on the ring (path = [start, …]; the
          // closing hop is the last node back to start).
          for (var i = 0; i < path.Count; i++)
            hops.Add((path[i], i + 1 < path.Count ? path[i + 1] : start));
        }
        else if (path.Count < maxLen && string.CompareOrdinal(to, start) > 0 && !onPath.Contains(to))
        {
          path.Add(to);
          onPath.Add(to);
          Dfs(to, start);
          onPath.Remove(to);
          path.RemoveAt(path.Count - 1);
        }
      }
    }

    var starts = labelAdj.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    if (sw is not null)
      Console.Error.WriteLine(
        $"[LabelCycleHops] enumerating label cycles ≤{maxLen} over {starts.Count} label nodes…"
      );
    var done = 0;
    foreach (var start in starts)
    {
      onPath.Clear();
      onPath.Add(start);
      path.Clear();
      path.Add(start);
      Dfs(start, start);
      if (sw is not null && ++done % 50 == 0)
        Console.Error.WriteLine(
          $"[LabelCycleHops] {done}/{starts.Count} label nodes, {hops.Count} hops, {sw.Elapsed.TotalSeconds:F0}s"
        );
    }
    if (sw is not null)
      Console.Error.WriteLine(
        $"[LabelCycleHops] done: {hops.Count} hops over {starts.Count} label nodes, {sw.Elapsed.TotalSeconds:F0}s"
      );
    return hops;
  }

  /// <summary>
  /// Co-cost siblings (§8): two consume ports are co-costs of one ability iff each has a card-defined
  /// edge to a common effect (within an ability every cost drives every effect, §5). Maps a cost's
  /// Identity → its sibling cost ports.
  /// </summary>
  private static IReadOnlyDictionary<string, IReadOnlyList<PortNode>> CoCostMap(
    IReadOnlyList<PortEdge> edges
  )
  {
    var costsByEffect = edges
      .Where(e => e.Provenance == EdgeProvenance.CardDefined && e.From.Side == PortSide.Consume)
      .ToLookup(e => e.To.Identity, e => e.From);

    var map = new Dictionary<string, Dictionary<string, PortNode>>(StringComparer.Ordinal);
    foreach (var group in costsByEffect)
      foreach (var a in group)
        foreach (var b in group)
          if (!string.Equals(a.Identity, b.Identity, StringComparison.Ordinal))
          {
            if (!map.TryGetValue(a.Identity, out var siblings))
              map[a.Identity] = siblings = new Dictionary<string, PortNode>(StringComparer.Ordinal);
            siblings[b.Identity] = b;
          }

    return map.ToDictionary(
      kv => kv.Key,
      kv => (IReadOnlyList<PortNode>)kv.Value.Values.ToList(),
      StringComparer.Ordinal
    );
  }

  /// <summary>
  /// The §8 multi-cost conjunction: for every consume port the cycle traverses, each of its co-costs
  /// must be payable <b>each iteration</b> — already in the loop, a free cost (tap), or <b>fed by the
  /// loop itself</b> (a producer <see cref="ReachableWithinLoop">reachable from the loop's flow</see>
  /// targets it). The "fed by the loop" tightening (phase 10): a co-cost is NOT satisfied merely because
  /// some producer exists in the corpus, nor merely because a producer sits on a cycle card — it must be
  /// fed by a producer the loop actually <em>drives</em>. This separates Chatterfang × Pitiless (its
  /// <c>{B}</c> is fed by the very Treasure the loop produces — reached via the loop's own
  /// emit:token → sac:treasure → emit:mana, §9) from the Ruthless Knave family (its "Sacrifice a creature"
  /// co-cost is fed only by an unrelated creature-maker the loop never drives — the loop makes only
  /// Treasures, so the ability can't fire each iteration → not certifiable).
  /// </summary>
  private static bool ConjunctionHolds(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyDictionary<string, IReadOnlyList<PortNode>> coCosts,
    IReadOnlyList<PortEdge> edges
  )
  {
    var inCycle = cycle
      .SelectMany(e => new[] { e.From.Identity, e.To.Identity })
      .ToHashSet(StringComparer.Ordinal);
    var reachable = ReachableWithinLoop(cycle, edges);

    // A co-cost is fed by the loop iff a producer the loop drives (in the reachable closure) targets it.
    bool LoopFeeds(PortNode s) =>
      edges.Any(e =>
        string.Equals(e.To.Identity, s.Identity, StringComparison.Ordinal)
        && reachable.Contains(e.From.Identity)
      );

    foreach (var id in inCycle)
      if (coCosts.TryGetValue(id, out var siblings))
        foreach (var s in siblings)
          if (!IsFreeCost(s.Label) && !inCycle.Contains(s.Identity) && !LoopFeeds(s))
            return false;
    return true;
  }

  /// <summary>
  /// The ports the loop's own flow <b>drives</b> (ADR-0002 §8): the forward closure from the cycle's
  /// ports, following only edges whose <b>both</b> endpoints are on cycle cards. Tighter than cycle-card
  /// membership — it reaches a §9 token-ability the loop produces (the Treasure's mana, via the loop's own
  /// <c>emit:token → sac:treasure → emit:mana</c>) but EXCLUDES an incidental same-card ability the loop
  /// never drives. The within-cards restriction is essential: the loop's mana flows to <c>pay:mana</c>
  /// ports corpus-wide, so unrestricted reachability would re-admit the corpus-global leniency. Shared by
  /// <see cref="ConjunctionHolds"/> (co-cost feeders) and <see cref="ManaBalanced"/> (mana producers).
  /// </summary>
  private static HashSet<string> ReachableWithinLoop(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyList<PortEdge> edges
  )
  {
    var cycleCards = cycle
      .SelectMany(e => new[] { e.From.Card, e.To.Card })
      .ToHashSet(StringComparer.Ordinal);
    var reachable = cycle
      .SelectMany(e => new[] { e.From.Identity, e.To.Identity })
      .ToHashSet(StringComparer.Ordinal);

    // Seed the FREE producers on cycle cards too: an emit with no driving cost (no card-defined
    // consume→emit edge) is unconditional, so it fires every iteration regardless of the loop's flow —
    // unlike a DRIVEN producer (an ETB/cost-gated ability), which fires only if the loop drives it
    // (reached via the BFS below). This is what keeps the latent gap closed (incidental cost-gated
    // same-card abilities are NOT seeded) while still counting genuine free sources.
    var driven = edges
      .Where(e => e.Provenance == EdgeProvenance.CardDefined && e.From.Side == PortSide.Consume)
      .Select(e => e.To.Identity)
      .ToHashSet(StringComparer.Ordinal);
    foreach (var p in edges.SelectMany(e => new[] { e.From, e.To }))
      if (p.Side == PortSide.Emit && cycleCards.Contains(p.Card) && !driven.Contains(p.Identity))
        reachable.Add(p.Identity);

    var adjacency = edges
      .Where(e => cycleCards.Contains(e.From.Card) && cycleCards.Contains(e.To.Card))
      .GroupBy(e => e.From.Identity, StringComparer.Ordinal)
      .ToDictionary(g => g.Key, g => g.Select(e => e.To.Identity).ToList(), StringComparer.Ordinal);

    var queue = new Queue<string>(reachable);
    while (queue.Count > 0)
      if (adjacency.TryGetValue(queue.Dequeue(), out var outs))
        foreach (var t in outs)
          if (reachable.Add(t))
            queue.Enqueue(t);
    return reachable;
  }

  /// <summary>A cost always payable without producing a resource — a tap (its untap rate-limit is the separate §8 gate).</summary>
  private static bool IsFreeCost(string label) =>
    label.StartsWith("tap:", StringComparison.Ordinal);

  /// <summary>
  /// The §8 mana-balance: the loop's per-iteration mana production must cover its mana costs
  /// (<c>net(mana) ≥ 0</c>). The costs are the <c>pay:mana</c> co-costs of the cycle's abilities; the
  /// production is the distinct <c>emit:mana</c> ports — <b>on the cycle's own cards</b> (so a Treasure
  /// from an unrelated combo can't subsidise it) — that feed those costs. CONSERVATIVE: returns true
  /// (no floor) when there is no mana cost, or when any relevant quantity is symbolic/unknown — it only
  /// floors when the shortfall is provable. Catches Chatterfang × Ruthless Knave ({2}{B}=3 vs 2 Treasures).
  /// </summary>
  private static bool ManaBalanced(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyDictionary<string, IReadOnlyList<PortNode>> coCosts,
    IReadOnlyList<PortEdge> edges
  )
  {
    var flow = GatherManaFlow(cycle, coCosts, edges);
    if (flow is null)
      return true; // no provable mana cost — nothing to balance (conservative)
    var (costs, producers) = flow.Value;

    // Per-colour balance: produced mana must cover each COLOURED pip in its OWN colour, not just the
    // fungible total — colorless can pay a generic {N} but never a {G} (CR 107.4). 'any'-colour
    // production is a flexible pool (a Treasure picks the colour, ADR-0002 §3b†); a specific colour pays
    // its own pip first and lends any surplus to the generic cost.
    var anyPool = producers
      .Where(p => string.Equals(ManaColor(p.Label), "any", StringComparison.OrdinalIgnoreCase))
      .Sum(p => p.Quantity!.Value);
    var supply = producers
      .Where(p => !string.Equals(ManaColor(p.Label), "any", StringComparison.OrdinalIgnoreCase))
      .GroupBy(p => ManaColor(p.Label) ?? "", StringComparer.OrdinalIgnoreCase)
      .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity!.Value), StringComparer.OrdinalIgnoreCase);

    var genericNeed = 0;
    var colouredNeed = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var c in costs)
    {
      var colour = ManaColor(c.Label);
      if (colour is null)
        genericNeed += c.Quantity!.Value;
      else
        colouredNeed[colour] = colouredNeed.GetValueOrDefault(colour) + c.Quantity!.Value;
    }

    // Each coloured pip draws first on its own colour, then on the flexible 'any' pool.
    var requiredAny = 0;
    var colourSurplus = 0;
    foreach (var (colour, need) in colouredNeed)
    {
      var own = supply.GetValueOrDefault(colour);
      if (own >= need)
        colourSurplus += own - need;
      else
        requiredAny += need - own;
    }
    if (requiredAny > anyPool)
      return false; // a coloured pip is provably unpayable by the produced colours
    // Generic {N} is covered by leftover 'any' + every colour's surplus (incl. colours with no pip).
    var unusedColour = supply.Where(kv => !colouredNeed.ContainsKey(kv.Key)).Sum(kv => kv.Value);
    var leftover = anyPool - requiredAny + colourSurplus + unusedColour;
    return leftover >= genericNeed;
  }

  /// <summary>
  /// The §8 productivity test: a <b>pure-mana</b> loop (every emit is mana) must net <em>positive</em>
  /// mana — a 1-for-1 filter (produced == cost) cycles the same mana forever and yields no advantage, so
  /// it is a do-nothing, not an infinite combo (Bog Initiate <c>{1}:Add{B}</c> ↔ Farrelite Priest
  /// <c>{1}:Add{W}</c>). A loop with a NON-mana output (a created token, a counter, a trigger) is
  /// productive via that output even at net-zero mana, so only pure-mana loops are tested. CONSERVATIVE:
  /// returns true when there's no provable mana cost or any quantity is symbolic.
  /// </summary>
  private static bool ManaProductive(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyDictionary<string, IReadOnlyList<PortNode>> coCosts,
    IReadOnlyList<PortEdge> edges
  )
  {
    var flow = GatherManaFlow(cycle, coCosts, edges);
    if (flow is null)
      return true; // no provable mana cost — productivity isn't mana-gated (a free / token loop)
    var pureMana = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.Side == PortSide.Emit)
      .All(p => IsEmitMana(p.Label));
    if (!pureMana)
      return true; // a non-mana output makes the loop productive even at net-zero mana
    var (costs, producers) = flow.Value;
    return producers.Sum(p => p.Quantity!.Value) > costs.Sum(p => p.Quantity!.Value);
  }

  /// <summary>
  /// Shared mana-flow gather (§8): the cycle's <c>pay:mana</c> costs (co-costs of its consumes + any
  /// in-cycle pay) and the distinct <c>emit:mana</c> producers <see cref="ReachableWithinLoop">the loop
  /// drives</see> that feed them. Returns <c>null</c> — the conservative "can't prove anything" signal —
  /// when there's no mana cost or any relevant quantity is symbolic.
  /// </summary>
  private static (List<PortNode> Costs, List<PortNode> Producers)? GatherManaFlow(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyDictionary<string, IReadOnlyList<PortNode>> coCosts,
    IReadOnlyList<PortEdge> edges
  )
  {
    var inCycle = cycle
      .SelectMany(e => new[] { e.From.Identity, e.To.Identity })
      .ToHashSet(StringComparer.Ordinal);
    var reachable = ReachableWithinLoop(cycle, edges);

    var costs = new Dictionary<string, PortNode>(StringComparer.Ordinal);
    void AddIfMana(PortNode p)
    {
      if (p.Side == PortSide.Consume && IsPayMana(p.Label))
        costs[p.Identity] = p;
    }
    foreach (var id in inCycle)
      if (coCosts.TryGetValue(id, out var siblings))
        foreach (var s in siblings)
          AddIfMana(s);
    foreach (var e in cycle)
    {
      AddIfMana(e.From);
      AddIfMana(e.To);
    }
    if (costs.Count == 0 || costs.Values.Any(p => p.Quantity is null))
      return null;

    var producers = edges
      .Where(e =>
        costs.ContainsKey(e.To.Identity)
        && IsEmitMana(e.From.Label)
        && reachable.Contains(e.From.Identity)
      )
      .Select(e => e.From)
      .GroupBy(p => p.Identity, StringComparer.Ordinal)
      .Select(g => g.First())
      .ToList();
    if (producers.Any(p => p.Quantity is null))
      return null;
    return (costs.Values.ToList(), producers);
  }

  /// <summary>
  /// ADR-0002 §8 ("B") — one-shot self-removal. A cycle that traverses a source's OWN
  /// leaves-the-battlefield-to-graveyard trigger (a self-scoped <c>ltb:…:to-graveyard:self</c> consume)
  /// is <b>structurally non-repeatable</b>: the source is a single object that dies at most once and the
  /// trigger fires only for that one death (CR 603.x); the objects the loop feeds back (created tokens)
  /// are <em>different</em> objects that can't re-satisfy the source's self-trigger. So the loop closes
  /// in the graph but cannot fire twice — it is <b>pruned</b> (impossible), not floored to Amber. This
  /// is the structural reading the user gave: "the token creation is attached to the creature itself
  /// dying, making it fundamentally non-reusable" — a property of the AST, not a game-state trace.
  /// <para><b>Carve-out (Persist/Undying):</b> if the same self-death also returns the source to the
  /// battlefield (a card-defined <c>ltb:…:self → emit:returntobattlefield</c>), the source can die
  /// again, so the cycle is retained (its finiteness then turns on counters — a separate axis).</para>
  /// </summary>
  private static bool IsOneShotSelfRemoval(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyList<PortEdge> edges
  )
  {
    var selfDeaths = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.Side == PortSide.Consume && IsSelfLeavesToGraveyard(p.Label))
      .GroupBy(p => p.Identity, StringComparer.Ordinal)
      .Select(g => g.First());

    foreach (var death in selfDeaths)
    {
      var returnsSelf = edges.Any(e =>
        e.Provenance == EdgeProvenance.CardDefined
        && string.Equals(e.From.Identity, death.Identity, StringComparison.Ordinal)
        && e.To.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
      );
      if (!returnsSelf)
        return true; // a self-death with no self-return — the source dies once
    }
    return false;
  }

  /// <summary>
  /// ADR-0002 §8 — every tap-gated permanent the cycle traverses is <b>renewed</b>: it untaps itself
  /// each iteration on an event the loop produces. A card is renewed iff it has a card-defined <b>self</b>
  /// untap (an <c>etb:X → emit:untap:self</c> trigger — "untap THIS", not "untap target permanent" which
  /// renews someone else) and the loop creates a token whose type triggers <c>etb:X</c> (the token enters
  /// → untaps it). Blasting Station — "untap this whenever a creature enters" — is
  /// renewed by the very creature tokens its sac outlet consumes. STRICT (the dual of §8-B's self-return
  /// carve-out): a tap gate with no provable in-loop untap stays floored. Vacuously true when the cycle
  /// has no tap gate.
  /// </summary>
  private bool TapGatesRenewed(IReadOnlyList<PortEdge> cycle, IReadOnlyList<PortEdge> edges)
  {
    var tapCards = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.TapGated)
      .Select(p => p.Card)
      .ToHashSet(StringComparer.Ordinal);
    if (tapCards.Count == 0)
      return true;

    // Tokens the loop creates each iteration (they enter the battlefield, triggering an etb untap).
    var tokens = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.Side == PortSide.Emit && ResourceKind(p.Label) == "token" && p.Subject is not null)
      .GroupBy(p => p.Identity, StringComparer.Ordinal)
      .Select(g => g.First())
      .ToList();

    foreach (var card in tapCards)
    {
      // (a) Blasting Station — a card-defined SELF-untap on the tap-gated card, fed by a loop token whose
      // type triggers its etb (the original renewal).
      var untapTriggers = edges
        .Where(e =>
          e.Provenance == EdgeProvenance.CardDefined
          && string.Equals(e.From.Card, card, StringComparison.Ordinal)
          && e.From.Side == PortSide.Consume
          && Role(e.From.Label) == "etb"
          && e.To.Label == "emit:untap:self" // a SELF-untap — untapping a target renews someone else
          && e.From.Subject is not null
        )
        .Select(e => e.From)
        .ToList();
      var selfRenewed = untapTriggers.Any(trig =>
        tokens.Any(tok =>
          ObjectFilterRelations.Intersects(tok.Subject!, trig.Subject!, _ontology).Relation
          != FilterRelation.Disjoint
        )
      );

      // (b) Copy-inheritance (Decision 4) — the cycle traverses a synthesized closing edge from an
      // inherited untap, on a copy this card GRAFTED, into this card's tap:self. The graft already
      // certified the untap reaches the source (the disjunctive target test) and tiered the edge; here we
      // just confirm such a renewing hop is on the cycle. Covers both the inherited self-untap (4a, the
      // copy untapping itself when the engine treats the copy's tap as the copier's — n/a for Kiki) and
      // the target-untap aimed at the tap-gated source (4b, Corridor Monitor → Kiki). The blink arm adds
      // a third renewing kind: an inherited BLINK aimed at the copier (Kiki copies Restoration Angel /
      // Felidar — the copy's optional ETB blinks Kiki, which re-enters untapped, CR 603.6e/400.7).
      var copyRenewed = cycle.Any(e =>
        e.Provenance == EdgeProvenance.RulesDefined
        && e.From.Side == PortSide.Emit
        && (IsUntap(e.From.Label) || IsBlink(e.From.Label))
        && string.Equals(e.From.Grafter, card, StringComparison.Ordinal) // a copy THIS card grafted
        && string.Equals(e.To.Card, card, StringComparison.Ordinal)
        && e.To.Label == "tap:self"
      );

      if (!selfRenewed && !copyRenewed)
        return false;
    }
    return true;
  }

  /// <summary>
  /// ADR-0002 §8 — the sac→death <b>bridge respects the loop's token type</b>. A bridge claims "a
  /// sacrificed creature dies, feeding a dies-trigger" (CR 701.21a→700.4), but the dies-trigger fires
  /// only if the thing sacrificed is the type it requires. When the object the loop feeds into the sac
  /// is a <em>created token</em> whose at-creation type can't be that (a Treasure — artifact, CR 111.10
  /// — sacrificed into "a creature you control dies"), the bridge can never fire, so the loop can't
  /// close → prune (Lithatog / Extruder × Pitiless Plunderer). The dual of the token→sac flow guard
  /// (<see cref="TokenSatisfiesAtCreation"/>); like it, this lives in the engine as a loop-reconstruction
  /// policy (an in-loop animation that makes the token a creature is an out-of-boundary false-negative),
  /// keeping the <see cref="ObjectFilterRelations"/> operator a pure type relation.
  /// </summary>
  private bool BridgeFedByIncompatibleToken(IReadOnlyList<PortEdge> cycle)
  {
    foreach (var bridge in cycle)
    {
      if (
        Role(bridge.From.Label) != "sac"
        || Role(bridge.To.Label) != "ltb"
        || !bridge.To.Label.Contains(":to-graveyard", StringComparison.Ordinal)
        || bridge.To.Subject?.IsSelf == true // a :self death is the §8-B one-shot rule's domain, not a type mismatch
      )
        continue;
      var dies = bridge.To;
      foreach (var feed in cycle)
        if (
          string.Equals(feed.To.Identity, bridge.From.Identity, StringComparison.Ordinal)
          && feed.From.Side == PortSide.Emit
          && ResourceKind(feed.From.Label) == "token"
          && !TokenSatisfiesAtCreation(feed.From, dies)
        )
          return true; // the sacrificed token can't be the dies-trigger's type — the bridge can't fire
    }
    return false;
  }

  /// <summary>
  /// ADR-0002 §8 — a death-trigger's <b>counter gate</b> the loop can never re-satisfy. A trigger that
  /// fires only "if [the dying creature] had a +1/+1 counter on it" (Basri's Lieutenant; <c>RequiresCounter</c>,
  /// CR 603.10 look-back) makes tokens that enter <em>without</em> that counter, so a loop fed by those
  /// tokens dies counter-less each iteration and the gate never re-fires — prune. UNLESS the loop has a
  /// <b>per-iteration</b> counter source: a card-defined <c>etb:X → emit:counter:&lt;kind&gt;</c> on a loop
  /// card whose <c>etb:X</c> a loop-created token triggers (a creature-enters → put-a-counter, Cathars'
  /// Crusade) — that counters each token before it dies. A one-time <em>self</em>-ETB counter (the source's
  /// own enter) is excluded (it fires once). The firability dual of the tap-renewal carve-out. STRICT.
  /// <para>Boundary: the loop's tokens are assumed to enter without the counter — a token effect that
  /// creates it WITH a counter (none known that re-satisfies its own gate) would be a false-prune, the
  /// declared modeling limit (no in-loop counter-state, like the bridge guard's in-loop animation).</para>
  /// </summary>
  private bool CounterGateUnsatisfiable(IReadOnlyList<PortEdge> cycle, IReadOnlyList<PortEdge> edges)
  {
    var gates = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.RequiresCounter is not null)
      .Select(p => p.RequiresCounter!)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();
    if (gates.Count == 0)
      return false;

    var cards = cycle.SelectMany(e => new[] { e.From.Card, e.To.Card }).ToHashSet(StringComparer.Ordinal);
    var tokens = cycle
      .SelectMany(e => new[] { e.From, e.To })
      .Where(p => p.Side == PortSide.Emit && ResourceKind(p.Label) == "token" && p.Subject is not null)
      .GroupBy(p => p.Identity, StringComparer.Ordinal)
      .Select(g => g.First())
      .ToList();

    foreach (var counter in gates)
    {
      var active = edges.Any(e =>
        e.Provenance == EdgeProvenance.CardDefined
        && cards.Contains(e.From.Card)
        && e.From.Side == PortSide.Consume
        && Role(e.From.Label) == "etb"
        && e.From.Subject is not null
        && e.From.Subject.IsSelf != true // a one-time self-ETB doesn't sustain the loop
        && e.To.Label.StartsWith($"emit:counter:{counter.ToLowerInvariant()}", StringComparison.Ordinal)
        && tokens.Any(tok =>
          ObjectFilterRelations.Intersects(tok.Subject!, e.From.Subject, _ontology).Relation
          != FilterRelation.Disjoint
        )
      );
      if (!active)
        return true; // the gate requires a counter the loop's tokens never carry and nothing renews
    }
    return false;
  }

  /// <summary>A self-scoped dies-trigger: <c>ltb</c> role, destination <c>to-graveyard</c> (CR 700.4), scope <c>self</c>.</summary>
  private static bool IsSelfLeavesToGraveyard(string label)
  {
    var segments = label.Split(':');
    return segments.Length > 0
      && segments[0] == "ltb"
      && segments.Contains("to-graveyard")
      && segments.Contains("self");
  }

  private static bool IsPayMana(string label) =>
    label.StartsWith("pay:mana", StringComparison.Ordinal);

  private static bool IsEmitMana(string label) =>
    label.StartsWith("emit:mana", StringComparison.Ordinal);

  private static string Role(string label) => label.Split(':', 2)[0];

  private static string? ResourceKind(string label)
  {
    var parts = label.Split(':');
    return parts.Length >= 2 ? parts[1] : null;
  }
}
