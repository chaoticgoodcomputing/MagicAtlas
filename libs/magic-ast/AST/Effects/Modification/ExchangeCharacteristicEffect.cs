namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Exchange a characteristic between two objects.
/// e.g., "exchange text boxes", "exchange power and toughness", "exchange control"
/// </summary>
[OracleEffect("exchangeCharacteristic")]
public sealed record ExchangeCharacteristicEffect : ContinuousEffect
{
  /// <summary>
  /// What is being exchanged: TextBox, PowerAndToughness, Control, LifeTotals, etc.
  /// </summary>
  public required ExchangeableCharacteristic Characteristic { get; init; }

  /// <summary>
  /// First object in the exchange (often Self).
  /// </summary>
  public required ObjectReference First { get; init; }

  /// <summary>
  /// Second object in the exchange.
  /// </summary>
  public required ObjectReference Second { get; init; }
}
