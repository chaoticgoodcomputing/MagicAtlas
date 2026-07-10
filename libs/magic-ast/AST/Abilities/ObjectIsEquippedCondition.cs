namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "that creature is equipped" — a game-state predicate that holds while a
/// specific back-referenced object currently has an Equipment attached to it
/// (Éowyn, Lady of Rohan: "If that creature is equipped, it gains first strike
/// and vigilance until end of turn instead.").
///
/// <para>
/// CR 702.6 (Equip): an Equipment can become attached to a creature; a creature
/// with an Equipment attached to it is "equipped". This is the attachment-state
/// analogue of the zone-state <see cref="ObjectInZoneCondition"/> and the
/// combat-state <see cref="SourceCombatStateCondition"/>: a boolean predicate on
/// one referenced object's current state, not a filtered count
/// (<see cref="CountCondition"/>) and not a card-type check
/// (<see cref="ObjectHasCardTypeCondition"/>). Reference-not-resolution
/// (ADR 0004): MAST records the printed predicate; the engine reads whether the
/// referenced object has an Equipment attached, MAST does not pre-evaluate it.
/// </para>
///
/// <para>
/// <see cref="Reference"/> names the object being checked — typically
/// <c>{Kind:"It"}</c> ("that creature", the target a preceding effect in the
/// same ability just granted an ability to).
/// </para>
/// </summary>
[ConditionKind("objectIsEquipped")]
public sealed record ObjectIsEquippedCondition : Condition
{
  /// <summary>
  /// The object whose equipped state is being checked — typically
  /// <c>{Kind:"It"}</c> ("that creature" from a preceding grant effect).
  /// </summary>
  public required ObjectReference Reference { get; init; }
}
