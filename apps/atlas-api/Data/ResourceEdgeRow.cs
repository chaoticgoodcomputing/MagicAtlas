using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A directed line between resource-family stations (dataset D2, <c>resource-graph.json</c> lines),
/// realized by ≥1 reconstructed combo. <see cref="Origin"/> distinguishes the metro's plain-rail
/// (grammar-derived) from card-◆ (realized by a specific card) edges; it defaults to "" until the
/// rollup emits it.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A directed line between two resource families, realized by reconstructed combos."
)]
[Table("resource_edges", Schema = "atlas")]
public class ResourceEdgeRow
{
    /// <summary>Stable synthetic id, <c>"{from}&gt;{to}"</c>.</summary>
    [Key]
    [Column("id")]
    public string Id { get; set; } = "";

    [Required]
    [Column("from_family")]
    public string FromFamily { get; set; } = "";

    [Required]
    [Column("to_family")]
    public string ToFamily { get; set; } = "";

    /// <summary>Reconstructed combos (D4) whose ring traverses this family hop.</summary>
    [Column("realizing_combos")]
    public int RealizingCombos { get; set; }

    /// <summary>Best certainty tier among the realizing combos (Green &gt; Amber), "" if none.</summary>
    [Column("best_tier")]
    public string BestTier { get; set; } = "";

    /// <summary>True iff the reverse line (To→From) is also realized — a bidirectional engine.</summary>
    [Column("engine")]
    public bool Engine { get; set; }

    /// <summary><c>rules</c> (grammar-derived) or <c>card</c> (realized by a specific card); "" until emitted.</summary>
    [Column("origin")]
    public string? Origin { get; set; }
}
