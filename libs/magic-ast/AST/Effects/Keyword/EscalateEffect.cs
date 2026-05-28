namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Escalate effect: a modal spell's "Escalate [cost]" keyword. Paying the escalate
/// cost for each mode chosen beyond the first lets the controller choose multiple modes
/// of a modal spell as an additional cost. CR 702.120a:
/// "Escalate is a static ability of modal spells (see rule 700.2) that functions while
/// the spell with escalate is on the stack. \"Escalate [cost]\" means \"For each mode you
/// choose beyond the first as you cast this spell, you pay an additional [cost].\" Paying
/// a spell's escalate cost follows the rules for paying additional costs in rules 601.2f-h."
/// MAST records only the keyword's presence and its cost parameter; the per-mode-cost
/// multiplication is engine territory, not described by the oracle line itself.
/// </summary>
[OracleEffect("escalate")]
public sealed record EscalateEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The additional cost paid per mode chosen beyond the first.
  /// </summary>
  public required Cost Cost { get; init; }

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
