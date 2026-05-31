namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Dash (Rule 702.109). "You may cast this card for its dash cost. If you do,
/// it gains haste, and it's returned from the battlefield to its owner's hand
/// at the beginning of the next end step." MAST records the keyword's presence
/// and the dash cost; the haste-grant and return-to-hand-at-end-step semantics
/// are conventionally inferred from the rules (per the descriptive-not-engine
/// doctrine), mirroring the KickerEffect, BestowEffect, and EchoEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Kicker, Bestow, Echo, Unearth, Plot) — all
/// known Dash printings use a <see cref="ManaCost"/>, but the polymorphic base
/// accommodates future non-mana variants.
/// </para>
/// </summary>
[OracleEffect("dash")]
public sealed record DashEffect : Effect
{
  /// <summary>
  /// The dash cost paid to cast this card via the dash alternative cost. Most
  /// commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
