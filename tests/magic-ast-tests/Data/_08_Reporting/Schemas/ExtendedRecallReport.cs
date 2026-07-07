using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// The WIDE reconstruction-recall measurement — the measurement-tier complement to the strict gold
/// bench (<c>tools/bench/MagicAtlas.Bench</c>). The gold bench is a GATE: it reconstructs only combos
/// whose every card has a committed hand-parsed fixture (33 of ~95k), pins each combo's tier, and HALTs
/// on drift — offline, deterministic, CI-safe. This report is the opposite trade: NO pins, NO gate, but
/// a denominator two orders of magnitude larger — <b>every</b> combo whose cards are projection-ready
/// in the current corpus (~thousands). It answers "as coverage grows, is the engine reconstructing more
/// of the combos that actually exist?" — the per-batch progress signal the 33-combo gate is too small
/// to show. Produced by the CardAtlas <c>ReconstructCombos</c> pass (same per-combo engine run that
/// builds D4), so it needs the gitignored corpus and only runs where it is present (main, not worktrees).
/// A diagnostic, never a gate.
/// </summary>
[FlowthruSchema]
public partial record ExtendedRecallReport
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>
  /// Denominator: multi-card combos whose EVERY card is projection-ready — the card projects at least
  /// one port and NONE is <c>emit:unparsed</c> (so the engine will actually attempt a reconstruction).
  /// This is a DIFFERENT axis from InteractionTriage's <c>parseReady</c> (every ability parsed), not a
  /// subset of it: a fully-parsed card can still project <c>emit:unparsed</c> (excluded here), while a
  /// partially-parsed card whose unparsed abilities project no port at all can still be projection-ready
  /// — which is why this count (~30k) exceeds the parse-ready count (~7.5k). It is the honest recall
  /// denominator (combos the engine tries), and it moves with both parse AND projection coverage.
  /// </summary>
  [SerializedLabel("projectionReadyCombos")]
  public int ProjectionReadyCombos { get; init; }

  /// <summary>Combos with at least one spanning interaction cycle (best tier Green or Amber).</summary>
  [SerializedLabel("reconstructed")]
  public int Reconstructed { get; init; }

  /// <summary>Combos whose best reconstructed cycle is Green (certified).</summary>
  [SerializedLabel("green")]
  public int Green { get; init; }

  /// <summary>Combos reconstructed only at Amber (conditional — best cycle is Amber).</summary>
  [SerializedLabel("amber")]
  public int Amber { get; init; }

  /// <summary>Projection-ready combos with no spanning cycle — the engine found no interaction.</summary>
  [SerializedLabel("missed")]
  public int Missed { get; init; }

  /// <summary>Green / ProjectionReadyCombos.</summary>
  [SerializedLabel("recallAtGreen")]
  public double RecallAtGreen { get; init; }

  /// <summary>(Green + Amber) / ProjectionReadyCombos.</summary>
  [SerializedLabel("recallAtAmber")]
  public double RecallAtAmber { get; init; }

  /// <summary>Total CSB popularity of the projection-ready combos (the value-weighted denominator).</summary>
  [SerializedLabel("projectionReadyPopularityMass")]
  public long ProjectionReadyPopularityMass { get; init; }

  /// <summary>Total CSB popularity of the reconstructed combos.</summary>
  [SerializedLabel("reconstructedPopularityMass")]
  public long ReconstructedPopularityMass { get; init; }

  /// <summary>
  /// Reconstructed ÷ projection-ready popularity mass — does the engine reconstruct the <i>popular</i>
  /// combos, not just the long tail? The value-aligned recall number to watch per batch.
  /// </summary>
  [SerializedLabel("popularityWeightedRecall")]
  public double PopularityWeightedRecall { get; init; }
}
