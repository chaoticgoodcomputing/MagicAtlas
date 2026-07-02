namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Unearth (Rule 702.84). "[Cost]: Return this card from your graveyard to the
/// battlefield. It gains haste. Exile it at the beginning of the next end step
/// or if it would leave the battlefield. Unearth only as a sorcery."
/// MAST records the keyword's presence and the unearth cost; the return-to-
/// battlefield, haste-grant, and exile-at-end-step semantics are conventionally
/// inferred from the rules (per the descriptive-not-engine doctrine), mirroring
/// the KickerEffect, BestowEffect, and EchoEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow, Echo, Kicker) — all
/// known Unearth printings use a <see cref="ManaCost"/>, but the base
/// accommodates future variants.
/// </para>
/// </summary>
[OracleEffect("unearth")]
public sealed record UnearthEffect : Effect
{
  /// <summary>
  /// The unearth cost paid to return this card from the graveyard to the
  /// battlefield. Most commonly a <see cref="ManaCost"/>, but the polymorphic
  /// <see cref="Cost"/> base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
