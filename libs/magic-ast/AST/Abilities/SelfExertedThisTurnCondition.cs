namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this creature hasn't been exerted this turn" — a backward-looking, turn-scoped gate on
/// whether the source creature has been EXERTED (CR 701.43) during the current turn. Combat
/// Celebrant's attack-time gate: "If this creature hasn't been exerted this turn, you may
/// exert it as it attacks." Exert is a keyword action (CR 701.43) that stops a permanent from
/// untapping during its controller's next untap step; the gate reads the per-turn exert
/// history of the source to decide whether the exert-on-attack option is offered.
///
/// <para>
/// The subject is always the source permanent (<c>Self</c> — "this creature"), so it is not a
/// field. <see cref="Exerted"/> carries the polarity: <c>false</c> encodes the observed
/// "hasn't been exerted this turn" form (the exert option is offered only when the creature
/// has NOT already been exerted this turn); <c>true</c> would encode an affirmative "has been
/// exerted this turn" gate. Reference-not-resolution (ADR 0004): MAST records the printed
/// exert-history gate; the engine tracks whether the source was exerted this turn and
/// evaluates it, MAST does not pre-evaluate it. Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text <see cref="OtherCondition"/>
/// residual.
/// </para>
///
/// CR 701.43a (excerpt): "To exert a permanent as it attacks is to choose to have it not untap
/// during your next untap step. … Some effects check whether a permanent was exerted this turn."
/// </summary>
[ConditionKind("exerted")]
public sealed record SelfExertedThisTurnCondition : Condition
{
  /// <summary>
  /// Whether the gate requires the source to HAVE BEEN exerted this turn. <c>false</c>
  /// encodes the observed "hasn't been exerted this turn" form (Combat Celebrant); <c>true</c>
  /// the affirmative.
  /// </summary>
  public required bool Exerted { get; init; }
}
