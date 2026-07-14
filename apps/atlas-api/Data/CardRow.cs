using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A flattened projection of a Scryfall card suitable for GraphQL exposure and indexed querying.
/// Nested data from the original record (legalities, image URIs, prices) is collapsed onto scalar
/// columns; list-like fields (colors, types, keywords) are stored as Postgres jsonb.
/// </summary>
// Read-only public catalog: Trax 1.39+ requires every GraphQL-exposed query
// model to state its auth posture explicitly.
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A Magic: The Gathering card (flattened projection of Scryfall oracle data)."
)]
[Table("cards", Schema = "atlas")]
public class CardRow
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("oracle_id")]
    public Guid? OracleId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = "";

    [Column("lang")]
    public string Lang { get; set; } = "en";

    [Column("released_at", TypeName = "date")]
    public DateTime ReleasedAt { get; set; }

    [Column("scryfall_uri")]
    public string ScryfallUri { get; set; } = "";

    [Column("layout")]
    public string Layout { get; set; } = "normal";

    [Column("mana_cost")]
    public string? ManaCost { get; set; }

    [Column("cmc")]
    public decimal Cmc { get; set; }

    [Column("type_line")]
    public string? TypeLine { get; set; }

    [Column("oracle_text")]
    public string? OracleText { get; set; }

    [Column("power")]
    public string? Power { get; set; }

    [Column("toughness")]
    public string? Toughness { get; set; }

    [Column("loyalty")]
    public string? Loyalty { get; set; }

    [Column("rarity")]
    public string Rarity { get; set; } = "common";

    [Column("colors", TypeName = "jsonb")]
    public List<string> Colors { get; set; } = new();

    [Column("color_identity", TypeName = "jsonb")]
    public List<string> ColorIdentity { get; set; } = new();

    [Column("types", TypeName = "jsonb")]
    public List<string> Types { get; set; } = new();

    [Column("subtypes", TypeName = "jsonb")]
    public List<string> Subtypes { get; set; } = new();

    [Column("keywords", TypeName = "jsonb")]
    public List<string> Keywords { get; set; } = new();

    [Column("games", TypeName = "jsonb")]
    public List<string> Games { get; set; } = new();

    [Column("reserved")]
    public bool Reserved { get; set; }

    [Column("foil")]
    public bool Foil { get; set; }

    [Column("nonfoil")]
    public bool Nonfoil { get; set; }

    [Column("set_code")]
    public string Set { get; set; } = "";

    [Column("set_name")]
    public string SetName { get; set; } = "";

    [Column("set_type")]
    public string SetType { get; set; } = "";

    [Column("collector_number")]
    public string CollectorNumber { get; set; } = "";

    [Column("artist")]
    public string? Artist { get; set; }

    [Column("image_uri_normal")]
    public string? ImageUriNormal { get; set; }

    [Column("image_uri_large")]
    public string? ImageUriLarge { get; set; }

    [Column("image_uri_art_crop")]
    public string? ImageUriArtCrop { get; set; }

    [Column("price_usd")]
    public decimal? PriceUsd { get; set; }

    [Column("price_usd_foil")]
    public decimal? PriceUsdFoil { get; set; }

    [Column("edhrec_rank")]
    public int? EdhrecRank { get; set; }
}
