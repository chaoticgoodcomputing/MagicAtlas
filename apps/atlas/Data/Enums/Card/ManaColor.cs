using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Magic: The Gathering colors (WUBRG).
/// </summary>
public enum ManaColor
{
  [SerializedEnum("W")]
  White,

  [SerializedEnum("U")]
  Blue,

  [SerializedEnum("B")]
  Black,

  [SerializedEnum("R")]
  Red,

  [SerializedEnum("G")]
  Green,
}
