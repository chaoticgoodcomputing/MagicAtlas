namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Exchange a characteristic between two objects.
/// e.g., "exchange text boxes", "exchange power and toughness", "exchange control"
/// </summary>
public sealed record ExchangeCharacteristicEffect : Effect
{
  /// <summary>
  /// What is being exchanged: TextBox, PowerAndToughness, Control, LifeTotals, etc.
  /// </summary>
  [JsonPropertyName("characteristic")]
  public required ExchangeableCharacteristic Characteristic { get; init; }

  /// <summary>
  /// First object in the exchange (often Self).
  /// </summary>
  [JsonPropertyName("first")]
  public required ObjectReference First { get; init; }

  /// <summary>
  /// Second object in the exchange.
  /// </summary>
  [JsonPropertyName("second")]
  public required ObjectReference Second { get; init; }
}
