namespace MagicAST.AST.Effects.Core;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You win the game." — an absolute game-ending effect where the controller
/// immediately wins (CR 104.3a: "A player can win the game by having the
/// highest life total when the game ends, by having their opponent lose the game,
/// or by a card effect that says 'you win the game'.").
///
/// <para>
/// MAST models this descriptively — it records that the oracle text says
/// "you win the game", without encoding any turn-state or priority machinery.
/// Distinct from opponent-loss effects (which say "an opponent loses the game").
/// </para>
/// </summary>
[OracleEffect("winTheGame")]
public sealed record WinTheGameEffect : Effect
{
  /// <summary>
  /// The player who wins. Typically <c>You</c> (the controller of the source).
  /// Carried explicitly so downstream consumers know who wins (e.g. mill-win vs
  /// combat-win vs poison-win are all <c>winTheGame</c> from the text perspective).
  /// </summary>
  public required MagicAST.AST.References.ObjectReference Player { get; init; }
}
