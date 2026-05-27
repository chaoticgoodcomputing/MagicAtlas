namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Start your engines! (introduced in Aetherdrift). A triggered keyword ability
/// printed as "Start your engines! (If you have no speed, it starts at 1. It
/// increases once on each of your turns when an opponent loses life. Max speed
/// is 4.)". The oracle text includes a trailing exclamation mark; the
/// combinator matches "Start your engines" since the tokenizer silently drops
/// the '!' character. MAST records the keyword's presence; the speed-counter
/// initialization and increment semantics are engine territory per the
/// descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; mirrors the EvolveEffect and FlankingEffect
/// shape.
/// </para>
/// </summary>
[OracleEffect("startYourEngines")]
public sealed record StartYourEnginesEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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
