namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Converge — ability word. The number of +1/+1 counters (or other effects)
/// scales with the number of colors of mana spent to cast the spell.
/// MAST records the ability-word marker; the color-counting and counter-placement
/// are engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless ability-word marker; mirrors <see cref="SunburstEffect"/> which
/// describes the same color-counting concept as a keyword ability (Rule 702.44).
/// Converge is not a keyword in the comp-rules sense — it is an ability word
/// (Rule 207.2c) — but MAST records it as a plain effect marker consistent with
/// how the parser attaches <c>AbilityWord</c> to the containing ability node.
/// </para>
/// </summary>
[OracleEffect("converge")]
public sealed record ConvergeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
