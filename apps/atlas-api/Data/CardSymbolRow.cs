using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// A mana / card symbol from Scryfall's <c>/symbology</c> endpoint — e.g., <c>{W}</c>, <c>{2/U}</c>, <c>{T}</c>.
/// The symbol key is the primary key; clients look up SVG URIs for rendering mana costs as pips.
/// </summary>
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "A card or mana symbol with its SVG rendering URI."
)]
[Table("card_symbols", Schema = "atlas")]
public class CardSymbolRow
{
    /// <summary>The token as it appears in mana cost strings, e.g. <c>{W}</c> or <c>{2/U}</c>.</summary>
    [Key]
    [Column("symbol")]
    public string Symbol { get; set; } = "";

    [Column("svg_uri")]
    public string SvgUri { get; set; } = "";

    [Column("english")]
    public string English { get; set; } = "";

    [Column("transposable")]
    public bool Transposable { get; set; }

    [Column("represents_mana")]
    public bool RepresentsMana { get; set; }

    [Column("appears_in_mana_costs")]
    public bool AppearsInManaCosts { get; set; }

    [Column("mana_value")]
    public decimal? ManaValue { get; set; }

    [Column("hybrid")]
    public bool Hybrid { get; set; }

    [Column("phyrexian")]
    public bool Phyrexian { get; set; }

    [Column("colors", TypeName = "jsonb")]
    public List<string> Colors { get; set; } = new();
}
