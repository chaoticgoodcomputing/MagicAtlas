namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Cost reduction effect: reduces the cost to cast this spell.
/// "This spell costs {X} less to cast..." where X is determined by some condition.
/// </summary>
[OracleEffect("costReduction")]
public sealed record CostReductionEffect : Effect
{
  /// <summary>
  /// The amount of the reduction.
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>
  /// The class of <i>other</i> abilities/spells whose cost this reduces — Strong
  /// Back's "Equip abilities you activate … cost {3} less" and "Aura spells you
  /// cast … cost {3} less" (ADR 0003 follow-up 1). Null for the self-only case
  /// ("this spell costs {X} less to cast"), which remains the default.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public AbilityReference? AppliesTo { get; init; }

  /// <summary>
  /// What the reduction scales with (e.g., "noncombat damage dealt to your opponents this turn").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? BasedOn { get; init; }

  /// <summary>
  /// What this reduction is "for each" of.
  /// e.g., "for each creature you control"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? PerObject { get; init; }

  /// <summary>
  /// Optional condition for when the reduction applies.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }

  /// <summary>
  /// Floor on the mana component of the cost after reduction — "this effect
  /// can't reduce the mana in that cost to less than <c>N</c> mana" printed
  /// on some cards (e.g. Forensic Gadgeteer: <c>1</c>). Null when no floor
  /// is stated (default: the cost may be reduced to zero mana, per CR 601.2f).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumManaCost { get; init; }
}
