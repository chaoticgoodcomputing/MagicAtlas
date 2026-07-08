using Flowthru.Data.Schema;
using MagicAST;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Top-level triage report consumed by the <c>mast-tdd-loop</c> skill. Ranks
/// failure patterns by projected coverage gain and surfaces clean exemplars for
/// each. The on-disk JSON is PascalCase — Flowthru's default — and the
/// <c>mast-tdd-loop</c> skill is the consuming contract.
/// </summary>
[FlowthruSchema]
public partial record TriageReport
{
  public required DateTime GeneratedAt { get; init; }
  public required GlobalMetrics GlobalMetrics { get; init; }

  /// <summary>
  /// PRIMARY pick surface. Data-derived families: unparsed oracle lines
  /// clustered by normalized lexical template, each enriched with a
  /// proximity-weighted <see cref="YieldClusterSummary.FractionalYield"/>, the
  /// dominant <c>(Pattern, LastAttemptedRule)</c> telling the agent WHERE the
  /// parser bails, and hand-parse-ready exemplars. A template is a buildable
  /// family — one parser surface closes it — which the coarse
  /// <c>(Pattern, LastAttemptedRule)</c> gap key is not (it groups failures by
  /// where the parser stood, not by what's missing). Pick from here;
  /// <see cref="TopGaps"/> and <see cref="TopGapsByLineFrequency"/> are
  /// secondary diagnostics.
  /// </summary>
  public required IReadOnlyList<YieldClusterSummary> TopYieldClusters { get; init; }

  /// <summary>
  /// DIAGNOSTIC surface (not the primary pick surface — see
  /// <see cref="TopYieldClusters"/>). Failures grouped by the coarse
  /// <c>(Pattern, LastAttemptedRule)</c> key, ranked by fractional yield. Useful
  /// for seeing which broad parser bail points dominate the corpus, but a single
  /// entry often spans several distinct buildable families (e.g.
  /// "UnparsedTriggered" lumps proliferate-triggers, roll-a-d20, and
  /// play-a-card-triggers), so it is not pickable as one family.
  /// </summary>
  public required IReadOnlyList<GapEntry> TopGaps { get; init; }

  /// <summary>
  /// DIAGNOSTIC surface. Same gap entries as TopGaps but ranked by raw line
  /// frequency (Frequency.Lines) descending. Surfaces the highest-frequency
  /// parser bail points regardless of whether they exclusively flip whole cards.
  /// </summary>
  public required IReadOnlyList<GapEntry> TopGapsByLineFrequency { get; init; }

  /// <summary>
  /// The L1→L2 BURN-DOWN pick surface — fragment families over the <c>UnstructuredEffect</c> residual
  /// interiors (the deferred effect text held by L1 ability shells). Where <see cref="TopYieldClusters"/>
  /// closes L0 parse gaps (whole cards that don't parse), this closes L1 residual debt: each cluster is a
  /// normalized effect FRAGMENT (not a whole line), so a single new effect rule closes it across every
  /// shell that carries it — families that compose. Ranked by the same fused combo-value score. This is
  /// the surface the shell fallback UNLOCKED: with most cards now at L1, the residual interiors are the
  /// real remaining work, and clustering the exactly-delimited fragments concentrates it far more than
  /// whole-line templates could. Empty on a report generated before residual capture landed.
  /// </summary>
  public IReadOnlyList<ResidualClusterSummary> TopResidualClusters { get; init; } = [];
}

/// <summary>One fragment family in the L1→L2 residual burn-down surface — a normalized effect interior.</summary>
[FlowthruSchema]
public partial record ResidualClusterSummary
{
  /// <summary>Position in the fused-score ranking (1-indexed).</summary>
  public required int Rank { get; init; }

  /// <summary>Placeholder-normalized effect fragment (the residual interior template) that defines this family.</summary>
  public required string Template { get; init; }

