namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "mill [count] cards"
/// </summary>
[OracleEffect("mill")]
public sealed record MillEffect : Effect
{
  public required Quantity Count { get; init; }

  public required ObjectReference Player { get; init; }
}
