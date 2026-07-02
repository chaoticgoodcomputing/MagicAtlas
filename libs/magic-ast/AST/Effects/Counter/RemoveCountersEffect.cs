namespace MagicAST.AST.Effects.Counter;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "remove [count] [counter type] counters from [target]"
/// </summary>
[OracleEffect("removeCounters")]
public sealed record RemoveCountersEffect : Effect
{
  public required ObjectReference Target { get; init; }

  public required string CounterType { get; init; }

  public required Quantity Count { get; init; }
}
