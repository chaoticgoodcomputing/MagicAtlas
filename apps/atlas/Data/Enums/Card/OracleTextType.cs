using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Types of oracle text entries for embedding model input.
/// </summary>
public enum OracleTextType
{
  /// <summary>
  /// Full oracle text of the card.
  /// </summary>
  [SerializedEnum("raw")]
  Full,

  /// <summary>
  /// A single-word or comma-separated keyword ability (e.g., "Flying", "Vigilance, trample").
  /// </summary>
  [SerializedEnum("kw")]
  KeywordAbility,

  /// <summary>
  /// A named triggered ability with keyword and effect (e.g., "Landfall — ...").
  /// </summary>
  [SerializedEnum("named_trig")]
  NamedTriggeredAbility,

  /// <summary>
  /// An activated ability with costs and effect.
  /// </summary>
  [SerializedEnum("act")]
  ActivatedAbility,

  /// <summary>
  /// A triggered ability with trigger and effect.
  /// </summary>
  [SerializedEnum("trig")]
  TriggeredAbility,

  /// <summary>
  /// A passive ability or static effect.
  /// </summary>
  [SerializedEnum("pass")]
  PassiveAbility,
}
