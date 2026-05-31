namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Kicker (Rule 702.33). "You may pay an additional [cost] as you cast this spell."
/// MAST records the keyword's presence and the kicker cost; the conditional
/// resolution of kicked effects ("if this spell was kicked") is conventionally
/// inferred from the rules (per the descriptive-not-engine doctrine), mirroring
/// the EquipEffect, CyclingEffect, BestowEffect, and EchoEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow, Echo) — most printings
/// use a <see cref="ManaCost"/>, but the base accommodates future variants.
/// </para>
///
/// <para>
/// Scope: single-cost kicker only (Rule 702.33a). Multi-cost kicker
/// ("Kicker {A} and/or {B}", Rule 702.33b) and Multikicker (Rule 702.33c)
/// are deferred to a future batch.
/// </para>
/// </summary>
[OracleEffect("kicker")]
public sealed record KickerEffect : Effect
{
  /// <summary>
  /// The kicker cost paid as an additional cost when casting this spell. Most
  /// commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
