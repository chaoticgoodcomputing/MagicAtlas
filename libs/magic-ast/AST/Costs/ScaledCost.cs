namespace MagicAST.AST.Costs;

using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "pay [cost] for each [count]" — a cost whose total scales with a runtime count,
/// generalizing <see cref="ScaledManaCost"/> beyond mana to any base <see cref="Cost"/>.
/// Cumulative upkeep (CR 702.24a, verbatim, in relevant part): "...you may pay
/// [cost] for each age counter on it. If you don't, sacrifice it." The per-age-counter
/// cost (<see cref="PerUnit"/>, e.g. a <see cref="PayLifeCost"/> for "Pay 2 life") is
/// paid once per unit named by <see cref="Count"/> (a <see cref="CounterCountQuantity"/>
/// counting age counters on the permanent).
///
/// <para>
/// Distinct from <see cref="ScaledManaCost"/>: that sibling is mana-specific
/// (<c>PerUnit: ManaCost</c>) for the "pays {1} for each card revealed this way"
/// shape (Scent of Brine). This node's <see cref="PerUnit"/> is the polymorphic
/// <see cref="Cost"/> base so non-mana per-unit costs (life, energy, ...) can scale
/// the same way, rather than adding a parallel ScaledLifeCost/ScaledEnergyCost
/// sibling per base-cost kind.
/// </para>
///
/// <para>
/// CR 702.24a (verbatim): "If [cost] has choices associated with it, each choice is
/// made separately for each age counter, then either the entire set of costs is
/// paid, or none of them is paid. Partial payments aren't allowed." Reference-not-
/// resolution (ADR 0004): MAST records the per-unit cost and the count reference;
/// the engine evaluates the actual total (and the all-or-nothing payment rule) at
/// payment time.
/// </para>
/// </summary>
[OracleCost("scaledCost")]
public sealed record ScaledCost : Cost
{
  /// <summary>
  /// The cost paid per unit of <see cref="Count"/> — e.g. a <see cref="PayLifeCost"/>
  /// for "Pay 2 life" (Inner Sanctum's cumulative upkeep cost).
  /// </summary>
  public required Cost PerUnit { get; init; }

  /// <summary>
  /// How many units are paid — e.g. "for each age counter on it", a
  /// <see cref="CounterCountQuantity"/>.
  /// </summary>
  public required Quantity Count { get; init; }
}
