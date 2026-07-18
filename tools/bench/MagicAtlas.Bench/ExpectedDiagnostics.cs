using System.Text.Json.Serialization;
using MagicAST.Interaction;

namespace MagicAtlas.Bench;

/// <summary>
/// The single limiting hop of a reconstructed cycle, reduced to just the fields that identify WHY it
/// limits the tier — for the pinned <c>expected</c> block (<see cref="ExpectedDiagnostics"/>), not the
/// full edge trail.
/// </summary>
public sealed record LimitingHopSummary
{
  [JsonPropertyName("from")]
  public required string From { get; init; }

  [JsonPropertyName("to")]
  public required string To { get; init; }

  [JsonPropertyName("reliability")]
  public required string Reliability { get; init; }

  [JsonPropertyName("reason")]
  public required string Reason { get; init; }

  /// <summary>
  /// Picks the worst hop the SAME way <see cref="PortCycle.LimitingHop"/> does (highest edge tier,
  /// tie-broken by FromLabel ordinal) — reduced from the already-projected <see cref="HopDiagnostic"/>
  /// list rather than re-touching the engine, so this stays a pure, mechanical, byte-for-byte-derived
  /// projection of <see cref="ComboDiagnostics"/>.
  /// </summary>
  public static LimitingHopSummary? FromWorst(IReadOnlyList<HopDiagnostic> edges)
  {
    if (edges.Count == 0)
      return null;

    var worst = edges
      .OrderByDescending(e => TierRank(e.EdgeTier))
      .ThenBy(e => e.FromLabel, StringComparer.Ordinal)
      .First();

    return new LimitingHopSummary
    {
      From = worst.FromLabel,
      To = worst.ToLabel,
      Reliability = worst.Reliability,
      Reason = worst.Reason,
    };
  }

  private static int TierRank(string tier) =>
    tier switch
    {
      "Green" => 0,
      "Amber" => 1,
      "Red" => 2,
      _ => -1,
    };
}

/// <summary>
/// The MECHANICALLY-derived (never hand-typed) "expected" block pinned per combo in
/// <c>combo-expected-tiers.json</c> — a reduced projection of the live run's <see cref="ComboDiagnostics"/>
/// carrying just the fields that distinguish WHY a combo landed at its tier (the §8 verdict + the single
/// limiting hop), not the full edge trail (that stays in the live run / <c>--explain</c> output; baking
/// the whole trail into the pin file would make every parser/engine refactor touch dozens of hand-reviewed
/// pins even when the VERDICT didn't change).
/// <para>
/// <see cref="ComboExpectedTierTest"/> asserts the live run's <c>ComboDiagnostics</c>, reduced through
/// <see cref="FromDiagnostics"/>, structurally equals the pinned <c>expected</c> block — the assertion
/// that catches "stayed Amber but the REASON changed", invisible under a tier-only check.
/// </para>
/// </summary>
public sealed record ExpectedDiagnostics
{
  [JsonPropertyName("limitingReason")]
  public required string LimitingReason { get; init; }

  [JsonPropertyName("firable")]
  public required bool Firable { get; init; }

  [JsonPropertyName("coCostsSatisfied")]
  public required bool CoCostsSatisfied { get; init; }

  [JsonPropertyName("balanced")]
  public required bool Balanced { get; init; }

  [JsonPropertyName("lifeBalanced")]
  public required bool LifeBalanced { get; init; }

  [JsonPropertyName("productive")]
  public required bool Productive { get; init; }

  [JsonPropertyName("limitingHop")]
  public required LimitingHopSummary? LimitingHop { get; init; }

  /// <summary>The ONLY way an <c>expected</c> block is produced — mechanically, from a live run.</summary>
  public static ExpectedDiagnostics FromDiagnostics(ComboDiagnostics d) =>
    new()
    {
      LimitingReason = d.LimitingReason,
      Firable = d.Firable,
      CoCostsSatisfied = d.CoCostsSatisfied,
      Balanced = d.Balanced,
      LifeBalanced = d.LifeBalanced,
      Productive = d.Productive,
      LimitingHop = LimitingHopSummary.FromWorst(d.Edges),
    };
}
