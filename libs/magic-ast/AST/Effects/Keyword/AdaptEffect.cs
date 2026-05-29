namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Adapt N (CR 701.46a: "\"Adapt N\" means \"If this permanent has no +1/+1 counters
/// on it, put N +1/+1 counters on it.\"")
///
/// <para>
/// MAST records the keyword-action and its integer value N as a descriptive node.
/// The conditional check (no +1/+1 counters present) and counter placement are
/// engine territory — the node names the action, not the execution.
/// </para>
///
/// <para>
/// Integer-parameterized keyword-action; mirrors the <see cref="ScryEffect"/> shape
/// with a <see cref="Count"/> quantity field. Distinct from
/// <see cref="MonstrosityEffect"/> — Adapt is an activated keyword-action, Monstrosity
/// is a different mechanic.
/// </para>
/// </summary>
[OracleEffect("adapt")]
public sealed record AdaptEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The adapt value N printed on the card (e.g., "Adapt 2" → 2).</summary>
  public required Quantity Count { get; init; }

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
