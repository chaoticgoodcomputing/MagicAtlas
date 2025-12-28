using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Card frame versions.
/// </summary>
public enum Frame
{
  [SerializedEnum("1993")]
  Frame1993,

  [SerializedEnum("1997")]
  Frame1997,

  [SerializedEnum("2003")]
  Frame2003,

  [SerializedEnum("2015")]
  Frame2015,

  [SerializedEnum("future")]
  Future,
}
