namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "lose [amount] life"
/// </summary>
[OracleEffect("loseLife")]
public sealed record LoseLifeEffect : Effect
{
  public required Quantity Amount { get; init; }

  public required ObjectReference Player { get; init; }
}
