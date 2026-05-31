namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// A characteristic-defining ability that sets a creature's power and/or toughness
/// to a derived value. Rule 604.3 — the value is equal to some game-state quantity.
/// e.g., "[CardName]'s power is equal to the number of lands you control."
/// e.g., "[CardName]'s power and toughness are each equal to the number of creatures in your graveyard."
///
/// Distinct from <see cref="ModifyPTEffect"/> which adds to or subtracts from P/T
/// (layer 7c). This effect defines the base value (layer 7a) — it answers "what is
/// the * in the P/T box?" rather than "what modifier is applied?".
/// </summary>
[OracleEffect("definePT")]
public sealed record DefinePTEffect : ContinuousEffect
{
  /// <summary>
  /// Which characteristic is being defined: Power, Toughness, or Both.
  /// </summary>
  public required PTCharacteristic Characteristic { get; init; }

  /// <summary>
  /// The quantity that the characteristic is equal to.
  /// </summary>
  public required Quantity Value { get; init; }

}

/// <summary>
/// Which power/toughness characteristic a <see cref="DefinePTEffect"/> defines.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PTCharacteristic
{
  /// <summary>"[self]'s power is equal to ..."</summary>
  Power,

  /// <summary>"[self]'s toughness is equal to ..."</summary>
  Toughness,

  /// <summary>"[self]'s power and toughness are each equal to ..."</summary>
  Both,
}