  /// <summary>Total <c>UnstructuredEffect</c> residual instances matching this template across the corpus.</summary>
  public required int FragmentCount { get; init; }

  /// <summary>Distinct cards carrying at least one residual of this template.</summary>
  public required int CardCount { get; init; }

  /// <summary>Combo-popularity mass of the cards carrying this residual family (the downstream-value axis).</summary>
  public required double ComboPopularityMass { get; init; }

  /// <summary>
  /// Primary ranking key: <c>FragmentCount × (1 + log10(1 + ComboPopularityMass))</c> — how many L1→L2
  /// upgrades this one effect rule yields, weighted by the combo value it unblocks. Degrades to raw
  /// FragmentCount with no value map.
  /// </summary>
  public required double FusedScore { get; init; }

  /// <summary>A few example residual fragments (verbatim) + their cards — where to look to build the rule.</summary>
  public required IReadOnlyList<ResidualExemplar> Exemplars { get; init; }
}

/// <summary>A verbatim residual fragment and the card it came from.</summary>
[FlowthruSchema]
public partial record ResidualExemplar
{
  public required string CardName { get; init; }
  public required string Fragment { get; init; }
}

/// <summary>
/// One cluster in the yield-projection top-K. Templates are placeholder-
/// substituted oracle text (e.g., <c>"Counter target &lt;TYPE&gt; spell."</c>).
/// </summary>
[FlowthruSchema]
public partial record YieldClusterSummary
{
  /// <summary>Position in the fractional-yield ranking (1-indexed).</summary>
  public required int Rank { get; init; }

  /// <summary>Placeholder-substituted oracle template that defines this cluster.</summary>
  public required string Template { get; init; }

  /// <summary>Total unparsed lines matching this template across the corpus.</summary>
  public required int LineCount { get; init; }

  /// <summary>Number of distinct cards with at least one line matching this template.</summary>
  public required int CardCount { get; init; }

  /// <summary>
  /// Cards whose ENTIRE unparsed-line set is this single template — closing
  /// this cluster would flip them green directly. Order-independent whole-card
  /// yield; a hard floor under <see cref="FractionalYield"/>.
  /// </summary>
  public required int DirectYield { get; init; }

  /// <summary>
  /// Proximity-weighted yield: the sum, over every card with a line in this
  /// cluster, of <c>1 / (distinct templates on that card)</c>. A card whose
  /// only unparsed template is this one contributes 1.0; a card three templates
  /// away contributes 0.33. Unlike <see cref="DirectYield"/> (whole-card flips
  /// only), this credits partial progress — so it ranks templates that are the
  /// last-or-near-last missing piece across the broad base of nearly-complete
  /// cards. This is the PRIMARY ranking key for the cluster surface.
  /// </summary>
  public required double FractionalYield { get; init; }

  /// <summary>
  /// Fractional combo-blocking count for this cluster: the sum, over every card
  /// with a line in this cluster, of <c>blockedComboCount / (distinct templates
  /// on that card)</c>. Same 1/N attribution as <see cref="FractionalYield"/>,
  /// so a card split across several unparsed templates shares its combo credit
  /// across them rather than double-counting. Zero when no InteractionTriage
  /// value map was available at report time.
  /// </summary>
  public double ComboBlockedCount { get; init; }

  /// <summary>
  /// Fractional combo-popularity mass this cluster unblocks: the same 1/N-weighted
  /// sum of each member card's <c>popularityMass</c> (total popularity of the
  /// combos it gates). This is the downstream-value signal — how much real,
  /// popularity-weighted combo coverage is waiting behind this parser surface.
  /// </summary>
  public double ComboPopularityMass { get; init; }

  /// <summary>
  /// The interaction-value boost factor, <c>log10(1 + ComboPopularityMass)</c>.
  /// Compresses the wide popularity-mass range into a bounded, additive weight
  /// (mass 0 → 0, mass 1M → ~6). A cluster that unblocks no known combos scores 0.
  /// </summary>
  public double InteractionValueScore { get; init; }

