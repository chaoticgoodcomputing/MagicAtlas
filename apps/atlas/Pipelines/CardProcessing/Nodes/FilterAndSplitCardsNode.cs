using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data.Enums.Card;
using MagicAtlas.Helpers;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Filters cards based on configurable criteria and splits them into core data and metadata.
/// </summary>
public static class FilterAndSplitCardsNode
{
  /// <summary>
  /// Configuration options for filtering cards based on various criteria.
  /// </summary>
  public record FilterOptions
  {
    /// <summary>
    /// Filter by format legality. If null, no legality filtering is applied.
    /// </summary>
    /// <remarks>
    /// Supported formats: Standard, Future, Historic, Timeless, Gladiator, Pioneer, Explorer,
    /// Modern, Legacy, Pauper, Vintage, Penny, Commander, Oathbreaker, StandardBrawl, Brawl,
    /// Alchemy, PauperCommander, Duel, OldSchool, Premodern, Predh.
    /// </remarks>
    public string? Format { get; init; }

    /// <summary>
    /// Filter by colors. If null or empty, no color filtering is applied.
    /// </summary>
    /// <remarks>
    /// Uses "contains" logic - cards that contain any of the specified colors will match.
    /// </remarks>
    public List<ManaColor>? Colors { get; init; }

    /// <summary>
    /// Filter by color identity. If null or empty, no color identity filtering is applied.
    /// </summary>
    /// <remarks>
    /// Uses "subset" logic - cards whose color identity is a subset of the specified colors will match.
    /// This is useful for Commander deckbuilding constraints.
    /// </remarks>
    public List<ManaColor>? ColorIdentity { get; init; }

    /// <summary>
    /// Filter by card types. If null or empty, no type filtering is applied.
    /// </summary>
    /// <remarks>
    /// Cards that contain any of the specified types will match (e.g., "Creature", "Instant", "Legendary").
    /// </remarks>
    public List<string>? Types { get; init; }

    /// <summary>
    /// Filter by rarity. If null or empty, no rarity filtering is applied.
    /// </summary>
    public List<Rarity>? Rarities { get; init; }

    /// <summary>
    /// Filter by set code. If null or empty, no set filtering is applied.
    /// </summary>
    public List<string>? Sets { get; init; }

    /// <summary>
    /// Minimum mana value (CMC). If null, no minimum is enforced.
    /// </summary>
    public decimal? MinCmc { get; init; }

    /// <summary>
    /// Maximum mana value (CMC). If null, no maximum is enforced.
    /// </summary>
    public decimal? MaxCmc { get; init; }

    /// <summary>
    /// Exclude digital-only cards. Default is false.
    /// </summary>
    public bool ExcludeDigital { get; init; }

    /// <summary>
    /// Exclude promo cards. Default is false.
    /// </summary>
    public bool ExcludePromo { get; init; }

    /// <summary>
    /// Exclude unreleased cards. Default is false.
    /// </summary>
    public bool ExcludeUnreleased { get; init; }
  }

