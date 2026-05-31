namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Spectacle [cost] (Rule 702.136). "You may cast this spell for its spectacle
/// cost if an opponent lost life this turn." An alternative-cost keyword from
/// Ravnica Allegiance. MAST records the keyword's presence and the spectacle
/// cost; the opponent-lost-life precondition check and alternative-cast
/// semantics are engine territory (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// cost-bearing keyword effects (Kicker, Flashback, Madness, Dash, Plot).
/// All known printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("spectacle")]
public sealed record SpectacleEffect : Effect
{
  /// <summary>
  /// The spectacle cost paid as the alternative casting cost. Most commonly a
  /// <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base
  /// accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
