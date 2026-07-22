namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "the player you attacked has the most life or is tied for most life" — the Dethrone
/// superlative gate (CR 702.105a; Marchesa's Emissary). The counter is put only if the
/// player being attacked has the most life among all players (ties included). "Most life"
/// spans the whole set of players — a maximum over a population that no per-object filter
/// axis can express.
///
/// <para>
/// <see cref="Player"/> names whose life is asserted greatest — Dethrone's "the player you
/// attacked" is the defending player of the attack trigger
/// (<see cref="ObjectReferenceKind.DefendingPlayer"/>, CR 508.1b).
/// <see cref="IncludeTies"/> is <c>true</c> for Dethrone's "or is tied for most life" form.
/// The life sibling of the power superlative <see cref="GreatestPowerCondition"/> and the
/// colour superlative <see cref="MostCommonColorCondition"/>: a card-defined,
/// engine-evaluated maximum recorded as written — there is no CR rule for "most life".
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed superlative; the engine
/// tallies life totals and compares, MAST does not pre-evaluate it. Structured rather than
/// left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 119.1 (life total); CR 702.105a (Dethrone).
/// </summary>
[ConditionKind("mostLife")]
public sealed record PlayerHasMostLifeCondition : Condition
{
  /// <summary>Whose life is asserted greatest — Dethrone's "the player you attacked" is <see cref="ObjectReferenceKind.DefendingPlayer"/>.</summary>
  public required ObjectReference Player { get; init; }

  /// <summary><c>true</c> when a tie for most life still satisfies the gate ("or is tied for most life"); <c>false</c> for a strict maximum.</summary>
  public required bool IncludeTies { get; init; }
}
