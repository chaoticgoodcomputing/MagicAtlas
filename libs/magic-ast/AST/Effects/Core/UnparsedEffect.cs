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
  /// Non-null projection of the inherited <see cref="Effect.SourceSpan"/> for the
  /// <see cref="IUnparsed"/> contract, which needs a concrete span to attribute the
  /// failure to its oracle line. Every unparsed node is constructed with a span, so
  /// this is effectively always the real span; the <c>?? Empty</c> is a defensive
  /// floor. An explicit interface implementation — NOT a serialized property, so the
  /// single serialized SourceSpan remains the inherited <see cref="Effect.SourceSpan"/>
  /// and there is no duplicate key.
  /// </summary>
  TextSpan IUnparsed.SourceSpan => SourceSpan ?? TextSpan.Empty;

  public required string RawText { get; init; }
}
