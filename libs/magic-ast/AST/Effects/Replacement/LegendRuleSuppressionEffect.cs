namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "The 'legend rule' doesn't apply." A meta-rule suppression that disables
/// the state-based action described by Rule 704.5j.
///
/// Rule 704.5j: "If a player controls two or more legendary permanents with
/// the same name, that player chooses one of them, and the rest are put into
/// their owners' graveyards. This is called the 'legend rule.'"
///
/// Mirror Gallery (MRD) is the canonical card. The effect carries no
/// parameters; its mere presence on a <see cref="Abilities.StaticAbility"/>
/// records that the legend rule's state-based action is suppressed while the
/// source permanent is on the battlefield.
/// </summary>
/// <remarks>
/// <para>
/// Placed under <c>Replacement/</c> because suppressing a state-based action
/// is structurally a replacement-shaped intervention on the rules check; it
/// neither carries a duration phrase nor produces a continuous-effect output
/// the way a P/T-modifying or ability-granting effect does.
/// </para>
/// <para>
/// MAST is descriptive: this effect records what the oracle text says
/// ("legend rule doesn't apply"), not the runtime SBA-loop suppression
/// machinery that a rules engine would implement.
/// </para>
/// </remarks>
[OracleEffect("legendRuleSuppression")]
public sealed record LegendRuleSuppressionEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
