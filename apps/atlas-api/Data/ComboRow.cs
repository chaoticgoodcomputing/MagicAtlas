using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A reconstructed combo instance (dataset D4, <c>combo-instances.json</c>) — one row per
/// (parse-ready CSB combo, distinct family-signature cycle). The "shape → buildable" payoff: named
/// cards, certainty tier, and what it does.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A reconstructed combo: named cards, family ring, certainty tier, and produced results."
)]
[Table("combos", Schema = "atlas")]
public class ComboRow
{
    [Key]
    [Column("combo_id")]
    public string ComboId { get; set; } = "";

    /// <summary>The cycle's distinct cards, " + "-joined (the buildable piece list).</summary>
    [Required]
    [Column("cards")]
    public string Cards { get; set; } = "";

    [Column("card_count")]
    public int CardCount { get; set; }

    /// <summary>The sorted distinct canonical families the cycle touches, ", "-joined (the archetype key).</summary>
    [Required]
    [Column("family_signature")]
    public string FamilySignature { get; set; } = "";

    /// <summary>The families in ring order, " → "-joined (the loop's shape).</summary>
    [Required]
    [Column("family_ring")]
    public string FamilyRing { get; set; } = "";

    /// <summary>The engine's cycle-level certainty tier: Green (reliable) / Amber (conditional).</summary>
    [Required]
    [Column("tier")]
    public string Tier { get; set; } = "";

    /// <summary>Whether the loop is firable (no unrenewed gate) — a fast reliability read.</summary>
    [Column("firable")]
    public bool Firable { get; set; }

    /// <summary>What the combo produces, from the CSB variant's declared results, "; "-joined.</summary>
    [Required]
    [Column("results")]
    public string Results { get; set; } = "";

    /// <summary>The CSB popularity signal (build-priority).</summary>
    [Column("popularity")]
    public int Popularity { get; set; }
}
