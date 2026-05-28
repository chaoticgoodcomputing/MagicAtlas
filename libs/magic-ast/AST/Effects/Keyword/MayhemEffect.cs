namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mayhem (Rule 702.187). An alternative-cost keyword allowing a card to be
/// cast from the graveyard by paying the mayhem cost, but only if it was
/// discarded this turn. Oracle form: "Mayhem [cost]". MAST records the
/// keyword's presence and the alternative cost; the discard-condition and
/// graveyard-cast mechanics are reminder text and engine territory.
///
/// <para>
/// CR 702.187b: "\"Mayhem [cost]\" means \"As long as you discarded this
/// card this turn, you may cast it from your graveyard by paying [cost]
/// rather than paying its mana cost.\" Casting a spell using its mayhem
/// ability follows the rules for paying alternative costs in rules 601.2b
/// and 601.2f-h."
/// </para>
/// </summary>
[OracleEffect("mayhem")]
public sealed record MayhemEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The alternative cost paid to cast this card using its mayhem ability.
  /// Typically a <see cref="ManaCost"/>.
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
