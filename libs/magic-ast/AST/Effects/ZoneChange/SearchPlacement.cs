namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.Quantities;

/// <summary>
/// One share of a searched-card distribution: <see cref="Count"/> of the cards
/// found by a single <see cref="SearchLibraryEffect"/> go to
/// <see cref="Destination"/>. Used by <see cref="SearchLibraryEffect.Placements"/>
/// when the search puts its found cards into more than one zone — Cultivate /
/// Kodama's Reach: "put one onto the battlefield tapped and the other into your
/// hand" (CR 701.23, Search). A plain data holder (not a polymorphic node), so it
/// carries no discriminator.
/// </summary>
public sealed record SearchPlacement
{
  /// <summary>How many of the found cards this share covers, e.g. "one".</summary>
  public required Quantity Count { get; init; }

  /// <summary>The zone this share of found cards goes to.</summary>
  public required SearchDestination Destination { get; init; }
}
