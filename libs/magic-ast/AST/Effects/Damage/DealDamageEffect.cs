namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "deals N damage to [target]"
/// </summary>
[OracleEffect("dealDamage")]
public sealed record DealDamageEffect : Effect
{
  public required Quantity Amount { get; init; }

  public required ObjectReference Target { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Source { get; init; }
}
