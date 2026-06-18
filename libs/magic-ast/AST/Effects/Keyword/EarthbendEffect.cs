namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Earthbend N" keyword action (CR 701.66a).
///
/// <para>
/// CR 701.66a (verbatim): "\"Earthbend N\" means \"Target land you control becomes
/// a 0/0 land creature with haste in addition to its other types. Put N +1/+1 counters
/// on it. When that land dies or is put into exile, return it to the battlefield tapped
/// under your control.\""
/// </para>
///
/// <para>
/// MAST records the keyword-action and its integer value N descriptively. The land
/// animation, counter placement, and delayed triggered ability (return on death/exile)
/// are engine territory; the node names the action and its parameter, not the execution.
/// </para>
///
/// <para>
/// CR 701.66b: "An ability that triggers whenever a player earthbends triggers when
/// the delayed triggered ability described in rule 701.66a is created."
/// </para>
/// </summary>
[OracleEffect("earthbend")]
public sealed record EarthbendEffect : Effect
{
  /// <summary>
  /// The N value: number of +1/+1 counters placed on the earthbended land
  /// (CR 701.66a).
  /// </summary>
  public required Quantity Count { get; init; }
}
