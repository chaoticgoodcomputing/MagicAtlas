using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A resource-family "station" on the metro map (dataset D2, <c>resource-graph.json</c> stations):
/// how many in-scope cards touch the family and how many distinct port labels it spans. Hue and metro
/// coordinates stay client-side (presentation); the API ships family identity and stats.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A resource-family station: card count and distinct label count for one family."
)]
[Table("resource_families", Schema = "atlas")]
public class ResourceFamilyRow
{
    [Key]
    [Column("family")]
    public string Family { get; set; } = "";

    [Column("cards")]
    public int Cards { get; set; }

    [Column("labels")]
    public int Labels { get; set; }
}
