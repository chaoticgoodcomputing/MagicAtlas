using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Card rarity levels.
/// </summary>
public enum Rarity
{
  [SerializedEnum("common")]
  Common,

  [SerializedEnum("uncommon")]
  Uncommon,

  [SerializedEnum("rare")]
  Rare,

  [SerializedEnum("mythic")]
  Mythic,

  [SerializedEnum("special")]
  Special,

  [SerializedEnum("bonus")]
  Bonus,
}
