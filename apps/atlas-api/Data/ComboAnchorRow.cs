using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Trax.Effect.Attributes;

namespace MagicAtlas.Api.Data;

/// <summary>
/// One unparsed hub card and the combo neighborhood it gates (dataset from
/// <c>combo-anchor-report.json</c> top anchors). Powers the near-miss / "one card away" ranking in the
/// Deck Lens. <see cref="Id"/> is the hub card name (the report's natural key); <see cref="CardId"/>
/// joins back to <see cref="CardRow"/> when the name resolves.
/// </summary>
[TraxAllowAnonymous]
[TraxQueryModel(
    Namespace = GraphQLNamespaces.Atlas,
    Description = "An unparsed hub card ranked by the combo-popularity mass it gates."
)]
[Table("combo_anchors", Schema = "atlas")]
public class ComboAnchorRow
{
    /// <summary>The hub card name — the report's natural key.</summary>
    [Key]
    [Column("id")]
    public string Id { get; set; } = "";

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Required]
    [Column("card")]
    public string Card { get; set; } = "";

    [Column("type_line")]
    public string TypeLine { get; set; } = "";

    /// <summary><c>parser-family</c> | <c>empty-oracle-text</c> | <c>missing-from-corpus</c>.</summary>
    [Column("block_reason")]
    public string BlockReason { get; set; } = "";

    [Column("blocked_combo_count")]
    public int BlockedComboCount { get; set; }

    /// <summary>Combos in which this is the ONLY unparsed card — parse it alone and they reconstruct.</summary>
    [Column("sole_blocker_count")]
    public int SoleBlockerCount { get; set; }

    /// <summary>Sum of the popularity of every combo this hub blocks — the primary ranking key.</summary>
    [Column("popularity_mass")]
    public long PopularityMass { get; set; }

    [Column("max_combo_popularity")]
    public int MaxComboPopularity { get; set; }

    /// <summary>The most popular results the blocked combos produce (e.g. "Infinite ETB").</summary>
    [Column("top_payoffs", TypeName = "jsonb")]
    public List<string> TopPayoffs { get; set; } = new();

    /// <summary>
    /// The co-star neighborhood: cards that appear alongside this hub in the combos it blocks.
    /// Mapped as an EF Core owned collection serialized to the <c>co_stars</c> jsonb column
    /// (see <see cref="AtlasDbContext.OnModelCreating"/>) so nested GraphQL sub-selections project.
    /// </summary>
    public List<ComboCoStarJson> CoStars { get; set; } = new();
}

/// <summary>A card that appears alongside a hub in the combos the hub blocks (jsonb-embedded).</summary>
public sealed class ComboCoStarJson
{
    public string Card { get; set; } = "";
    public int SharedCombos { get; set; }
    public long SharedPopularity { get; set; }

    /// <summary>True if this co-star also doesn't fully parse; false = lights up free once the hub lands.</summary>
    public bool AlsoUnparsed { get; set; }
}
