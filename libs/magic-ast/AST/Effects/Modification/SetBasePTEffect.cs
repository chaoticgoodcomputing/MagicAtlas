namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[target] has base power and toughness N/M." — a layer-7b continuous effect
/// (CR 613.4: "Layer 7b: Effects that set power and/or toughness to a specific
/// number or value are applied.") that SETS the base power and/or toughness of
/// another object to a fixed value, replacing whatever base value it had.
///
/// <para>
/// Distinct from <see cref="ModifyPTEffect"/> (layer 7c, CR 613.4: adds to or
/// subtracts from the existing power/toughness rather than overwriting it) and
/// from <see cref="DefinePTEffect"/> (layer 7a, CR 604.3: a characteristic-defining
/// ability that defines an object's OWN power/toughness as a derived game-state
/// quantity, e.g. "this creature's power is equal to the number of lands you
/// control"). This node is layer 7b specifically: a static ability (typically an
/// Aura or Equipment's own static ability, or an anthem-style effect) that reaches
/// onto ANOTHER object and overwrites its base P/T box with a fixed value.
/// </para>
///
/// <para>
/// Example — Reduce in Stature: "Enchanted creature has base power and toughness
/// 0/2." → Target: EnchantedOrEquipped, Power: LiteralQuantity(0),
/// Toughness: LiteralQuantity(2). Persists while the source remains attached; no
/// <see cref="ContinuousEffect.Duration"/> in the Aura case (CR 702.5).
/// </para>
/// </summary>
[OracleEffect("setBasePT")]
public sealed record SetBasePTEffect : ContinuousEffect
{
  /// <summary>The object whose base power/toughness is being set.</summary>
  public required ObjectReference Target { get; init; }

  /// <summary>The value the base power is set to.</summary>
  public required Quantity Power { get; init; }

  /// <summary>The value the base toughness is set to.</summary>
  public required Quantity Toughness { get; init; }
}
