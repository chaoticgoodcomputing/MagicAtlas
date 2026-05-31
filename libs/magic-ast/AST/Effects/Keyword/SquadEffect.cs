namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Squad (Rule 702.150). "As an additional cost to cast this spell, you may pay
/// {cost} any number of times. When this creature enters, create that many token
/// copies of it." MAST records the keyword's presence and the squad cost; the
/// token-copy creation is conventionally inferred from the rules and captured in
/// the Reminder parenthetical.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type, mirroring <see cref="CyclingEffect"/>
/// and other cost-bearing keyword effects.
/// </para>
/// </summary>
[OracleEffect("squad")]
public sealed record SquadEffect : Effect
{
  /// <summary>
  /// The repeatable additional cost paid to generate token copies on entry.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic base
  /// accommodates future variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
