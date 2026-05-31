namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "counter [target spell/ability]"
/// </summary>
[OracleEffect("counterSpell")]
public sealed record CounterSpellEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "unless its controller pays [cost]"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? UnlessCost { get; init; }
}
