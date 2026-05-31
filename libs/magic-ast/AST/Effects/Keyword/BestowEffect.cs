namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Bestow (Rule 702.103). "If you cast this card for its bestow cost, it's an
/// Aura spell with enchant creature. It becomes a creature again if it's not
/// attached." MAST records the keyword's presence and the bestow cost; the
/// alternative-cost / Aura-mode / unattach semantics are conventionally inferred
/// from the rules (per the descriptive-not-engine doctrine), mirroring the
/// EquipEffect and CyclingEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling) — most printings use a
/// <see cref="ManaCost"/>, but the base accommodates future variants.
/// </para>
/// </summary>
[OracleEffect("bestow")]
public sealed record BestowEffect : Effect
{
  /// <summary>
  /// The bestow cost paid to cast this card as an Aura spell. Most commonly a
  /// <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base
  /// accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
