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
public sealed record ExchangeCharacteristicEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
