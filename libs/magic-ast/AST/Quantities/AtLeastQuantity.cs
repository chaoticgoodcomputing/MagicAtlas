namespace MagicAST.AST.Quantities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A quantity representing "one or more" — a variable target count whose minimum is
/// <see cref="Minimum"/> (1 for "one or more") and whose maximum is <b>unbounded</b>.
/// Used for spells that target a variable number of objects, e.g. Dwarven Song —
/// "One or more target creatures become red until end of turn."
///
/// <para>
/// CR 601.2c (verbatim): "The player announces their choice of an appropriate object
/// or player for each target the spell requires. ... If the spell has a variable number
/// of targets, the player announces how many targets they will choose before they
/// announce those targets. In some cases, the number of targets will be defined by the
/// spell's text. Once the number of targets the spell has is determined, that number
/// doesn't change, even if the information used to determine the number of targets does."
/// "One or more target creatures" is exactly 601.2c's variable number of targets: the
/// controller chooses how many, with a floor of one and no ceiling.
/// </para>
///
/// <para>
/// Distinct from its siblings:
/// <list type="bullet">
///   <item><see cref="AnyAmountQuantity"/> — "any number of" is minimum 0 (may choose
///   none); encoding "one or more" as <c>anyAmount</c> would be rules-inaccurate.</item>
///   <item><see cref="UpToQuantity"/> — "up to N" is bounded above by
///   <see cref="UpToQuantity.Maximum"/>; "one or more" has no upper bound, so this node
///   drops the maximum field.</item>
/// </list>
/// Serializes as <c>{"QuantityType":"atLeast","Minimum":1}</c>. Auto-discovered by
/// reflection (<c>PolymorphicReflectionConverter&lt;Quantity&gt;</c>); no registration.
/// </para>
/// </summary>
[OracleQuantity("atLeast")]
public sealed record AtLeastQuantity : Quantity
{
  /// <summary>
  /// The minimum value (1 in "one or more"). There is no upper bound.
  /// </summary>
  public required int Minimum { get; init; }
}
