namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Descriptive placeholder for "this is just the named keyword X" when the
/// keyword's internal mechanics don't yet have their own structured Effect
/// node. Carries only the keyword name — the keyword's semantics live in
/// the comprehensive rules and are not described further here.
/// </summary>
/// <remarks>
/// This is a deliberate escape hatch, used most often inside
/// <c>GainAbilityEffect.GainedAbility</c> when a granted keyword (e.g.
/// "suspend", "morph") hasn't been promoted to a first-class effect type.
/// Prefer a structured concrete Effect (Lifelink, Trample, …) when one
/// exists; reach for this only when the modeled alternative is genuinely
/// missing.
/// </remarks>
[OracleEffect("keywordReference")]
public sealed record KeywordReferenceEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The keyword name as it appears in oracle text (lowercased).
  /// </summary>
  public required string Keyword { get; init; }

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
