namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents the effect text of an instant or sorcery spell.
/// Rule 113.3a: Spell abilities are followed as instructions while resolving.
/// </summary>
[OracleAbility("spell")]
public sealed record SpellAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Spell;

  /// <summary>
  /// The effects that occur when this spell resolves.
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Optional instructions that modify how the spell can be cast or resolved.
  /// </summary>
  [FreeTextField]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Instructions { get; init; }
}
