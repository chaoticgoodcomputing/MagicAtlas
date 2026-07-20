namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

using Flowthru.Data.Schema;

/// <summary>
/// <b>Join 1 (ADR-0004 §4)</b> — <c>quarantined-oracle-text → gold → shipped combo tier</c>. The Parse
/// track's fidelity quarantine crossed with the Interaction track's pinned combo tiers.
///
/// <para>This is the Suture Priest artifact, materialized. That incident's quarantine entry was accurate,
/// current, and doing its job — it was simply a Parse-track fact with no edge to Interaction-track
/// tiering, so a card sat on the quarantine while underwriting a shipped GREEN. Regeneration and
/// gitignoring both verify a fact against itself and would have caught nothing; only this join, which
/// verifies one track's fact against another track's claims, catches it.</para>
///
/// <para>Derived, gitignored, never committed. The GATE over it is
/// <c>Tests/CrossTrackJoins/QuarantineTierJoinGateTests.cs</c>, which re-runs the same pure
/// <c>CrossTrackJoiner</c> rather than reading this file.</para>
/// </summary>
[FlowthruSchema]
public partial record QuarantineTierJoin
{
  [SerializedLabel("generatedAt")]
  public required string GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>Entries on the Parse track's quarantine (side A — must be non-zero or the join is vacuous).</summary>
  [SerializedLabel("quarantinedFixtureCount")]
  public int QuarantinedFixtureCount { get; init; }

  /// <summary>Those whose fixture path resolved to a card name through the gold tree.</summary>
  [SerializedLabel("resolvedFixtureCount")]
  public int ResolvedFixtureCount { get; init; }

  /// <summary>Pinned combos on the Interaction track (side B — must be non-zero).</summary>
  [SerializedLabel("pinCount")]
  public int PinCount { get; init; }

  /// <summary>Of those, pinned GREEN — the population the gate is about.</summary>
  [SerializedLabel("greenPinCount")]
  public int GreenPinCount { get; init; }

  /// <summary>Either side empty ⇒ the join proves nothing and is reported red, never green.</summary>
  [SerializedLabel("vacuous")]
  public bool Vacuous { get; init; }

  /// <summary>Crossings whose pin is GREEN and whose (combo, fixture) pair is NOT acknowledged — the
  /// gate's failure set.</summary>
  [SerializedLabel("violationCount")]
  public int ViolationCount { get; init; }

  /// <summary>Quarantine entries whose fixture path names no gold — a broken Parse-track key, which
  /// would silently shrink side A.</summary>
  [SerializedLabel("unresolvedFixtures")]
  public required IReadOnlyList<string> UnresolvedFixtures { get; init; }

  /// <summary>Every crossing, violations first.</summary>
  [SerializedLabel("risks")]
  public required IReadOnlyList<QuarantineRiskRow> Risks { get; init; }
}

/// <summary>One crossing: a pinned combo resting on a card whose gold fixture is quarantined.</summary>
[FlowthruSchema]
public partial record QuarantineRiskRow
{
  [SerializedLabel("comboId")]
  public required string ComboId { get; init; }

  [SerializedLabel("tier")]
  public required string Tier { get; init; }

  [SerializedLabel("card")]
  public required string Card { get; init; }

  [SerializedLabel("fixture")]
  public required string Fixture { get; init; }

  [SerializedLabel("tag")]
  public required string Tag { get; init; }

  [SerializedLabel("reason")]
  public required string Reason { get; init; }

  /// <summary>The <c>(comboId, fixture)</c> pair carries a named, human-reviewed carve-out.</summary>
  [SerializedLabel("acknowledged")]
  public bool Acknowledged { get; init; }

  /// <summary>GREEN and unacknowledged — the Suture Priest shape.</summary>
  [SerializedLabel("violation")]
  public bool Violation { get; init; }

  /// <summary>Interaction golds naming this card — the "gold" leg of the join, for orientation.</summary>
  [SerializedLabel("interactionGolds")]
  public required IReadOnlyList<string> InteractionGolds { get; init; }
}

/// <summary>
/// <b>Join 2 (ADR-0004 §4, §2 soundness half)</b> — <c>gold <c>declares</c> → rollup rule → engine
/// guard</c>. The <b>guard→witness map</b>: for every residual rule, the golds that witness it.
///
/// <para><b>Zero hand-authored entries.</b> The map is grouped out of the golds' own <c>declares</c>
/// blocks — the witness set of a rule <i>is</i> the set of gold ids declaring it. The rollup and the
/// engine-source scan are joined <i>onto</i> that map to check the downstream legs; neither contributes a
/// rule or a witness. ADR-0003 §6 states "every guard is registered with its witnessing golds" but never
/// gated it; this is the queryable output that lets issue #34 close the bijection.</para>
///
/// <para>Derived, gitignored, never committed. Gate:
/// <c>Tests/CrossTrackJoins/GuardWitnessJoinGateTests.cs</c>.</para>
/// </summary>
[FlowthruSchema]
public partial record GuardWitnessJoin
{
  [SerializedLabel("generatedAt")]
  public required string GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  [SerializedLabel("goldsScanned")]
  public int GoldsScanned { get; init; }

