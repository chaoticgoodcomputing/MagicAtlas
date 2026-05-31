namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Morph (Rule 702.37). A static ability functioning in any zone from which
/// the card could be cast: the player may cast it face down as a 2/2 colorless
/// creature spell for {3}, and may turn it face up later by paying its morph
/// cost. MAST records the keyword's presence and the morph cost; the
/// cast-face-down rules and turn-face-up mechanics are conventionally inferred
/// from the rules (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type, mirroring
/// <see cref="CyclingEffect"/> and <see cref="EquipEffect"/>. While Morph
/// costs in printed cards are always mana, the base accommodates future
/// variants (e.g., Megamorph and other morph-family keywords route through
/// separate Effect types) without a schema change.
/// </para>
/// </summary>
[OracleEffect("morph")]
public sealed record MorphEffect : Effect
{
  /// <summary>
  /// The cost paid to turn this card face up. Most commonly a <see cref="ManaCost"/>;
  /// the polymorphic <see cref="Cost"/> base accommodates future non-mana morph variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
