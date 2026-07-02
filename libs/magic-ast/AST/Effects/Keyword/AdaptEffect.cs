namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Adapt N (CR 701.46a: "\"Adapt N\" means \"If this permanent has no +1/+1 counters
/// on it, put N +1/+1 counters on it.\"")
///
/// <para>
/// MAST records the keyword-action and its integer value N as a descriptive node.
/// The conditional check (no +1/+1 counters present) and counter placement are
/// engine territory — the node names the action, not the execution.
/// </para>
///
/// <para>
/// Integer-parameterized keyword-action; mirrors the <see cref="ScryEffect"/> shape
/// with a <see cref="Count"/> quantity field. Distinct from
/// <see cref="MonstrosityEffect"/> — Adapt is an activated keyword-action, Monstrosity
/// is a different mechanic.
/// </para>
/// </summary>
[OracleEffect("adapt")]
public sealed record AdaptEffect : Effect
{
  /// <summary>The adapt value N printed on the card (e.g., "Adapt 2" → 2).</summary>
  public required Quantity Count { get; init; }
}
