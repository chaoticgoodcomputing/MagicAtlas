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
  /// An additional trigger condition that ALSO fires this ability — the "and" branch
  /// of a compound "When A and whenever B" trigger (e.g., Orcish Bowmasters:
  /// "When this creature enters and whenever an opponent draws a card …").
  ///
  /// <para>
  /// CR 603.2: a triggered ability triggers whenever its event occurs. When oracle
  /// text specifies two disjoint events with "and" or "or", both events can fire
  /// the same ability. <see cref="Trigger"/> carries the primary condition;
  /// <see cref="AdditionalTrigger"/> carries the secondary condition. Both share
  /// the same <see cref="Effects"/> list — that is the defining property of a
  /// compound trigger versus two separate triggered abilities.
  /// </para>
  ///
  /// <para>
  /// Null for the vast majority of triggered abilities that have exactly one trigger
  /// condition. Serialized only when present.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public TriggerCondition? AdditionalTrigger { get; init; }

  /// <summary>
  /// Two or more additional trigger conditions beyond <see cref="AdditionalTrigger"/> —
  /// used when oracle text lists three or more disjoint events on one ability
  /// (e.g. Syr Konrad, the Grim: "Whenever … dies, or … is put into a graveyard …,
  /// or … leaves your graveyard, …"). Items in this list are the 3rd, 4th, … conditions;
  /// <see cref="Trigger"/> is always the 1st and <see cref="AdditionalTrigger"/> the 2nd.
  ///
  /// <para>
  /// CR 603.2: a triggered ability fires whenever ANY of its stated events occurs.
  /// The split across Trigger / AdditionalTrigger / AdditionalTriggers is purely
  /// presentational (the list grew incrementally); semantically all conditions are
  /// peers. Serialized only when present (two-condition abilities continue to use
  /// only Trigger + AdditionalTrigger without any AdditionalTriggers list).
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<TriggerCondition>? AdditionalTriggers { get; init; }

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
  [FreeTextField]
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
