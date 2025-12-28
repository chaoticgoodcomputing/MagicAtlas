using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Represents a Magic: The Gathering card object from the Scryfall API with strongly-typed processed fields.
/// </summary>
/// <remarks>
/// Card objects represent individual Magic: The Gathering cards that players could obtain and add to their collection.
/// This is a processed version of the raw Scryfall data with enums, parsed types, and nested records for improved type safety.
/// See <see href="https://scryfall.com/docs/api/cards">Scryfall API Card Documentation</see> for complete field definitions.
/// </remarks>
public record Card : IStructuredSerializable
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

  // =====================
  // MARK: PROPERTIES
  // =====================

  /// <summary>
  /// The mana cost for this card.
  /// </summary>
  /// <remarks>
  /// This value will be an empty string "" if the cost is absent. Remember that per the game rules,
  /// a missing mana cost and a mana cost of {0} are different values. Multi-faced cards will report this value in card faces.
  /// </remarks>
  public string? ManaCost { get; init; }

  /// <summary>
  /// The card's mana value (formerly converted mana cost).
  /// </summary>
  /// <remarks>
  /// Note that some funny cards have fractional mana costs.
  /// </remarks>
  public required decimal Cmc { get; init; }

  /// <summary>
  /// The main card types parsed from the type line (e.g., "Legendary", "Creature", "Instant").
  /// </summary>
  /// <remarks>
  /// Parsed from the type line by splitting on em dash (—), en dash (–), or hyphen (-).
  /// Contains the types appearing before the dash separator.
  /// </remarks>
  public HashSet<string> Types { get; init; } = new();

  /// <summary>
  /// The subtypes parsed from the type line (e.g., "Elf", "Druid", "Aura").
  /// </summary>
  /// <remarks>
  /// Parsed from the type line by splitting on em dash (—), en dash (–), or hyphen (-).
  /// Contains the subtypes appearing after the dash separator.
  /// </remarks>
  public HashSet<string> Subtypes { get; init; } = new();

  /// <summary>
  /// The Oracle text for this card, if any.
  /// </summary>
  public string? OracleText { get; init; }

  /// <summary>
  /// This card's power, if any.
  /// </summary>
  /// <remarks>
  /// Note that some cards have powers that are not numeric, such as *.
  /// </remarks>
  public string? Power { get; init; }

  /// <summary>
  /// This card's toughness, if any.
  /// </summary>
  /// <remarks>
  /// Note that some cards have toughnesses that are not numeric, such as *.
  /// </remarks>
  public string? Toughness { get; init; }

  /// <summary>
  /// This loyalty if any.
  /// </summary>
  /// <remarks>
  /// Note that some cards have loyalties that are not numeric, such as X.
  /// </remarks>
  public string? Loyalty { get; init; }

  /// <summary>
  /// This card's life modifier, if it is a Vanguard card.
  /// </summary>
  /// <remarks>
  /// This value will contain a delta, such as +2.
  /// </remarks>
  public string? LifeModifier { get; init; }

  /// <summary>
  /// This card's hand modifier, if it is a Vanguard card.
  /// </summary>
  /// <remarks>
  /// This value will contain a delta, such as -1.
  /// </remarks>
  public string? HandModifier { get; init; }

  /// <summary>
  /// The lit Unfinity attractions lights on this card, if any.
  /// </summary>
  public List<int>? AttractionLights { get; init; }

  // =====================
  // MARK: IDENTITY
  // =====================

  /// <summary>
  /// This card's colors, if the overall card has colors defined by the rules.
  /// </summary>
  /// <remarks>
  /// If null, the colors will be on the card_faces objects instead.
  /// </remarks>
  public List<ManaColor>? Colors { get; init; }

  /// <summary>
  /// This card's color identity for Commander format deckbuilding.
  /// </summary>
  public List<ManaColor>? ColorIdentity { get; init; }

  /// <summary>
  /// The colors in this card's color indicator, if any.
  /// </summary>
  /// <remarks>
  /// A null value for this field indicates the card does not have a color indicator.
  /// </remarks>
  public List<ManaColor>? ColorIndicator { get; init; }

  /// <summary>
  /// An array of keywords that this card uses, such as 'Flying' and 'Cumulative upkeep'.
  /// </summary>
  public List<string>? Keywords { get; init; }

  /// <summary>
  /// Colors of mana that this card could produce.
  /// </summary>
  public List<ManaColor>? ProducedMana { get; init; }

  /// <summary>
  /// This card's rarity.
  /// </summary>
  /// <remarks>
  /// One of common, uncommon, rare, special, mythic, or bonus.
  /// </remarks>
  public required Rarity Rarity { get; init; }

  // =====================
  // MARK: RELATIONSHIPS
  // =====================

  /// <summary>
  /// If this card is closely related to other cards, this property contains Related Card objects.
  /// </summary>
  /// <remarks>
  /// Includes tokens, meld parts, combo pieces, and other related cards that this card references by name or generates.
  /// </remarks>
  public List<RelatedCard>? AllParts { get; init; }

  /// <summary>
  /// An array of Card Face objects, if this card is multifaced.
  /// </summary>
  /// <remarks>
  /// Applies to split, flip, transform, modal DFC, and other multi-face card layouts.
  /// </remarks>
  public List<CardFace>? CardFaces { get; init; }

  // =====================
  // MARK: LEGALITY/FORMATS
  // =====================

  /// <summary>
  /// An object describing the legality of this card across play formats.
  /// </summary>
  /// <remarks>
  /// Possible legalities are legal, not_legal, restricted, and banned.
  /// </remarks>
  public required Legalities Legalities { get; init; }

  /// <summary>
  /// A list of games that this card print is available in.
  /// </summary>
  /// <remarks>
  /// May include paper, arena, and/or mtgo.
  /// </remarks>
  public List<Platform> Games { get; init; } = new();

  /// <summary>
  /// True if this card is on the Reserved List.
  /// </summary>
  public bool Reserved { get; init; }

  /// <summary>
  /// True if this card is on the Commander Game Changer list.
  /// </summary>
  public bool GameChanger { get; init; }

  // =====================
  // MARK: APPEARANCE
  // =====================

  /// <summary>
  /// True if this card can be found in foil finish.
  /// </summary>
  public bool Foil { get; init; }

  /// <summary>
  /// True if this card can be found in non-foil finish.
  /// </summary>
  public bool Nonfoil { get; init; }

  /// <summary>
  /// Computer-readable flags that indicate if this card can come in foil, nonfoil, or etched finishes.
  /// </summary>
  public List<Finish> Finishes { get; init; } = new();

  /// <summary>
  /// True if this card is oversized.
  /// </summary>
  public bool Oversized { get; init; }

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
  public required Prices Prices { get; init; }

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

/// <summary>
/// Collection wrapper for processed Magic: The Gathering cards.
/// </summary>
/// <remarks>
/// Used to aggregate multiple Card objects for storage and processing in the Flowthru pipeline.
/// </remarks>
public record CardCollection : IStructuredSerializable
{
  /// <summary>
  /// The list of processed Card objects in this collection.
  /// </summary>
  public List<Card> Cards { get; init; } = new();
}
