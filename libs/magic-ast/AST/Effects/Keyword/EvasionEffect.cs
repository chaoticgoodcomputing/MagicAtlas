namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Evasion effect: restricts what can block this creature.
/// Covers: Flying, Menace, Shadow, Horsemanship, Fear, Intimidate, Skulk, Landwalk, etc.
/// "This creature can't be blocked except by [filter]"
/// "This creature can't be blocked as long as [condition]"
/// </summary>
[OracleEffect("evasion")]
public sealed record EvasionEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// Filter describing what CAN block this creature.
  /// e.g., for Flying: creatures with flying or reach
  /// e.g., for Menace: two or more creatures
  /// Null means "can't be blocked" (unblockable).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? CanBeBlockedBy { get; init; }

  /// <summary>
  /// Minimum number of creatures required to block (for Menace-style effects).
  /// Null for most evasion abilities.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumBlockers { get; init; }

  /// <summary>
  /// For landwalk: the condition based on defending player's state.
  /// e.g., "defending player controls a Forest"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public EvasionCondition? UnblockableCondition { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
