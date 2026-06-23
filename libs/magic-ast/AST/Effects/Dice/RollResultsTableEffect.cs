namespace MagicAST.AST.Effects.Dice;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A die-roll RESULTS TABLE (CR 706.3) — the effect that consumes a preceding
/// <see cref="RollDieEffect"/> result and maps it, by numeric RANGE, to the
/// effect(s) that apply.
///
/// <para>
/// CR 706.3: "Some abilities that instruct a player to roll one or more dice
/// include a results table." A results table is a list of rows, each beginning
/// with a number or a range of numbers followed by the effect(s) that happen if
/// the die's result falls in that range. The whole d20 attack/ETB-roller cohort
/// (Delina, Wild Mage; Lightfoot Rogue; Chaos Channeler; Earth-Cult Elemental;
/// Hoarding Ogre) prints exactly this shape:
/// <code>
///   roll a d20.
///   1—9   | [effect A]
///   10—19 | [effect B]
///   20    | [effect C]
/// </code>
/// </para>
///
/// <para>
/// This node composes AFTER the <see cref="RollDieEffect"/> in the same ability's
/// effect list — <c>[rollDie, rollResultsTable{rows}]</c> — mirroring the
/// table-less shape <c>[rollDie, createToken{dieRollResult}]</c> (Ancient Copper
/// Dragon). The antecedent of "the result" each row's range tests against is that
/// preceding roll (CR 706.2: "the final number is the result of the die roll").
/// Reference-not-resolution (ADR 0004): MAST records the range→effect mapping
/// structurally; the engine evaluates which row applies at resolution time.
/// </para>
///
/// <para>
/// Each row's outcome is expressed with the existing effect vocabulary
/// (sacrifice, createToken, gainLife, …) so a results table is purely a structural
/// dispatch over already-modelled effects — generalising over both the number of
/// rows and the die size.
/// </para>
/// </summary>
[OracleEffect("rollResultsTable")]
public sealed record RollResultsTableEffect : Effect
{
  /// <summary>
  /// The table rows, in printed order. Each row maps an inclusive result RANGE
  /// to the effect(s) that apply when the preceding roll lands in it.
  /// </summary>
  public required IReadOnlyList<ResultsTableRow> Rows { get; init; }
}

/// <summary>
/// One row of a die-roll <see cref="RollResultsTableEffect"/> (CR 706.3): an
/// inclusive numeric range and the effect(s) that apply when the die result falls
/// within it.
///
/// <para>
/// A single-value row ("20") is encoded as <see cref="MinResult"/> == <see cref="MaxResult"/>
/// (e.g. both 20), so the range is always closed on both ends; there is no separate
/// single-value shape. Ranges are inclusive at both bounds, matching the oracle
/// "1—14" / "15—20" convention (CR 706.1a: a dN produces outcomes 1..N).
/// </para>
/// </summary>
public sealed record ResultsTableRow
{
  /// <summary>
  /// Inclusive lower bound of the result range (the "1" in "1—9").
  /// </summary>
  public required int MinResult { get; init; }

  /// <summary>
  /// Inclusive upper bound of the result range (the "9" in "1—9"). Equal to
  /// <see cref="MinResult"/> for a single-value row ("20").
  /// </summary>
  public required int MaxResult { get; init; }

  /// <summary>
  /// The effect(s) that apply when the die's result is in [<see cref="MinResult"/>,
  /// <see cref="MaxResult"/>]. Drawn from the ordinary effect vocabulary; a row may
  /// carry more than one effect (the row body is its own little resolution sequence).
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }
}
