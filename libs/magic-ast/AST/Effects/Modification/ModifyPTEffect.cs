namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[target] gets +X/+Y" or "gets -X/-Y"
/// </summary>
public sealed record ModifyPTEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  [JsonPropertyName("powerModifier")]
  public required Quantity PowerModifier { get; init; }

  [JsonPropertyName("toughnessModifier")]
  public required Quantity ToughnessModifier { get; init; }
}
