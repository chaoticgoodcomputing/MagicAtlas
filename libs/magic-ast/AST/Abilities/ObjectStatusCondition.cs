namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this artifact is tapped" / "this artifact remains tapped" / "it's untapped" /
/// "this permanent is saddled" — a game-state predicate on a single referenced
/// object's current status or designation.
///
/// <para>
/// The status-state sibling of the zone-state <see cref="ObjectInZoneCondition"/>,
/// the attachment-state <see cref="ObjectIsEquippedCondition"/>, and the
/// combat-state <see cref="SourceCombatStateCondition"/>: a boolean predicate on one
/// referenced object's current state, not a filtered count
/// (<see cref="CountCondition"/>) and not a card-type check
/// (<see cref="ObjectHasCardTypeCondition"/>). Reference-not-resolution (ADR 0004):
/// MAST records the printed predicate exactly (the status word as written); the
/// engine reads the object's actual status, MAST does not pre-evaluate it. The
/// <see cref="Condition"/>-side analogue of the <see cref="ObjectFilter"/>-axis
/// <c>TappedStateCharacteristic</c> ("tapped creature") — this gates an ability on a
/// specific object's status, that selects other objects by status.
/// </para>
///
/// <para>
/// <see cref="Reference"/> names the object being checked, printed as written:
/// "this artifact"/"this permanent"/"this land" → <see cref="ObjectReferenceKind.Self"/>
/// (the ability's own source), the bare pronoun "it's" → <see cref="ObjectReferenceKind.It"/>
/// (a back-reference to a previously mentioned object).
/// </para>
///
/// <para>
/// <see cref="Status"/> carries the printed status/designation. Tapped and Untapped
/// are the two values of a permanent's tapped status (CR 110.6: "A permanent is
/// either tapped or untapped."); Saddled is a designation a Mount gains from its
/// Saddle ability (CR 702.166) — both are recorded as first-class status values so
/// the printed word ("untapped", not "not tapped") survives verbatim.
/// </para>
///
/// CR 110.6 (verbatim): "A permanent is either tapped or untapped." CR 702.166a
/// (Saddle, excerpt): a Mount becomes "saddled" when its saddle ability resolves;
/// "saddled" is a designation checked by the Mount's other abilities.
/// </summary>
[ConditionKind("objectStatus")]
public sealed record ObjectStatusCondition : Condition
{
  /// <summary>
  /// The object whose status is being checked — <c>{Kind:"Self"}</c> for "this
  /// artifact"/"this permanent"/"this land", <c>{Kind:"It"}</c> for the bare "it's".
  /// </summary>
  public required ObjectReference Reference { get; init; }

  /// <summary>
  /// The status or designation the referenced object must currently have —
  /// <see cref="ObjectStatus.Tapped"/> for "is/remains tapped",
  /// <see cref="ObjectStatus.Untapped"/> for "it's untapped",
  /// <see cref="ObjectStatus.Saddled"/> for "is saddled".
  /// </summary>
  public required ObjectStatus Status { get; init; }
}

/// <summary>
/// The status or designation an <see cref="ObjectStatusCondition"/> checks for.
/// Tapped/Untapped are the two values of a permanent's tapped status (CR 110.6);
/// Saddled is a Mount designation (CR 702.166). Recorded as written
/// (reference-not-resolution, ADR 0004): "untapped" is its own value, not the
/// negation of Tapped.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObjectStatus
{
  /// <summary>Tapped (CR 110.6a) — "this artifact is tapped", "remains tapped".</summary>
  Tapped,

  /// <summary>Untapped (CR 110.6) — "it's untapped".</summary>
  Untapped,

  /// <summary>Saddled (CR 702.166) — "this permanent is saddled". A Mount designation, not a status.</summary>
  Saddled,
}
