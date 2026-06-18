namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "unlock a locked door of a Room you control" — assigns an unlocked designation
/// to one locked half (door) of a Room permanent the ability controller controls.
///
/// <para>
/// CR 709.5e: "A player who controls a permanent that has one or more locked halves
/// may pay the mana cost of a locked half of that permanent to give that permanent
/// the appropriate unlocked designation." CR 709.5f: "Some spells and abilities
/// instruct a player to 'unlock' half of a permanent." Rule 709.5j: "a 'door' of a
/// Room permanent is a half of that permanent."
/// </para>
///
/// <para>
/// <see cref="Target"/> carries the Room filter (CardTypes = ["enchantment"],
/// Subtypes = ["Room"], Controller = You). The choice of which locked half to
/// unlock is the controller's at resolution — engine territory (CR 709.5f).
/// </para>
/// </summary>
[OracleEffect("unlockDoor")]
public sealed record UnlockDoorEffect : Effect
{
  /// <summary>
  /// The Room permanent whose door is to be unlocked.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
