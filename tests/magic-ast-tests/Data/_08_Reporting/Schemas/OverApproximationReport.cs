namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

using Flowthru.Data.Schema;

/// <summary>
/// The <b>over-approximation report</b> (ADR-0004 §6, modeled-dependency completeness). Makes "which
/// GREENs rest on unmodeled conditions" a QUERY rather than an act of memory.
///
/// <para>Derived, never declared: an over-approximation is detectable as an <b>AST condition node the
/// projection dropped</b>, so the content is <c>AST condition nodes − conditions the projection
/// consumed</c>. "Consumed" is itself derived by ablation (delete the node, re-project, compare) — see
/// <c>MagicAST.Interaction.ConditionConsumption</c>. There is no hand-maintained register anywhere in this
/// pipeline, and a future parser/projection slice that starts reading a condition removes it from this
/// report automatically.</para>
///
/// <para>An accepted over-approximation stays LEGAL — the projection over-proposes and the operator/board
/// prunes (ADR-0003 §7). What ADR-0004 §6 requires is that it be <b>enumerable</b>. This report is that
/// enumeration. Corpus-gated diagnostic (gitignored, never committed); never a gate.</para>
///
/// <para>Not to be confused with <c>known-coarse-projections.json</c>: that whitelist names
/// <em>discriminators projected coarsely</em> (a resolution loss, hand-authored, gate-enforced); this
/// report enumerates <em>condition node instances dropped entirely</em> (a guard loss, fully derived,
/// diagnostic).</para>
/// </summary>
[FlowthruSchema]
public partial record OverApproximationReport
{
  [SerializedLabel("generatedAt")]
  public required string GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>Cards scanned (the parse-ready CSB combo-card union — the D1 CardPorts card set).</summary>
  [SerializedLabel("cardsScanned")]
  public int CardsScanned { get; init; }

  /// <summary>Total <c>Condition</c> nodes found in those cards' ASTs (the minuend).</summary>
  [SerializedLabel("conditionNodesTotal")]
  public int ConditionNodesTotal { get; init; }

  /// <summary>Condition nodes the projection CONSUMED — ablating them changes the port graph
  /// (an <c>InterveningIf</c> raising the §8 <c>Gated</c> flag is the dominant case). The subtrahend.</summary>
  [SerializedLabel("conditionNodesConsumed")]
  public int ConditionNodesConsumed { get; init; }

  /// <summary>Condition nodes the projection DROPPED — the over-approximations. The delta.</summary>
  [SerializedLabel("conditionNodesDropped")]
  public int ConditionNodesDropped { get; init; }

  /// <summary>Cards carrying at least one dropped condition node.</summary>
  [SerializedLabel("cardsWithDroppedConditions")]
  public int CardsWithDroppedConditions { get; init; }

  /// <summary>Distinct (card, port label) pairs at D1 tier <c>Green</c> whose ability carries a dropped
  /// condition — <b>the answer to "which GREENs rest on unmodeled conditions"</b>.</summary>
  [SerializedLabel("greenPortsOnUnmodeledConditions")]
  public int GreenPortsOnUnmodeledConditions { get; init; }

  /// <summary>The same count at tier <c>Amber</c> — already floored, so a dropped condition costs less
  /// there; reported so the GREEN figure is readable against its denominator.</summary>
  [SerializedLabel("amberPortsOnUnmodeledConditions")]
  public int AmberPortsOnUnmodeledConditions { get; init; }

  /// <summary>Dropped condition nodes grouped by <c>ConditionType</c>, most frequent first — the
  /// burn-down worklist (one projection slice per type closes every instance of it).</summary>
  [SerializedLabel("byConditionType")]
  public required IReadOnlyList<DroppedConditionTypeRow> ByConditionType { get; init; }

  /// <summary>Every dropped condition node, one row each, ranked GREEN-bearing first.</summary>
  [SerializedLabel("dropped")]
  public required IReadOnlyList<DroppedConditionRow> Dropped { get; init; }
}

/// <summary>One <c>ConditionType</c>'s dropped-instance tally.</summary>
[FlowthruSchema]
public partial record DroppedConditionTypeRow
{
  [SerializedLabel("conditionType")]
  public required string ConditionType { get; init; }

  [SerializedLabel("droppedCount")]
  public int DroppedCount { get; init; }

  [SerializedLabel("cardCount")]
  public int CardCount { get; init; }

  /// <summary>Distinct GREEN (card, label) ports resting on a dropped condition of this type.</summary>
  [SerializedLabel("greenPorts")]
  public int GreenPorts { get; init; }

  /// <summary>A sample card, for orientation.</summary>
  [SerializedLabel("exampleCard")]
  public required string ExampleCard { get; init; }
}

/// <summary>One dropped condition node: the unmodeled dependency, and the ports certified without it.</summary>
[FlowthruSchema]
public partial record DroppedConditionRow
{
  [SerializedLabel("card")]
  public required string Card { get; init; }

  [SerializedLabel("conditionType")]
  public required string ConditionType { get; init; }

  /// <summary>JSON path from the abilities array — e.g. <c>[1].Effects[0].Condition</c>.</summary>
  [SerializedLabel("path")]
  public required string Path { get; init; }

  /// <summary>The condition node's own JSON — the clause exactly as the AST states it.</summary>
  [SerializedLabel("conditionJson")]
  public required string ConditionJson { get; init; }

  /// <summary>The enclosing ability's oracle text, sliced from its <c>SourceSpan</c> — the human-readable
  /// clause ("You may cast this card from your graveyard as long as you control a Zombie."). Empty when
  /// the ability carries no span.</summary>
  [SerializedLabel("oracleClause")]
  public required string OracleClause { get; init; }

  /// <summary>The port labels the enclosing ability projects — each certified without regard to the
  /// condition, hence resting on it.</summary>
  [SerializedLabel("affectedPorts")]
  public required IReadOnlyList<string> AffectedPorts { get; init; }

  /// <summary>Those of <see cref="AffectedPorts"/> the D1 index tiers <c>Green</c> — the GREENs this
  /// unmodeled condition underwrites.</summary>
  [SerializedLabel("greenPorts")]
  public required IReadOnlyList<string> GreenPorts { get; init; }
}
