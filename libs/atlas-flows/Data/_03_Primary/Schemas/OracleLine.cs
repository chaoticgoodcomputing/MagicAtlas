using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A single line of oracle text fed to the Python embedding pipeline — one row per ability,
/// not per card. A card with "Flying / When this enters, draw a card. / {T}: add {G}." yields
/// three rows.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline's central join key. Every downstream artifact (encoded vectors, atlas
/// coordinates, cluster assignments, cluster labels) keys on <see cref="LineId"/> and reaches
/// back to <see cref="CardId"/> via this table. <see cref="LineId"/> is a deterministic hash
/// (see <c>ProjectOracleLinesNode</c>), so reruns produce stable identities and the cache plan
/// short-circuits unchanged work.
/// </para>
/// <para>
/// Today all rows carry oracle text only (flavor / printed-name variants are filtered upstream).
/// Synthetic per-keyword lines will live in this same table later and be distinguished only by
/// their stable id construction — no separate source-kind column.
/// </para>
/// <para>
/// Deliberately Arrow-friendly (no <c>decimal</c>, no nested types) because the row ships to a
/// Python subprocess via Apache Arrow.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record OracleLine
{
  /// <summary>
  /// Globally-unique line identifier — the join key for every downstream artifact. Derived
  /// deterministically from <c>(card_id, normalized_text_hash)</c>.
  /// </summary>
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  /// <summary>The Scryfall card id. NOT unique across rows — multiple lines per card.</summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>Scryfall <c>oracle_id</c> — across-printings stable identity copied through from
  /// <see cref="CardCoreData.OracleId"/>. Nullable to match the upstream contract: reversible-
  /// layout cards omit the card-level oracle_id. Carried here so downstream Python steps can
  /// join against Scryfall-side taxonomies (e.g. otag assignments) without a separate
  /// lookup-table input.</summary>
  [SerializedLabel("oracle_id")]
  public Guid? OracleId { get; init; }

  /// <summary>The cleaned ability text (reminder-parentheticals stripped upstream).</summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }
}
