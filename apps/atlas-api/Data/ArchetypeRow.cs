using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One realized archetype (dataset D3, <c>archetype-catalog.json</c> entries) — a family-signature and
/// its reconstructed combos' rollup. <see cref="Signature"/> is the sorted <see cref="Families"/> join,
/// the stable key.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A realized combo archetype: a family signature with its reconstructed-combo rollup."
)]
[Table("archetypes", Schema = "atlas")]
public class ArchetypeRow
{
    /// <summary>The sorted <see cref="Families"/> join — the stable archetype key.</summary>
    [Key]
    [Column("signature")]
    public string Signature { get; set; } = "";

    [Required]
    [Column("families")]
    public string Families { get; set; } = "";

    [Column("family_count")]
    public int FamilyCount { get; set; }

    /// <summary>Reconstructed combos (D4) with this exact family-signature.</summary>
    [Column("realizing_combos")]
    public int RealizingCombos { get; set; }

    /// <summary>Best certainty tier among the realizing combos (Green &gt; Amber).</summary>
    [Required]
    [Column("best_tier")]
    public string BestTier { get; set; } = "";

    /// <summary>Green-tier fraction among realizing combos (reliability at a glance), 0–1.</summary>
    [Column("green_fraction")]
    public double GreenFraction { get; set; }

    /// <summary>An example piece list from the most-popular realizing combo.</summary>
    [Required]
    [Column("example_cards")]
    public string ExampleCards { get; set; } = "";

    /// <summary>The union of the realizing combos' declared results, "; "-joined.</summary>
    [Required]
    [Column("results")]
    public string Results { get; set; } = "";
}
