namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Buyback [cost] (Rule 702.26). A keyword ability: you may pay an additional
/// [cost] as you cast this spell; if you do, put this card into your hand as
/// it resolves instead of into the graveyard. MAST records the keyword's
/// presence and the buyback cost; the conditional-hand-return resolution is
/// engine territory. Mirrors FlashbackEffect / KickerEffect for the
/// cost-parameterized keyword shape.
///
/// <para>
/// <see cref="BuybackCost"/> is the polymorphic <see cref="Cost"/> base type
/// because buyback can in principle appear with non-mana costs, mirroring
/// the FlashbackEffect / CyclingEffect pattern.
/// </para>
/// </summary>
[OracleEffect("buyback")]
public sealed record BuybackEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The additional cost paid to return this card to hand on resolution.</summary>
  public required Cost BuybackCost { get; init; }

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
