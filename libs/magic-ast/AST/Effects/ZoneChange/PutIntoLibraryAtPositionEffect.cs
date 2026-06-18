namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.Effects;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Put [card] into its owner's library Nth from the top." — a zone-change effect
/// that moves a card to a specific ordinal position from the top of its owner's
/// library (CR 401.7: "If an effect causes a player to put a card into a library
/// 'Nth from the top,' and that library has fewer than N cards in it, the player
/// puts that card on the bottom of that library.").
///
/// <para>
/// The paradigmatic instance is Approach of the Second Sun's else-branch: "put
/// Approach of the Second Sun into its owner's library seventh from the top."
/// The card is self-referential (the source spell moves itself); the position is
/// always a fixed positive integer (1 = top, 2 = second from top, etc.).
/// </para>
///
/// <para>
/// Distinct from <see cref="PutOnTopOfLibraryEffect"/> (position 1, always the
/// very top) and from <see cref="ShuffleIntoLibraryEffect"/> (random insertion).
/// MAST records the ordinal descriptively; the engine resolves the boundary case
/// (library shorter than N) per CR 401.7.
/// </para>
/// </summary>
[OracleEffect("putIntoLibraryAtPosition")]
public sealed record PutIntoLibraryAtPositionEffect : Effect
{
  /// <summary>
  /// The card being placed. Typically <see cref="ObjectReferenceKind.Self"/> for
  /// self-referential spells like Approach of the Second Sun.
  /// </summary>
  public required ObjectReference Card { get; init; }

  /// <summary>
  /// Ordinal position from the top of the library (1 = top, 7 = seventh from top).
  /// CR 401.7 governs the fallback when the library is shorter than
  /// <see cref="Position"/>.
  /// </summary>
  public required int Position { get; init; }
}
