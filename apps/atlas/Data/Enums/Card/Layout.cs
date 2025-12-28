using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Card layout types from Scryfall.
/// </summary>
public enum Layout
{
  [SerializedEnum("normal")]
  Normal,

  [SerializedEnum("split")]
  Split,

  [SerializedEnum("flip")]
  Flip,

  [SerializedEnum("transform")]
  Transform,

  [SerializedEnum("modal_dfc")]
  ModalDfc,

  [SerializedEnum("meld")]
  Meld,

  [SerializedEnum("leveler")]
  Leveler,

  [SerializedEnum("class")]
  Class,

  [SerializedEnum("case")]
  Case,

  [SerializedEnum("saga")]
  Saga,

  [SerializedEnum("adventure")]
  Adventure,

  [SerializedEnum("mutate")]
  Mutate,

  [SerializedEnum("prototype")]
  Prototype,

  [SerializedEnum("battle")]
  Battle,

  [SerializedEnum("planar")]
  Planar,

  [SerializedEnum("scheme")]
  Scheme,

  [SerializedEnum("vanguard")]
  Vanguard,

  [SerializedEnum("token")]
  Token,

  [SerializedEnum("double_faced_token")]
  DoubleFacedToken,

  [SerializedEnum("emblem")]
  Emblem,

  [SerializedEnum("augment")]
  Augment,

  [SerializedEnum("host")]
  Host,

  [SerializedEnum("art_series")]
  ArtSeries,

  [SerializedEnum("reversible_card")]
  ReversibleCard,
}
