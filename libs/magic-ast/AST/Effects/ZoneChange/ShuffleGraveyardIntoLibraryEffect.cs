namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[Player] shuffles their graveyard into their library."
/// A whole-zone move (CR 400.12: "Some effects instruct a player to do something
/// to a zone (such as 'Shuffle your hand into your library'). That action is
/// performed on all cards in that zone. The zone itself is not affected.") — every
/// card in the subject's graveyard is moved into their library, and that library
/// is then shuffled (CR 701.24a: "To shuffle a library or a face-down pile of
/// cards, randomize the cards within it so that no player knows their order.").
///
/// <para>
/// Distinct from <see cref="ShuffleIntoLibraryEffect"/> (which moves a specific
/// TARGET OBJECT into its owner's library — not a whole-zone move) and from
/// <see cref="ShuffleEffect"/> (which shuffles a player's library with no
/// preceding zone move). This node exists so the graveyard-to-library recycle
/// is discoverable in the interaction/port graph.
/// </para>
/// </summary>
[OracleEffect(
  "shuffleGraveyardIntoLibrary",
  NearDuplicateOf = new[] { "shuffle" },
  Reason = "A primitive vs a whole-zone move that ends in it. 'shuffle' is the bare randomize action (CR 701.24a); 'shuffleGraveyardIntoLibrary' is a zone-wide move (CR 400.12) that relocates every card in the graveyard into the library AND THEN shuffles. Same relationship the already-justified shuffle ~ shuffleIntoLibrary and shuffle ~ shuffleCardsFromGraveyardIntoLibrary pairs carry. Not sprawl."
)]
public sealed record ShuffleGraveyardIntoLibraryEffect : Effect
{
  public required ObjectReference Player { get; init; }
}
