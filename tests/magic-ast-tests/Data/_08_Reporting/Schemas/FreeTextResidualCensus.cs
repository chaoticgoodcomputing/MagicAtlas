using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Free-text residual census — the initiative-05 ("de-string the AST leaves") burn-down measurement,
/// RECOMPUTED. A DIAGNOSTIC, never a gate: the gate is the stateless named-(card, sink) whitelist
/// invariant in <c>GoldFreeTextWhitelistTests</c>, which is unaffected by this report.
///
/// <para>This replaces <c>libs/magic-ast/schema/destring-worklist.json</c>, which held the same numbers
/// as a hand-committed, deliberately frozen snapshot with no regenerator — the exact shape ADR-0004
/// retired in <c>discriminator-baseline.json</c>, and carrying the same drift risk (it was seeded over
/// 951 golds; the corpus has since grown well past that, so every number in it was stale).</para>
/// </summary>
[FlowthruSchema]
public partial record FreeTextResidualCensus
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>Committed golds under <c>Fixtures/HandParsedCards/**</c> that were walked.</summary>
  [SerializedLabel("goldsScanned")]
  public int GoldsScanned { get; init; }

  /// <summary>Golds carrying at least one free-text sink (the union across sinks).</summary>
  [SerializedLabel("distinctAffectedGolds")]
  public int DistinctAffectedGolds { get; init; }

  /// <summary>Total free-text instances across every sink.</summary>
  [SerializedLabel("totalInstances")]
  public int TotalInstances { get; init; }

  /// <summary>Instances whose (card, sink) is NOT on the whitelist. The gate fails if this is non-zero,
  /// so a healthy run reports 0 — it is here to make the gate's subject visible, not to replace it.</summary>
  [SerializedLabel("unwhitelistedInstances")]
  public int UnwhitelistedInstances { get; init; }

  /// <summary>Whitelist entries naming a (card, sink) that no longer carries the sink. The gate fails on
  /// these too; reported so a burn-down slice can see its own cleanup list.</summary>
  [SerializedLabel("deadWhitelistEntries")]
  public required IReadOnlyList<string> DeadWhitelistEntries { get; init; }

  /// <summary>Per-sink burn-down, ordered by instance count descending. Every sink is reported, including
  /// the ones at zero — a sink that silently vanishes is a burn-down you can no longer see the end of.</summary>
  [SerializedLabel("bySink")]
  public required IReadOnlyList<FreeTextSinkBurndown> BySink { get; init; }
}

/// <summary>One sink's burn-down: how much debt it carries and which golds carry it.</summary>
[FlowthruSchema]
public partial record FreeTextSinkBurndown
{
  [SerializedLabel("sink")]
  public required string Sink { get; init; }

  [SerializedLabel("instances")]
  public int Instances { get; init; }

  [SerializedLabel("cards")]
  public int Cards { get; init; }

  /// <summary>Carve-outs tagged <c>debt</c> — slated for a burn-down slice.</summary>
  [SerializedLabel("debtCards")]
  public int DebtCards { get; init; }

  /// <summary>Carve-outs tagged <c>irreducible</c> — permanent, no structured representation.</summary>
  [SerializedLabel("irreducibleCards")]
  public int IrreducibleCards { get; init; }

  /// <summary>Every gold carrying this sink, by its whitelist key (path under
  /// <c>HandParsedCards/</c> without <c>.json</c>), ordinal-sorted.</summary>
  [SerializedLabel("cardList")]
  public required IReadOnlyList<string> CardList { get; init; }
}
