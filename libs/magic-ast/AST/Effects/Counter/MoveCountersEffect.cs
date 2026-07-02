namespace MagicAST.AST.Effects.Counter;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "move [count] [counter type] counter(s) from [source] onto [target]" — Graft
/// (CR 702.58): "Whenever another creature enters, if this permanent has a +1/+1
/// counter on it, you may move a +1/+1 counter from this permanent onto that creature."
///
/// <para>
/// A distinct cluster axis from <c>putCounters</c> / <c>removeCounters</c>: a move
/// relocates an existing counter, conserving the total, rather than creating or
/// destroying one. Modeled with the shared <see cref="ObjectReference"/> /
/// <see cref="Quantity"/> primitives (ADR 0003).
/// </para>
/// </summary>
[OracleEffect("moveCounters")]
public sealed record MoveCountersEffect : Effect
{
  /// <summary>The object the counters are moved FROM (Graft: "this creature").</summary>
  public required ObjectReference From { get; init; }

  /// <summary>The object the counters are moved ONTO (Graft: "it").</summary>
  public required ObjectReference To { get; init; }

  /// <summary>The kind of counter moved (lowercase: "+1/+1", "time", …).</summary>
  public required string CounterType { get; init; }

  /// <summary>How many counters to move.</summary>
  public required Quantity Count { get; init; }
}
