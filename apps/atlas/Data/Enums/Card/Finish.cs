using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Card finish types (foil, nonfoil, etched).
/// </summary>
public enum Finish
{
  [SerializedEnum("nonfoil")]
  NonFoil,

  [SerializedEnum("foil")]
  Foil,

  [SerializedEnum("etched")]
  Etched,
}
