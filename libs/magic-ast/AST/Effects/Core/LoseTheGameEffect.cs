namespace MagicAST.AST.Effects.Core;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You lose the game." — an absolute game-ending effect where the named player
/// immediately loses. The mirror of <see cref="WinTheGameEffect"/>: a card effect
/// that states a player loses the game (CR 104.3a: "A player still in the game
/// loses the game as a result of … an effect that states that the player loses the
/// game.").
///
/// <para>
/// Primary use is the Pact deferred-cost consequence — "At the beginning of your
/// next upkeep, pay [cost]. If you don't, you lose the game." (Intervention Pact,
/// Pact of Negation, Slaughter Pact, Summoner's Pact, Pact of the Titan) — where
/// this loss is the <see cref="PreventableEffect.Inner"/> that occurs unless the
/// stated cost is paid (paying a cost is never automatic — CR 118.5).
/// </para>
///
/// <para>
/// MAST models this descriptively — it records that the oracle text says "you lose
/// the game", without encoding any turn-state or state-based-action machinery.
/// Distinct from the loss-prohibition statics (<see cref="MagicAST.AST.Effects.Resource.CantLoseGameEffect"/>,
/// <see cref="MagicAST.AST.Effects.Resource.CantLoseGameForZeroLifeEffect"/>), which
/// prevent a player from losing rather than causing a loss.
/// </para>
/// </summary>
[OracleEffect("loseTheGame")]
public sealed record LoseTheGameEffect : Effect
{
  /// <summary>
  /// The player who loses. Typically <c>You</c> (the controller of the source).
  /// Carried explicitly so downstream consumers know who loses, mirroring
  /// <see cref="WinTheGameEffect.Player"/>.
  /// </summary>
  public required MagicAST.AST.References.ObjectReference Player { get; init; }
}
