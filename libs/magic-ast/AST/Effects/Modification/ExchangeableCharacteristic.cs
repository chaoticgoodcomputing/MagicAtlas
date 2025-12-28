namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Characteristics that can be exchanged.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExchangeableCharacteristic
{
  TextBox,
  PowerAndToughness,
  Control,
  LifeTotals,
}
