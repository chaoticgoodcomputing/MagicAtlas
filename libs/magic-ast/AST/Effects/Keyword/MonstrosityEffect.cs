namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Monstrosity N (CR 701.37a): "Monstrosity N means 'If this permanent isn't
/// monstrous, put N +1/+1 counters on it and it becomes monstrous.'"
///
/// <para>
/// MAST records the keyword-action and its integer value N. The monstrous
/// designation (CR 701.37b: "Monstrous is a designation that has no rules
/// meaning other than to act as a marker that the monstrosity action and
/// other spells and abilities can identify."), state tracking, and counter
/// placement are engine territory — this node is purely descriptive.
/// </para>
///
/// <para>
/// Do NOT decompose into PutCounters + state-change nodes; the full
/// monstrosity keyword-action is the atomic unit (per MAST doctrine:
/// describes, does not execute). Mirrors the integer-parameterized shape
/// of <see cref="RenownEffect"/> and <see cref="TributeEffect"/> — the
/// <see cref="Value"/> field carries N from the oracle text.
/// </para>
/// </summary>
[OracleEffect("monstrosity")]
public sealed record MonstrosityEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The monstrosity value N printed on the card (e.g., "Monstrosity 4" → 4).</summary>
  public required int Value { get; init; }

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
