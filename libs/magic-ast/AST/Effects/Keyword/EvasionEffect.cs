namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Evasion effect: restricts what can block this creature.
/// Covers: Flying, Menace, Shadow, Horsemanship, Fear, Intimidate, Skulk, Landwalk, etc.
/// "This creature can't be blocked except by [filter]"
/// "This creature can't be blocked as long as [condition]"
/// </summary>
public sealed record EvasionEffect : Effect
{
  /// <summary>
  /// Filter describing what CAN block this creature.
  /// e.g., for Flying: creatures with flying or reach
  /// e.g., for Menace: two or more creatures
  /// Null means "can't be blocked" (unblockable).
  /// </summary>
  [JsonPropertyName("canBeBlockedBy")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? CanBeBlockedBy { get; init; }

  /// <summary>
  /// Minimum number of creatures required to block (for Menace-style effects).
  /// Null for most evasion abilities.
  /// </summary>
  [JsonPropertyName("minimumBlockers")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumBlockers { get; init; }

  /// <summary>
  /// For landwalk: the condition based on defending player's state.
  /// e.g., "defending player controls a Forest"
  /// </summary>
  [JsonPropertyName("unblockableCondition")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public EvasionCondition? UnblockableCondition { get; init; }
}
