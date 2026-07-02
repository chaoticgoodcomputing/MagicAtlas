namespace MagicAST.AST.Effects.Counter;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "put [count] [counter type] counters on [target]"
/// </summary>
[OracleEffect("putCounters")]
public sealed record PutCountersEffect : Effect
{
  public required ObjectReference Target { get; init; }

  public required string CounterType { get; init; }

  public required Quantity Count { get; init; }
}
