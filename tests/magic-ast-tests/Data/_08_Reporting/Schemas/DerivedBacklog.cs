using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// The <b>derived interaction backlog</b> (ADR-0004 §2, issue #32) — computed, never stored. Replaces the
/// dissolved <c>topology-scaffold.json</c> <c>holes{}</c> registry (retired by #26) AND the hand-maintained
/// <c>libs/mast-interaction/known-coarse-projections.json</c> whitelist (deleted by this issue): both were
/// a set difference someone had frozen into a file.
///
/// <para><b>The formula.</b> <c>backlog = projected(corpus) − served(rollup ∪ guards) − asserted-unarmable(golds)</c>,
/// all three terms derived:
/// <list type="bullet">
///   <item><b>projected(corpus)</b> — every discriminator the PortWalk dispatch can produce a port from
///     (every <c>EffectType</c>, <c>CostType</c>, trigger <c>Event</c>, restriction), reflected from the
///     AST schema + enums. Corpus-INDEPENDENT: it is the projectable universe, not the observed one, which
///     is why the backlog size is deterministic on a corpus-less checkout.</item>
///   <item><b>served</b> — the discriminators <see cref="MagicAST.Interaction.PortWalkProjection"/> gives a
///     semantic projection a flow rule reads, reflected from engine code (not a stored list).</item>
///   <item><b>asserted-unarmable</b> — the subtrahend, derived LIVE from the interaction golds carrying a
///     <c>no_arm</c> assertion over a projected port. One entry today (<c>anyNumberInDeck</c>, from
///     <c>rat-colony-deck-construction-terminal.json</c>); an empty subtrahend is the normal case.</item>
/// </list></para>
///
/// <para><b>The distinction that is the whole job:</b> an unserved projection with no gold is
/// <b>backlog</b>; an unserved projection with an asserted-absence gold is a <b>decision</b> (§2). The two
/// are the <see cref="Dimensions"/> entries vs. the <see cref="Decisions"/> list here.</para>
///
/// <para><b>Not a gate.</b> Derivation is Flowthru's job; the GATE is the NUnit
/// <c>PortWalkExhaustivenessTests</c>, which re-runs the same pure <c>BacklogDerivation.Compute</c> over the
/// live schema + engine + golds rather than reading this (gitignored) output.</para>
/// </summary>
[FlowthruSchema]
public partial record DerivedBacklog
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>Total discriminators in the backlog across all four dispatch dimensions.</summary>
  [SerializedLabel("totalBacklog")]
  public int TotalBacklog { get; init; }

  /// <summary>Per-dispatch-dimension counts + the backlog member discriminators.</summary>
  [SerializedLabel("dimensions")]
  public required IReadOnlyList<BacklogDimension> Dimensions { get; init; }

  /// <summary>The asserted-unarmable subtrahend, live from the golds — an unserved projection that has a
  /// gold, hence a DECISION not backlog. Each names the gold and the claim, so it is traceable.</summary>
  [SerializedLabel("decisions")]
  public required IReadOnlyList<BacklogDecision> Decisions { get; init; }

  /// <summary>Discriminators excluded from the backlog as NOT-A-PORT-CANDIDATE — the recognition-failure
  /// escape hatches (<c>unparsed</c>/<c>unstructured</c>/<c>Other</c>) that hold raw text. They belong on
  /// the parse ledger (fidelity ladder / L2), never the interaction backlog; listing them here (rather than
  /// silently dropping them) keeps the accounting total — see ADR-0004 Appendix C.</summary>
  [SerializedLabel("excludedNotPortCandidates")]
  public required IReadOnlyList<BacklogExclusion> ExcludedNotPortCandidates { get; init; }

  /// <summary>A gold that asserts <c>no_arm</c> over a name that is not (or no longer) a discriminator, or
  /// over one that is now SERVED — a stale or contradictory decision. Empty in the healthy case; surfaced
  /// rather than dropped so a rotted gold is loud.</summary>
  [SerializedLabel("danglingDecisions")]
  public required IReadOnlyList<BacklogDecision> DanglingDecisions { get; init; }

  /// <summary>The attribute-AXIS backlog (ADR-0003 §4a.1): declared attribute axes no gold witnesses. The
  /// <c>owner</c> axis lives here — an unwitnessed prediction is backlog, and it reappears (leaves this
  /// list) the moment a gold's port carries an <c>owner</c> attr. Derived: declared − witnessed(golds).</summary>
  [SerializedLabel("attributeAxes")]
  public required AttributeAxisBacklog AttributeAxes { get; init; }

  /// <summary>Combo-level unserved demand (a DIFFERENT granularity from the discriminator backlog): combos
  /// whose interaction the engine reconstructs no spanning cycle over. Derived from the reconstruction pins
  /// (<c>combo-axis-expectations.json</c> <c>unreconstructed</c>, #31a's "#32 inheritance"). Graceful
  /// degrade: empty + a note when the pins file is absent.</summary>
  [SerializedLabel("combos")]
  public required ComboBacklog Combos { get; init; }
}

