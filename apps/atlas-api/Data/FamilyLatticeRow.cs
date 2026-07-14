using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One edge of the family super/subgroup lattice (the DAG the client currently hardcodes as
/// <c>GROUPS = { death:["sacrifice"], card:["mill"] }</c>). Ships the containment as data so the client
/// need not recompute the transitive "counts-as" closure. Source: the family grammar
/// (<c>libs/mast-interaction/FamilyGrammar.cs</c>).
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A super/subgroup containment edge between two resource families."
)]
[Table("family_lattices", Schema = "atlas")]
public class FamilyLatticeRow
{
    /// <summary>Stable synthetic id, <c>"{family}&gt;{subFamily}"</c>.</summary>
    [Key]
    [Column("id")]
    public string Id { get; set; } = "";

    [Required]
    [Column("family")]
    public string Family { get; set; } = "";

    [Required]
    [Column("sub_family")]
    public string SubFamily { get; set; } = "";
}
