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
/// CR 603.9 (descriptive reference, verbatim from rules-structure.json):
/// "If a triggered ability is linked to a static ability that prevents it from triggering
/// unless some condition has been met, it won't trigger if that condition hasn't been met."
/// The doubling/additional-time interaction is engine territory; MAST describes the
/// <em>scope</em> of which triggered abilities are multiplied (via the AffectedObjects
/// filter) and the multiplying factor (Modifier.Type).
/// </para>
/// </summary>
[OracleReplacementEvent("abilityTrigger")]
public sealed record AbilityTriggerEvent : ReplacementEvent;
