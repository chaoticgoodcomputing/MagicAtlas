using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A ruling (judge/WotC clarification) attached to a card's oracle identity.
/// Rulings share across reprints — they key off <see cref="OracleId"/> rather than printing ID.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A ruling attached to a card's oracle identity (shared across reprints)."
)]
[Table("rulings", Schema = "atlas")]
public class RulingRow
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("oracle_id")]
    public Guid OracleId { get; set; }

    /// <summary>"wotc" or "scryfall".</summary>
    [Column("source")]
    public string Source { get; set; } = "";

    [Column("published_at", TypeName = "date")]
    public DateTime PublishedAt { get; set; }

    [Required]
    [Column("comment")]
    public string Comment { get; set; } = "";
}
