using Flowthru.Abstractions;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Opinionated core data for a Magic: The Gathering card. This represents data that I've
/// personally determined might be material for analysis.
/// </summary>
public record CardCoreData : IStructuredSerializable
{
  // =====================
  // MARK: IDENTIFIERS
  // =====================

  /// <summary>
  /// A unique ID for this card in Scryfall's database.
  /// </summary>
  public required Guid Id { get; init; }

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
  /// The date this card was first released.
  /// </summary>
  public required DateTime ReleasedAt { get; init; }

  /// <summary>
  /// A code for this card's layout (e.g., normal, split, flip, transform, modal_dfc, etc).
  /// </summary>
  public required Layout Layout { get; init; }

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
  public List<string> Types { get; init; } = new();

  /// <summary>
  /// The subtypes parsed from the type line (e.g., "Elf", "Druid", "Aura").
  /// </summary>
  /// <remarks>
  /// Parsed from the type line by splitting on em dash (—), en dash (–), or hyphen (-).
  /// Contains the subtypes appearing after the dash separator.
  /// </remarks>
  public List<string> Subtypes { get; init; } = new();

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
  /// The IDs of the artists that illustrated this card.
  /// </summary>
  /// <remarks>
  /// Newly spoiled cards may not have this field yet.
  /// </remarks>
  public List<Guid>? ArtistIds { get; init; }

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
  // MARK: SET
  // =====================

  /// <summary>
  /// This card's set code.
  /// </summary>
  public required string Set { get; init; }

  // =====================
  // MARK: PRINT
  // =====================

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
  /// Whether this card is found in boosters.
  /// </summary>
  public bool Booster { get; init; }

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
}
