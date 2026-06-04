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
  /// floors to Amber even when every edge is Green.
  /// </summary>
  public bool Firable => !Edges.Any(e => e.From.Gated || e.To.Gated);

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

  /// <summary>The worst hop sets the tier, floored to Amber when the cycle is not firable, has an unfed co-cost, or is resource-negative (§8).</summary>
  public CertaintyTier Tier =>
    Edges.Count == 0
      ? CertaintyTier.Green
      : (CertaintyTier)
        Math.Max(
          (int)Edges.Max(e => e.Tier),
          Firable && CoCostsSatisfied && Balanced ? 0 : (int)CertaintyTier.Amber
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
    : !Firable ? "gated (rate-limit / intervening-if)"
    : !Balanced ? "mana-negative"
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
      ("token", "sac") => TokenSatisfiesSacAtCreation(emit, consume),
      ("mana", "pay") => ResourceKind(consume.Label) == "mana" // mana refunds a mana cost…
        && ManaColorFeeds(ManaColor(emit.Label), ManaColor(consume.Label)), // …of a colour it can pay
      _ => false,
    };

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
  /// A created token refuels a sacrifice cost only if its <b>at-creation</b> type already satisfies the
  /// sac's required card types. A create-token effect fully specifies the token's type (CR 111.10 — a
  /// Treasure is a non-creature artifact), so the emit's <c>CardTypes</c> are <b>exact</b>: a Treasure
  /// cannot feed "sacrifice a creature", and a creature token cannot feed "sacrifice an artifact". An
  /// intervening <em>animation</em> that later adds <c>creature</c> is an external enabler outside the
  /// reconstructed loop — a deliberate modeling boundary, <b>not</b> a claim the types are
  /// <see cref="FilterRelation.Disjoint"/> (the <see cref="ObjectFilterRelations"/> operator stays a
  /// pure type relation; a crewed Vehicle genuinely IS a creature). This guard lives at the engine as a
  /// loop-reconstruction policy, accepting that animation false-negative. (MAST judge panel 2026-06-04:
  /// operator subtype→cardtype exclusivity is UNSOUND — Vehicles CR 301.7, Equipment CR 301.5c — so the
  /// fix belongs here, not in the operator.) Removes the corpus's creature-token ↔ artifact-sac junk.
  /// </summary>
  private bool TokenSatisfiesSacAtCreation(PortNode emit, PortNode consume)
  {
    if (emit.Subject is null || consume.Subject is null)
      return true;
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

    // §8 conjunction inputs: which ports are "fed" (a producer targets them), and each cost's co-costs.
    var fed = edges.Select(e => e.To.Identity).ToHashSet(StringComparer.Ordinal);
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
          cycles.Add(
            new PortCycle
            {
              Edges = loop,
              CoCostsSatisfied = ConjunctionHolds(loop, coCosts, fed),
              Balanced = ManaBalanced(loop, coCosts, edges),
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
  /// must be payable each iteration — already in the loop, fed by a producer, or a free cost (tap). A
  /// resource co-cost with no feeder means the ability can't fire, so the loop is not certifiable.
  /// </summary>
  private static bool ConjunctionHolds(
    IReadOnlyList<PortEdge> cycle,
    IReadOnlyDictionary<string, IReadOnlyList<PortNode>> coCosts,
    ISet<string> fed
  )
  {
    var inCycle = cycle
      .SelectMany(e => new[] { e.From.Identity, e.To.Identity })
      .ToHashSet(StringComparer.Ordinal);

    foreach (var id in inCycle)
      if (coCosts.TryGetValue(id, out var siblings))
        foreach (var s in siblings)
          if (
            !IsFreeCost(s.Label)
            && !inCycle.Contains(s.Identity)
            && !fed.Contains(s.Identity)
          )
            return false;
    return true;
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
    var inCycle = cycle
      .SelectMany(e => new[] { e.From.Identity, e.To.Identity })
      .ToHashSet(StringComparer.Ordinal);
    var cycleCards = cycle
      .SelectMany(e => new[] { e.From.Card, e.To.Card })
      .ToHashSet(StringComparer.Ordinal);

    // Mana costs: the pay:mana co-costs of the cycle's consumes (+ any in-cycle pay:mana consume).
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
    if (costs.Count == 0)
      return true; // no mana cost — nothing to balance
    if (costs.Values.Any(p => p.Quantity is null))
      return true; // symbolic cost — can't prove a shortfall (conservative)
    var manaCost = costs.Values.Sum(p => p.Quantity!.Value);

    // Producers: distinct emit:mana ports ON THE CYCLE'S CARDS that feed those costs.
    var producers = edges
      .Where(e =>
        costs.ContainsKey(e.To.Identity)
        && IsEmitMana(e.From.Label)
        && cycleCards.Contains(e.From.Card)
      )
      .Select(e => e.From)
      .GroupBy(p => p.Identity, StringComparer.Ordinal)
      .Select(g => g.First())
      .ToList();
    if (producers.Any(p => p.Quantity is null))
      return true; // symbolic production — conservative
    var manaProduced = producers.Sum(p => p.Quantity!.Value);

    return manaProduced >= manaCost;
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
