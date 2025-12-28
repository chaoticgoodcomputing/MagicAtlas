using Flowthru.Abstractions;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Opinionated metadata for a Magic: The Gathering card. This represents data that I've
/// personally determined is not likely material for analysis, but is useful to retain
/// </summary>
public record CardMetadata : IStructuredSerializable
{
  // =====================
  // MARK: IDENTIFIERS
  // =====================

  /// <summary>
  /// A unique ID for this card in Scryfall's database.
  /// </summary>
  public required Guid Id { get; init; }

  /// <summary>
  /// A unique ID for this card's oracle identity. This value is consistent across reprinted card editions,
  /// and unique among different cards with the same name (tokens, Unstable variants, etc).
  /// </summary>
  /// <remarks>
  /// Always present except for the reversible_card layout where it will be absent; oracle_id will be found on each face instead.
  /// </remarks>
  public Guid? OracleId { get; init; }

  /// <summary>
  /// This card's multiverse IDs on Gatherer, if any.
  /// </summary>
  /// <remarks>
  /// Note that Scryfall includes many promo cards, tokens, and other esoteric objects that do not have these identifiers.
  /// </remarks>
  public List<int>? MultiverseIds { get; init; }

  /// <summary>
  /// This card's Magic Online ID (also known as the Catalog ID), if any.
  /// </summary>
  /// <remarks>
  /// A large percentage of cards are not available on Magic Online and do not have this ID.
  /// </remarks>
  public int? MtgoId { get; init; }

  /// <summary>
  /// This card's foil Magic Online ID (also known as the Catalog ID), if any.
  /// </summary>
  /// <remarks>
  /// A large percentage of cards are not available on Magic Online and do not have this ID.
  /// </remarks>
  public int? MtgoFoilId { get; init; }

  /// <summary>
  /// This card's Arena ID, if any.
  /// </summary>
  /// <remarks>
  /// A large percentage of cards are not available on Arena and do not have this ID.
  /// </remarks>
  public int? ArenaId { get; init; }

  /// <summary>
  /// This card's ID on TCGplayer's API, also known as the productId.
  /// </summary>
  public int? TcgplayerId { get; init; }

  /// <summary>
  /// This card's ID on Cardmarket's API, also known as the idProduct.
  /// </summary>
  public int? CardmarketId { get; init; }

  // =====================
  // MARK: CONTENT
  // =====================

  /// <summary>
  /// The name of this card.
  /// </summary>
  /// <remarks>
  /// If this card has multiple faces, this field will contain both names separated by ␣//␣.
  /// </remarks>
  public required string Name { get; init; }

  /// <summary>
  /// A language code for this printing.
  /// </summary>
  public required string Lang { get; init; }

  /// <summary>
  /// The date this card was first released.
  /// </summary>
  public required DateTime ReleasedAt { get; init; }

  /// <summary>
  /// A link to this card object on Scryfall's API.
  /// </summary>
  public required string Uri { get; init; }

  /// <summary>
  /// A link to this card's permapage on Scryfall's website.
  /// </summary>
  public required string ScryfallUri { get; init; }

  /// <summary>
  /// A code for this card's layout (e.g., normal, split, flip, transform, modal_dfc, etc).
  /// </summary>
  public required Layout Layout { get; init; }

  /// <summary>
  /// True if this card's imagery is high resolution.
  /// </summary>
  public bool HighresImage { get; init; }

  /// <summary>
  /// A computer-readable indicator for the state of this card's image.
  /// </summary>
  /// <remarks>
  /// One of missing, placeholder, lowres, or highres_scan.
  /// </remarks>
  public ImageStatus? ImageStatus { get; init; }

  /// <summary>
  /// An object listing available imagery for this card.
  /// </summary>
  /// <remarks>
  /// See the Card Imagery article on Scryfall for more information.
  /// </remarks>
  public ImageUris? ImageUris { get; init; }

  /// <summary>
  /// The name of the illustrator of this card.
  /// </summary>
  /// <remarks>
  /// Newly spoiled cards may not have this field yet.
  /// </remarks>
  public string? Artist { get; init; }

  /// <summary>
  /// The IDs of the artists that illustrated this card.
  /// </summary>
  /// <remarks>
  /// Newly spoiled cards may not have this field yet.
  /// </remarks>
  public List<Guid>? ArtistIds { get; init; }

  /// <summary>
  /// A unique identifier for the card artwork that remains consistent across reprints.
  /// </summary>
  /// <remarks>
  /// Newly spoiled cards may not have this field yet.
  /// </remarks>
  public Guid? IllustrationId { get; init; }

  // Frame and appearance

  /// <summary>
  /// This card's border color.
  /// </summary>
  /// <remarks>
  /// One of black, white, borderless, silver, or gold.
  /// </remarks>
  public required BorderColor BorderColor { get; init; }

  /// <summary>
  /// This card's frame layout.
  /// </summary>
  public required Frame Frame { get; init; }

  /// <summary>
  /// This card's frame effects, if any.
  /// </summary>
  public List<string>? FrameEffects { get; init; }

