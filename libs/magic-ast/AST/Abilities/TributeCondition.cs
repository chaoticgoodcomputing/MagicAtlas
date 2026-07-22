namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "tribute was paid" / "tribute wasn't paid" — the Tribute keyword's entry gate
/// (CR 702.108). Tribute lets the chosen opponent decide, as the creature enters,
/// whether to place the tribute +1/+1 counters ("pay tribute") or decline; a
/// "When this creature enters, if tribute wasn't paid, …" trigger fires on the
/// decline branch (Snake of the Golden Grove: "Tribute 3 … When this creature
/// enters, if tribute wasn't paid, you gain 4 life.").
///
/// <para>
/// The <see cref="Paid"/> field carries the polarity (mirroring
/// <see cref="TriggeringObjectCounterCondition.Present"/> /
/// <see cref="TriggeringAbilityIsManaCondition.IsManaAbility"/>): <c>false</c> for
/// "tribute wasn't paid" (the opponent declined the counters), <c>true</c> for the
/// affirmative "tribute was paid". The subject is always the source creature —
/// Tribute is a self-referential entry keyword (CR 702.108a) — so there is no
/// referent to capture; only the paid/declined polarity varies.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed gate; the engine
/// reads whether the chosen opponent paid tribute as the creature entered (CR
/// 702.108b), MAST does not pre-evaluate it. Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 702.108a (excerpt): "Tribute is a static ability … represented by 'Tribute N
/// (As this creature enters, choose an opponent. That player may put N +1/+1 counters
/// on this creature. If they don't, it enters with an intervening 'if' ability … )'."
/// CR 702.108c (excerpt): abilities that trigger "if tribute wasn't paid" check whether
/// the chosen player declined to place the tribute counters.
/// </summary>
[ConditionKind("tribute")]
public sealed record TributeCondition : Condition
{
  /// <summary>
  /// The tribute polarity — <c>false</c> for "tribute wasn't paid" (the chosen opponent
  /// declined the +1/+1 counters), <c>true</c> for "tribute was paid".
  /// </summary>
  public required bool Paid { get; init; }
}
