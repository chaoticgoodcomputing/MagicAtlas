namespace MagicAST.AST.Effects.Dice;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "roll a dN" — instructs a player to roll an N-sided die (CR 706.1).
///
/// <para>
/// CR 706.1: "An effect that instructs a player to roll a die will specify what
/// kind of die to roll and how many of those dice to roll." CR 706.1a: "Such an
/// effect may refer to … one or more 'dN,' where N is a positive integer. … a d20
/// is a twenty-sided die with possible outcomes from 1 to 20."
/// </para>
///
/// <para>
/// CR 706.4: "Some abilities that instruct a player to roll one or more dice do
/// not include a results table. The text of those abilities will indicate how to
/// use the results of the die rolls, if at all." Ancient Copper Dragon rolls a d20
/// without a results table: the result is used directly by the following
/// CreateTokenEffect via <see cref="DieRollResultQuantity"/>.
/// </para>
///
/// <para>
/// The <see cref="Sides"/> field captures N from "dN" (e.g. 20 for a d20).
/// The number of dice rolled is 1 (the common case); multi-die variants are out
/// of scope for this card and would extend this node.
/// </para>
/// </summary>
[OracleEffect("rollDie")]
public sealed record RollDieEffect : Effect
{
  /// <summary>
  /// The number of sides on the die (N in "dN"). For a d20, this is 20.
  /// CR 706.1a: the die must have N equally likely outcomes, numbered 1 to N.
  /// </summary>
  public required int Sides { get; init; }
}
