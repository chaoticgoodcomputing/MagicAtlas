using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// One row per fine-tune health metric, comparing the same scalar measured under the base
/// embedding model vs the fine-tuned model. Produced by <c>evaluate_fine_tune_health</c>.
/// </summary>
/// <remarks>
/// <para>
/// Long-form on purpose so adding metrics doesn't reshape the schema. Two tiers:
/// </para>
/// <list type="bullet">
///   <item><b>geometry</b> — spread/collapse diagnostics over the encoded oracle-line corpus:
///     pair-cosine moments, dissimilar-fraction, hubness. Tests whether fine-tuning collapsed
///     the embedding space uniformly. Source is always <c>"*"</c> (corpus-wide).</item>
///   <item><b>objective</b> — discrimination metrics over the training-pair set:
///     positive_cos_mean, negative_cos_mean, margin_mean. Tests whether fine-tuning achieved
///     the discrimination it was trained for. Source field names the training tier
///     (e.g., <c>"glossary"</c>, <c>"reminder_text"</c>, <c>"template:seed"</c>) so per-source
///     contribution is visible.</item>
/// </list>
/// </remarks>
[FlowthruSchema]
public partial record FineTuneHealthMetric
{
  [SerializedLabel("tier")]
  public required string Tier { get; init; }

  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  /// <summary>Sub-aggregate label. <c>"*"</c> for corpus-wide metrics; for objective-tier
  /// metrics, the source-string from the training pair (e.g. <c>"glossary"</c>,
  /// <c>"reminder_text"</c>, <c>"template:seed"</c>).</summary>
  [SerializedLabel("source")]
  public string Source { get; init; } = "*";

  [SerializedLabel("base_value")]
  public required double BaseValue { get; init; }

  [SerializedLabel("finetuned_value")]
  public required double FineTunedValue { get; init; }

  /// <summary>Sample size the metric was computed over (pairs for objective tier, vectors
  /// for geometry tier). Lets downstream filter out under-powered rows.</summary>
  [SerializedLabel("n")]
  public required int N { get; init; }
}
