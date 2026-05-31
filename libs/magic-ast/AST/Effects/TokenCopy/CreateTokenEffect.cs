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
}
