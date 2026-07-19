namespace MagicAST.Interaction;

/// <summary>
/// The "why" behind a reconstructed <see cref="PortCycle"/> — the full §8 verdict plus its edge trail,
/// captured as a plain nested projection so a consumer (the combo-recall bench, a future interaction
/// scope test, an <c>--explain</c> CLI) can report WHY a combo landed at its tier without re-deriving
/// the cycle. Lives in <c>libs/mast-interaction</c> (not the bench project) so any interaction-scope
/// consumer can build on it, not just the bench.
/// <para>
/// Deliberately NOT flattened like <c>CycleEdgeRow</c> (tests/magic-ast-tests/Flows/InteractionTriage) —
/// that type is flat because it crosses the Flowthru → Python (Arrow) boundary (FT2009 forbids nested
/// POCOs there). This type has no such constraint (plain-JSON bench output), so it stays a natural
/// nested shape: one diagnostics object per combo, with a list of hop objects inside it.
/// </para>
/// </summary>
public sealed record ComboDiagnostics
{
  /// <summary>The cycle-level verdict (<see cref="PortCycle.Tier"/>, stringified — Green/Amber/Red).</summary>
  public required string CycleTier { get; init; }

  /// <summary>Hop count (<see cref="PortCycle.Edges"/>.Count).</summary>
  public required int CycleLength { get; init; }

  /// <summary>§8 firability — no hop touches an undischarged gate.</summary>
  public required bool Firable { get; init; }

  /// <summary>§8 — every tap gate the cycle touches is renewed each iteration.</summary>
  public required bool TapRenewed { get; init; }

  /// <summary>§8 multi-cost conjunction — every cost port of every hop's ability is fed.</summary>
  public required bool CoCostsSatisfied { get; init; }

  /// <summary>§8 mana balance — net(mana) ≥ 0 per iteration.</summary>
  public required bool Balanced { get; init; }

  /// <summary>§8 life balance — net(life) ≥ 0 per iteration (CR 119.4/119.6).</summary>
  public required bool LifeBalanced { get; init; }

  /// <summary>§8 productivity — the loop nets an unbounded resource, not just a net-zero filter.</summary>
  public required bool Productive { get; init; }

  /// <summary>Why the cycle isn't certified-GREEN (<see cref="PortCycle.LimitingReason"/>); "" when GREEN.</summary>
  public required string LimitingReason { get; init; }

  /// <summary>The cycle's edge trail, in hop order.</summary>
  public required IReadOnlyList<HopDiagnostic> Edges { get; init; }

  /// <summary>Projects a materialized, winning <see cref="PortCycle"/> into its full diagnostic shape.</summary>
  public static ComboDiagnostics FromCycle(PortCycle cycle) =>
    new()
    {
      CycleTier = cycle.Tier.ToString(),
      CycleLength = cycle.Edges.Count,
      Firable = cycle.Firable,
      TapRenewed = cycle.TapRenewed,
      CoCostsSatisfied = cycle.CoCostsSatisfied,
      Balanced = cycle.Balanced,
      LifeBalanced = cycle.LifeBalanced,
      Productive = cycle.Productive,
      LimitingReason = cycle.LimitingReason ?? "",
      Edges = [.. cycle.Edges.Select(HopDiagnostic.FromEdge)],
    };
}

/// <summary>One hop of a reconstructed cycle's edge trail — the per-edge operator verdict (ADR-0002 §5/§7).</summary>
public sealed record HopDiagnostic
{
  /// <summary>Hop order within the cycle (0-based, matches <see cref="PortCycle.Edges"/> order).</summary>
  public required int Hop { get; init; }

  /// <summary>The edge's deterministic identity (<see cref="PortEdge.Id"/>, ADR-0004 Migration Stage 0/1)
  /// — stable across separate materializations of the same corpus state.</summary>
  public required string EdgeId { get; init; }

  public required string FromCard { get; init; }
  public required string FromLabel { get; init; }
  public required string ToCard { get; init; }
  public required string ToLabel { get; init; }

  /// <summary>Card-defined (certain, §5) vs rules-defined (operator-tiered).</summary>
  public required string Provenance { get; init; }

  /// <summary>Flow (resource handoff) vs Modifier (a replacement rewriting an emission).</summary>
  public required string Family { get; init; }

  /// <summary><see cref="MagicAST.AST.References.FilterRelation"/> — Overlaps/Disjoint/Unknown.</summary>
  public required string Overlap { get; init; }

  /// <summary><see cref="MagicAST.AST.References.Trilean"/> — the operator's reliability verdict.</summary>
  public required string Reliability { get; init; }

  /// <summary>This hop's own certainty tier (<see cref="PortEdge.Tier"/>).</summary>
  public required string EdgeTier { get; init; }

  /// <summary>The operator's reason for a non-Yes/non-Overlaps verdict; "" if none.</summary>
  public required string Reason { get; init; }

  /// <summary>Whether either endpoint carries a §8 gate (rate-limit / intervening-if).</summary>
  public required bool Gated { get; init; }

  public static HopDiagnostic FromEdge(PortEdge edge, int hop) =>
    new()
    {
      Hop = hop,
      EdgeId = edge.Id,
      FromCard = edge.From.Card,
      FromLabel = edge.From.Label,
      ToCard = edge.To.Card,
      ToLabel = edge.To.Label,
      Provenance = edge.Provenance.ToString(),
      Family = edge.Family.ToString(),
      Overlap = edge.Overlap.ToString(),
      Reliability = edge.Reliability.ToString(),
      EdgeTier = edge.Tier.ToString(),
      Reason = edge.Reason ?? "",
      Gated = edge.From.Gated || edge.To.Gated,
    };
}
