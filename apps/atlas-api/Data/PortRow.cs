using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One (card, port) row — the card↔port index (dataset D1, <c>card-ports.json</c>). A card appears once
/// per distinct interaction-port label it projects. <see cref="Side"/> is <c>emit</c> (produces the
/// resource) or <c>consume</c> (a cost/trigger that uses it). The synthetic <see cref="PortId"/> gives the
/// frontend a stable per-port deep-link id; <see cref="CardId"/> joins back to <see cref="CardRow"/>.
/// Oracle provenance (<see cref="OracleLineIndex"/>, <see cref="Spans"/>) and the split fidelity
/// dimensions (<see cref="Conditionality"/>, <see cref="Provenance"/>) are populated once the MAST
/// source-span / statistical-backfill passes land — null/default until then.
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

    /// <summary><b>Conditionality</b> — dimension 1 of the retired four-valued port tier (ADR 0004 #43).
    /// Plain-language answer to "is this mechanism conditional, and how?": <c>fires unconditionally</c>
    /// (the old Green) or a "·"-joined list of the gates that apply (<c>needs to tap</c>,
    /// <c>needs a counter on it</c>, <c>rate-limited</c>). PROVISIONAL copy. <c>""</c> for a backfill row.</summary>
    [Column("conditionality")]
    public string? Conditionality { get; set; }

    /// <summary><b>Provenance</b> — dimension 2 of the retired port tier (ADR 0004 #43), orthogonal to
    /// <see cref="Conditionality"/>. <c>""</c> = parsed (the default); <c>Inferred</c> = statistically
    /// backfilled (carries <see cref="Confidence"/>); <c>Declared</c> = catalogued only.</summary>
    [Column("provenance")]
    public string? Provenance { get; set; }

    /// <summary>Co-occurrence strength (0–1) for an <c>Inferred</c> port; null for parsed and Declared rows.</summary>
    [Column("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Index of the oracle-text line this port was minted from (§4 provenance).</summary>
    [Column("oracle_line_index")]
    public int OracleLineIndex { get; set; }

    /// <summary>Source spans in the oracle text as <c>[[start,end), …]</c> (§4 provenance), null until produced.</summary>
    [Column("spans", TypeName = "jsonb")]
    public int[][]? Spans { get; set; }

    /// <summary>ADR-0003 structured stem (<c>removal:creature</c>, <c>damage</c>, …); null for an
    /// unconverted family or a backfill row.</summary>
    [Column("stem")]
    public string? Stem { get; set; }

    /// <summary>ADR-0003 <c>manner</c> facet (<c>combat</c>/<c>noncombat</c>/<c>sacrificed</c>/<c>blink</c>);
    /// null when the stem carries no manner. The frontend's damage-flow prune keys on this.</summary>
    [Column("manner")]
    public string? Manner { get; set; }

    /// <summary>Whether the port's Subject is self-scoped (<c>this creature</c> — a self-source damage
    /// trigger / self ETB), the engine's same-card guard axis.</summary>
    [Column("is_self")]
    public bool IsSelf { get; set; }
}
