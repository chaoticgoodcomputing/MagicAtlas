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
  private readonly TypeOntology _ontology;

  public PortGraphEngine(TypeOntology ontology) => _ontology = ontology;

  public IReadOnlyList<PortEdge> Materialize(IReadOnlyList<PortGraph> graphs)
  {
    var ports = graphs.SelectMany(g => g.Ports).ToList();
    var edges = new List<PortEdge>();

    // (1) Card-defined edges — the walk's intra-ability causality, certain by construction (§5).
    foreach (var graph in graphs)
      foreach (var edge in graph.CardDefinedEdges)
        edges.Add(
          new PortEdge { From = edge.From, To = edge.To, Provenance = EdgeProvenance.CardDefined }
        );

    var emits = ports.Where(p => p.Side == PortSide.Emit).ToList();
    var consumes = ports.Where(p => p.Side == PortSide.Consume).ToList();
    var intercepts = ports.Where(p => p.Side == PortSide.Intercept).ToList();

    // (2) Flow — an emitted object refuels a consume; mana refunds a mana cost.
    foreach (var emit in emits)
      foreach (var consume in consumes)
        if (FlowFeasible(emit, consume))
          AddRulesEdge(edges, emit, consume, EdgeFamily.Flow);

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

  /// <summary>The minimal derived flow grammar (§6) the gold needs: a created token refuels a sac; mana refunds a mana cost.</summary>
  private bool FlowFeasible(PortNode emit, PortNode consume) =>
    (ResourceKind(emit.Label), Role(consume.Label)) switch
    {
      ("token", "sac") => TokenSatisfiesAtCreation(emit, consume),
      ("mana", "pay") => ResourceKind(consume.Label) == "mana" // mana refunds a mana cost…
        && ManaColorFeeds(ManaColor(emit.Label), ManaColor(consume.Label)), // …of a colour it can pay
      ("life", "trigger") => LifeFlowFeasible(emit, consume), // a life event feeds a same-direction life trigger (CR 119)
      _ => false,
    };

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
  /// </summary>
  public IReadOnlyList<PortCycle> FindCycles(
    IReadOnlyList<PortEdge> edges,
    int maxLength = int.MaxValue
  )
  {
    var adjacency = edges
      .GroupBy(e => e.From.Identity)
      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

    // §8 conjunction input: each cost's co-costs. (Whether a co-cost is "fed" is now decided per-cycle
    // against the LOOP's own producers, not a corpus-global set — see ConjunctionHolds.)
    var coCosts = CoCostMap(edges);

    var cycles = new List<PortCycle>();
    var path = new List<PortEdge>();
    var onPath = new HashSet<string>(StringComparer.Ordinal);

    void Dfs(string nodeId, string startId)
    {
      if (!adjacency.TryGetValue(nodeId, out var outgoing))
        return;
      foreach (var edge in outgoing)
      {
        var toId = edge.To.Identity;
        if (toId == startId)
        {
          path.Add(edge);
          var loop = path.ToList();
          if (
            !IsOneShotSelfRemoval(loop, edges) // §8 "B": prune the structurally non-repeatable
            && !BridgeFedByIncompatibleToken(loop) // §8: the loop's token can't satisfy the dies-trigger
            && !CounterGateUnsatisfiable(loop, edges) // §8: a "had a counter" gate the loop can't re-satisfy
          )
            cycles.Add(
              new PortCycle
              {
                Edges = loop,
                CoCostsSatisfied = ConjunctionHolds(loop, coCosts, edges),
                Balanced = ManaBalanced(loop, coCosts, edges),
                Productive = ManaProductive(loop, coCosts, edges),
                TapRenewed = TapGatesRenewed(loop, edges),
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

    foreach (var start in adjacency.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
      onPath.Clear();
      onPath.Add(start);
      path.Clear();
      Dfs(start, start);
    }
    return cycles;
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
      var renewed = untapTriggers.Any(trig =>
        tokens.Any(tok =>
          ObjectFilterRelations.Intersects(tok.Subject!, trig.Subject!, _ontology).Relation
          != FilterRelation.Disjoint
        )
      );
      if (!renewed)
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
