using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Hand-curated archetype seed: a tag identifier with a natural-language description and a
/// handful of exemplar oracle-line phrasings. Embedded by the tag-labeling pipeline to produce
/// the curated-intent track of cluster anchors, alongside the data-derived Scryfall-tag-centroid
/// track.
/// </summary>
/// <remarks>
/// <para>
/// One row per archetype. The combined <see cref="Description"/> + <see cref="Examples"/> text is
/// what's fed to the encoder — the embedding therefore captures both the abstract definition and
/// the concrete phrasing patterns. Pairs nicely with the Scryfall track: the Scryfall centroid is
/// "what cards Scryfall says are X"; the exemplar centroid is "what an X card sounds like."
/// </para>
/// <para>
/// Schema is intentionally flat (scalars + <see cref="List{T}"/> of strings) so the Arrow
/// marshaller can ship it as a Python step input. No nested POCOs.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record TagExemplar
{
  /// <summary>Canonical slug — kebab-case, matches Scryfall's tagger taxonomy where possible
  /// (e.g. <c>"counterspell"</c>, <c>"creature-removal"</c>, <c>"enters-the-battlefield"</c>).</summary>
  [SerializedLabel("slug")]
  public required string Slug { get; init; }

  /// <summary>Human-readable display name (e.g. <c>"Counterspell"</c>, <c>"Enters the Battlefield"</c>).</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>One-paragraph description of the archetype. Combined with <see cref="Examples"/>
  /// during embedding so the centroid captures both abstract intent and surface phrasing.</summary>
  [SerializedLabel("description")]
  public required string Description { get; init; }

  /// <summary>Representative oracle-line phrasings (3–10 entries). Choose phrasings that span the
  /// archetype — e.g. for "counterspell" include both classic ("Counter target spell") and modal
  /// variations ("Counter target creature spell").</summary>
  [SerializedLabel("examples")]
  public required List<string> Examples { get; init; }
}
