namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Multiple effects combined.
/// </summary>
[OracleEffect("composite")]
public sealed record CompositeEffect : Effect
{
  public required IReadOnlyList<Effect> Effects { get; init; }
}
