using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Helpers;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Transforms raw Scryfall cards into processed cards with strong types.
/// Applies enum conversions, date parsing, and text normalization.
/// </summary>
public static class ParseCardsNode
{
  public static Func<IEnumerable<RawScryfallCard>, Task<CardCollection>> Create()
  {
    return async (input) =>
    {
      var cards = input.Select(ParseCard).ToList();
      return await Task.FromResult(new CardCollection { Cards = cards });
    };
  }

  private static Card ParseCard(RawScryfallCard raw)
  {
    return new Card
    {
      // Core identification
      Id = Guid.Parse(raw.Id),
      OracleId = raw.Oracle_Id != null ? Guid.Parse(raw.Oracle_Id) : null,
      MultiverseIds = raw.Multiverse_Ids,
      MtgoId = raw.Mtgo_Id,
      MtgoFoilId = raw.Mtgo_Foil_Id,
      ArenaId = raw.Arena_Id,
      TcgplayerId = raw.Tcgplayer_Id,
      CardmarketId = raw.Cardmarket_Id,

      // Card content
      Name = raw.Name,
      Lang = raw.Lang,
      ReleasedAt = DateTime.Parse(raw.Released_At),
      Uri = raw.Uri,
      ScryfallUri = raw.Scryfall_Uri,
      Layout = EnumExtensions.ParseLayout(raw.Layout),
      HighresImage = raw.Highres_Image,
      ImageStatus = EnumExtensions.ParseImageStatus(raw.Image_Status),
      ImageUris = ParseImageUris(raw.Image_Uris),

      // Gameplay - mana and costs
      ManaCost = raw.Mana_Cost,
      Cmc = raw.Cmc,
      Types = ParseTypes(raw.Type_Line).types,
      Subtypes = ParseTypes(raw.Type_Line).subtypes,
      OracleText = raw.Oracle_Text != null ? TextNormalizer.NormalizeText(raw.Oracle_Text) : null,
      Power = raw.Power,
      Toughness = raw.Toughness,
      Loyalty = raw.Loyalty,
      LifeModifier = raw.Life_Modifier,
      HandModifier = raw.Hand_Modifier,

      // Gameplay - colors and identity
      Colors = EnumExtensions.ParseManaColors(raw.Colors),
      ColorIdentity = EnumExtensions.ParseManaColors(raw.Color_Identity),
      ColorIndicator = EnumExtensions.ParseManaColors(raw.Color_Indicator),
      Keywords = raw.Keywords,
      ProducedMana = EnumExtensions.ParseManaColors(raw.Produced_Mana),

      // Multiface and related cards
      AllParts = ParseRelatedCards(raw.All_Parts),
      CardFaces = ParseCardFaces(raw.Card_Faces),

      // Legalities
      Legalities = ParseLegalities(raw.Legalities),

      // Games and formats
      Games = EnumExtensions.ParsePlatforms(raw.Games),
      Reserved = raw.Reserved,
      GameChanger = raw.Game_Changer,

      // Finishes
      Foil = raw.Foil,
      Nonfoil = raw.Nonfoil,
      Finishes = EnumExtensions.ParseFinishes(raw.Finishes),
      Oversized = raw.Oversized,

      // Promotional and variations
      Promo = raw.Promo,
      PromoTypes = raw.Promo_Types,
      Reprint = raw.Reprint,
      Variation = raw.Variation,

      // Set information
      SetId = Guid.Parse(raw.Set_Id),
      Set = raw.Set,
      SetName = raw.Set_Name,
      SetType = EnumExtensions.ParseSetType(raw.Set_Type),
      SetUri = raw.Set_Uri,
      SetSearchUri = raw.Set_Search_Uri,
      ScryfallSetUri = raw.Scryfall_Set_Uri,
      RulingsUri = raw.Rulings_Uri,
      PrintsSearchUri = raw.Prints_Search_Uri,

      // Print information
      CollectorNumber = raw.Collector_Number,
      Digital = raw.Digital,
      Rarity = EnumExtensions.ParseRarity(raw.Rarity),
      FlavorText = raw.Flavor_Text != null ? TextNormalizer.NormalizeText(raw.Flavor_Text) : null,
      FlavorName = raw.Flavor_Name,
      CardBackId = raw.Card_Back_Id != null ? Guid.Parse(raw.Card_Back_Id) : null,

      // Artist information
      Artist = raw.Artist,
      ArtistIds = raw.Artist_Ids?.Select(Guid.Parse).ToList(),
      IllustrationId = raw.Illustration_Id != null ? Guid.Parse(raw.Illustration_Id) : null,

      // Frame and appearance
      BorderColor = EnumExtensions.ParseBorderColor(raw.Border_Color),
      Frame = EnumExtensions.ParseFrame(raw.Frame),
      FrameEffects = raw.Frame_Effects,
      SecurityStamp = EnumExtensions.ParseSecurityStamp(raw.Security_Stamp),
      FullArt = raw.Full_Art,
      Textless = raw.Textless,
      Booster = raw.Booster,
      StorySpotlight = raw.Story_Spotlight,
      Watermark = raw.Watermark,

      // Rankings
      EdhrecRank = raw.Edhrec_Rank,
      PennyRank = raw.Penny_Rank,

      // Pricing
      Prices = ParsePrices(raw.Prices),

      // URIs
      RelatedUris = raw.Related_Uris,
      PurchaseUris = raw.Purchase_Uris,

      // Preview
      Preview = ParsePreview(raw.Preview),

      // Special mechanics
      AttractionLights = raw.Attraction_Lights,
    };
  }

