namespace MagicAST.AST.Effects.Counter;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "remove [count] [counter type] counters from [target]"
/// </summary>
[OracleEffect("removeCounters")]
public sealed record RemoveCountersEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  [JsonPropertyName("counterType")]
  public required string CounterType { get; init; }

  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }
}
