namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Replacement event for the triggering of a triggered ability — "if [a triggered ability
/// of X] triggers, it triggers an additional time."
///
/// <para>
/// This models the trigger-multiplication static ability found on cards like Echoes of
/// Eternity and Panharmonicon. Per CR 603: triggered abilities trigger whenever the event
/// they watch for occurs. When a trigger-multiplying effect is in play, the matching
/// triggering event produces an additional trigger occurrence — i.e. the ability goes on
/// the stack twice (or more). MAST records the <em>source</em> of the watched ability via
/// <see cref="ReplacementEvent.AffectedObjects"/> (the permanent or spell whose triggered
/// ability fires) and the multiplying modifier via
/// <see cref="ReplacementEffect.Modifier"/> (<c>Type:"additionalTime"</c>), while
/// <see cref="ReplacementEffect.OriginalEventOccurs"/> is <c>true</c> (the original
/// trigger still fires once; the additional occurrence is on top of it).
/// </para>
///
/// <para>
/// When the trigger-multiplying effect is scoped to triggers <em>caused by</em> a
/// particular entering object (Panharmonicon: "if an artifact or creature entering
/// causes … to trigger"), <see cref="CausedByEntering"/> carries an
/// <see cref="ObjectFilter"/> for that entering object. Null when the scope is
/// unconditional (Echoes of Eternity) — omitted in serialization so existing cards
/// round-trip unchanged.
/// </para>
///
/// <para>
/// CR 603.2d (verbatim from rules-structure.json):
/// "An ability may state that a triggered ability triggers additional times. In this case,
/// rather than simply determining that such an ability has triggered, determine how many
/// times it should trigger, then that ability triggers that many times."
/// </para>
///
/// <para>
/// CR 603.9 (descriptive reference, verbatim from rules-structure.json):
/// "If a triggered ability is linked to a static ability that prevents it from triggering
/// unless some condition has been met, it won't trigger if that condition hasn't been met."
/// The doubling/additional-time interaction is engine territory; MAST describes the
/// <em>scope</em> of which triggered abilities are multiplied (via the AffectedObjects
/// filter) and the multiplying factor (Modifier.Type).
/// </para>
/// </summary>
[OracleReplacementEvent("abilityTrigger")]
public sealed record AbilityTriggerEvent : ReplacementEvent
{
  /// <summary>
  /// When present, restricts this trigger-multiplication to triggered abilities that were
  /// caused by an object matching this filter entering the battlefield — i.e. the oracle
  /// text reads "if [a filter] entering causes a triggered ability … to trigger." Null
  /// when the scope is unconditional (all matching triggered abilities are doubled,
  /// regardless of what caused them to fire).
  ///
  /// <para>
  /// Example: Panharmonicon — "if an <c>artifact or creature</c> entering causes …" →
  /// <c>CausedByEntering = { CardTypes: ["artifact", "creature"] }</c>. The filter
  /// narrows the universe of triggers affected: only ETB-triggered abilities whose trigger
  /// event was an artifact or creature entering are doubled.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? CausedByEntering { get; init; }
}
