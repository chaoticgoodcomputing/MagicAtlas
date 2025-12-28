namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Timing modification effect: changes when spells can be cast or abilities can be activated.
/// Covers: Flash, "only as a sorcery", "any time you could cast an instant", phase restrictions, etc.
/// </summary>
public sealed record TimingModificationEffect : Effect
{
  /// <summary>
  /// Whether this grants expanded timing or restricts timing.
  /// </summary>
  [JsonPropertyName("modification")]
  public required TimingModificationType Modification { get; init; }

  /// <summary>
  /// The timing being granted or restricted to.
  /// </summary>
  [JsonPropertyName("timing")]
  public required TimingWindow Timing { get; init; }

  /// <summary>
  /// For phase-specific restrictions, which phase.
  /// e.g., "upkeep", "combat", "end step"
  /// </summary>
  [JsonPropertyName("phase")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Phase { get; init; }

  /// <summary>
  /// Whose turn this applies to: "yours", "any", "opponents".
  /// </summary>
  [JsonPropertyName("whoseTurn")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WhoseTurn { get; init; }

  /// <summary>
  /// If this grants timing to other abilities (like Leonin Shikari granting instant-speed equip),
  /// this filter describes which abilities are affected.
  /// </summary>
  [JsonPropertyName("appliesTo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AppliesTo { get; init; }

  /// <summary>
  /// Condition that must be met for the timing modification.
  /// e.g., "as long as The Wandering Emperor entered this turn"
  /// </summary>
  [JsonPropertyName("condition")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Condition { get; init; }

  /// <summary>
  /// Consequence if the modified timing is used.
  /// e.g., Armor of Thorns: "sacrifice it at the beginning of the next cleanup step"
  /// </summary>
  [JsonPropertyName("consequence")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? Consequence { get; init; }
}
