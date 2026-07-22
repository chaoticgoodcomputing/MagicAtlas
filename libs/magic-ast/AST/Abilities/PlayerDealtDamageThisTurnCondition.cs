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
  /// <summary>Whose being-dealt-damage is checked — <see cref="ControllerFilter.Opponent"/> for "an opponent was dealt damage" (Bloodthirst); <see cref="ControllerFilter.Any"/> for "a player was dealt combat damage" (Prowl/Freerunning).</summary>
  public required ControllerFilter Player { get; init; }

  /// <summary>
  /// The optional threshold on how much damage was dealt this turn. Null for the bare
  /// existence form ("an opponent was dealt damage this turn" — one or more, Bloodthirst);
  /// a <see cref="Comparison"/> for an explicit bound.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? Amount { get; init; }

  /// <summary>
  /// <c>true</c> restricts the gate to COMBAT damage specifically (CR 510.1c — combat damage,
  /// as distinct from the general damage of CR 120): Prowl (CR 702.76a) and Freerunning
  /// (CR 702.173a) both key on "a player was dealt COMBAT damage this turn". Null for the
  /// unqualified any-damage form (Bloodthirst), which serializes unchanged. Combat damage is
  /// not a separate event type but a qualified subset, so it is a boolean refinement of this
  /// same turn-scoped damage-history gate rather than a distinct condition arm.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? Combat { get; init; }

  /// <summary>
  /// Restricts to damage dealt BY a source matching this filter, evaluated as of the moment it
  /// dealt the damage (CR 608.2h last-known information — "at the time it dealt that damage").
  /// Prowl's source is <c>{Controller:You, SharesCreatureTypeWith:{Kind:Self}}</c> ("under your
  /// control and had any of the creature types of this spell"); Freerunning's is
  /// <c>{Controller:You, AnyOf:[{CardTypes:["creature"],Subtypes:["Assassin"]},{IsCommander:true}]}</c>
  /// ("an Assassin creature or a commander under your control"). Null for the unqualified
  /// Bloodthirst form (any source), which serializes unchanged. Reference-not-resolution
  /// (ADR 0004): MAST records the source's stated characteristics; the engine applies the
  /// last-known snapshot.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Source { get; init; }
}
