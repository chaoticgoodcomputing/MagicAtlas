namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may pay an additional [cost] [any number of times] as you cast this spell" — the
/// shared permission a CR-static additional-cost keyword grants: Kicker (CR 702.33),
/// Multikicker (702.33c), Buyback (702.27), Entwine (702.42), Escalate (702.120), and the
/// repeatable copy/token keywords Squad / Replicate / Conspire (the cost half). (CR numbers
/// verified against rules-structure.json — 702.32 is Fading, 702.121 is Melee.)
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
  /// The additional cost this keyword grants permission to pay as you cast the spell,
  /// carrying its own <see cref="AdditionalCost.IsOptional"/> ("you may pay") and
  /// <see cref="AdditionalCost.Repeatable"/> ("any number of times") flags. This is the
  /// SAME <see cref="AdditionalCost"/> value type carried by the card-level
  /// <c>AdditionalCostsAttribute</c> for generic prose costs — ADR 0003's "share the
  /// value type, keep the wrappers": the keyword wrapper retains keyword identity via
  /// <c>KeywordSource</c>, and "additional cost to cast" clusters as a projection over
  /// both wrappers. The cost itself is a <see cref="ManaCost"/>, or a
  /// <see cref="CompositeCost"/> bundling mana with a non-mana cost (Conspire's "tap two
  /// untapped creatures you control that share a color with it"). The synthesized cost
  /// omits <see cref="AdditionalCost.SourceSpan"/> (identity rides on this ability's
  /// <c>KeywordSource</c>, not on a text frontier).
  /// </summary>
  public required AdditionalCost AdditionalCost { get; init; }
}
