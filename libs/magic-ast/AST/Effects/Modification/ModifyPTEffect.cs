namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[target] gets +X/+Y" or "gets -X/-Y"
/// </summary>
[OracleEffect("modifyPT")]
public sealed record ModifyPTEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  public required Quantity PowerModifier { get; init; }

  public required Quantity ToughnessModifier { get; init; }
}
