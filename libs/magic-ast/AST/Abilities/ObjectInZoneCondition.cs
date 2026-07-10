namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "for as long as it remains exiled" / "as long as it's in your graveyard" — a
/// zone-membership gate on a specific back-referenced object, true while that
/// object continues to occupy the named zone (CR 400.1: "A zone is a place where
/// objects can be during a game."). The structured form of Savvy Trader's "You
/// may play that card for as long as it remains exiled." — the permission is
/// unbounded by time (unlike a "this turn" clock), lasting only while the
/// zone-membership predicate holds (CR 611.2c: the state or condition described
/// by "as long as" is checked continuously).
///
/// <para>
/// <see cref="Reference"/> names the object being checked — typically
/// <see cref="ObjectReferenceKind.It"/> ("that card", the object a sibling effect
/// in the same ability just exiled). Reference-not-resolution (ADR 0004): MAST
/// records the printed gate; the engine reads the object's current zone, MAST
/// does not pre-evaluate it. Distinct from <see cref="CountCondition"/> (which
/// counts objects matching a filter): this gates on a specific back-referenced
/// object's own zone, not a filtered count.
/// </para>
///
/// CR 400.1 (verbatim): "A zone is a place where objects can be during a game.
/// There are normally seven zones: library, hand, battlefield, graveyard, stack,
/// exile, and command." CR 611.2c (verbatim, excerpt): "If the effect is
/// conditional ... the effect stops applying if the condition it's based on stops
/// applying."
/// </summary>
[ConditionKind("objectInZone")]
public sealed record ObjectInZoneCondition : Condition
{
  /// <summary>
  /// The object whose zone membership is being checked — typically
  /// <c>{Kind:"It"}</c> ("that card" from a preceding exile effect).
  /// </summary>
  public required ObjectReference Reference { get; init; }

  /// <summary>The zone the referenced object must remain in for the condition to hold.</summary>
  public required Zone Zone { get; init; }
}
