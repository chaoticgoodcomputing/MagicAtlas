namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Target player shuffles up to [N] target cards from their graveyard into
/// their library." A TARGETED-SUBSET graveyard-to-library recycle (CR 701.24;
/// CR 701.24a: "To shuffle a library or a face-down pile of cards, randomize
/// the cards within it so that no player knows their order.") — a targeted
/// player is instructed to shuffle a bounded, separately-targeted selection
/// of cards from that player's own graveyard into their library. Rule 701.24's
/// own worked example spells out this exact shape: "Loaming Shaman says 'When
/// this creature enters, target player shuffles any number of target cards
/// from their graveyard into their library.' … When the ability resolves, the
/// targeted player will still have to shuffle their library." Krosan
/// Reclamation is the "up to two" bounded variant of that template. Because
/// the word "target" appears twice, CR 601.2c permits choosing a target once
/// per occurrence — the player and the cards are targeted independently.
///
/// <para>
/// Distinct from <see cref="ShuffleGraveyardIntoLibraryEffect"/> (a WHOLE-ZONE
/// move — every card in the subject's graveyard goes in, with only a
/// <c>Player</c> reference and no card-level selection) and from
/// <see cref="ShuffleIntoLibraryEffect"/> (a SINGLE targeted object moved into
/// its owner's library). This node carries both the targeted player who
/// performs the shuffle and the separately-targeted, quantity-bounded subset
/// of that player's graveyard cards being recycled.
/// </para>
/// </summary>
[OracleEffect(
  "shuffleCardsFromGraveyardIntoLibrary",
  NearDuplicateOf = new[] { "shuffle" },
  Reason = "'shuffle' shuffles a library/zone in place; 'shuffleCardsFromGraveyardIntoLibrary' (Krosan Reclamation, CR 701.24) is a targeted-subset zone-change that moves separately-targeted graveyard cards into their owner's library and then shuffles. Shares only the 'shuffle' stem; carries both a targeted Player and a quantity-bounded Cards subset. Also distinct from whole-zone 'shuffleGraveyardIntoLibrary' and single-object 'shuffleIntoLibrary'. Not sprawl."
)]
public sealed record ShuffleCardsFromGraveyardIntoLibraryEffect : Effect
{
  public required ObjectReference Player { get; init; }
  public required ObjectReference Cards { get; init; }
}
