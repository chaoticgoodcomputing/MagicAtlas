namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;

/// <summary>
/// The category of ability as defined by Comprehensive Rules 113.3.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AbilityKind
{
  /// <summary>
  /// Abilities that are followed as instructions while an instant or sorcery spell resolves.
  /// Rule 113.3a
  /// </summary>
  Spell,

  /// <summary>
  /// Abilities written as "[Cost]: [Effect.]"
  /// Rule 113.3b, Rule 602
  /// </summary>
  Activated,

  /// <summary>
  /// Abilities written as "[When/Whenever/At] [trigger condition], [effect]"
  /// Rule 113.3c, Rule 603
  /// </summary>
  Triggered,

  /// <summary>
  /// Abilities written as statements that are simply true.
  /// Rule 113.3d, Rule 604
  /// </summary>
  Static,

  /// <summary>
  /// Modal abilities that offer a choice between effects.
  /// e.g., "Choose one —" or "Choose two —"
  /// </summary>
  Modal,

  /// <summary>
  /// Saga abilities — the chapter-triggered structure on Saga cards.
  /// Rule 715. Each chapter is an implicit triggered ability fired when
  /// the Nth lore counter is added; the saga itself is the container.
  /// </summary>
  Saga,

  /// <summary>
  /// Level-up abilities — the cost-driven stanza structure on Leveler cards.
  /// Rule 711 (historical / leveler). The level-up cost is an activated
  /// ability; the LEVEL N-M stanzas describe the creature's characteristics
  /// at each level.
  /// </summary>
  LevelUp,

  /// <summary>
  /// Class abilities — the level-bar superstructure on "Enchantment — Class"
  /// cards (CR 716, "Class Cards"; AFR). A base ability active at all times
  /// (CR 716.3) plus an ordered series of class level bars, each a keyword
  /// ability that pairs a level-up activation cost with the abilities that
  /// section grants (CR 716.2 / 107.16).
  /// </summary>
  Class,

  /// <summary>
  /// Ability that could not be parsed. Contains raw text and diagnostics.
  /// </summary>
  Unparsed,
}
