namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Evoke [cost] (Rule 702.73). "You may cast this spell for its evoke cost. If
/// you do, it's sacrificed when it enters." An alternative-cost keyword found
/// on Elemental creatures from Lorwyn block and later sets. MAST records the
/// keyword's presence and the evoke cost; the sacrifice-on-entry semantics are
/// engine territory (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// cost-bearing keyword effects (Kicker, Flashback, Dash, Plot). All known
/// printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("evoke")]
public sealed record EvokeEffect : Effect
{
  /// <summary>
  /// The evoke cost paid as the alternative casting cost. Most commonly a
  /// <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/> base
  /// accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
