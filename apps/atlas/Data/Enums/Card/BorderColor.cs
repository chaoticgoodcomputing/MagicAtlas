using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Card border colors.
/// </summary>
public enum BorderColor
{
  [SerializedEnum("black")]
  Black,

  [SerializedEnum("white")]
  White,

  [SerializedEnum("borderless")]
  Borderless,

  [SerializedEnum("silver")]
  Silver,

  [SerializedEnum("gold")]
  Gold,
}
