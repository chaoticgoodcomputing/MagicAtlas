using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One (card, port) row — the card↔port index (dataset D1, <c>card-ports.json</c>). A card appears once
/// per distinct interaction-port label it projects. <see cref="Side"/> is <c>emit</c> (produces the
/// resource) or <c>consume</c> (a cost/trigger that uses it). The synthetic <see cref="PortId"/> gives the
/// frontend a stable per-port deep-link id; <see cref="CardId"/> joins back to <see cref="CardRow"/>.
/// Oracle provenance (<see cref="OracleLineIndex"/>, <see cref="Spans"/>) and <see cref="Tier"/> are
/// populated once the MAST source-span / statistical-backfill passes land — null/default until then.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "One interaction port a card projects (emit or consume) into a resource family."
)]
[Table("ports", Schema = "atlas")]
public class PortRow
{
    /// <summary>Stable synthetic id, <c>"{card-slug}#{index}"</c>.</summary>
    [Key]
    [Column("id")]
    public string PortId { get; set; } = "";

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Required]
    [Column("card")]
    public string Card { get; set; } = "";

    [Required]
    [Column("label")]
    public string Label { get; set; } = "";

    [Required]
    [Column("family")]
    public string Family { get; set; } = "";

    /// <summary><c>emit</c> (producer) or <c>consume</c> (cost/trigger).</summary>
    [Column("side")]
    public string Side { get; set; } = "";

    /// <summary>Green | Amber | Inferred | Declared (from the backfill pass; "" until produced).</summary>
    [Column("tier")]
    public string? Tier { get; set; }

    /// <summary>Co-occurrence strength (0–1) for an <c>Inferred</c> port; null for parsed (Green/Amber)
    /// and Declared rows.</summary>
    [Column("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Index of the oracle-text line this port was minted from (§4 provenance).</summary>
    [Column("oracle_line_index")]
    public int OracleLineIndex { get; set; }

    /// <summary>Source spans in the oracle text as <c>[[start,end), …]</c> (§4 provenance), null until produced.</summary>
    [Column("spans", TypeName = "jsonb")]
    public int[][]? Spans { get; set; }
}
