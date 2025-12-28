using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Set types from Scryfall.
/// </summary>
public enum SetType
{
  [SerializedEnum("core")]
  Core,

  [SerializedEnum("expansion")]
  Expansion,

  [SerializedEnum("masters")]
  Masters,

  [SerializedEnum("alchemy")]
  Alchemy,

  [SerializedEnum("masterpiece")]
  Masterpiece,

  [SerializedEnum("arsenal")]
  Arsenal,

  [SerializedEnum("from_the_vault")]
  FromTheVault,

  [SerializedEnum("spellbook")]
  Spellbook,

  [SerializedEnum("premium_deck")]
  PremiumDeck,

  [SerializedEnum("duel_deck")]
  DuelDeck,

  [SerializedEnum("draft_innovation")]
  DraftInnovation,

  [SerializedEnum("treasure_chest")]
  TreasureChest,

  [SerializedEnum("commander")]
  Commander,

  [SerializedEnum("planechase")]
  Planechase,

  [SerializedEnum("archenemy")]
  Archenemy,

  [SerializedEnum("vanguard")]
  Vanguard,

  [SerializedEnum("funny")]
  Funny,

  [SerializedEnum("starter")]
  Starter,

  [SerializedEnum("box")]
  Box,

  [SerializedEnum("promo")]
  Promo,

  [SerializedEnum("token")]
  Token,

  [SerializedEnum("memorabilia")]
  Memorabilia,

  [SerializedEnum("minigame")]
  Minigame,
}
