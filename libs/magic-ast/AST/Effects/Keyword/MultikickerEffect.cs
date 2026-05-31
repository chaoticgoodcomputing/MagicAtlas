namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Multikicker (Rule 702.33c). "You may pay an additional [cost] any number of times
/// as you cast this spell."
/// MAST records the keyword's presence and the multikicker cost; the "for each time
/// it was kicked" scaling on conditional effects is inferred from the rules
/// (descriptive-not-engine doctrine).
///
/// <para>
/// Distinct from single-cost Kicker (Rule 702.33a), which ADR 0003 decomposes into an
/// <c>AdditionalCost{IsOptional:true}</c> on the card's cast cost rather than an oracle
/// effect: Multikicker is paid any number of times (Rule 702.33c) whereas single-cost
/// Kicker is paid at most once. Multikicker decomposition is deferred to a future batch,
/// so it retains this dedicated effect type for now.
/// </para>
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow, Echo, Kicker) — most
/// printings use a <see cref="ManaCost"/>, but the base accommodates future variants.
/// </para>
/// </summary>
[OracleEffect("multikicker")]
public sealed record MultikickerEffect : Effect
{
  /// <summary>
  /// The multikicker cost paid any number of times as an additional cost when
  /// casting this spell. Most commonly a <see cref="ManaCost"/>, but the
  /// polymorphic <see cref="Cost"/> base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
