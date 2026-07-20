using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// The <b>ADR-0004 §1 artifact classification manifest</b> — every artifact on the declared surface,
/// classified Evidence / Derived / architectural-decision, with the ambiguous residue explicitly flagged
/// for human classification.
/// </summary>
/// <remarks>
/// Produced by the <c>ArtifactCensus</c> Flowthru flow (ADR 0004 §1: "derivation is Flowthru's job;
/// NUnit's job is gates" — a census that lived in NUnit would itself be a hand-rolled artifact). The
/// <b>gate</b> over it is <c>Tests/ArtifactCensus/ArtifactClassificationGateTests.cs</c>, which does NOT
/// read this file: it re-runs the same pure <c>ArtifactClassifier</c> so the invariant holds on a clean
/// checkout with no reporting layer present.
/// </remarks>
[FlowthruSchema]
public partial record ArtifactCensus
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>The declared scan surface — each root, why it is in scope, and whether it exists (a
  /// generated-and-gitignored root is legitimately absent on a clean checkout).</summary>
  [SerializedLabel("scanRoots")]
  public required IReadOnlyList<ScanRootSummary> ScanRoots { get; init; }

  [SerializedLabel("totalArtifacts")]
  public int TotalArtifacts { get; init; }

  /// <summary>Counts per classification kind. A reporting figure, never a gate input — the gate is the
  /// membership invariant in <see cref="Unclassified"/>, not a number.</summary>
  [SerializedLabel("byKind")]
  public required IReadOnlyList<KindCount> ByKind { get; init; }

  /// <summary>Counts per classification RULE — how much of the manifest each derivation rule carries.
  /// A rule doing no work is a rule to delete; a rule doing all the work is a rule to distrust.</summary>
  [SerializedLabel("byRule")]
  public required IReadOnlyList<KindCount> ByRule { get; init; }

  /// <summary>
  /// <b>The primary human-facing output.</b> Artifacts no rule could resolve. An acknowledged entry
  /// carries the reason it is genuinely ambiguous; an UNacknowledged entry fails the build.
  /// </summary>
  [SerializedLabel("needsHumanClassification")]
  public required IReadOnlyList<ArtifactEntry> NeedsHumanClassification { get; init; }

  /// <summary>The gate's failure set: unresolved AND unacknowledged. Empty on a green build.</summary>
  [SerializedLabel("unclassified")]
  public required IReadOnlyList<ArtifactEntry> Unclassified { get; init; }

  /// <summary>Every artifact, classified. The full manifest.</summary>
  [SerializedLabel("artifacts")]
  public required IReadOnlyList<ArtifactEntry> Artifacts { get; init; }

  /// <summary>Files inside a scan root that are not data artifacts (source, docs, build config, VCS
  /// plumbing). Reported rather than silently dropped — an exclusion nobody can see is itself a
  /// hand-maintained claim.</summary>
  [SerializedLabel("exclusions")]
  public required IReadOnlyList<ExclusionEntry> Exclusions { get; init; }

  [SerializedLabel("exclusionsByRule")]
  public required IReadOnlyList<KindCount> ExclusionsByRule { get; init; }
}

/// <summary>One classified artifact.</summary>
[FlowthruSchema]
public partial record ArtifactEntry
{
  /// <summary>Repository-relative path.</summary>
  [SerializedLabel("path")]
  public required string Path { get; init; }

  /// <summary><c>Evidence</c> | <c>Derived</c> | <c>architectural-decision</c> |
  /// <c>needs-human-classification</c>.</summary>
  [SerializedLabel("kind")]
  public required string Kind { get; init; }

  /// <summary>The classification rule that fired.</summary>
  [SerializedLabel("rule")]
  public required string Rule { get; init; }

  /// <summary>The structural fact the rule keyed on (a catalog site, a writer site, a convention) — or,
  /// for an unresolved artifact, why it is ambiguous.</summary>
  [SerializedLabel("basis")]
  public required string Basis { get; init; }

  /// <summary>True when a human has explicitly acknowledged this artifact's ambiguity or ruled on it.</summary>
  [SerializedLabel("acknowledged")]
  public bool Acknowledged { get; init; }
}

/// <summary>A file set aside by a named exclusion rule.</summary>
[FlowthruSchema]
public partial record ExclusionEntry
{
  [SerializedLabel("path")]
  public required string Path { get; init; }

  [SerializedLabel("rule")]
  public required string Rule { get; init; }
}

/// <summary>One scan root and what it yielded.</summary>
[FlowthruSchema]
public partial record ScanRootSummary
{
  [SerializedLabel("path")]
  public required string Path { get; init; }

  [SerializedLabel("rationale")]
  public required string Rationale { get; init; }

  [SerializedLabel("exists")]
  public bool Exists { get; init; }

  [SerializedLabel("artifactCount")]
  public int ArtifactCount { get; init; }
}

/// <summary>A labelled count (kind, rule, or exclusion rule).</summary>
[FlowthruSchema]
public partial record KindCount
{
  [SerializedLabel("label")]
  public required string Label { get; init; }

  [SerializedLabel("count")]
  public int Count { get; init; }
}
