using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Image status for card images on Scryfall.
/// </summary>
public enum ImageStatus
{
  [SerializedEnum("missing")]
  Missing,

  [SerializedEnum("placeholder")]
  Placeholder,

  [SerializedEnum("lowres")]
  LowRes,

  [SerializedEnum("highres_scan")]
  HighResScan,
}
