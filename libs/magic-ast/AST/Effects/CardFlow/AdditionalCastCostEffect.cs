namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may pay an additional [cost] [any number of times] as you cast this spell" — the
/// shared permission a CR-static additional-cost keyword grants: Kicker (CR 702.32),
/// Multikicker (702.33), Buyback (702.27), Entwine (702.42), Escalate (702.121), and the
/// repeatable copy/token keywords Squad (702.158) / Replicate / Conspire (702.79's cost
/// half).
///
/// <para>
/// These keywords are static abilities per their CR, so the keyword combinator emits a
/// <c>StaticAbility</c> carrying this effect with its <c>KeywordSource</c> label — NOT a
/// card-level <c>AdditionalCostsAttribute</c>, which loses the keyword identity and so
/// cannot express the production/reference duality ADR 0003 requires ("Kicker abilities
/// cost {1} less"). Sibling of <see cref="AlternativeCastEffect"/> (alternative cost) on
/// the additional-cost side.
/// </para>
///
/// <para>
/// Riders keyed to whether/how-many-times the cost was paid (Buyback's return-to-hand
/// replacement, Conspire/Replicate's copy, Squad's ETB tokens) are SEPARATE abilities on
/// the card — they reference "if the [keyword] cost was paid", and the topology of two
/// abilities is faithful (ADR 0004). This effect models only the additional cost itself.
/// </para>
/// </summary>
[OracleEffect("additionalCastCost")]
public sealed record AdditionalCastCostEffect : Effect
{
  /// <summary>
  /// The additional cost paid as you cast this spell. A <see cref="ManaCost"/>, or a
  /// <see cref="CompositeCost"/> bundling mana with a non-mana cost (Conspire's "tap two
  /// untapped creatures you control that share a color with it").
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>
  /// "you may pay" — the additional cost is optional (Kicker, Multikicker, Buyback,
  /// Squad, Replicate). False for a mandatory additional cost.
  /// </summary>
  public bool IsOptional { get; init; }

  /// <summary>
  /// "any number of times" — the additional cost may be paid repeatedly (Multikicker,
  /// Squad, Replicate). False when it may be paid at most once (Kicker, Buyback).
  /// </summary>
  public bool Repeatable { get; init; }
}
