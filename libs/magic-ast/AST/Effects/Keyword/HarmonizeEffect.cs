namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Harmonize (Rule 702.157). "You may cast this card from your graveyard for its
/// harmonize cost. You may tap a creature you control to reduce that cost by {X},
/// where X is its power. Then exile this spell." MAST records the keyword's presence
/// and the printed harmonize cost; the graveyard-cast mechanics, power-based reduction,
/// and exile-after-cast are conventionally inferred from the rules (reminder text).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other cost-bearing
/// keyword effects (Cycling, Bestow, Dash) — most printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("harmonize")]
public sealed record HarmonizeEffect : Effect
{
  /// <summary>
  /// The harmonize cost printed on the card. Most commonly a <see cref="ManaCost"/>,
  /// but the polymorphic <see cref="Cost"/> base accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
