using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// One row per archetype tag with a mean-pooled embedding centroid. Two sibling catalog items
/// use this schema: one fed by hand-curated <see cref="TagExemplar"/> records (the "config"
/// track), one fed by Scryfall's tagged-card cohorts (the "data" track). Downstream labeling
/// joins cluster centroids against both and uses Qwen to pick the best label given the
/// candidate set.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Embedding"/> is packed as little-endian float16 bytes, matching
/// <c>EncodedText.Embedding</c> so the same numpy-decode path works on both. Parquet-stored
/// because binary embedding blobs in JSON would be unworkable at scale.
/// </para>
/// <para>
/// <see cref="Source"/> distinguishes provenance ("exemplar" or "scryfall") so a downstream
/// labeler can weight tracks differently or filter to one for ablation studies. <see cref="NInputs"/>
/// records how many vectors were averaged — useful for confidence-weighting in the final label
/// arbitration (a centroid built from 500 cards is more reliable than one built from 3).
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record TagCentroid
{
  /// <summary>Canonical slug (matches <see cref="TagExemplar.Slug"/> or the Scryfall otag slug).</summary>
  [SerializedLabel("slug")]
  public required string Slug { get; init; }

  /// <summary>Human-readable display name. Carried through from <see cref="TagExemplar.Name"/>;
  /// for Scryfall centroids the slug-derived form is used (e.g. "Creature Removal" from
  /// "creature-removal").</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Optional natural-language description. Populated for exemplar-track centroids;
  /// for Scryfall-track centroids this may be empty (filled in later by the Qwen labeling step
  /// or a Fandom-distillation pass).</summary>
  [SerializedLabel("description")]
  public required string Description { get; init; }

  /// <summary>Provenance: <c>"exemplar"</c> for config-derived, <c>"scryfall"</c> for
  /// otag-derived. Other values reserved for future tracks.</summary>
  [SerializedLabel("source")]
  public required string Source { get; init; }

  /// <summary>Count of vectors averaged to form this centroid — exemplar count for the config
  /// track, tagged-card count for the Scryfall track.</summary>
  [SerializedLabel("n_inputs")]
  public required int NInputs { get; init; }

  /// <summary>Mean-pooled embedding centroid, packed as little-endian float16 bytes
  /// (dim × 2 bytes). Same encoding as <c>EncodedText.Embedding</c>.</summary>
  [SerializedLabel("embedding")]
  public required byte[] Embedding { get; init; }
}
