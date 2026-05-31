namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "gain control of [target]"
/// </summary>
[OracleEffect("gainControl")]
public sealed record GainControlEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }
}
