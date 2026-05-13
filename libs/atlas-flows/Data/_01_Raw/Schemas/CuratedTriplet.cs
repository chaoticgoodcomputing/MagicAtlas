using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Hand-authored hard-negative triplet for fine-tuning the embedding model. Surfaces
/// word-level MTG-mechanical distinctions that template-based extractors are unlikely to
/// catch — for example, <c>"sacrifice another target creature"</c> vs <c>"sacrifice any target
/// creature"</c> (combo-relevant) or <c>"destroy target creature"</c> vs
/// <c>"destroy all creatures"</c> (single-target vs board wipe).
/// </summary>
/// <remarks>
/// Trains a <c>TripletLoss</c>: <see cref="Anchor"/> should be pulled toward <see cref="Positive"/>
/// and pushed away from <see cref="Negative"/> in embedding space.
/// </remarks>
[FlowthruSchema]
public partial record CuratedTriplet
{
  [SerializedLabel("anchor")]
  public required string Anchor { get; init; }

  [SerializedLabel("positive")]
  public required string Positive { get; init; }

  [SerializedLabel("negative")]
  public required string Negative { get; init; }

  /// <summary>Free-text explanation of what mechanical distinction the triplet is teaching.
  /// Not consumed by training — purely human-readable provenance.</summary>
  [SerializedLabel("rationale")]
  public string? Rationale { get; init; }
}
