namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "As this [permanent] enters, you may pay N life. If you don't, it enters
/// tapped." — the Shockland pattern (Rule 614.1c, as-enters static
/// replacement). The oracle line describes a single optional payment made
/// during the as-enters event: the controller MAY pay the stated life amount;
/// the <see cref="IOptionalEffect.IfYouDoNot"/> branch carries the fallback
/// effect when payment is declined (typically <c>EntersTappedEffect</c>).
///
/// <para>Why a dedicated effect rather than overloading <c>EntersTappedEffect</c>:
/// the surface verb in the oracle text is "you may pay [N] life on entry" —
/// that's the optional action. The enters-tapped consequence lives on the
/// negative branch via the reusable <see cref="IOptionalEffect.IfYouDoNot"/>
/// machinery introduced by batch 41. Modelling the verb directly keeps the AST
/// descriptively faithful (MAST records what oracle text says, not what the
/// rules engine derives at run-time).</para>
///
/// <para>Distinct from the <c>EntersTappedEffect</c> + <c>EntryCondition</c>
/// shape used for checklands/fastlands: those carry a board-state predicate
/// ("unless you control two or fewer lands"); painlands/shocklands carry a
/// life-payment cost instead. Rule 614.1c covers both as as-enters static
/// replacement effects, but the descriptive shapes differ.</para>
/// </summary>
[OracleEffect("payLifeOnEntry")]
public sealed record PayLifeOnEntryEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The amount of life the controller may pay on entry. Typically a
  /// <see cref="LiteralQuantity"/> (every printed painland/shockland uses a
  /// fixed integer); the polymorphic <see cref="Quantity"/> base accommodates
  /// future variants (e.g. an X-cost printing).
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>Whether this effect carries a "you may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to pay. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to pay. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
