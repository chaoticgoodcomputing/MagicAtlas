namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Transmute {cost} (Rule 702.49). An activated ability usable only in hand (as a
/// sorcery): pay the transmute cost and discard this card to search your library for
/// a card with the same mana value, reveal it, put it into your hand, then shuffle.
/// MAST records the keyword's presence and the transmute cost; the discard/search/
/// reveal/shuffle resolution is described by the rules and left to the engine.
///
/// <para>
/// Mirrors <see cref="CyclingEffect"/> — keyword presence + polymorphic cost,
/// no inner effect structure needed.
/// </para>
/// </summary>
[OracleEffect("transmute")]
public sealed record TransmuteEffect : Effect
{
  /// <summary>
  /// The cost paid to activate transmute. Most commonly a three-symbol mana cost
  /// (e.g. {1}{U}{U}), but the polymorphic <see cref="Cost"/> base accommodates
  /// any future variant costs.
  /// </summary>
  public required Cost Cost { get; init; }
}
