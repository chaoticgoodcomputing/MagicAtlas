namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "counter [target spell/ability]"
/// </summary>
public sealed record CounterSpellEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "unless its controller pays [cost]"
  /// </summary>
  [JsonPropertyName("unlessCost")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? UnlessCost { get; init; }
}
