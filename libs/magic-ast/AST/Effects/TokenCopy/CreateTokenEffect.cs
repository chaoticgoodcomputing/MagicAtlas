namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "create [count] [token description]"
/// </summary>
[OracleEffect("createToken")]
public sealed record CreateTokenEffect : Effect
{
  public required Quantity Count { get; init; }

  public required TokenDefinition Token { get; init; }

  /// <summary>
  /// The player who creates the tokens, and therefore controls them (CR 111.2: "the token enters
  /// the battlefield under that player's control"). The implicit subject of "create a token" is the
  /// ability's controller — <see cref="ObjectReference.You"/>; cards naming another creator ("target
  /// opponent creates …", "each player creates …") carry that reference instead. Parity with the
  /// <c>Player</c> field the other player-affecting effects (draw, gain/lose life) already carry.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
