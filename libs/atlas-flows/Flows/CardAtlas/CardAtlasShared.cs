using System.Text.Json;
using MagicAST;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Flows.Shared;

namespace MagicAtlas.Flows.CardAtlas;

/// <summary>Helpers shared across the CardAtlas steps (D1/D4): the parse → project idiom
/// (<see cref="Project"/>, mirroring <c>InteractionUnion.GraphFor</c>) and the cycle → resource-family
/// projections (signature / ring / cards) that turn a reconstructed <see cref="PortCycle"/> into the
/// combo-instance shape. Promoted from tests/magic-ast-tests/Flows/CardAtlas/CardAtlasShared.cs.</summary>
internal static class CardAtlasShared
{
  /// <summary>Parse a card's oracle text and project its port graph.</summary>
  internal static PortGraph Project(string name, CardInputDTO dto, OracleParser parser, PortWalk walk)
  {
    var text = dto.OracleText;
    if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
      text = string.Join("\n\n", dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0));
    if (string.IsNullOrWhiteSpace(text))
      return new PortGraph();
    var abilities = JsonSerializer.SerializeToNode(
      parser.Parse(text).Output.Abilities,
      MagicASTJsonOptions.Strict
    );
    return walk.Project(name, abilities);
  }

  /// <summary>The From-family of each hop in cycle order (canonical families only) — the node sequence a
  /// cycle traverses. Since a cycle closes (To(eᵢ) = From(eᵢ₊₁)), this is the full family node set.</summary>
  private static List<string> FromFamilies(PortCycle cycle) =>
    cycle
      .Edges.Select(e => ResourceFamilies.Of(e.From.Label))
      .Where(ResourceFamilies.Canonical.Contains)
      .ToList();

  /// <summary>Distinct in-scope cards on the cycle (the buildable piece list).</summary>
  internal static List<string> CardsOf(PortCycle cycle) =>
    cycle
      .Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
      .Distinct(StringComparer.Ordinal)
      .OrderBy(c => c, StringComparer.Ordinal)
      .ToList();

  /// <summary>The archetype key: sorted distinct canonical families the cycle touches, ", "-joined.</summary>
  internal static string SignatureOf(PortCycle cycle) =>
    string.Join(
      ", ",
      FromFamilies(cycle).Distinct(StringComparer.Ordinal).OrderBy(f => f, StringComparer.Ordinal)
    );

  /// <summary>The loop shape: canonical families in ring order with consecutive duplicates collapsed,
  /// " → "-joined (e.g. <c>token → sacrifice → death</c>). Consecutive dedupe drops within-family hops so
  /// the ring reads as resource transitions; the closing hop (last → first) is implicit.</summary>
  internal static string RingOf(PortCycle cycle)
  {
    var fams = FromFamilies(cycle);
    var ring = new List<string>(fams.Count);
    foreach (var f in fams)
      if (ring.Count == 0 || !string.Equals(ring[^1], f, StringComparison.Ordinal))
        ring.Add(f);
    // Collapse a wraparound duplicate (ring closes on the same family it opened).
    if (ring.Count > 1 && string.Equals(ring[0], ring[^1], StringComparison.Ordinal))
      ring.RemoveAt(ring.Count - 1);
    return string.Join(" → ", ring);
  }
}
