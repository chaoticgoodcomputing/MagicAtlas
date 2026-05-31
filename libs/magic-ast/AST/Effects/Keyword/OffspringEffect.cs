namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Offspring (Rule 702.175). "You may pay an additional {cost} as you cast this
/// spell. If you do, when this creature enters, create a 1/1 token that's a copy
/// of it." MAST records the keyword's presence and the additional cost; the
/// token-copy creation is conventionally inferred from the rules and captured in
/// the Reminder parenthetical.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type, mirroring <see cref="CyclingEffect"/>
/// and other cost-bearing keyword effects.
/// </para>
/// </summary>
[OracleEffect("offspring")]
public sealed record OffspringEffect : Effect
{
  /// <summary>
  /// The additional cost paid to create the 1/1 token copy on entry.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic base
  /// accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
