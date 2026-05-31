namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Buyback [cost] (Rule 702.26). A keyword ability: you may pay an additional
/// [cost] as you cast this spell; if you do, put this card into your hand as
/// it resolves instead of into the graveyard. MAST records the keyword's
/// presence and the buyback cost; the conditional-hand-return resolution is
/// engine territory. Mirrors FlashbackEffect / KickerEffect for the
/// cost-parameterized keyword shape.
///
/// <para>
/// <see cref="BuybackCost"/> is the polymorphic <see cref="Cost"/> base type
/// because buyback can in principle appear with non-mana costs, mirroring
/// the FlashbackEffect / CyclingEffect pattern.
/// </para>
/// </summary>
[OracleEffect("buyback")]
public sealed record BuybackEffect : Effect
{
  /// <summary>The additional cost paid to return this card to hand on resolution.</summary>
  public required Cost BuybackCost { get; init; }
}
