namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

using Flowthru.Data.Schema;

/// <summary>
/// The <b>span-witness error-check</b> report (the mast-loop Error-check track's entry surface). Treats a
/// port's <c>SourceSpan</c> as a WITNESS: the exact oracle-text characters the port claims. For every
/// parsed port, the check slices the span and verifies it contains the anchor word the port's label
/// asserts (a <c>sac</c> port's text should say "sacrifice", an <c>emit:token</c> should say "create", a
/// <c>trigger:damage</c> should say "damage"). A port whose claimed text lacks its anchor is a suspect —
/// either a false-positive port (the parser over-generated) or a span mis-attribution (the port points at
/// the wrong clause). Each suspect is routed to the interaction golds that WITNESS its ADR-3 stem (from the
/// cited topology), so the loop knows which gold — or which parser slice — to refine. Corpus-gated
/// (gitignored, never committed); degrades to no witness-routing when the cited topology is absent.
/// </summary>
[FlowthruSchema]
public partial record SpanWitnessReport
{
  [SerializedLabel("generatedAt")]
  public required string GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>Parsed ports with a span whose label carries a checkable anchor (the denominator).</summary>
  [SerializedLabel("checkedPorts")]
  public int CheckedPorts { get; init; }

  /// <summary>Derived affordance ports excluded from the check — a created token's own ability projected
  /// onto the creator (its span is the creating clause, not its own text; ADR-0003 §7). Not a defect.</summary>
  [SerializedLabel("derivedExcluded")]
  public int DerivedExcluded { get; init; }

  /// <summary>Ports whose span offsets run PAST the stored oracle text (empty slice) — the double-faced-card
  /// class (the composed CardFaces text the spans index is not the served text). A distinct, systematic
  /// defect surfaced separately so it does not drown the semantic suspects.</summary>
  [SerializedLabel("misalignedDfc")]
  public int MisalignedDfc { get; init; }

  /// <summary>The actionable suspects: the span has text, but it lacks the port's anchor word.</summary>
  [SerializedLabel("semanticOutlierCount")]
  public int SemanticOutlierCount { get; init; }

  /// <summary>The semantic suspects, ranked (unwitnessed stems first — a suspect on a stem no gold covers
  /// is both a QA flag AND an accretion gap), then by stem, then by card.</summary>
  [SerializedLabel("outliers")]
  public required IReadOnlyList<SpanOutlierRow> Outliers { get; init; }
}

/// <summary>One span-witness suspect: a port whose claimed span text lacks its anchor, plus the golds that
/// witness its stem (the refine-these routing).</summary>
[FlowthruSchema]
public partial record SpanOutlierRow
{
  [SerializedLabel("card")]
  public required string Card { get; init; }

  [SerializedLabel("label")]
  public required string Label { get; init; }

  [SerializedLabel("family")]
  public required string Family { get; init; }

  /// <summary>The ADR-3 stem the port claims (the join key to the cited topology's witnesses).</summary>
  [SerializedLabel("stem")]
  public string? Stem { get; init; }

  /// <summary>The anchor word(s) the label asserts should appear in the claimed text.</summary>
  [SerializedLabel("expectedAnchor")]
  public required string ExpectedAnchor { get; init; }

  /// <summary>The exact oracle-text the port's span claims — the witness that fails the anchor.</summary>
  [SerializedLabel("claimedText")]
  public required string ClaimedText { get; init; }

  /// <summary>The interaction golds that witness this stem (cited topology <c>stems[stem].witnesses</c>);
  /// empty when the stem is declared-only / uncovered — itself a signal to witness it.</summary>
  [SerializedLabel("witnessGolds")]
  public required IReadOnlyList<string> WitnessGolds { get; init; }
}
