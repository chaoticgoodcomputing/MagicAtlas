namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Switches a creature's power and toughness — CR 613.4d (layer 7d): "Effects that
/// switch a creature's power and toughness are applied. Such effects take the value
/// of power and apply it to the creature's toughness, and take the value of
/// toughness and apply it to the creature's power."
/// e.g., "Switch target creature's power and toughness until end of turn." (Twisted Image)
///
/// Distinct from <see cref="ExchangeCharacteristicEffect"/>, which exchanges a
/// characteristic BETWEEN two objects (First/Second). This effect switches a
/// SINGLE creature's own power and toughness with each other.
/// </summary>
[OracleEffect("switchPT")]
public sealed record SwitchPTEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }
}
