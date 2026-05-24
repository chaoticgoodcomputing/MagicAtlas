using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// One row per (Scryfall oracle_id, Scryfall functional tag) pair. Produced by the standalone
/// <c>scripts/scrape_scryfall_tags.py</c> helper from the Scryfall Tagger taxonomy and consumed
/// downstream by the tag-labeling pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OracleId"/> is the Scryfall <c>oracle_id</c> — the across-printings stable card
/// identity. Joins to our <c>CardMetadata.OracleId</c> rather than <c>CardCoreData.Id</c>
/// (which is per-printing).
/// </para>
/// <para>
/// <see cref="TagSlug"/> is the kebab-case Scryfall otag slug (e.g. <c>"counterspell"</c>,
/// <c>"creature-removal"</c>). The same oracle_id appears in N rows when a card has N tags.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record TagAssignment
{
  /// <summary>Stored as a UUID string (matches what pandas/pyarrow write from the scraper).
  /// Downstream consumers compare on string equality with <c>OracleLine.OracleId</c>'s string
  /// form, sidestepping a Guid round-trip.</summary>
  [SerializedLabel("oracle_id")]
  public required string OracleId { get; init; }

  [SerializedLabel("tag_slug")]
  public required string TagSlug { get; init; }
}
