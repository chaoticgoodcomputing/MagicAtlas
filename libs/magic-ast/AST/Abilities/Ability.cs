namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization;

/// <summary>
/// Base type for all ability nodes in the AST.
/// Abilities represent the functional components of card text.
/// </summary>
[PolymorphicBase("Kind")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Ability>))]
public abstract record Ability
{
  /// <summary>
  /// The category of this ability.
  /// Note: Not serialized - the polymorphic "kind" discriminator provides this information.
  /// </summary>
  [JsonIgnore]
  public abstract AbilityKind AbilityKind { get; }

  /// <summary>
  /// Optional ability word prefix (e.g., \"Landfall\", \"Enrage\", \"Revolt\").
  /// Ability words have no rules meaning but tie together similar abilities.
  /// Rule 207.2c
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? AbilityWord { get; init; }

  /// <summary>
  /// If this ability was expanded from a keyword, the keyword's typed identity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public KeywordAbility? KeywordSource { get; init; }

  /// <summary>
  /// Optional parenthetical reminder text associated with this ability.
  /// Rule 207.2: Reminder text has no rules meaning but aids comprehension.
  /// Preserved for round-tripping, display, and educational purposes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Parenthetical? Reminder { get; init; }
}
