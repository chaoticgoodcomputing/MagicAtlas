namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you discarded this card this turn" — the Mayhem gate (CR 702.187): a backward-looking,
/// turn-scoped check on whether the referenced card was DISCARDED by you during the current
/// turn. Mayhem lets you cast a card from your graveyard for an alternative cost "if you
/// discarded it this turn" (Electro's Bolt: "Mayhem {1}{R} (You may cast this card from your
/// graveyard for {1}{R} if you discarded it this turn.)"). The gate reads the per-turn discard
/// history of the source card to decide whether the alternative cast is available.
///
/// <para>
/// <see cref="Reference"/> names the discarded object, printed as written — "this card" →
/// <see cref="ObjectReferenceKind.Self"/> (the ability's own source), preserving the pronoun
/// rather than resolving it (reference-not-resolution, ADR 0004). It is a field (not implicit
/// Self) to mirror the pronoun-preserving convention of the sibling source-state conditions
/// (<see cref="ObjectStatusCondition"/>, <see cref="ObjectIsEquippedCondition"/>), leaving room
/// for a bare "it" back-reference form. The engine reads whether the card was discarded this
/// turn; MAST records the printed gate. Structured to this dedicated <see cref="Condition"/>
/// arm rather than left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 702.187a (excerpt): "Mayhem [cost] means 'You may cast this card from your graveyard by
/// paying [cost] rather than paying its mana cost if you discarded it this turn.'"
/// </summary>
[ConditionKind("discardedThisTurn")]
public sealed record DiscardedThisTurnCondition : Condition
{
  /// <summary>
  /// The object whose this-turn discard is checked — <c>{Kind:"Self"}</c> for "this card".
  /// </summary>
  public required ObjectReference Reference { get; init; }
}
