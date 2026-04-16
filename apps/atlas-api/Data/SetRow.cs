using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A Magic set (expansion, core, masters, commander, etc.) from Scryfall's <c>/sets</c> endpoint.
/// </summary>
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A Magic: The Gathering set (expansion, masters, commander deck, etc.)."
)]
[Table("sets", Schema = "atlas")]
public class SetRow
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("code")]
    public string Code { get; set; } = "";

    [Column("mtgo_code")]
    public string? MtgoCode { get; set; }

    [Column("arena_code")]
    public string? ArenaCode { get; set; }

    [Column("tcgplayer_id")]
    public int? TcgplayerId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = "";

    [Column("set_type")]
    public string SetType { get; set; } = "";

    [Column("released_at", TypeName = "date")]
    public DateTime? ReleasedAt { get; set; }

    [Column("block_code")]
    public string? BlockCode { get; set; }

    [Column("block")]
    public string? Block { get; set; }

    [Column("parent_set_code")]
    public string? ParentSetCode { get; set; }

    [Column("card_count")]
    public int CardCount { get; set; }

    [Column("printed_size")]
    public int? PrintedSize { get; set; }

    [Column("digital")]
    public bool Digital { get; set; }

    [Column("foil_only")]
    public bool FoilOnly { get; set; }

    [Column("nonfoil_only")]
    public bool NonfoilOnly { get; set; }

    [Column("scryfall_uri")]
    public string ScryfallUri { get; set; } = "";

    [Column("icon_svg_uri")]
    public string IconSvgUri { get; set; } = "";
}
