namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Harmonize (Rule 702.157). "You may cast this card from your graveyard for its
/// harmonize cost. You may tap a creature you control to reduce that cost by {X},
/// where X is its power. Then exile this spell." MAST records the keyword's presence
/// and the printed harmonize cost; the graveyard-cast mechanics, power-based reduction,
/// and exile-after-cast are conventionally inferred from the rules (reminder text).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other cost-bearing
/// keyword effects (Cycling, Bestow, Dash) — most printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("harmonize")]
public sealed record HarmonizeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The harmonize cost printed on the card. Most commonly a <see cref="ManaCost"/>,
  /// but the polymorphic <see cref="Cost"/> base accommodates future variants.
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
