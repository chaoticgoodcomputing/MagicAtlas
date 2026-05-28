namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Recover [cost] (Rule 702.59). A triggered ability that functions only while the
/// card with recover is in a player's graveyard: "When a creature is put into your
/// graveyard from the battlefield, you may pay [cost]. If you do, return this card
/// from your graveyard to your hand. Otherwise, exile this card." MAST records the
/// keyword's presence and the recover cost; the trigger, conditional return, and
/// exile clause are all reminder-text territory.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic <see cref="Cost"/> base type so future
/// variants with non-mana costs can plug in without a schema change, mirroring
/// the <see cref="CyclingEffect"/> pattern.
/// </para>
/// </summary>
[OracleEffect("recover")]
public sealed record RecoverEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The cost paid to recover this card. Most commonly a <see cref="ManaCost"/>.
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