  /// <summary>
  /// The security stamp on this card, if any.
  /// </summary>
  /// <remarks>
  /// One of oval, triangle, acorn, circle, arena, or heart.
  /// </remarks>
  public SecurityStamp? SecurityStamp { get; init; }

  /// <summary>
  /// True if this card's artwork is larger than normal.
  /// </summary>
  public bool FullArt { get; init; }

  // =====================
  // MARK: VARIATIONS
  // =====================

  /// <summary>
  /// True if this card is a promotional print.
  /// </summary>
  public bool Promo { get; init; }

  /// <summary>
  /// An array of strings describing what categories of promo cards this card falls into.
  /// </summary>
  public List<string>? PromoTypes { get; init; }

  /// <summary>
  /// True if this card is a reprint.
  /// </summary>
  public bool Reprint { get; init; }

  /// <summary>
  /// Whether this card is a variation of another printing.
  /// </summary>
  public bool Variation { get; init; }

  // =====================
  // MARK: SET
  // =====================

  /// <summary>
  /// This card's Set object UUID.
  /// </summary>
  public required Guid SetId { get; init; }

  /// <summary>
  /// This card's set code.
  /// </summary>
  public required string Set { get; init; }

  /// <summary>
  /// This card's full set name.
  /// </summary>
  public required string SetName { get; init; }

  /// <summary>
  /// The type of set this printing is in.
  /// </summary>
  public required SetType SetType { get; init; }

  /// <summary>
  /// A link to this card's set object on Scryfall's API.
  /// </summary>
  public required string SetUri { get; init; }

  /// <summary>
  /// A link to where you can begin paginating this card's set on the Scryfall API.
  /// </summary>
  public required string SetSearchUri { get; init; }

  /// <summary>
  /// A link to this card's set on Scryfall's website.
  /// </summary>
  public required string ScryfallSetUri { get; init; }

  /// <summary>
  /// A link to this card's rulings list on Scryfall's API.
  /// </summary>
  public required string RulingsUri { get; init; }

  /// <summary>
  /// A link to where you can begin paginating all re/prints for this card on Scryfall's API.
  /// </summary>
  public required string PrintsSearchUri { get; init; }

  // =====================
  // MARK: PRINT
  // =====================

  /// <summary>
  /// This card's collector number.
  /// </summary>
  /// <remarks>
  /// Note that collector numbers can contain non-numeric characters, such as letters or ★.
  /// </remarks>
  public required string CollectorNumber { get; init; }

  /// <summary>
  /// True if this card was only released in a video game.
  /// </summary>
  public bool Digital { get; init; }

  /// <summary>
  /// The flavor text, if any.
  /// </summary>
  public string? FlavorText { get; init; }

  /// <summary>
  /// The just-for-fun name printed on the card (such as for Godzilla series cards).
  /// </summary>
  public string? FlavorName { get; init; }

  /// <summary>
  /// The Scryfall ID for the card back design present on this card.
  /// </summary>
  public Guid? CardBackId { get; init; }

  /// <summary>
  /// True if the card is printed without text.
  /// </summary>
  public bool Textless { get; init; }

  /// <summary>
  /// Whether this card is found in boosters.
  /// </summary>
  public bool Booster { get; init; }

  /// <summary>
  /// True if this card is a Story Spotlight.
  /// </summary>
  public bool StorySpotlight { get; init; }

  /// <summary>
  /// This card's watermark, if any.
  /// </summary>
  public string? Watermark { get; init; }

  // =====================
  // MARK: META
  // =====================

  /// <summary>
  /// This card's overall rank/popularity on EDHREC.
  /// </summary>
  /// <remarks>
  /// Not all cards are ranked.
  /// </remarks>
  public int? EdhrecRank { get; init; }

  /// <summary>
  /// This card's rank/popularity on Penny Dreadful.
  /// </summary>
  /// <remarks>
  /// Not all cards are ranked.
  /// </remarks>
  public int? PennyRank { get; init; }

  // Pricing

  /// <summary>
  /// An object containing daily price information for this card.
  /// </summary>
  /// <remarks>
  /// Includes usd, usd_foil, usd_etched, eur, eur_foil, eur_etched, and tix prices, as strings.
  /// </remarks>
  public Prices Prices { get; init; } = new();

  // =====================
  // MARK: URIS
  // =====================

  /// <summary>
  /// An object providing URIs to this card's listing on other Magic: The Gathering online resources.
  /// </summary>
  public Dictionary<string, string>? RelatedUris { get; init; }

  /// <summary>
  /// An object providing URIs to this card's listing on major marketplaces.
  /// </summary>
  /// <remarks>
  /// Omitted if the card is unpurchaseable.
  /// </remarks>
  public Dictionary<string, string>? PurchaseUris { get; init; }

  // Preview

  /// <summary>
  /// Preview information for this card, if it was previewed before official release.
  /// </summary>
  /// <remarks>
  /// Contains previewed_at date, source_uri, and source name.
  /// </remarks>
  public Preview? Preview { get; init; }
}
