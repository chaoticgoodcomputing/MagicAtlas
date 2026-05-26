namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Cycling (Rule 702.32). An activated ability functioning only in hand:
/// "[Cost], Discard this card: Draw a card." MAST records the keyword's
/// presence and the cycling cost; the inner discard/draw structure is
/// conventionally inferred from the rules.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type so future typecycling
/// (Mountaincycling, Plainscycling) and similar variants can plug in
/// without a schema change — those carry the same shape with a different
/// concrete cost.
/// </para>
/// </summary>
[OracleEffect("cycling")]
public sealed record CyclingEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The cost paid to cycle this card. Most commonly a <see cref="ManaCost"/>,
  /// but the polymorphic <see cref="Cost"/> base accommodates typecycling and
  /// similar variants.
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
