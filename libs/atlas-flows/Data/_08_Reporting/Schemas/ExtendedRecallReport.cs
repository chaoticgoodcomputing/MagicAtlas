using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// The WIDE reconstruction-recall measurement — co-produced with the CardAtlas <c>ReconstructCombos</c>
/// pass (same per-combo engine run that builds D4). It answers "as coverage grows, is the engine
/// reconstructing more of the combos that actually exist?" over EVERY combo whose cards are
/// projection-ready in the current corpus (~thousands), with no pins and no gate — the per-batch
/// progress signal the strict gold bench is too small to show.
///
/// <para>Promoted verbatim from tests/magic-ast-tests/Data/_08_Reporting/Schemas/ExtendedRecallReport.cs.
/// A diagnostic, never a gate.</para>
/// </summary>
[FlowthruSchema]
public partial record ExtendedRecallReport
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>
  /// Denominator: multi-card combos whose EVERY card is projection-ready — the card projects at least
  /// one port and NONE is <c>emit:unparsed</c> (so the engine will actually attempt a reconstruction).
  /// The honest recall denominator (combos the engine tries); it moves with both parse AND projection
  /// coverage.
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
