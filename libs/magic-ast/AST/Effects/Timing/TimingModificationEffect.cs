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
public sealed record TimingModificationEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
  /// If this grants timing to other abilities (like Leonin Shikari granting instant-speed equip),
  /// this filter describes which abilities are affected.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AppliesTo { get; init; }

  /// <summary>
  /// Condition that must be met for the timing modification.
  /// e.g., "as long as The Wandering Emperor entered this turn"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Condition { get; init; }

  /// <summary>
  /// Consequence if the modified timing is used.
  /// e.g., Armor of Thorns: "sacrifice it at the beginning of the next cleanup step"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? Consequence { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
