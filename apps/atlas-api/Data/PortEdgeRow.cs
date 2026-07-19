using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One directed <b>port → port interaction edge</b> from the engine's materialized union
/// (<c>InteractionUnion.Materialize</c> → <c>card-edges.json</c>). The row is <b>denormalized</b> so the
/// whole explorer is a flat filter/sort over one indexed table — no online join or graph traversal
/// (design: <c>docs/design/port-native-graph-api.md</c>). The engine is the single source of truth for
/// which edges exist and their <see cref="Tier"/>; this table just serves them.
///
/// <para>Denormalized columns exist purely so a query can filter/sort without touching another table:
/// <see cref="ToCmc"/>/<see cref="ToColors"/>/<see cref="ToEdhrec"/> are the <em>target</em> card's
/// attributes (deck-building filters), <see cref="Popularity"/> is the relevance sort key
/// (combo-derived; 0 until the back-annotation pass lands), and <see cref="TargetReaches"/> is the
/// <b>second-degree</b> reachability — the resource families the target port's card produces — so the
/// "filter consumers by what they connect to" query is a flat GIN array-overlap, not a live 2-hop join.</para>
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A directed port->port interaction edge (denormalized for flat filter/sort/limit)."
)]
[Table("port_edges", Schema = "atlas")]
public class PortEdgeRow
{
    /// <summary>Synthetic surrogate key (bigserial) — EF's row identity. The NATURAL identity is
    /// <see cref="EdgeId"/> (ADR-0004 Migration Stage 0/1): a hash is regeneration-stable, a bigserial is
    /// not (a reseed assigns different <see cref="Id"/> values in a different order), so anything that
    /// must recognize "the same edge" across two separate seedings (a provenance annotation, a
    /// cross-run diff) keys on <see cref="EdgeId"/>, never this column.</summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>The deterministic edge identity (<c>PortEdge.Id</c>, <c>CardEdgeRow.Id</c> upstream) —
    /// hashed from <c>(fromCard, fromLabel, toCard, toLabel, family)</c>, byte-identical across separate
    /// materializations of the same corpus state. The natural key; see <see cref="Id"/>.</summary>
    [Required]
    [Column("edge_id")]
    public string EdgeId { get; set; } = "";

    [Required]
    [Column("from_card")]
    public string FromCard { get; set; } = "";

    [Required]
    [Column("from_label")]
    public string FromLabel { get; set; } = "";

    /// <summary>The source port's resource family (the engine's classification, denormalized from the ports
    /// index) — so the frontend renders/colors the hop without re-deriving families client-side (no drift).</summary>
    [Column("from_family")]
    public string? FromFamily { get; set; }

    [Required]
    [Column("to_card")]
    public string ToCard { get; set; } = "";

    [Required]
    [Column("to_label")]
    public string ToLabel { get; set; } = "";

    /// <summary>The target port's resource family (engine classification, denormalized).</summary>
    [Column("to_family")]
    public string? ToFamily { get; set; }

    /// <summary>The edge relation from the engine: <c>Flow</c> | <c>Bridge</c> | <c>Modifier</c> |
    /// <c>CardDefined</c> (the <c>PortEdge.Family</c>).</summary>
    [Required]
    [Column("relation")]
    public string Relation { get; set; } = "";

    /// <summary>Certainty tier from the operator: <c>Green</c> | <c>Amber</c> (| <c>Red</c> is pruned upstream).</summary>
    [Column("tier")]
    public string? Tier { get; set; }

    // Both endpoints are denormalized symmetrically so EITHER column can filter/sort by the NEIGHBOUR's
    // attributes: the "what feeds X" column (edges where to_card=X) filters/sorts on the from_* side (the
    // feeder), the "what X feeds" column (from_card=X) on the to_* side (the consumer).

    /// <summary>Denormalized source-card mana value (feeder-side deck-building filter).</summary>
    [Column("from_cmc")]
    public double? FromCmc { get; set; }

    /// <summary>Denormalized source-card EDHREC rank (feeder-side relevance sort).</summary>
    [Column("from_edhrec")]
    public int? FromEdhrec { get; set; }

    /// <summary>Denormalized source-card colors (feeder-side color filter).</summary>
    [Column("from_colors")]
    public string[]? FromColors { get; set; }

    /// <summary>The families the <b>source</b> port's card produces (its emit-side families) — the
    /// feeder-side second-degree reachability, GIN-indexed.</summary>
    [Column("source_reaches")]
    public string[]? SourceReaches { get; set; }

    /// <summary>Denormalized target-card mana value (deck-building filter).</summary>
    [Column("to_cmc")]
    public double? ToCmc { get; set; }

    /// <summary>Denormalized target-card EDHREC rank (lower = more played); the deck-building relevance sort.</summary>
    [Column("to_edhrec")]
    public int? ToEdhrec { get; set; }

    /// <summary>Denormalized target-card colors (WUBRG), a <c>text[]</c> for a flat <c>some</c> filter.</summary>
    [Column("to_colors")]
    public string[]? ToColors { get; set; }

    /// <summary>Combo-derived relevance sort key (max popularity of the combos this edge realizes); 0 until
    /// the back-annotation pass lands.</summary>
    [Column("popularity")]
    public int Popularity { get; set; }

    /// <summary>The second-degree reachability tags — the resource families the <b>target</b> port's card
    /// produces (its emit-side families). GIN-indexed so "filter these consumers by what they connect to"
    /// is a flat array-overlap, not a live 2-hop join.</summary>
    [Column("target_reaches")]
    public string[]? TargetReaches { get; set; }
}
