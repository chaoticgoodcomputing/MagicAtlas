namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "untap [target]"
/// </summary>
[OracleEffect("untap")]
public sealed record UntapEffect : Effect
{
  public required ObjectReference Target { get; init; }
}