  /// <summary>
  /// PRIMARY ranking key when a value map is present:
  /// <c>FractionalYield × (1 + InteractionValueScore)</c>. Blends parse-proximity
  /// (how close this surface is to flipping whole cards) with downstream combo
  /// value (how much popular-combo coverage it unblocks). Degrades EXACTLY to
  /// <see cref="FractionalYield"/> when no InteractionTriage value map is present
  /// (InteractionValueScore = 0), so the surface is backward-compatible: a run
  /// without the interaction overlay ranks identically to the pre-fusion loop.
  /// </summary>
  public double FusedScore { get; init; }

  /// <summary>
  /// The most common <c>Diagnostic.Pattern</c> among this cluster's lines —
  /// the "where it fails" hint. Tells a sub-agent which parser this template
  /// bails in (e.g. "UnparsedTriggered") without being the cluster key itself.
  /// </summary>
  public required string DominantPattern { get; init; }

  /// <summary>
  /// The most common <c>Diagnostic.LastAttemptedRule</c> among this cluster's
  /// lines — points at the parser method that gave up. Coarse (often a top-level
  /// <c>*.Parse</c> dispatcher) when the construct is unrecognized outright; the
  /// template is the precise family signal, this is the navigation hint. Null
  /// when no line carried rule telemetry.
  /// </summary>
  public string? DominantLastAttemptedRule { get; init; }

  /// <summary>
  /// Diagnostic-spread homogeneity in [0,1]: the fraction of this cluster's failure
  /// signals that are the single dominant <c>(Pattern, LastAttemptedRule)</c>. 1.0 means
  /// every line bails the same way; a low value means one normalized template has
  /// over-collapsed lines that fail in *different* parsers — the heterogeneity that
  /// exact-template clustering can't see (alignment initiative 02). The triage cluster
  /// gate (<c>tools/gate-triage-cluster.sh</c>) excludes low-homogeneity clusters from
  /// dispatch.
  /// </summary>
  public required double DominantShare { get; init; }

  /// <summary>
  /// Best fixture candidates — cards with the FEWEST other unparsed templates
  /// (cleanest exemplars). Capped at 5.
  /// </summary>
  public required IReadOnlyList<YieldExemplar> Exemplars { get; init; }
}

/// <summary>A specific oracle line that exemplifies a yield cluster's template.</summary>
[FlowthruSchema]
public partial record YieldExemplar
{
  public required string CardName { get; init; }
  public required string ScryfallId { get; init; }
  public required string OracleLine { get; init; }

  /// <summary>How many OTHER unparsed templates this card has (lower = cleaner fixture).</summary>
  public required int OtherUnparsedClusters { get; init; }

  /// <summary>
  /// True if this card's parse is <b>lossy-but-clean</b> — it dropped structure
  /// (a trigger deficit) WITHOUT an <c>UnparsedAbility</c>, so <c>OtherUnparsedClusters</c>
  /// understates its risk: a non-target line may have silently collapsed (e.g.
  /// Keranos's reveal-triggers → a bare damage spell). Exemplars with this set are
  /// ranked LAST within a cluster — prefer a genuinely-clean single-line card. When
  /// no such alternative exists, a flagged exemplar is still shown (with this
  /// warning) so the orchestrator authors the WHOLE-card gold and verifies each
  /// line rather than trusting "the other lines parse". See <c>LossyParseAnalyzer</c>.
  /// </summary>
  public bool LossyRisk { get; init; }

  /// <summary>True if this card already has a hand-parsed fixture under <c>HandParsedCards/</c>.</summary>
  public required bool AlreadyHandParsed { get; init; }

  /// <summary>The DTO an agent can hand-parse directly without re-fetching from Scryfall.</summary>
  public required CardInputDTO Input { get; init; }
}

