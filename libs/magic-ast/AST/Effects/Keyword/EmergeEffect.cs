namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Emerge (Rule 702.119). An alternative cost: sacrifice a creature and pay the
/// emerge cost reduced by that creature's mana value. MAST records the keyword's
/// presence and the printed emerge cost; the sacrifice-a-creature, cost-reduction,
/// and timing semantics are conventionally inferred from the rules.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type so future variants can plug in
/// without a schema change.
/// </para>
/// </summary>
[OracleEffect("emerge")]
public sealed record EmergeEffect : Effect
{
  /// <summary>
  /// The emerge cost printed on the card. Most commonly a <see cref="ManaCost"/>.
  /// The cost can be paid reduced by the sacrificed creature's mana value, but
  /// that reduction is engine territory — MAST records only the printed value.
  /// </summary>
  public required Cost Cost { get; init; }
}
