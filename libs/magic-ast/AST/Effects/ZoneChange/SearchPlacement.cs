namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

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

  /// <summary>
  /// Who controls the permanent(s) put onto the battlefield by this share —
  /// Verdant Mastery: "put one of them onto the battlefield tapped under an
  /// opponent's control". Null (omitted) for the overwhelmingly common case
  /// where the share goes under the searching player's own control, matching
  /// every pre-existing fixture (Cultivate never states a controller because
  /// CR 701.23's default is the searcher). Meaningless for a non-battlefield
  /// destination (Hand/Graveyard/TopOfLibrary), so left null there too.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Controller { get; init; }

  /// <summary>
  /// Gates this specific share on a condition — Verdant Mastery: "... if the
  /// {3}{G} cost was paid" gates only the opponent's-control share, not the
  /// other two shares of the same search. Null (omitted) for the common
  /// unconditional share.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
