namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "destroy [target]"
/// </summary>
[OracleEffect("destroy")]
public sealed record DestroyEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "It can't be regenerated"
  /// </summary>
  public bool CantBeRegenerated { get; init; }
}
