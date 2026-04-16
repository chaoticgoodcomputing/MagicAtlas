using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A card's 2D position in UMAP-reduced oracle-text embedding space.
/// Populated from <c>dumps/atlas-points.json</c>, which the MagicAtlas Flowthru pipeline
/// (OracleEmbedding flow, sentence-transformers + umap-learn) produces.
/// </summary>
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A card's 2D position in oracle-text embedding space (UMAP output)."
)]
[Table("atlas_points", Schema = "atlas")]
public class AtlasPointRow
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("x")]
    public double X { get; set; }

    [Column("y")]
    public double Y { get; set; }

    /// <summary>Ability-type classification of the embedded text (currently always "oracle").</summary>
    [Column("text_type")]
    public string TextType { get; set; } = "oracle";
}
