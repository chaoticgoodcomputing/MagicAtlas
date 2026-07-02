namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Life change event: "would gain/lose life"
/// </summary>
[OracleReplacementEvent("lifeChange")]
public sealed record LifeChangeEvent : ReplacementEvent
{
  /// <summary>
  /// Whether this is life gain or life loss.
  /// </summary>
  public required string ChangeType { get; init; }
}
