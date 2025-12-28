namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;

/// <summary>
/// Represents the effect text of an instant or sorcery spell.
/// Rule 113.3a: Spell abilities are followed as instructions while resolving.
/// </summary>
public sealed record SpellAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Spell;

  /// <summary>
  /// The effects that occur when this spell resolves.
  /// </summary>
  [JsonPropertyName("effects")]
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Optional instructions that modify how the spell can be cast or resolved.
  /// </summary>
  [JsonPropertyName("instructions")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Instructions { get; init; }
}
