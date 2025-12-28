using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Related card component types.
/// </summary>
public enum RelatedCardComponent
{
  [SerializedEnum("token")]
  Token,

  [SerializedEnum("meld_part")]
  MeldPart,

  [SerializedEnum("meld_result")]
  MeldResult,

  [SerializedEnum("combo_piece")]
  ComboPiece,
}
