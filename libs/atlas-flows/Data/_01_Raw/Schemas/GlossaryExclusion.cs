using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Names of glossary entries to drop before constructing training pairs. Useful for obsolete
/// keywords (e.g. <c>"Ante"</c>, <c>"Conspiracy"</c>) whose definitions would only add noise to
/// the fine-tune corpus, or for ability words / casual-vocabulary entries that don't carry
/// mechanical weight.
/// </summary>
[FlowthruSchema]
public partial record GlossaryExclusion
{
  /// <summary>Glossary entry name to exclude. Case-insensitive.</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Free-text rationale. Not consumed by training — purely human-readable provenance.</summary>
  [SerializedLabel("reason")]
  public string? Reason { get; init; }
}
