namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "destroy [target]"
/// </summary>
public sealed record DestroyEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "It can't be regenerated"
  /// </summary>
  [JsonPropertyName("cantBeRegenerated")]
  public bool CantBeRegenerated { get; init; }
}
