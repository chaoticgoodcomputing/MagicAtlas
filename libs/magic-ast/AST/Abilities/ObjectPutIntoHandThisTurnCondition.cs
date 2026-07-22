namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "a permanent was put into your hand from the battlefield this turn" — a turn-scoped
/// zone-move history gate. Barrin, Tolarian Archmage's end-step intervening-if (CR 603.4):
/// the draw fires only if at least one object matching <see cref="Filter"/> was moved into a
/// hand (the <see cref="HandOwner"/>'s) from <see cref="FromZone"/> during the current turn —
/// the bounce-history the card's own ETB return feeds.
///
/// <para>
/// The hand-destination sibling of <see cref="MagicAST.AST.References.PutIntoGraveyardThisTurnPredicate"/>
/// (the graveyard-destination move-history the Fraying Sanity family counts). Modelled as a
/// dedicated condition rather than a <see cref="CountCondition"/> over that predicate because
/// this is a yes/no GATE, not a mill COUNT, and the destination is the hand rather than a
/// graveyard. <see cref="Filter"/> describes the moved object as last known on the
/// battlefield (Barrin's is <c>{CardTypes:["permanent"]}</c>, CR 608.2h last-known
/// information); <see cref="FromZone"/> is the origin (Battlefield); <see cref="HandOwner"/>
/// is whose hand it entered (You — "your hand").
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed zone-move history gate; the
/// engine reads whether a matching move happened this turn, MAST does not pre-evaluate it.
/// Structured rather than left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 400.7 (zone changes); CR 402 (the hand zone); CR 514 ("this turn" bounds the window).
/// </summary>
[ConditionKind("objectPutIntoHandThisTurn")]
public sealed record ObjectPutIntoHandThisTurnCondition : Condition
{
  /// <summary>The moved object as last known before the move — Barrin's is <c>{CardTypes:["permanent"]}</c>.</summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>The zone the object was moved FROM — Barrin's is <see cref="Zone.Battlefield"/>.</summary>
  public required Zone FromZone { get; init; }

  /// <summary>Whose hand the object entered — Barrin's "your hand" is <see cref="ControllerFilter.You"/>.</summary>
  public required ControllerFilter HandOwner { get; init; }
}
