namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Its owner shuffles [target] into their library."
/// A zone-change effect that moves the target from its current zone into its
/// owner's library, then the owner shuffles their library. Distinct from
/// <see cref="PutOnTopOfLibraryEffect"/> (which places on top without shuffling)
/// and from <see cref="ShuffleEffect"/> (which shuffles a player's library
/// without moving a target object into it).
///
/// <para>
/// Most commonly appears as the resolution of the Unravel the Aether pattern —
/// a two-sentence oracle form "Choose target artifact or enchantment. Its owner
/// shuffles it into their library." The card moves to the library (a zone change)
/// and the owner shuffles immediately after the card is placed there. Rule 701.24
/// governs shuffle (its own CR example is Guile's "put into a graveyard … shuffle" text).
/// </para>
/// </summary>
[OracleEffect("shuffleIntoLibrary")]
public sealed record ShuffleIntoLibraryEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
