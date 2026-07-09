namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Where cards go after searching.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchDestination
{
  Hand,
  Battlefield,
  BattlefieldTapped,
  TopOfLibrary,
  Graveyard,

  /// <summary>
  /// Sentinel for a single search whose found cards are split across more than one
  /// zone — Cultivate / Kodama's Reach: "put one onto the battlefield tapped and the
  /// other into your hand". The per-share destinations are carried by
  /// <see cref="SearchLibraryEffect.Placements"/>; this value flags the reader to
  /// consult that list rather than a single destination.
  /// </summary>
  Distributed,
}
