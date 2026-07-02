using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// Port-graph overview: a quick node/edge census of the materialized union interaction graph, computed
/// from the <see cref="CardEdgeRow"/> edge export. Emitted ALONGSIDE the edges by
/// <see cref="MaterializeCardEdgesStep"/> (not as a trailing step) so it lands with CardEdges — well
/// before the expensive cycle enumeration — and is always produced even when the cycle/viz tail is too
/// slow to finish at union scale. Pairs with <see cref="PortLabelCensus"/> (whole-corpus label
/// vocabulary); this one is the SHAPE of the actual reconstruction graph the engine runs cycles over.
/// </summary>
internal static class PortGraphMetricsCensus
{
  public static PortGraphMetrics Compute(IEnumerable<CardEdgeRow> edgeRows)
  {
    var edges = edgeRows as IReadOnlyList<CardEdgeRow> ?? edgeRows.ToList();

    // A port node is a distinct (card, label) instance on either end of an edge.
    var ports = new HashSet<string>(StringComparer.Ordinal);
    var cards = new HashSet<string>(StringComparer.Ordinal);
    var labels = new HashSet<string>(StringComparer.Ordinal);
    foreach (var e in edges)
    {
      ports.Add(e.FromCard + "::" + e.FromLabel);
      ports.Add(e.ToCard + "::" + e.ToLabel);
      cards.Add(e.FromCard);
      cards.Add(e.ToCard);
      labels.Add(e.FromLabel);
      labels.Add(e.ToLabel);
    }

    var emitPorts = ports.Count(p => LabelOf(p).StartsWith("emit:", StringComparison.Ordinal));

    return new PortGraphMetrics
    {
      GeneratedAt = DateTime.UtcNow,
      TotalPorts = ports.Count,
      TotalEdges = edges.Count,
      DistinctCards = cards.Count,
      DistinctPortLabels = labels.Count,
      EmitPorts = emitPorts,
      ConsumePorts = ports.Count - emitPorts,
      EdgesPerPort = ports.Count == 0 ? 0.0 : Math.Round((double)edges.Count / ports.Count, 3),
      EdgesByTier = Tally(edges.Select(e => e.Tier)),
      EdgesByFamily = Tally(edges.Select(e => e.Family)),
      EdgesByResource = Tally(edges.Select(e => e.Resource)),
    };
  }

  /// <summary>The label segment of a <c>card::label</c> port key.</summary>
  private static string LabelOf(string portKey)
  {
    var idx = portKey.IndexOf("::", StringComparison.Ordinal);
    return idx < 0 ? portKey : portKey[(idx + 2)..];
  }

  /// <summary>Count by category, descending (ties broken by name for determinism), blanks bucketed as "(none)".</summary>
  private static IReadOnlyList<LabeledCount> Tally(IEnumerable<string> values) =>
    values
      .Select(v => string.IsNullOrEmpty(v) ? "(none)" : v)
      .GroupBy(v => v, StringComparer.Ordinal)
      .Select(g => new LabeledCount { Label = g.Key, Count = g.Count() })
      .OrderByDescending(c => c.Count)
      .ThenBy(c => c.Label, StringComparer.Ordinal)
      .ToList();
}
