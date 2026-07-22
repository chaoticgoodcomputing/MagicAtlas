namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "an opponent was dealt damage this turn" — the turn-scoped damage-history gate the
/// Bloodthirst keyword's entry ability checks (CR 702.54a: "Bloodthirst N" means "If an
/// opponent was dealt damage this turn, this permanent enters the battlefield with N
/// +1/+1 counters on it."; Carnage Wurm: "Bloodthirst 3").
///
/// <para>
/// The damage-event sibling of the life-loss <see cref="PlayerLostLifeCondition"/> ("an
/// opponent lost life this turn"): distinct because being dealt damage (CR 120) is not
/// the same event as losing life (CR 119.3) — damage to a planeswalker or that is
/// prevented/redirected need not reduce a player's life total, and life can be lost
/// without any damage. <see cref="Player"/> names whose damage is checked
/// (<see cref="ControllerFilter.Opponent"/> for "an opponent was dealt damage"), and the
/// optional <see cref="Amount"/> carries an explicit threshold (null for Bloodthirst's
/// bare existence form — any nonzero damage). Mirrors <see cref="PlayerLostLifeCondition"/>
/// field-for-field so the two turn-scoped player-event gates read consistently.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed damage-history gate; the
/// engine reads whether a matching player was dealt damage this turn, MAST does not
/// pre-evaluate it. Structured to this dedicated <see cref="Condition"/> arm rather than
/// left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 702.54a (verbatim): "Bloodthirst is a static ability … 'Bloodthirst N' means 'If an
/// opponent was dealt damage this turn, this permanent enters the battlefield with N
/// +1/+1 counters on it.'"
/// </summary>
[ConditionKind("dealtDamageThisTurn")]
public sealed record PlayerDealtDamageThisTurnCondition : Condition
{
  /// <summary>Whose being-dealt-damage is checked — <see cref="ControllerFilter.Opponent"/> for "an opponent was dealt damage".</summary>
  public required ControllerFilter Player { get; init; }

  /// <summary>
  /// The optional threshold on how much damage was dealt this turn. Null for the bare
  /// existence form ("an opponent was dealt damage this turn" — one or more, Bloodthirst);
  /// a <see cref="Comparison"/> for an explicit bound.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? Amount { get; init; }
}
