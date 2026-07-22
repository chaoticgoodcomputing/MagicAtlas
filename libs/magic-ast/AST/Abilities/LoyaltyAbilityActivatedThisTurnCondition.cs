namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you didn't activate a loyalty ability of a planeswalker this turn" — a backward-looking,
/// turn-scoped gate on whether YOU activated a loyalty ability (CR 606) during the current
/// turn. The Chain Veil's end-step intervening-if: "At the beginning of your end step, if you
/// didn't activate a loyalty ability of a planeswalker this turn, you lose 2 life." A loyalty
/// ability is an activated ability with a loyalty symbol in its cost (CR 606.1); a player may
/// normally activate one only once each turn per permanent (CR 606.3), so "didn't activate a
/// loyalty ability this turn" reads that per-turn activation history.
///
/// <para>
/// The subject is the controller ("you"), implicit rather than a field because every observed
/// surface is self-scoped. <see cref="Activated"/> carries the polarity: <c>false</c> encodes
/// the observed "didn't activate … this turn" form; <c>true</c> would encode an affirmative
/// "activated a loyalty ability this turn" gate. Reference-not-resolution (ADR 0004): MAST
/// records the printed activation-history gate; the engine tracks whether you activated a
/// loyalty ability this turn and evaluates it, MAST does not pre-evaluate it. Structured to
/// this dedicated <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 606.3 (excerpt): "A player may activate a loyalty ability of a permanent they control
/// any time they have priority and the stack is empty during a main phase of their turn, but
/// only if … they haven't previously activated a loyalty ability of that permanent that turn."
/// </summary>
[ConditionKind("loyaltyAbilityActivated")]
public sealed record LoyaltyAbilityActivatedThisTurnCondition : Condition
{
  /// <summary>
  /// Whether the gate requires that you HAVE activated a loyalty ability this turn. <c>false</c>
  /// encodes the observed "didn't activate a loyalty ability … this turn" form (The Chain Veil);
  /// <c>true</c> the affirmative.
  /// </summary>
  public required bool Activated { get; init; }
}
