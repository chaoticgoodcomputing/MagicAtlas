using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A single oracle-text fragment's 2D position in UMAP-reduced BERT embedding space.
/// One card can have multiple rows (one per ability: keyword, triggered, activated, etc.),
/// so the primary key is synthetic and <see cref="CardId"/> is the link back.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A 2D position for one ability fragment of a card in oracle-text embedding space."
)]
[Table("atlas_points", Schema = "atlas")]
public class AtlasPointRow
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Column("x")]
    public double X { get; set; }

    [Column("y")]
    public double Y { get; set; }

    /// <summary>keyword | named_triggered | triggered | activated | passive</summary>
    [Column("text_type")]
    public string TextType { get; set; } = "passive";
}
