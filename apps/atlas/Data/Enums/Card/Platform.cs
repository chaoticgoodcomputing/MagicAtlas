using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// MTG platforms/games where cards are available.
/// </summary>
public enum Platform
{
  [SerializedEnum("paper")]
  Paper,

  [SerializedEnum("mtgo")]
  Mtgo,

  [SerializedEnum("arena")]
  Arena,

  [SerializedEnum("astral")]
  Astral,

  [SerializedEnum("sega")]
  Sega,
}