/// <summary>One PortWalk dispatch dimension: its full vocabulary, served count, and the backlog members.</summary>
[FlowthruSchema]
public partial record BacklogDimension
{
  /// <summary><c>effectType</c> / <c>costType</c> / <c>triggerEvent</c> / <c>restriction</c>.</summary>
  [SerializedLabel("dimension")]
  public required string Dimension { get; init; }

  /// <summary>|projected(corpus)| — every discriminator the dispatch can produce.</summary>
  [SerializedLabel("all")]
  public int All { get; init; }

  /// <summary>|served| — the semantically-projected discriminators (PortWalkProjection).</summary>
  [SerializedLabel("served")]
  public int Served { get; init; }

  /// <summary>|backlog| for this dimension.</summary>
  [SerializedLabel("backlogCount")]
  public int BacklogCount { get; init; }

  /// <summary>The backlog discriminators (unserved, no gold, port-candidate), sorted.</summary>
  [SerializedLabel("backlog")]
  public required IReadOnlyList<string> Backlog { get; init; }
}

/// <summary>An asserted-unarmable decision, traceable to its gold.</summary>
[FlowthruSchema]
public partial record BacklogDecision
{
  [SerializedLabel("dimension")]
  public required string Dimension { get; init; }

  [SerializedLabel("discriminator")]
  public required string Discriminator { get; init; }

  [SerializedLabel("gold")]
  public required string Gold { get; init; }

  [SerializedLabel("claim")]
  public required string Claim { get; init; }
}

/// <summary>A discriminator excluded as not-a-port-candidate (parse-ledger, not interaction backlog).</summary>
[FlowthruSchema]
public partial record BacklogExclusion
{
  [SerializedLabel("dimension")]
  public required string Dimension { get; init; }

  [SerializedLabel("discriminator")]
  public required string Discriminator { get; init; }

  [SerializedLabel("reason")]
  public required string Reason { get; init; }
}

/// <summary>The attribute-axis backlog (declared − witnessed).</summary>
[FlowthruSchema]
public partial record AttributeAxisBacklog
{
  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>The attribute axes any gold's port carries — the witnessed set, derived live.</summary>
  [SerializedLabel("witnessed")]
  public required IReadOnlyList<string> Witnessed { get; init; }

  /// <summary>Declared attribute axes no gold witnesses — the axis backlog (currently <c>owner</c>).</summary>
  [SerializedLabel("backlog")]
  public required IReadOnlyList<string> Backlog { get; init; }
}

/// <summary>Combo-level unserved demand.</summary>
[FlowthruSchema]
public partial record ComboBacklog
{
  [SerializedLabel("note")]
  public required string Note { get; init; }

  [SerializedLabel("available")]
  public bool Available { get; init; }

  [SerializedLabel("unreconstructed")]
  public required IReadOnlyList<UnreconstructedCombo> Unreconstructed { get; init; }
}

/// <summary>A combo with no reconstructed spanning cycle (a "Missed" tier — combo-level backlog).</summary>
[FlowthruSchema]
public partial record UnreconstructedCombo
{
  [SerializedLabel("combo")]
  public required string Combo { get; init; }

  [SerializedLabel("verdict")]
  public required string Verdict { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }
}
