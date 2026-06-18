namespace MagicAST.AST.Effects.Core;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Repeat this process [N] more times." — a loop-repeat instruction that names a
/// previously stated base process (<see cref="Inner"/>) and the number of
/// <b>additional</b> repetitions (<see cref="AdditionalTimes"/>). The total
/// execution count is <c>AdditionalTimes + 1</c> (the initial occurrence plus the
/// stated extra iterations).
///
/// <para>
/// Example: Professor Onyx's −8:
/// "Each opponent may discard a card. If they don't, they lose 3 life.
/// Repeat this process six more times."
/// → <see cref="AdditionalTimes"/> = 6; total iterations = 7.
/// </para>
///
/// <para>
/// MAST describes, does not execute (ADR 0004): the engine evaluates
/// <see cref="Inner"/> the stated total number of times; MAST records that
/// oracle text requests a fixed-count repetition of the given process.
/// </para>
///
/// <para>
/// CR 701 (game actions); CR 608.2 (effects resolve once on resolution, unless
/// the text explicitly directs a repetition — as here). Cluster axis: any card
/// with "repeat this process N more times" maps to this node keyed on the count,
/// making the repeat count queryable without re-parsing raw text.
/// </para>
/// </summary>
[OracleEffect("repeat")]
public sealed record RepeatEffect : Effect
{
  /// <summary>
  /// The base effect that is performed the first time and then repeated.
  /// </summary>
  public required Effect Inner { get; init; }

  /// <summary>
  /// How many <b>additional</b> times <see cref="Inner"/> is performed after the
  /// initial occurrence. The oracle phrase "six more times" → <c>6</c>; total
  /// iterations = <c>AdditionalTimes + 1</c>.
  /// </summary>
  public required int AdditionalTimes { get; init; }
}
