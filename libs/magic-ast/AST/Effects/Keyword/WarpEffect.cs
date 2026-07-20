namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Warp [cost] (Rule 702.185). "You may cast this card from your hand for its
/// warp cost. It enters the battlefield tapped..." An alternative-cast keyword
/// that lets a controller cast a permanent for an alternative mana cost, with
/// the permanent entering tapped as a consequence. MAST records the keyword
/// and the warp cost; the alternative-cast and enters-tapped mechanics are
/// engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Mana-cost-parameterized keyword; mirrors the KickerEffect, PlotEffect,
/// and FlashbackEffect shape.
/// </para>
/// </summary>
[OracleEffect(
  "warp",
  NearDuplicateOf = new[] { "tap" },
  Reason = "Unrelated concepts; heuristic false positive (Levenshtein 2). 'tap' is the tap action; 'warp' is the Warp keyword effect. Coincidental letter proximity, no semantic overlap."
)]
public sealed record WarpEffect : Effect
{
  /// <summary>
  /// The alternative mana cost paid to cast this card via warp. Always a
  /// <see cref="ManaCost"/> in all known printings; the polymorphic
  /// <see cref="Cost"/> base accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
