namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Timing modification effect: changes when spells can be cast or abilities can be activated.
/// Covers: Flash, "only as a sorcery", "any time you could cast an instant", phase restrictions, etc.
/// </summary>
[OracleEffect("timingModification")]
public sealed record TimingModificationEffect : Effect
{
  /// <summary>
  /// Whether this grants expanded timing or restricts timing.
  /// </summary>
  public required TimingModificationType Modification { get; init; }

  /// <summary>
  /// The timing being granted or restricted to.
  /// </summary>
  public required TimingWindow Timing { get; init; }

  /// <summary>
  /// For phase-specific restrictions, which phase.
  /// e.g., "upkeep", "combat", "end step"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Phase { get; init; }

  /// <summary>
  /// Whose turn this applies to: "yours", "any", "opponents".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WhoseTurn { get; init; }

  /// <summary>
  /// If this grants timing to other abilities (Leonin Shikari granting
  /// instant-speed equip), this reference describes which abilities/spells are
  /// affected — keyed on the surviving keyword identity (ADR 0003). Shares the
  /// <see cref="AbilityReference"/> value type with
  /// <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect.AppliesTo"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public AbilityReference? AppliesTo { get; init; }

  /// <summary>
  /// Condition that must be met for the timing modification.
  /// e.g., "as long as The Wandering Emperor entered this turn"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }

  /// <summary>
  /// How long this timing modification lasts when it is a duration-bounded grant
  /// (CR 611.2) — e.g. Teferi, Time Raveler's +1: "Until your next turn, you may
  /// cast sorcery spells as though they had flash." Null for permanent static
  /// abilities (Vedalken Orrery — the grant persists as long as the permanent is
  /// on the battlefield, per CR 604.2, so no explicit duration is stated).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>
  /// Consequence if the modified timing is used.
  /// e.g., Armor of Thorns: "sacrifice it at the beginning of the next cleanup step"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? Consequence { get; init; }
}
