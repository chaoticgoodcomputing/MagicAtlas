namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// The analytical interaction engine (mast-interaction ADR-0001). Materialises the three-valued
/// port-instance graph by evaluating the MAST-owned <c>ObjectFilter</c> relation operators per
/// port-pair — <c>Intersects</c> is the flow/modifier prune, <c>Subsumes</c> grades reliability —
/// then finds cycles and assigns each a certainty tier. No board state; it overgenerates candidates
/// and the <c>Intersects</c> prune keeps the parse honest.
/// </summary>
public sealed class InteractionEngine
{
  private readonly TypeOntology _ontology;

  public InteractionEngine(TypeOntology ontology) => _ontology = ontology;

  /// <summary>
  /// Materialise the directed port-instance graph by expanding the authored <paramref name="grammar"/>
  /// over the derived <paramref name="ports"/>: for each family edge, pair the ports labelled its
  /// <c>From</c> with those labelled its <c>To</c> and keep only pairs whose operator-join holds
  /// (Flow → emitter.Emits vs consumer.Consumes; Modifier → emitter.Emits vs modifier.Intercepts).
  /// The grammar bounds the expansion — never a blanket cartesian product — and the <c>Intersects</c>
  /// prune drops Disjoint pairs.
  /// </summary>
  public IReadOnlyList<InteractionEdge> Materialize(
    IReadOnlyList<Port> ports,
    IReadOnlyList<FamilyEdge> grammar
  )
  {
    var byLabel = ports
      .GroupBy(p => p.Label, StringComparer.Ordinal)
      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

    var edges = new List<InteractionEdge>();
    foreach (var rule in grammar)
    {
      if (!byLabel.TryGetValue(rule.From, out var froms) || !byLabel.TryGetValue(rule.To, out var tos))
        continue;
      foreach (var from in froms)
        foreach (var to in tos)
        {
          if (ReferenceEquals(from, to))
            continue;
          var sinks = rule.Family == EdgeFamily.Flow ? to.Consumes : to.Intercepts;
          foreach (var emit in from.Emits.Where(r => r.Kind == rule.Resource))
            foreach (var sink in sinks.Where(r => r.Kind == rule.Resource))
              TryAdd(edges, from, to, emit, sink, rule.Family);
        }
    }
    return edges;
  }

  private void TryAdd(
    List<InteractionEdge> edges,
    Port from,
    Port to,
    Resource emit,
    Resource sink,
    EdgeFamily family
  )
  {
    FilterMatch overlap;
    SubsumeMatch reliability;
    if (emit.Subject is null || sink.Subject is null)
    {
      // Scalar resource (mana, generic counter): kind-match alone, no ObjectFilter overlap.
      overlap = new FilterMatch(FilterRelation.Overlaps);
      reliability = new SubsumeMatch(Trilean.Yes);
    }
    else
    {
      overlap = ObjectFilterRelations.Intersects(emit.Subject, sink.Subject, _ontology);
      // Reliable iff every object the emitter produces is one the sink accepts: emit ⊆ sink.
      reliability = ObjectFilterRelations.Subsumes(emit.Subject, sink.Subject, _ontology);
    }

    if (overlap.Relation == FilterRelation.Disjoint)
      return; // the prune — no edge

    edges.Add(
      new InteractionEdge
      {
        From = from,
        To = to,
        Resource = emit.Kind,
        Family = family,
        Overlap = overlap.Relation,
        Reliability = reliability.Value,
        Reason = overlap.Relation == FilterRelation.Unknown ? overlap.Reason : reliability.Reason,
      }
    );
  }

  /// <summary>
  /// Elementary cycles over the materialised graph (each rooted at its lowest-identity port, so it
  /// surfaces once). A cycle is a candidate interaction loop; its tier is the worst hop.
  /// </summary>
  public IReadOnlyList<InteractionCycle> FindCycles(IReadOnlyList<InteractionEdge> edges)
  {
    var adjacency = edges
      .GroupBy(e => e.From.Identity)
      .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

    var cycles = new List<InteractionCycle>();
    var path = new List<InteractionEdge>();
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
          cycles.Add(new InteractionCycle { Edges = path.ToList() });
          path.RemoveAt(path.Count - 1);
        }
        // Restrict to nodes after the start (and not already on the path) so each elementary cycle
        // is found exactly once — at its lowest-identity rotation.
        else if (
          string.CompareOrdinal(toId, startId) > 0
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

    foreach (
      var start in adjacency.Keys.OrderBy(k => k, StringComparer.Ordinal)
    )
    {
      onPath.Clear();
      onPath.Add(start);
      path.Clear();
      Dfs(start, start);
    }
    return cycles;
  }
}
