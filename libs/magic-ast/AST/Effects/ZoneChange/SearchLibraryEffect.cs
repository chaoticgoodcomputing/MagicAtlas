namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "search your library for [filter]"
/// </summary>
[OracleEffect("searchLibrary")]
public sealed record SearchLibraryEffect : Effect
{
  public required ObjectFilter Filter { get; init; }

  public required Quantity Count { get; init; }

  /// <summary>
  /// Zones searched, e.g. "your library and/or graveyard". When omitted, the
  /// search is library-only (the default Rule 701.23 case, and the shape every
  /// pre-existing fixture carries). Present only when the oracle names additional
  /// or alternative source zones beyond the library.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Zone>? Sources { get; init; }

  public required SearchDestination Destination { get; init; }

  /// <summary>
  /// How the found cards are split across destinations when a single search puts
  /// them into more than one zone — the Cultivate / Kodama's Reach family:
  /// "reveal those cards, put one onto the battlefield tapped and the other into
  /// your hand". Each <see cref="SearchPlacement"/> names how many of the found
  /// cards go where; the placements together cover the found cards in text order.
  /// When present, <see cref="Destination"/> carries the sentinel
  /// <see cref="SearchDestination.Distributed"/>. Omitted for the common
  /// single-destination search (every pre-existing fixture), where
  /// <see cref="Destination"/> alone names the zone.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<SearchPlacement>? Placements { get; init; }

  public bool Revealed { get; init; }
}
