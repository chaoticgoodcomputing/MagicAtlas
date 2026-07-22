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

  /// <summary>
  /// Sentinel for a search that does NOT relocate the found card to any zone — the
  /// card is retained where it was found (its owner's library) and a sibling effect
  /// governs what happens next. Knowledge Exploitation: "Search target opponent's
  /// library for an instant or sorcery card. You may cast that card without paying
  /// its mana cost." — the search only FINDS the card (CR 701.20); casting it from
  /// the library (CR 601) is a separate
  /// <see cref="MagicAST.AST.Effects.Timing.CastWithoutPayingEffect"/>, and if it is
  /// not cast the card stays in the library. Forcing a zone (Hand/Battlefield/…) would
  /// misstate the rules — the search itself moves nothing. Mirrors
  /// <see cref="Distributed"/> as a semantic sentinel (no single literal zone) rather
  /// than a relocation target: this one flags "no relocation," deferring the card's fate
  /// to a sibling effect in the enclosing
  /// <see cref="MagicAST.AST.Effects.Core.CompositeEffect"/>.
  /// </summary>
  Retained,
}
