namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Escape (Rule 702.138). A keyword on instants and sorceries: "Escape—[cost],
/// Exile N other cards from your graveyard." lets the player cast the card from
/// their graveyard for an alternative cost. That escape cost has two parts — a
/// mana cost and the additional cost of exiling a fixed number of other cards
/// from the controller's graveyard. MAST records both components of the printed
/// escape cost; the cast-from-graveyard permission itself is conventionally
/// inferred from the rules and captured as the <c>Reminder</c> parenthetical.
///
/// <para>
/// <see cref="CardsToExile"/> is the count of cards in the graveyard-exile half
/// of the escape cost — the additional cost, not a separate game action. MAST
/// describes the cost; it does not model the exile as an <c>ExileEffect</c>
/// subtree.
/// </para>
/// </summary>
[OracleEffect("escape")]
public sealed record EscapeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// "Escape—[cost], …" — the mana half of the alternative escape cost.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>
  /// "…, Exile N other cards from your graveyard." — the number of other cards
  /// that must be exiled from the controller's graveyard as the additional half
  /// of the escape cost. Null when the keyword carries no printed exile clause.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? CardsToExile { get; init; }

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