/// <summary>Coverage and pattern-frequency metrics aggregated across the corpus.</summary>
[FlowthruSchema]
public partial record GlobalMetrics
{
  public required CoverageStat CardCoverage { get; init; }
  public required CoverageStat LineCoverage { get; init; }
  public required CoverageStat AbilityCoverage { get; init; }
  public required int DistinctUnresolvedPatterns { get; init; }
  public required CoverageStat HandParsedCoverage { get; init; }

  /// <summary>
  /// Corpus-wide residual debt (ADR 0001 forcing-function): not-yet-structured
  /// free-text residuals across all parsed ASTs, by kind, descending. A visible,
  /// trending number so the typed residual arms (<c>OtherCharacteristic</c>,
  /// <c>OtherHistoryPredicate</c>, …) and free-text fields don't quietly become
  /// junk drawers. Should trend DOWN as follow-up batches carve residuals into
  /// structured variants.
  /// </summary>
  public required IReadOnlyList<ResidualKindCount> ResidualDebt { get; init; }

  /// <summary>Total residual occurrences across all kinds, corpus-wide.</summary>
  public required int TotalResidualDebt { get; init; }

  /// <summary>
  /// Count of cards that look FULLY parsed (no unparsed line) yet are
  /// <b>lossy-but-clean</b> — a trigger deficit shows they silently dropped
  /// structure (see <c>LossyParseAnalyzer</c>). This is the size of the blind spot
  /// the per-line diagnostics miss: these cards would be picked as "clean"
  /// exemplars but under-represent their oracle text. Should trend DOWN as the
  /// dropping rules (over-greedy collapses) are fixed. Defaults 0.
  /// </summary>
  public int SuspectedLossyCleanCards { get; init; }

  /// <summary>
  /// The fidelity ladder histogram — the honest coverage picture that separates
  /// what <see cref="CardCoverage"/> conflates. <c>CardCoverage</c> (no
  /// <c>IUnparsed</c>) counts L1 + L2 together; this splits them: <b>L0</b> cards
  /// carry an unstructured hole, <b>L1</b> cards are typed shells with a deferred
  /// residual interior (accounted, not dropped), <b>L2</b> cards are fully
  /// structured. <see cref="L2Coverage"/> is the strict "fully structured" number
  /// and is the anti-gaming headline: growing L1 (e.g. via shell fallbacks) must
  /// not inflate L2. Card coverage → 100% is reached by driving L0 → L1; real
  /// progress is L1 → L2 (residual burn-down). Defaults to an all-zero histogram
  /// on a report generated before the ladder landed.
  /// </summary>
  public FidelityHistogram Fidelity { get; init; } = new();

  /// <summary>Strictly-structured cards (fidelity L2: no holes AND no residuals) over the corpus.</summary>
  public CoverageStat L2Coverage { get; init; } = new()
  {
    Passing = 0,
    Total = 0,
    Pct = 0,
  };
}

/// <summary>Corpus card counts by fidelity level (L0 hole / L1 residual shell / L2 fully structured).</summary>
[FlowthruSchema]
public partial record FidelityHistogram
{
  /// <summary>Cards with an <c>IUnparsed</c> hole somewhere in the AST.</summary>
  public int L0 { get; init; }

  /// <summary>Cards with no hole but at least one <c>IResidual</c> (deferred interior / free text).</summary>
  public int L1 { get; init; }

  /// <summary>Fully structured cards — no holes, no residuals.</summary>
  public int L2 { get; init; }
}

/// <summary>A passing-out-of-total ratio plus its percent form.</summary>
[FlowthruSchema]
public partial record CoverageStat
{
  public required int Passing { get; init; }
  public required int Total { get; init; }
  public required double Pct { get; init; }
}

/// <summary>One row in the top-gaps ranking — a failure pattern and its actionable exemplars.</summary>
[FlowthruSchema]
public partial record GapEntry
{
  public required int Rank { get; init; }
  public required string Pattern { get; init; }

