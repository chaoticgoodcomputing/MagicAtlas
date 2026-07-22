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

  /// <summary>
  /// The CR 603.4-style intervening "if" condition gating this spell's effect — the
  /// structured home for an ability-word conditional preamble ("Fateful hour — If you
  /// have 5 or less life, draw a card.", CR 207.2c: "Fateful hour" is an ability word
  /// with no rules meaning, so the printed "if" is the whole gate). Mirrors
  /// <see cref="TriggeredAbility.InterveningIf"/>: the spell resolves its
  /// <see cref="Effects"/> only if the condition holds. Structured via
  /// <see cref="MagicAST.Parsing.ConditionParser"/> rather than held verbatim on
  /// <see cref="Instructions"/> — reference-not-resolution (ADR 0004).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? InterveningIf { get; init; }
}
