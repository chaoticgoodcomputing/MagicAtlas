namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Miracle {cost} (Rule 702.94). An alternative cost a player may pay when they
/// draw this card as the first card drawn during their draw step. MAST records the
/// keyword's presence and its printed miracle cost; the draw-trigger and
/// cast-timing semantics are conventionally inferred from the rules.
/// </summary>
[OracleEffect("miracle")]
public sealed record MiracleEffect : Effect
{
  /// <summary>
  /// The alternative cost printed after "Miracle". Most commonly a single-color
  /// <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base accommodates
  /// any cost shape (generic + colored, {X}, etc.).
  /// </summary>
  public required Cost Cost { get; init; }
}
