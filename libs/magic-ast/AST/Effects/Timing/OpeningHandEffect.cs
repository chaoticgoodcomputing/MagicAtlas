namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "If this card is in your opening hand, you may begin the game with it on
/// the battlefield." — the Leyline pattern (Rule 103.6a, opening-hand
/// pre-game placement).
///
/// <para>
/// The oracle line describes a conditional pre-game placement: IF the card is
/// in the controller's opening hand, the controller MAY place it onto the
/// battlefield before the game begins. <see cref="IOptionalEffect.IsOptional"/>
/// is true because the oracle text carries "you may".
/// </para>
///
/// <para>
/// MAST records this as a descriptive static ability. The conditional ("if this
/// card is in your opening hand") is structural to the ability — all printed
/// Leylines use identical phrasing — so no separate Condition field is needed;
/// the presence of this effect type implies the opening-hand condition.
/// </para>
///
/// <para>
/// Rule context: Rule 103.6a governs the mechanic. Rule 103.5 (mulligan) runs
/// before 103.6 actions, so the opening hand is fully resolved before this
/// placement option is offered.
/// </para>
/// </summary>
[OracleEffect("openingHand")]
public sealed record OpeningHandEffect : Effect, IOptionalEffect
{
  /// <summary>Whether this effect carries a "you may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }
}
