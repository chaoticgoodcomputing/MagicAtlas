namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// For Mirrodin! (Phyrexia: All Will Be One). A triggered keyword ability printed
/// as "For Mirrodin! (When this Equipment enters, create a 2/2 red Rebel creature
/// token, then attach this to it.)". MAST records the keyword's presence; the
/// ETB trigger, token-creation (2/2 red Rebel), and auto-attach semantics are
/// engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; mirrors <see cref="LivingWeaponEffect"/> which
/// describes the same enters-create-token-attach pattern on Phyrexian Equipment
/// (Rule 702.77). "For Mirrodin!" is a separate keyword with a different token
/// definition (2/2 red Rebel vs. 0/0 black Phyrexian Germ).
/// The '!' in the oracle text is silently dropped by the tokenizer (unknown
/// character handling), so the combinator matches "For Mirrodin".
/// </para>
/// </summary>
[OracleEffect("forMirrodin")]
public sealed record ForMirrodinEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