  /// <summary>
  /// Creates a filter and split function that processes cards based on the specified options.
  /// </summary>
  /// <param name="options">Configuration options controlling the filter behavior.</param>
  /// <returns>
  /// A function that filters cards according to the specified criteria and splits each card
  /// into core data (JSON output) and metadata (Parquet output).
  /// </returns>
  public static Func<
    CardCollection,
    Task<(IEnumerable<CardCoreData>, IEnumerable<CardMetadata>)>
  > Create(FilterOptions options)
  {
    return async (input) =>
    {
      var filteredCards = input.Cards.AsEnumerable();

      // Apply format legality filter
      if (!string.IsNullOrWhiteSpace(options.Format))
      {
        filteredCards = filteredCards.Where(card =>
        {
          // Check if card is unreleased (release date in the future)
          bool isUnreleased = card.ReleasedAt > DateTime.Now;

          // If card is unreleased and we're not excluding unreleased cards, include it
          if (isUnreleased && !options.ExcludeUnreleased)
          {
            return true;
          }

          // Otherwise, check format legality
          return IsLegalInFormat(card.Legalities, options.Format);
        });
      }

      // Apply color filter (contains any of the specified colors)
      if (options.Colors?.Count > 0)
      {
        filteredCards = filteredCards.Where(card =>
          card.Colors != null && options.Colors.Any(color => card.Colors.Contains(color))
        );
      }

      // Apply color identity filter (color identity is subset of specified colors)
      if (options.ColorIdentity?.Count > 0)
      {
        filteredCards = filteredCards.Where(card =>
          card.ColorIdentity != null
          && card.ColorIdentity.All(color => options.ColorIdentity.Contains(color))
        );
      }

      // Apply type filter (contains any of the specified types)
      if (options.Types?.Count > 0)
      {
        filteredCards = filteredCards.Where(card =>
          options.Types.Any(type => card.Types.Contains(type))
        );
      }

      // Apply rarity filter
      if (options.Rarities?.Count > 0)
      {
        filteredCards = filteredCards.Where(card => options.Rarities.Contains(card.Rarity));
      }

      // Apply set filter
      if (options.Sets?.Count > 0)
      {
        filteredCards = filteredCards.Where(card =>
          options.Sets.Contains(card.Set, StringComparer.OrdinalIgnoreCase)
        );
      }

      // Apply CMC filters
      if (options.MinCmc.HasValue)
      {
        filteredCards = filteredCards.Where(card => card.Cmc >= options.MinCmc.Value);
      }

      if (options.MaxCmc.HasValue)
      {
        filteredCards = filteredCards.Where(card => card.Cmc <= options.MaxCmc.Value);
      }

      // Apply digital exclusion filter
      if (options.ExcludeDigital)
      {
        filteredCards = filteredCards.Where(card => !card.Digital);
      }

      // Apply promo exclusion filter
      if (options.ExcludePromo)
      {
        filteredCards = filteredCards.Where(card => !card.Promo);
      }

      // Materialize the filtered collection once
      var materializedCards = filteredCards.ToList();

      // Split into core data and metadata
      var coreData = materializedCards.Select(MapToCardCoreData);
      var metadata = materializedCards.Select(MapToCardMetadata);

      return await Task.FromResult((coreData, metadata));
    };
  }

  /// <summary>
  /// Determines if a card is legal in the specified format.
  /// </summary>
  private static bool IsLegalInFormat(Legalities legalities, string format)
  {
    return format.ToLowerInvariant() switch
    {
      "standard" => legalities.Standard,
      "future" => legalities.Future,
      "historic" => legalities.Historic,
      "timeless" => legalities.Timeless,
      "gladiator" => legalities.Gladiator,
      "pioneer" => legalities.Pioneer,
      "explorer" => legalities.Explorer,
      "modern" => legalities.Modern,
      "legacy" => legalities.Legacy,
      "pauper" => legalities.Pauper,
      "vintage" => legalities.Vintage,
      "penny" => legalities.Penny,
      "commander" => legalities.Commander,
      "oathbreaker" => legalities.Oathbreaker,
      "standardbrawl" => legalities.StandardBrawl,
      "brawl" => legalities.Brawl,
      "alchemy" => legalities.Alchemy,
      "paupercommander" => legalities.PauperCommander,
      "duel" => legalities.Duel,
      "oldschool" => legalities.OldSchool,
      "premodern" => legalities.Premodern,
      "predh" => legalities.Predh,
      _ => true, // Unknown format - don't filter
    };
  }

