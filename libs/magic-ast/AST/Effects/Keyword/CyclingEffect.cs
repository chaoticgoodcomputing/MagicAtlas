namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Cycling (Rule 702.32). An activated ability functioning only in hand:
/// "[Cost], Discard this card: Draw a card." MAST records the keyword's
/// presence and the cycling cost; the inner discard/draw structure is
/// conventionally inferred from the rules.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type so future typecycling
/// (Mountaincycling, Plainscycling) and similar variants can plug in
/// without a schema change — those carry the same shape with a different
/// concrete cost.
/// </para>
/// </summary>
[OracleEffect("cycling")]
public sealed record CyclingEffect : Effect
{
  /// <summary>
  /// The cost paid to cycle this card. Most commonly a <see cref="ManaCost"/>,
  /// but the polymorphic <see cref="Cost"/> base accommodates typecycling and
  /// similar variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