  private static ImageUris? ParseImageUris(Dictionary<string, string>? raw)
  {
    if (raw == null)
    {
      return null;
    }

    return new ImageUris
    {
      Small = raw.GetValueOrDefault("small"),
      Normal = raw.GetValueOrDefault("normal"),
      Large = raw.GetValueOrDefault("large"),
      Png = raw.GetValueOrDefault("png"),
      ArtCrop = raw.GetValueOrDefault("art_crop"),
      BorderCrop = raw.GetValueOrDefault("border_crop"),
    };
  }

  private static List<RelatedCard>? ParseRelatedCards(List<Dictionary<string, string>>? raw)
  {
    if (raw == null || raw.Count == 0)
    {
      return null;
    }

    return raw.Select(r => new RelatedCard
      {
        Id = Guid.Parse(r["id"]),
        Component = EnumExtensions.ParseRelatedCardComponent(r["component"]),
        Name = r["name"],
        TypeLine = r["type_line"],
        Uri = r["uri"],
      })
      .ToList();
  }

  private static List<CardFace>? ParseCardFaces(List<Dictionary<string, object>>? raw)
  {
    if (raw == null || raw.Count == 0)
    {
      return null;
    }

    return raw.Select(r => new CardFace
      {
        Artist = r.GetValueOrDefault("artist")?.ToString(),
        ArtistId =
          r.ContainsKey("artist_id") && r["artist_id"] != null
            ? Guid.Parse(r["artist_id"].ToString()!)
            : null,
        Cmc = r.ContainsKey("cmc") && r["cmc"] != null ? Convert.ToDecimal(r["cmc"]) : null,
        ColorIndicator = r.ContainsKey("color_indicator")
          ? EnumExtensions.ParseManaColors(
            (r["color_indicator"] as IEnumerable<object>)?.Select(o => o.ToString()!).ToList()
          )
          : null,
        Colors = r.ContainsKey("colors")
          ? EnumExtensions.ParseManaColors(
            (r["colors"] as IEnumerable<object>)?.Select(o => o.ToString()!).ToList()
          )
          : null,
        Defense = r.GetValueOrDefault("defense")?.ToString(),
        FlavorText = r.GetValueOrDefault("flavor_text")?.ToString() is string ft
          ? TextNormalizer.NormalizeText(ft)
          : null,
        IllustrationId =
          r.ContainsKey("illustration_id") && r["illustration_id"] != null
            ? Guid.Parse(r["illustration_id"].ToString()!)
            : null,
        ImageUris = null, // Would need separate parsing for nested dictionary
        Layout =
          r.ContainsKey("layout") && r["layout"] != null
            ? EnumExtensions.ParseLayout(r["layout"].ToString()!)
            : null,
        Loyalty = r.GetValueOrDefault("loyalty")?.ToString(),
        ManaCost = r.GetValueOrDefault("mana_cost")?.ToString() ?? "",
        Name = r.GetValueOrDefault("name")?.ToString() ?? "",
        OracleId =
          r.ContainsKey("oracle_id") && r["oracle_id"] != null
            ? Guid.Parse(r["oracle_id"].ToString()!)
            : null,
        OracleText = r.GetValueOrDefault("oracle_text")?.ToString() is string ot
          ? TextNormalizer.NormalizeText(ot)
          : null,
        Power = r.GetValueOrDefault("power")?.ToString(),
        PrintedName = r.GetValueOrDefault("printed_name")?.ToString(),
        PrintedText = r.GetValueOrDefault("printed_text")?.ToString(),
        PrintedTypeLine = r.GetValueOrDefault("printed_type_line")?.ToString(),
        Toughness = r.GetValueOrDefault("toughness")?.ToString(),
        Types = ParseTypes(r.GetValueOrDefault("type_line")?.ToString() ?? "").types,
        Subtypes = ParseTypes(r.GetValueOrDefault("type_line")?.ToString() ?? "").subtypes,
        Watermark = r.GetValueOrDefault("watermark")?.ToString(),
      })
      .ToList();
  }