  /// <summary>
  /// Maps a Card to CardCoreData, extracting analysis-relevant fields.
  /// </summary>
  private static CardCoreData MapToCardCoreData(Card card)
  {
    return new CardCoreData
    {
      // Identifiers
      Id = card.Id,

      // Content
      Name = card.Name,
      ReleasedAt = card.ReleasedAt,
      Layout = card.Layout,

      // Properties
      ManaCost = card.ManaCost,
      Cmc = card.Cmc,
      Types = card.Types.ToList(),
      Subtypes = card.Subtypes.ToList(),
      OracleText = string.IsNullOrEmpty(card.OracleText)
        ? card.OracleText
        : TextNormalizer.NormalizeText(card.OracleText),
      Power = card.Power,
      Toughness = card.Toughness,
      Loyalty = card.Loyalty,
      LifeModifier = card.LifeModifier,
      HandModifier = card.HandModifier,

      // Identity
      Colors = card.Colors,
      ColorIdentity = card.ColorIdentity,
      ColorIndicator = card.ColorIndicator,
      Keywords = card.Keywords,
      ProducedMana = card.ProducedMana,
      Rarity = card.Rarity,

      // Legality/Formats
      Legalities = card.Legalities,
      Games = card.Games,
      Reserved = card.Reserved,
      GameChanger = card.GameChanger,

      // Appearance
      Foil = card.Foil,
      Nonfoil = card.Nonfoil,
      Finishes = card.Finishes,
      Oversized = card.Oversized,
      ArtistIds = card.ArtistIds,
      BorderColor = card.BorderColor,
      Frame = card.Frame,
      FrameEffects = card.FrameEffects,
      SecurityStamp = card.SecurityStamp,
      FullArt = card.FullArt,

      // Set
      Set = card.Set,

      // Print
      Digital = card.Digital,
      FlavorText = card.FlavorText,
      FlavorName = card.FlavorName,
      Booster = card.Booster,

      // Meta
      EdhrecRank = card.EdhrecRank,
      PennyRank = card.PennyRank,
      Prices = card.Prices,
    };
  }

  /// <summary>
  /// Maps a Card to CardMetadata, extracting non-analysis metadata fields.
  /// </summary>
  private static CardMetadata MapToCardMetadata(Card card)
  {
    return new CardMetadata
    {
      // Identifiers
      Id = card.Id,
      OracleId = card.OracleId,
      MultiverseIds = card.MultiverseIds,
      MtgoId = card.MtgoId,
      MtgoFoilId = card.MtgoFoilId,
      ArenaId = card.ArenaId,
      TcgplayerId = card.TcgplayerId,
      CardmarketId = card.CardmarketId,

      // Content
      Name = card.Name,
      Lang = card.Lang,
      ReleasedAt = card.ReleasedAt,
      Uri = card.Uri,
      ScryfallUri = card.ScryfallUri,
      Layout = card.Layout,
      HighresImage = card.HighresImage,
      ImageStatus = card.ImageStatus,
      ImageUris = card.ImageUris,
      Artist = card.Artist,
      ArtistIds = card.ArtistIds,
      IllustrationId = card.IllustrationId,

      // Frame and appearance
      BorderColor = card.BorderColor,
      Frame = card.Frame,
      FrameEffects = card.FrameEffects,
      SecurityStamp = card.SecurityStamp,
      FullArt = card.FullArt,

      // Variations
      Promo = card.Promo,
      PromoTypes = card.PromoTypes,
      Reprint = card.Reprint,
      Variation = card.Variation,

      // Set information
      SetId = card.SetId,
      Set = card.Set,
      SetName = card.SetName,
      SetType = card.SetType,
      SetUri = card.SetUri,
      SetSearchUri = card.SetSearchUri,
      ScryfallSetUri = card.ScryfallSetUri,
      RulingsUri = card.RulingsUri,
      PrintsSearchUri = card.PrintsSearchUri,

      // Print details
      CollectorNumber = card.CollectorNumber,
      Digital = card.Digital,
      FlavorText = card.FlavorText,
      FlavorName = card.FlavorName,
      CardBackId = card.CardBackId,
      Textless = card.Textless,
      Booster = card.Booster,
      StorySpotlight = card.StorySpotlight,
      Watermark = card.Watermark,

      // Meta
      EdhrecRank = card.EdhrecRank,
      PennyRank = card.PennyRank,
      Prices = card.Prices,

      // Purchase URIs
      PurchaseUris = card.PurchaseUris,
      RelatedUris = card.RelatedUris,
      Preview = card.Preview,
    };
  }
}
