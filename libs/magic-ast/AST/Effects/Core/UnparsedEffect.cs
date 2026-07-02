namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// An effect that couldn't be parsed.
/// </summary>
[OracleEffect("unparsed")]
public sealed record UnparsedEffect : Effect, IUnparsed
{
  /// <summary>
  /// Location of this unparsed effect in the original oracle text.
  /// </summary>
  public required TextSpan SourceSpan { get; init; }

  public required string RawText { get; init; }
}