  private static Legalities ParseLegalities(Dictionary<string, string> raw)
  {
    return new Legalities
    {
      Standard = EnumExtensions.ParseLegality(raw.GetValueOrDefault("standard")),
      Future = EnumExtensions.ParseLegality(raw.GetValueOrDefault("future")),
      Historic = EnumExtensions.ParseLegality(raw.GetValueOrDefault("historic")),
      Timeless = EnumExtensions.ParseLegality(raw.GetValueOrDefault("timeless")),
      Gladiator = EnumExtensions.ParseLegality(raw.GetValueOrDefault("gladiator")),
      Pioneer = EnumExtensions.ParseLegality(raw.GetValueOrDefault("pioneer")),
      Explorer = EnumExtensions.ParseLegality(raw.GetValueOrDefault("explorer")),
      Modern = EnumExtensions.ParseLegality(raw.GetValueOrDefault("modern")),
      Legacy = EnumExtensions.ParseLegality(raw.GetValueOrDefault("legacy")),
      Pauper = EnumExtensions.ParseLegality(raw.GetValueOrDefault("pauper")),
      Vintage = EnumExtensions.ParseLegality(raw.GetValueOrDefault("vintage")),
      Penny = EnumExtensions.ParseLegality(raw.GetValueOrDefault("penny")),
      Commander = EnumExtensions.ParseLegality(raw.GetValueOrDefault("commander")),
      Oathbreaker = EnumExtensions.ParseLegality(raw.GetValueOrDefault("oathbreaker")),
      StandardBrawl = EnumExtensions.ParseLegality(raw.GetValueOrDefault("standardbrawl")),
      Brawl = EnumExtensions.ParseLegality(raw.GetValueOrDefault("brawl")),
      Alchemy = EnumExtensions.ParseLegality(raw.GetValueOrDefault("alchemy")),
      PauperCommander = EnumExtensions.ParseLegality(raw.GetValueOrDefault("paupercommander")),
      Duel = EnumExtensions.ParseLegality(raw.GetValueOrDefault("duel")),
      OldSchool = EnumExtensions.ParseLegality(raw.GetValueOrDefault("oldschool")),
      Premodern = EnumExtensions.ParseLegality(raw.GetValueOrDefault("premodern")),
      Predh = EnumExtensions.ParseLegality(raw.GetValueOrDefault("predh")),
    };
  }

  private static Prices ParsePrices(Dictionary<string, string?>? raw)
  {
    if (raw == null)
    {
      return new Prices();
    }

    return new Prices
    {
      Usd = ParsePrice(raw.GetValueOrDefault("usd")),
      UsdFoil = ParsePrice(raw.GetValueOrDefault("usd_foil")),
      UsdEtched = ParsePrice(raw.GetValueOrDefault("usd_etched")),
      Eur = ParsePrice(raw.GetValueOrDefault("eur")),
      EurFoil = ParsePrice(raw.GetValueOrDefault("eur_foil")),
      EurEtched = ParsePrice(raw.GetValueOrDefault("eur_etched")),
      Tix = ParsePrice(raw.GetValueOrDefault("tix")),
    };
  }

  private static decimal? ParsePrice(string? value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return null;
    }

    return decimal.TryParse(value, out var result) ? result : null;
  }

  private static Preview? ParsePreview(Dictionary<string, string>? raw)
  {
    if (raw == null || raw.Count == 0)
    {
      return null;
    }

    return new Preview
    {
      PreviewedAt =
        raw.ContainsKey("previewed_at") && !string.IsNullOrEmpty(raw["previewed_at"])
          ? DateTime.Parse(raw["previewed_at"])
          : null,
      SourceUri = raw.GetValueOrDefault("source_uri"),
      Source = raw.GetValueOrDefault("source"),
    };
  }

  private static (HashSet<string> types, HashSet<string> subtypes) ParseTypes(string typeLine)
  {
    // Type line format: "Type1 Type2 — Subtype1 Subtype2" or just "Type1 Type2"
    var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var subtypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(typeLine))
    {
      return (types, subtypes);
    }

    // Split by em dash (—), en dash (–), or hyphen (-) to separate types from subtypes
    var parts = typeLine.Split(new[] { '—', '–', '-' }, 2);

    // Parse main types (before the dash)
    var mainTypes = parts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var type in mainTypes)
    {
      types.Add(type.Trim());
    }

    // Parse subtypes (after the dash, if present)
    if (parts.Length > 1)
    {
      var subTypes = parts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var subtype in subTypes)
      {
        subtypes.Add(subtype.Trim());
      }
    }

    return (types, subtypes);
  }
}
