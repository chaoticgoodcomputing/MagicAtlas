namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if this is the [Nth] time this ability has resolved this turn" — a backward-looking
/// count on how many times THIS ability has finished resolving (CR 608.1) during the
/// current turn. The intervening/within-resolution gate the escalating "storm-count on
/// self" family prints: Ashling, Flame Dancer ("If this is the second time … / If it's
/// the third time …"), Nissa, Resurgent Animist ("if this is the second time …"),
/// Sephiroth, Fabled SOLDIER ("If this is the fourth time …"), Ashling the Pilgrim
/// ("If this is the third time …"). Each resolution of the same ability tallies against
/// its own per-turn counter; when the tally reaches <see cref="Ordinal"/>, the gated
/// consequent applies.
///
/// <para>
/// Carries a single <see cref="Ordinal"/> — the occurrence number the printed text names
/// (2 for "the second time", 3 for "the third time", 4 for "the fourth time"). The
/// elided continuation form "it's the [Nth] time" (Ashling's "If it's the third time,
/// add {R}{R}{R}{R}") names the same per-turn resolution count as the full "this is the
/// [Nth] time this ability has resolved this turn" and structures to the same node with
/// the corresponding ordinal — the omitted tail ("this ability has resolved this turn")
/// is understood from the preceding sentence. Mirrors
/// <see cref="MagicAST.AST.Triggers.TriggerCondition.Ordinal"/>'s descriptive-only
/// convention: MAST records WHICH occurrence the text names; the per-turn tally and its
/// reset at end of turn are the engine's job.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed ordinal gate; the engine
/// counts this ability's resolutions this turn (CR 608 — an ability resolves when it
/// leaves the stack with its instructions followed) and compares, MAST does not
/// pre-evaluate it. Structured to this dedicated <see cref="Condition"/> arm rather than
/// left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 608.1 (excerpt): "Each time all players pass in succession, the spell or ability on
/// top of the stack resolves."
/// </summary>
[ConditionKind("nthTimeResolved")]
public sealed record NthTimeAbilityResolvedCondition : Condition
{
  /// <summary>
  /// The occurrence number the printed text names — 2 for "the second time", 3 for "the
  /// third time", 4 for "the fourth time". The gated consequent applies on the resolution
  /// whose per-turn tally equals this value.
  /// </summary>
  public required int Ordinal { get; init; }
}
