namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.References;

/// <summary>
/// Represents a static ability: a statement that is simply true.
/// Rule 113.3d, Rule 604
/// </summary>
public sealed record StaticAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Static;

  /// <summary>
  /// The continuous effect this static ability creates.
  /// </summary>
  [JsonPropertyName("effect")]
  public required Effect Effect { get; init; }

  /// <summary>
  /// Optional condition for when this static ability applies.
  /// e.g., "as long as you control a Forest"
  /// </summary>
  [JsonPropertyName("condition")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }

  /// <summary>
  /// Which objects this static ability affects.
  /// Null if it affects the object itself or the game in general.
  /// </summary>
  [JsonPropertyName("affectedObjects")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AffectedObjects { get; init; }
}
