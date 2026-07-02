namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Change the target of target spell with a single target."
/// Redirects the target of a spell on the stack to a new legal target. Rule 115.7.
/// Covers Divert, Misdirection, and functional equivalents.
/// </summary>
[OracleEffect("changeTarget")]
public sealed record ChangeTargetEffect : Effect
{
  /// <summary>
  /// The spell whose target is being changed. Always a "target spell with a single target"
  /// reference in the canonical pattern.
  /// </summary>
  public required ObjectReference Spell { get; init; }
}
