namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Ninjutsu (Rule 702.49). "[Cost], Return an unblocked attacker you control to
/// hand: Put this card onto the battlefield from your hand tapped and attacking."
/// MAST records the keyword's presence and the ninjutsu cost; the return-attacker,
/// enter-tapped-and-attacking semantics are conventionally inferred from the rules
/// (per the descriptive-not-engine doctrine), mirroring the BestowEffect,
/// KickerEffect, and EchoEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Bestow, Kicker, Echo, Equip) — all known
/// Ninjutsu printings use a <see cref="ManaCost"/>, but the polymorphic base
/// accommodates future non-mana variants.
/// </para>
/// </summary>
[OracleEffect("ninjutsu")]
public sealed record NinjutsuEffect : Effect
{
  /// <summary>
  /// The ninjutsu activation cost. Most commonly a <see cref="ManaCost"/>, but
  /// the polymorphic <see cref="Cost"/> base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
