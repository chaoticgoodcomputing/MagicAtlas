namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Echo (Rule 702.30). "At the beginning of your upkeep, if this came under your
/// control since the beginning of your last upkeep, sacrifice it unless you pay
/// its echo cost." MAST records the keyword's presence and the echo cost; the
/// upkeep-trigger / sacrifice-unless-pay semantics are conventionally inferred
/// from the rules (per the descriptive-not-engine doctrine), mirroring the
/// EquipEffect, CyclingEffect, and BestowEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow) — most printings use a
/// <see cref="ManaCost"/>, but the base accommodates future variants.
/// </para>
/// </summary>
[OracleEffect("echo")]
public sealed record EchoEffect : Effect
{
  /// <summary>
  /// The echo cost paid on each upkeep to avoid sacrificing this permanent.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic
  /// <see cref="Cost"/> base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