  /// <summary>
  /// Parser-rule near-miss identifier — the <c>Diagnostic.LastAttemptedRule</c>
  /// value shared by this gap's failures. Combined with <see cref="Pattern"/>,
  /// the pair is the unique key for a gap entry: grouping by
  /// <c>(Pattern, LastAttemptedRule)</c> distinguishes e.g. a "ConditionalEffect"
  /// pattern arriving via the spell dispatch chain from the same pattern
  /// arriving via the triggered dispatch chain. Null only when the entry was
  /// produced before the telemetry wiring landed (legacy diagnostics).
  /// </summary>
  public string? LastAttemptedRule { get; init; }

  /// <summary>
  /// The mode (most-common) <c>FailurePosition</c> across the failures in this
  /// group. The mode is preferred over the median because failure positions
  /// tend to cluster on specific offsets (e.g. clause-start of an offending
  /// sub-rule), and the mode preserves the cluster rather than averaging it
  /// away. Null when no failures in the group carry a position.
  /// </summary>
  public int? ModeFailurePosition { get; init; }

  public required GapFrequency Frequency { get; init; }
  public required CoverageGain ProjectedCoverageGain { get; init; }

  /// <summary>
  /// Proximity-weighted yield: the sum, over every card touching this gap, of
  /// <c>1 / (distinct gap-keys on that card)</c>. A card one gap away from
  /// completion contributes 1.0, two-away 0.5, three-away 0.33, etc. This
  /// generalises the binary "exclusive card" count
  /// (<see cref="CoverageGain.CardCoveragePct"/>) into a continuous signal:
  /// gaps that are the last-or-nearly-last missing piece on many cards rank
  /// highest. It is the primary ranking key for <c>TopGaps</c> — a factor in
  /// the ranking, not a hard one-away filter.
  /// </summary>
  public required double FractionalYield { get; init; }

  /// <summary>
  /// Patterns that frequently co-occur on the same lines as this one
  /// (per-line Jaccard ≥ threshold). The orchestrator should avoid
  /// dispatching paralleled sub-agents on related patterns.
  /// </summary>
  public required IReadOnlyList<string> RelatedPatterns { get; init; }

  /// <summary>Clean exemplar lines, ranked by cleanliness ascending. Capped at ~10.</summary>
  public required IReadOnlyList<CandidateLine> CandidateLines { get; init; }
}

/// <summary>How many lines and how many distinct cards exhibit this pattern.</summary>
[FlowthruSchema]
public partial record GapFrequency
{
  public required int Lines { get; init; }
  public required int Cards { get; init; }
}

/// <summary>Projected percentage-point gain in card- and line-level coverage.</summary>
[FlowthruSchema]
public partial record CoverageGain
{
  public required double CardCoveragePct { get; init; }
  public required double LineCoveragePct { get; init; }
}

/// <summary>
/// A candidate oracle line surfaced for hand-parsing. Lower
/// <see cref="CleanlinessScore"/> means the line's failures are dominated by
/// the parent <c>GapEntry.Pattern</c> (Definition D: 1 - P-purity).
/// </summary>
[FlowthruSchema]
public partial record CandidateLine
{
  public required string OracleText { get; init; }
  public required CandidateLineSource SourceCard { get; init; }
  public required double CleanlinessScore { get; init; }
  public required int LineLength { get; init; }

  /// <summary>True if this card already has a hand-parsed fixture under <c>HandParsedCards/</c>.</summary>
  public required bool AlreadyHandParsed { get; init; }
}

/// <summary>Source-card pointer attached to each candidate line.</summary>
[FlowthruSchema]
public partial record CandidateLineSource
{
  public required string Name { get; init; }
  public required string ScryfallId { get; init; }

  /// <summary>The DTO an agent can hand-parse directly without re-fetching from Scryfall.</summary>
  public required CardInputDTO Input { get; init; }
}