  /// <summary>Golds carrying at least one declared rule (the rest declare nothing, which is legal).</summary>
  [SerializedLabel("goldsDeclaringRules")]
  public int GoldsDeclaringRules { get; init; }

  [SerializedLabel("ruleCount")]
  public int RuleCount { get; init; }

  /// <summary>Rules in the committed rollup — the middle leg's denominator.</summary>
  [SerializedLabel("rollupRuleCount")]
  public int RollupRuleCount { get; init; }

  /// <summary>Engine source files scanned for rule-id references — the third leg's denominator.</summary>
  [SerializedLabel("sourceFilesScanned")]
  public int SourceFilesScanned { get; init; }

  [SerializedLabel("vacuous")]
  public bool Vacuous { get; init; }

  /// <summary>Rules with no witnessing gold — §2 soundness violations. Structurally zero while the map
  /// is derived from <c>declares</c>; non-zero the moment a rule enters the rollup any other way.</summary>
  [SerializedLabel("unwitnessedRuleCount")]
  public int UnwitnessedRuleCount { get; init; }

  /// <summary>Rules whose id appears in no engine source — the rule→code leg that cannot yet be checked.
  /// A REPORT, not a gate: closing it is issue #34.</summary>
  [SerializedLabel("codeUnlinkedRuleCount")]
  public int CodeUnlinkedRuleCount { get; init; }

  /// <summary>Rules where the committed rollup's witness attribution differs from the golds' own
  /// <c>declares</c> — i.e. a stale committed rollup.</summary>
  [SerializedLabel("witnessDisagreements")]
  public required IReadOnlyList<WitnessDisagreementRow> WitnessDisagreements { get; init; }

  /// <summary>In the rollup, declared by no gold.</summary>
  [SerializedLabel("rollupRulesMissingFromGolds")]
  public required IReadOnlyList<string> RollupRulesMissingFromGolds { get; init; }

  /// <summary>Declared by a gold, absent from the committed rollup.</summary>
  [SerializedLabel("goldRulesMissingFromRollup")]
  public required IReadOnlyList<string> GoldRulesMissingFromRollup { get; init; }

  /// <summary>Gold edges citing a rule no gold declares.</summary>
  [SerializedLabel("danglingCitations")]
  public required IReadOnlyList<string> DanglingCitations { get; init; }

  /// <summary>THE MAP.</summary>
  [SerializedLabel("rules")]
  public required IReadOnlyList<GuardWitnessRow> Rules { get; init; }
}

/// <summary>One residual rule and its full derivation: witnesses, edge realizations, code linkage.</summary>
[FlowthruSchema]
public partial record GuardWitnessRow
{
  [SerializedLabel("ruleId")]
  public required string RuleId { get; init; }

  /// <summary><c>polarity</c> | <c>match_policy</c> | <c>guards</c> | <c>bridges</c>.</summary>
  [SerializedLabel("section")]
  public required string Section { get; init; }

  [SerializedLabel("impl")]
  public string? Impl { get; init; }

  /// <summary><c>observed</c> → <c>corroborated</c> → <c>confirmed</c>, recomputed from the golds.</summary>
  [SerializedLabel("status")]
  public required string Status { get; init; }

  /// <summary>The witnessing golds — derived, never authored.</summary>
  [SerializedLabel("witnesses")]
  public required IReadOnlyList<string> Witnesses { get; init; }

  /// <summary>Golds whose EDGES cite this rule (a guard prunes rather than builds, so guards legitimately
  /// have none — this column separates "witnessed by declaration" from "realized on an edge").</summary>
  [SerializedLabel("citingGolds")]
  public required IReadOnlyList<string> CitingGolds { get; init; }

  /// <summary>Engine source references to this rule id (<c>path:line</c>), derived by literal scan.</summary>
  [SerializedLabel("codeReferences")]
  public required IReadOnlyList<string> CodeReferences { get; init; }

  [SerializedLabel("desc")]
  public string? Desc { get; init; }

  [SerializedLabel("cr")]
  public required IReadOnlyList<string> Cr { get; init; }
}

/// <summary>A rule whose rollup witnesses and gold-derived witnesses disagree.</summary>
[FlowthruSchema]
public partial record WitnessDisagreementRow
{
  [SerializedLabel("ruleId")]
  public required string RuleId { get; init; }

  [SerializedLabel("inGoldsOnly")]
  public required IReadOnlyList<string> InGoldsOnly { get; init; }

  [SerializedLabel("inRollupOnly")]
  public required IReadOnlyList<string> InRollupOnly { get; init; }
}
