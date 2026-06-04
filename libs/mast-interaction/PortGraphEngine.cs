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

  public CertaintyTier Tier =>
    Edges.Count == 0 ? CertaintyTier.Green : Edges.Max(e => e.Tier);

  /// <summary>The hop that limits the tier (and its operator <see cref="PortEdge.Reason"/>).</summary>
  public PortEdge? LimitingHop =>
    Edges
      .OrderByDescending(e => (int)e.Tier)
      .ThenBy(e => e.From.Label, StringComparer.Ordinal)
      .FirstOrDefault();
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
        if (Flows(emit, consume))
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
  private static bool Flows(PortNode emit, PortNode consume) =>
    (ResourceKind(emit.Label), Role(consume.Label)) switch
    {
      ("token", "sac") => true, // a created token is a permanent that can be sacrificed
      ("mana", "pay") => ResourceKind(consume.Label) == "mana", // mana refunds a mana cost
      _ => false,
    };

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
  public IReadOnlyList<PortCycle> FindCycles(IReadOnlyList<PortEdge> edges)
  {
    var adjacency = edges
      .GroupBy(e => e.From.Identity)
      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

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
          cycles.Add(new PortCycle { Edges = path.ToList() });
          path.RemoveAt(path.Count - 1);
        }
        else if (string.CompareOrdinal(toId, startId) > 0 && !onPath.Contains(toId))
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

  private static string Role(string label) => label.Split(':', 2)[0];

  private static string? ResourceKind(string label)
  {
    var parts = label.Split(':');
    return parts.Length >= 2 ? parts[1] : null;
  }
}
