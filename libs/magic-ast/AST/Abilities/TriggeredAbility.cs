namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Triggers;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents a triggered ability: "[When/Whenever/At] [trigger condition], [effect]"
/// Rule 113.3c, Rule 603
/// </summary>
[OracleAbility("triggered")]
public sealed record TriggeredAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Triggered;

  /// <summary>
  /// The trigger condition that causes this ability to trigger.
  /// </summary>
  public required TriggerCondition Trigger { get; init; }

  /// <summary>
  /// Optional "intervening if" clause that must be true for the ability to trigger.
  /// Rule 603.4: "When/Whenever/At [trigger event], if [condition], [effect]."
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? InterveningIf { get; init; }

  /// <summary>
  /// The effects that occur when this ability resolves.
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Optional instructions or restrictions on the triggered ability.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Instructions { get; init; }

  /// <summary>
  /// Restrictions on the triggered ability, e.g., "only once each turn".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<TriggeredAbilityRestriction>? Restrictions { get; init; }
}

/// <summary>
/// Restrictions on when or how a triggered ability can resolve.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggeredAbilityRestriction
{
  /// <summary>"Do this only once each turn"</summary>
  OnlyOnceEachTurn,

  /// <summary>"Do this only during your turn"</summary>
  OnlyDuringYourTurn,

  /// <summary>"Do this only if [condition]" - see interveningIf for the condition</summary>
  Conditional,
}

/// <summary>
/// Represents a condition that must be true.
/// Used for intervening if clauses and other conditional checks.
/// </summary>
public sealed record Condition
{
  /// <summary>
  /// The raw text of the condition.
  /// </summary>
  public required string Text { get; init; }

  // TODO: Add structured condition representation as parsing matures
}
