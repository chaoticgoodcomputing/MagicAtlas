namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Awaken (Rule 702.113). Printed as "Awaken N—[cost]" on an instant or sorcery.
/// It establishes an alternative cost: if the spell is cast for its awaken cost,
/// the spell does its normal thing and additionally puts N +1/+1 counters on a
/// target land you control and turns that land into a 0/0 Elemental creature
/// that's still a land. MAST records the keyword's presence and the N + cost
/// parameters; the counters-on-land and land-becomes-creature semantics are
/// conventionally inferred from the rules (and echoed in reminder text).
/// </summary>
[OracleEffect("awaken")]
public sealed record AwakenEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// "Awaken N—[cost]" — the number of +1/+1 counters placed on the target land
  /// when the spell is cast for its awaken cost.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? N { get; init; }

  /// <summary>
  /// "Awaken N—[cost]" — the alternative cost paid to cast the spell with awaken.
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
