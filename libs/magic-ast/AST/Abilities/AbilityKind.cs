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
  /// Ability that could not be parsed. Contains raw text and diagnostics.
  /// </summary>
  Unparsed,
}
