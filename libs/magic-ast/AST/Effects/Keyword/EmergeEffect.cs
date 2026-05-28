namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Emerge (Rule 702.119). An alternative cost: sacrifice a creature and pay the
/// emerge cost reduced by that creature's mana value. MAST records the keyword's
/// presence and the printed emerge cost; the sacrifice-a-creature, cost-reduction,
/// and timing semantics are conventionally inferred from the rules.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type so future variants can plug in
/// without a schema change.
/// </para>
/// </summary>
[OracleEffect("emerge")]
public sealed record EmergeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The emerge cost printed on the card. Most commonly a <see cref="ManaCost"/>.
  /// The cost can be paid reduced by the sacrificed creature's mana value, but
  /// that reduction is engine territory — MAST records only the printed value.
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
