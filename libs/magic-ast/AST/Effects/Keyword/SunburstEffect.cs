namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Sunburst (Rule 702.44). A static ability printed as "Sunburst" (parameterless).
/// This permanent enters with a +1/+1 counter on it for each color of mana
/// spent to cast it. (For non-creature artifacts, uses charge counters instead.)
/// MAST records keyword presence; the color-counting and counter-placement are
/// engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker; mirrors AscendEffect, EvolveEffect, and
/// ConvokeEffect — no Value property because the counter count is derived
/// from the casting context, not from the printed text.
/// </para>
/// </summary>
[OracleEffect("sunburst")]
public sealed record SunburstEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
