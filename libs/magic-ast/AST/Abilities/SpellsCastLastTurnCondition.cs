namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// The classic Innistrad werewolf transform gate — a PREVIOUS-turn spell-cast count.
/// The two faces read as a matched pair:
/// <list type="bullet">
///   <item><description>day→night ("no spells were cast last turn"):
///     <see cref="Count"/> = <c>{Equal, 0}</c>, <see cref="PerPlayer"/> = <c>false</c> —
///     the TOTAL number of spells cast last turn (by anyone) is zero.</description></item>
///   <item><description>night→day ("a player cast two or more spells last turn"):
///     <see cref="Count"/> = <c>{GreaterThanOrEqual, 2}</c>, <see cref="PerPlayer"/> =
///     <c>true</c> — some SINGLE player cast at least two spells last turn.</description></item>
/// </list>
///
/// <para>
/// <see cref="PerPlayer"/> is the load-bearing axis that a plain
/// <see cref="CountCondition"/> over a spell filter cannot express: the night→day gate is
/// NOT "two or more spells were cast last turn" (which two different players each casting
/// one would satisfy) — it requires one player to have cast both (CR — the pre-daybound
/// werewolf reminder text). The day→night gate is a raw total, so <see cref="PerPlayer"/>
/// is <c>false</c>.
/// </para>
///
/// <para>
/// The "last turn" window has no structured history predicate (every existing predicate is
/// "this turn"), so this is a dedicated node rather than a <see cref="CountCondition"/> over
/// a nonexistent last-turn cast predicate. Reference-not-resolution (ADR 0004): MAST records
/// the printed spell-count gate; the engine reads the previous turn's cast tally, MAST does
/// not pre-evaluate it. Structured rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 601 (casting a spell); CR 514 (the turn structure bounds "last turn" to the
/// immediately preceding turn).
/// </summary>
[ConditionKind("spellsCastLastTurn")]
public sealed record SpellsCastLastTurnCondition : Condition
{
  /// <summary>The threshold the last-turn spell count is compared against — <c>{Equal,0}</c> (none) or <c>{GreaterThanOrEqual,2}</c> (two or more).</summary>
  public required Comparison Count { get; init; }

  /// <summary>
  /// <c>true</c> when the count is per a SINGLE player ("a player cast two or more spells" —
  /// one player must reach the threshold); <c>false</c> when it is the raw total across all
  /// players ("no spells were cast" — nobody cast any).
  /// </summary>
  public required bool PerPlayer { get; init; }
}
