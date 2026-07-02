namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "tap [target]"
/// </summary>
[OracleEffect("tap")]
public sealed record TapEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Multiplicity of the target set. Null = single target (default "tap target X").
  /// Used for variable- or literal-count target sets, e.g. "Tap X target lands"
  /// (Count = VariableQuantity "X") or "Tap two target creatures" (Count = literal 2).
  /// Mirrors <see cref="MagicAST.AST.Effects.ZoneChange.SacrificeEffect.Count"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }
}
