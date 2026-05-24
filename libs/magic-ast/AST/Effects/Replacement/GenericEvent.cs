namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Generic/unparsed event for complex cases.
/// </summary>
[OracleReplacementEvent("generic")]
public sealed record GenericEvent : ReplacementEvent
{
  /// <summary>
  /// Raw text description of the event.
  /// </summary>
  public required string Description { get; init; }
}
