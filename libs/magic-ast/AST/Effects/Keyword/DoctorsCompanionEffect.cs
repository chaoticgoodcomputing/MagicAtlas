namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Doctor's companion (Doctor Who Commander). A variant of the Partner keyword
/// printed as "Doctor's companion (You can have two commanders if the other is
/// the Doctor.)". Only one companion may pair with a Doctor commander at a time.
/// MAST records the keyword's presence; the commander-pairing restriction is
/// engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; mirrors <see cref="PartnerEffect"/> with
/// <see cref="PartnerType.Partner"/>. "Doctor's companion" is a separate keyword
/// from Partner (Rule 702.124) because it carries the specific constraint that
/// the second commander must be "the Doctor" (a creature with the Doctor
/// subtype). The apostrophe in "Doctor's" is consumed as part of the word token
/// by the tokenizer; the combinator matches "Doctor's" as a single Word token.
/// </para>
/// </summary>
[OracleEffect("doctorsCompanion")]
public sealed record DoctorsCompanionEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
