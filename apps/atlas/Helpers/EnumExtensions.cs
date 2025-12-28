using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Helpers;

/// <summary>
/// Extension methods for converting Scryfall API string values to strongly-typed enums.
/// </summary>
public static class EnumExtensions
{
  // ============================================================================
  // ManaColor Extensions
  // ============================================================================

  public static ManaColor? ParseManaColor(string? value)
  {
    return value switch
    {
      "W" => ManaColor.White,
      "U" => ManaColor.Blue,
      "B" => ManaColor.Black,
      "R" => ManaColor.Red,
      "G" => ManaColor.Green,
      _ => null,
    };
  }

  public static List<ManaColor>? ParseManaColors(List<string>? values)
  {
    if (values == null || values.Count == 0)
    {
      return null;
    }

    return values.Select(ParseManaColor).Where(c => c.HasValue).Select(c => c!.Value).ToList();
  }

  // ============================================================================
  // Layout Extensions
  // ============================================================================

  public static Layout ParseLayout(string value)
  {
    return value switch
    {
      "normal" => Layout.Normal,
      "split" => Layout.Split,
      "flip" => Layout.Flip,
      "transform" => Layout.Transform,
      "modal_dfc" => Layout.ModalDfc,
      "meld" => Layout.Meld,
      "leveler" => Layout.Leveler,
      "class" => Layout.Class,
      "case" => Layout.Case,
      "saga" => Layout.Saga,
      "adventure" => Layout.Adventure,
      "mutate" => Layout.Mutate,
      "prototype" => Layout.Prototype,
      "battle" => Layout.Battle,
      "planar" => Layout.Planar,
      "scheme" => Layout.Scheme,
      "vanguard" => Layout.Vanguard,
      "token" => Layout.Token,
      "double_faced_token" => Layout.DoubleFacedToken,
      "emblem" => Layout.Emblem,
      "augment" => Layout.Augment,
      "host" => Layout.Host,
      "art_series" => Layout.ArtSeries,
      "reversible_card" => Layout.ReversibleCard,
      _ => Layout.Normal,
    };
  }

  // ============================================================================
  // Rarity Extensions
  // ============================================================================

  public static Rarity ParseRarity(string value)
  {
    return value switch
    {
      "common" => Rarity.Common,
      "uncommon" => Rarity.Uncommon,
      "rare" => Rarity.Rare,
      "mythic" => Rarity.Mythic,
      "special" => Rarity.Special,
      "bonus" => Rarity.Bonus,
      _ => Rarity.Common,
    };
  }

  // ============================================================================
  // SetType Extensions
  // ============================================================================

  public static SetType ParseSetType(string value)
  {
    return value switch
    {
      "core" => SetType.Core,
      "expansion" => SetType.Expansion,
      "masters" => SetType.Masters,
      "alchemy" => SetType.Alchemy,
      "masterpiece" => SetType.Masterpiece,
      "arsenal" => SetType.Arsenal,
      "from_the_vault" => SetType.FromTheVault,
      "spellbook" => SetType.Spellbook,
      "premium_deck" => SetType.PremiumDeck,
      "duel_deck" => SetType.DuelDeck,
      "draft_innovation" => SetType.DraftInnovation,
      "treasure_chest" => SetType.TreasureChest,
      "commander" => SetType.Commander,
      "planechase" => SetType.Planechase,
      "archenemy" => SetType.Archenemy,
      "vanguard" => SetType.Vanguard,
      "funny" => SetType.Funny,
      "starter" => SetType.Starter,
      "box" => SetType.Box,
      "promo" => SetType.Promo,
      "token" => SetType.Token,
      "memorabilia" => SetType.Memorabilia,
      "minigame" => SetType.Minigame,
      _ => SetType.Core, // Default to Core for unknown types
    };
  }

  // ============================================================================
  // ImageStatus Extensions
  // ============================================================================

  public static ImageStatus? ParseImageStatus(string? value)
  {
    if (string.IsNullOrEmpty(value))
    {
      return null;
    }

    return value.ToLowerInvariant() switch
    {
      "missing" => ImageStatus.Missing,
      "placeholder" => ImageStatus.Placeholder,
      "lowres" => ImageStatus.LowRes,
      "highres_scan" => ImageStatus.HighResScan,
      _ => ImageStatus.Missing,
    };
  } // ============================================================================

  // SecurityStamp Extensions
  // ============================================================================

  public static SecurityStamp? ParseSecurityStamp(string? value)
  {
    return value switch
    {
      "oval" => SecurityStamp.Oval,
      "triangle" => SecurityStamp.Triangle,
      "acorn" => SecurityStamp.Acorn,
      "circle" => SecurityStamp.Circle,
      "arena" => SecurityStamp.Arena,
      "heart" => SecurityStamp.Heart,
      _ => null,
    };
  }

  // ============================================================================
  // Frame Extensions
  // ============================================================================

  public static Frame ParseFrame(string value)
  {
    return value switch
    {
      "1993" => Frame.Frame1993,
      "1997" => Frame.Frame1997,
      "2003" => Frame.Frame2003,
      "2015" => Frame.Frame2015,
      "future" => Frame.Future,
      _ => Frame.Frame2015,
    };
  }

  // ============================================================================
  // BorderColor Extensions
  // ============================================================================

  public static BorderColor ParseBorderColor(string value)
  {
    return value switch
    {
      "black" => BorderColor.Black,
      "white" => BorderColor.White,
      "borderless" => BorderColor.Borderless,
      "silver" => BorderColor.Silver,
      "gold" => BorderColor.Gold,
      _ => BorderColor.Black,
    };
  }

  // ============================================================================
  // Finish Extensions
  // ============================================================================

  public static Finish ParseFinish(string value)
  {
    return value switch
    {
      "foil" => Finish.Foil,
      "nonfoil" => Finish.NonFoil,
      "etched" => Finish.Etched,
      _ => Finish.NonFoil,
    };
  }

  public static List<Finish> ParseFinishes(List<string>? values)
  {
    if (values == null || values.Count == 0)
    {
      return new List<Finish>();
    }

    return values.Select(ParseFinish).ToList();
  }

  // ============================================================================
  // Platform Extensions
  // ============================================================================

  public static Platform ParsePlatform(string value)
  {
    return value switch
    {
      "paper" => Platform.Paper,
      "arena" => Platform.Arena,
      "mtgo" => Platform.Mtgo,
      _ => Platform.Paper,
    };
  }

  public static List<Platform> ParsePlatforms(List<string>? values)
  {
    if (values == null || values.Count == 0)
    {
      return new List<Platform>();
    }

    return values.Select(ParsePlatform).ToList();
  }

  // ============================================================================
  // RelatedCardComponent Extensions
  // ============================================================================

  public static RelatedCardComponent ParseRelatedCardComponent(string value)
  {
    return value switch
    {
      "token" => RelatedCardComponent.Token,
      "meld_part" => RelatedCardComponent.MeldPart,
      "meld_result" => RelatedCardComponent.MeldResult,
      "combo_piece" => RelatedCardComponent.ComboPiece,
      _ => RelatedCardComponent.ComboPiece,
    };
  }

  // ============================================================================
  // Legality Extensions
  // ============================================================================

  public static bool ParseLegality(string? value)
  {
    return value == "legal" || value == "restricted";
  }
}
