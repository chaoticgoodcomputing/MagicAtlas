namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mutate (Rule 702.140). An alternative casting cost: if you cast this spell for
/// its mutate cost, put it over or under target non-Human creature you own.
/// MAST records the keyword's presence and the mutate cost only; the over/under
/// stacking mechanics and the resulting merged permanent are engine territory
/// (Reminder text describes them).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic <see cref="AST.Costs.Cost"/> base type,
/// consistent with other keyword-cost effects (e.g. <see cref="CyclingEffect"/>).
/// </para>
/// </summary>
[OracleEffect("mutate")]
public sealed record MutateEffect : Effect
{
  /// <summary>
  /// The cost paid to cast this spell via its mutate cost.
  /// </summary>
  public required Cost Cost { get; init; }
}
