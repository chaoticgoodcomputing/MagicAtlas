namespace MagicAST.AST.Effects.Counter;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "put [count] [counter type] counters on [target]"
/// </summary>
[OracleEffect("putCounters")]
public sealed record PutCountersEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  [JsonPropertyName("counterType")]
  public required string CounterType { get; init; }

  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }
}
