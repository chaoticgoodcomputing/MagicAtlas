namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "add [mana]"
/// </summary>
public sealed record AddManaEffect : Effect
{
  [JsonPropertyName("mana")]
  public required string Mana { get; init; }

  /// <summary>
  /// For effects like "add one mana of any color"
  /// </summary>
  [JsonPropertyName("anyColor")]
  public bool AnyColor { get; init; }

  /// <summary>
  /// For variable amounts
  /// </summary>
  [JsonPropertyName("amount")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }
}
