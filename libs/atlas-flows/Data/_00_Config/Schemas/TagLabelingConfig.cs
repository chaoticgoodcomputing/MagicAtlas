using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the tag-labeling pipeline (Scryfall-tag centroid filtering, cluster→tag
/// affinity ranking, and Qwen labeling). Materialized at startup from
/// <c>Flowthru:Flows:TagLabeling</c> in <c>appsettings.json</c>. All fields are flat scalars
/// (Flowthru's Python marshaller can't encode nested POCOs as step inputs).
/// </summary>
[FlowthruSchema]
public partial record TagLabelingConfig
{
  /// <summary>
  /// Sanity floor: minimum cards (across all aliases of a canonical) that must appear in OUR
  /// corpus before the canonical gets a centroid. The curation file itself is the primary
  /// filter — this is just a guard against canonicals whose aliases happen to not match any
  /// commander-format cards (e.g. a tag scoped to a non-commander-legal set).
  /// </summary>
  public int ScryfallMinCardCount { get; init; } = 3;

  /// <summary>
  /// Minimum number of single-line tagged cards needed before a canonical self-anchors. Below
  /// this floor we fall back to the curated exemplar centroid (if one exists) or, failing
  /// that, the original card-level "all lines" rule. 5 is conservative — enough to average
  /// out a single oddball, low enough that most populated canonicals can self-anchor.
  /// </summary>
  public int AnchorFloor { get; init; } = 5;

  /// <summary>
  /// Minimum cosine similarity required to attribute a multi-line card's line to one of its
  /// canonicals' anchor centroids. Lines below this threshold are dropped from the centroid
  /// rebuild — they're likely on-card-but-off-topic relative to any of the card's tags.
  /// </summary>
  public double LineAnchorThreshold { get; init; } = 0.5;

  /// <summary>How many tag candidates (combined exemplar + Scryfall) to surface to the labeler
  /// per cluster.</summary>
  public int TopKAffinity { get; init; } = 8;

  /// <summary>Sample size of representative oracle lines included in the labeler prompt per
  /// cluster. Picked by distance to the cluster centroid (closest = most representative).</summary>
  public int MaxSampleLines { get; init; } = 6;

  /// <summary>Ollama model to use for the labeler call. Overrides
  /// <c>Flowthru:Services:Ollama:DefaultModel</c> if non-empty. Empty string = use default.</summary>
  public string LabelerModel { get; init; } = "";

  /// <summary>Additive bonus applied to exemplar-source tag candidates' cosine scores during
  /// affinity ranking. Reflects the curated-intent prior: when an exemplar is "close enough"
  /// to a cluster, prefer it over a noisier Scryfall meta-tag. Default 0.10 — enough to flip
  /// rankings when the exemplar is within ~10 cosine points but not enough to mask large
  /// genuine score gaps. Set to 0 to disable.</summary>
  public double ExemplarBonus { get; init; } = 0.10;

  /// <summary>
  /// Maximum number of canonical attributions emitted by either Pass 2a (scryfall-inference) or
  /// Pass 2b (embedding-inference) for a single line. With K=3, each pass keeps the top-3
  /// highest-cosine canonicals above <see cref="LineAnchorThreshold"/>; the union of the two
  /// passes' outputs may include a line in slightly more than K canonicals overall, but each
  /// pass independently respects the cap. Set per-line rather than per-card so a multi-mechanic
  /// line ("Whenever ~ attacks, draw a card" — attack-trigger + card-draw) survives.
  /// </summary>
  public int TopKInferences { get; init; } = 3;
}
