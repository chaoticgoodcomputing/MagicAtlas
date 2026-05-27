namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Warp [cost] (Rule 702.185). "You may cast this card from your hand for its
/// warp cost. It enters the battlefield tapped..." An alternative-cast keyword
/// that lets a controller cast a permanent for an alternative mana cost, with
/// the permanent entering tapped as a consequence. MAST records the keyword
/// and the warp cost; the alternative-cast and enters-tapped mechanics are
/// engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Mana-cost-parameterized keyword; mirrors the KickerEffect, PlotEffect,
/// and FlashbackEffect shape.
/// </para>
/// </summary>
[OracleEffect("warp")]
public sealed record WarpEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The alternative mana cost paid to cast this card via warp. Always a
  /// <see cref="ManaCost"/> in all known printings; the polymorphic
  /// <see cref="Cost"/> base accommodates future variants.
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
