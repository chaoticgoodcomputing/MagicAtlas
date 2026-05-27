namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Job select (Rule 702.182). "When this Equipment enters, create a 1/1
/// colorless Hero creature token, then attach this to it." Found on Equipment
/// cards from the Final Fantasy set. MAST records the keyword's presence;
/// the ETB trigger, Hero-token creation, and auto-attach semantics are engine
/// territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; structurally mirrors LivingWeaponEffect
/// (Rule 702.77), which also creates a token and attaches the Equipment to it
/// on entry. The distinctions (token type: Hero vs. Phyrexian Germ; power/
/// toughness: 1/1 vs. 0/0; colors: colorless vs. black) are engine territory.
/// </para>
/// </summary>
[OracleEffect("jobSelect")]
public sealed record JobSelectEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
