using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Hand-authored keyword definition merged into the training-pair builder's glossary view. Used
/// for keywords that are missing from the official Comprehensive Rules glossary (e.g. unreleased
/// set leaks, custom-format keywords) or whose CR text is stale relative to current card text.
/// </summary>
/// <remarks>
/// Treated as a higher-priority override: if a curated entry has the same <see cref="Name"/> as
/// a parsed glossary entry, the curated definition wins.
/// </remarks>
[FlowthruSchema]
public partial record CuratedDefinition
{
  /// <summary>Keyword as it appears in oracle text (e.g. <c>"Airbend"</c>). Case-insensitive
  /// match against parsed glossary entries.</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Full definition body — typically a one-paragraph rule summary plus a reminder-text
  /// equivalent. Treated as the positive target for keyword grounding pairs.</summary>
  [SerializedLabel("definition")]
  public required string Definition { get; init; }
}
