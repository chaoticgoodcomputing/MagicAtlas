using Flowthru.Abstractions;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Raw Scryfall card DTO matching API exactly (snake_case, all strings/primitives).
/// Represents the Scryfall card object from oracle-cards.json.
/// </summary>
public record RawScryfallCard : IStructuredSerializable
{
  // Core identification
  [SerializedLabel("object")]
  public string Object { get; init; } = "";

  [SerializedLabel("id")]
  public string Id { get; init; } = "";

  [SerializedLabel("oracle_id")]
  public string? Oracle_Id { get; init; }

  [SerializedLabel("multiverse_ids")]
  public List<int>? Multiverse_Ids { get; init; }

  [SerializedLabel("mtgo_id")]
  public int? Mtgo_Id { get; init; }

  [SerializedLabel("mtgo_foil_id")]
  public int? Mtgo_Foil_Id { get; init; }

  [SerializedLabel("arena_id")]
  public int? Arena_Id { get; init; }

  [SerializedLabel("tcgplayer_id")]
  public int? Tcgplayer_Id { get; init; }

  [SerializedLabel("cardmarket_id")]
  public int? Cardmarket_Id { get; init; }

  // Card content
  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("lang")]
  public string Lang { get; init; } = "";

  [SerializedLabel("released_at")]
  public string Released_At { get; init; } = "";

  [SerializedLabel("uri")]
  public string Uri { get; init; } = "";

  [SerializedLabel("scryfall_uri")]
  public string Scryfall_Uri { get; init; } = "";

  [SerializedLabel("layout")]
  public string Layout { get; init; } = "";

  [SerializedLabel("highres_image")]
  public bool Highres_Image { get; init; }

  [SerializedLabel("image_status")]
  public string? Image_Status { get; init; }

  [SerializedLabel("image_uris")]
  public Dictionary<string, string>? Image_Uris { get; init; }

  [SerializedLabel("mana_cost")]
  public string? Mana_Cost { get; init; }

  [SerializedLabel("cmc")]
  public decimal Cmc { get; init; }

  [SerializedLabel("type_line")]
  public string Type_Line { get; init; } = "";

  [SerializedLabel("oracle_text")]
  public string? Oracle_Text { get; init; }

  [SerializedLabel("power")]
  public string? Power { get; init; }

  [SerializedLabel("toughness")]
  public string? Toughness { get; init; }

  [SerializedLabel("loyalty")]
  public string? Loyalty { get; init; }

  [SerializedLabel("life_modifier")]
  public string? Life_Modifier { get; init; }

  [SerializedLabel("hand_modifier")]
  public string? Hand_Modifier { get; init; }

  [SerializedLabel("colors")]
  public List<string>? Colors { get; init; }

  [SerializedLabel("color_identity")]
  public List<string>? Color_Identity { get; init; }

  [SerializedLabel("color_indicator")]
  public List<string>? Color_Indicator { get; init; }

  [SerializedLabel("keywords")]
  public List<string>? Keywords { get; init; }

  [SerializedLabel("produced_mana")]
  public List<string>? Produced_Mana { get; init; }

  [SerializedLabel("all_parts")]
  public List<Dictionary<string, string>>? All_Parts { get; init; }

  [SerializedLabel("card_faces")]
  public List<Dictionary<string, object>>? Card_Faces { get; init; }

  // Legalities
  [SerializedLabel("legalities")]
  public Dictionary<string, string> Legalities { get; init; } = new();

  // Gameplay
  [SerializedLabel("games")]
  public List<string> Games { get; init; } = new();

  [SerializedLabel("reserved")]
  public bool Reserved { get; init; }

  [SerializedLabel("game_changer")]
  public bool Game_Changer { get; init; }

  [SerializedLabel("foil")]
  public bool Foil { get; init; }

  [SerializedLabel("nonfoil")]
  public bool Nonfoil { get; init; }

  [SerializedLabel("finishes")]
  public List<string> Finishes { get; init; } = new();

  [SerializedLabel("oversized")]
  public bool Oversized { get; init; }

  [SerializedLabel("promo")]
  public bool Promo { get; init; }

  [SerializedLabel("promo_types")]
  public List<string>? Promo_Types { get; init; }

  [SerializedLabel("reprint")]
  public bool Reprint { get; init; }

  [SerializedLabel("variation")]
  public bool Variation { get; init; }

  // Set information
  [SerializedLabel("set_id")]
  public string Set_Id { get; init; } = "";

  [SerializedLabel("set")]
  public string Set { get; init; } = "";

  [SerializedLabel("set_name")]
  public string Set_Name { get; init; } = "";

  [SerializedLabel("set_type")]
  public string Set_Type { get; init; } = "";

  [SerializedLabel("set_uri")]
  public string Set_Uri { get; init; } = "";

  [SerializedLabel("set_search_uri")]
  public string Set_Search_Uri { get; init; } = "";

  [SerializedLabel("scryfall_set_uri")]
  public string Scryfall_Set_Uri { get; init; } = "";

  [SerializedLabel("rulings_uri")]
  public string Rulings_Uri { get; init; } = "";

  [SerializedLabel("prints_search_uri")]
  public string Prints_Search_Uri { get; init; } = "";

  // Print information
  [SerializedLabel("collector_number")]
  public string Collector_Number { get; init; } = "";

  [SerializedLabel("digital")]
  public bool Digital { get; init; }

  [SerializedLabel("rarity")]
  public string Rarity { get; init; } = "";

  [SerializedLabel("flavor_text")]
  public string? Flavor_Text { get; init; }

  [SerializedLabel("flavor_name")]
  public string? Flavor_Name { get; init; }

  [SerializedLabel("card_back_id")]
  public string? Card_Back_Id { get; init; }

  [SerializedLabel("artist")]
  public string? Artist { get; init; }

  [SerializedLabel("artist_ids")]
  public List<string>? Artist_Ids { get; init; }

  [SerializedLabel("illustration_id")]
  public string? Illustration_Id { get; init; }

  [SerializedLabel("border_color")]
  public string Border_Color { get; init; } = "";

  [SerializedLabel("frame")]
  public string Frame { get; init; } = "";

  [SerializedLabel("frame_effects")]
  public List<string>? Frame_Effects { get; init; }

  [SerializedLabel("security_stamp")]
  public string? Security_Stamp { get; init; }

  [SerializedLabel("full_art")]
  public bool Full_Art { get; init; }

  [SerializedLabel("textless")]
  public bool Textless { get; init; }

  [SerializedLabel("booster")]
  public bool Booster { get; init; }

  [SerializedLabel("story_spotlight")]
  public bool Story_Spotlight { get; init; }

  [SerializedLabel("watermark")]
  public string? Watermark { get; init; }

  // Ranking
  [SerializedLabel("edhrec_rank")]
  public int? Edhrec_Rank { get; init; }

  [SerializedLabel("penny_rank")]
  public int? Penny_Rank { get; init; }

  // Pricing
  [SerializedLabel("prices")]
  public Dictionary<string, string?>? Prices { get; init; }

  // URIs
  [SerializedLabel("related_uris")]
  public Dictionary<string, string>? Related_Uris { get; init; }

  [SerializedLabel("purchase_uris")]
  public Dictionary<string, string>? Purchase_Uris { get; init; }

  // Preview information
  [SerializedLabel("preview")]
  public Dictionary<string, string>? Preview { get; init; }

  // Attraction-specific
  [SerializedLabel("attraction_lights")]
  public List<int>? Attraction_Lights { get; init; }
}

/// <summary>
/// Wrapper for Scryfall card list response.
/// </summary>
public record RawScryfallCardList : IStructuredSerializable
{
  [SerializedLabel("object")]
  public string Object { get; init; } = "";

  [SerializedLabel("has_more")]
  public bool Has_More { get; init; }

  [SerializedLabel("data")]
  public List<RawScryfallCard> Data { get; init; } = new();

  [SerializedLabel("next_page")]
  public string? Next_Page { get; init; }
}
