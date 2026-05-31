namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "scry [count]"
/// </summary>
[OracleEffect("scry")]
public sealed record ScryEffect : Effect
{
  public required Quantity Count { get; init; }
}
