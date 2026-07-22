namespace MagicAST.Interaction;

/// <summary>
/// The resource/event kinds a port emits or consumes (mast-interaction ADR-0001 §4). Seeded to the
/// kinds the canonical golds need; extended one vertical slice at a time. The single-role port model
/// (ADR-0002) carries the resource via the colon-label + an <c>ObjectFilter</c> subject rather than a
/// kind enum, but this enum still names the authored family-edge resources and seeds the resource-kind
/// label facet (<c>PortLabel</c>).
/// </summary>
public enum ResourceKind
{
  Mana,
  Token,
  Counter,
  Death,
  EntersBattlefield,
  LeavesBattlefield,
  Sacrifice,
  Cast,
}

/// <summary>
/// One authored family edge (mast-interaction ADR-0001 §3 — the source-of-truth label grammar): a
/// directed <c>fromLabel → toLabel</c> on a resource, joined by its edge family. Authored as JSON;
/// the label-level viz subplot reads it. (ADR-0002's <c>PortGraphEngine</c> <em>derives</em> the flow
/// grammar from colon-labels and no longer consumes this; it remains the authored label-grammar view.)
/// </summary>
public sealed record FamilyEdge
{
  [System.Text.Json.Serialization.JsonPropertyName("from")]
  public required string From { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("to")]
  public required string To { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("resource")]
  public required ResourceKind Resource { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("family")]
  public required EdgeFamily Family { get; init; }
}

/// <summary>Flow = a resource handoff (A emits R, B consumes R). Modifier = A rewrites B's emission (a doubler/replacement).</summary>
public enum EdgeFamily
{
  Flow,
  Modifier,
}

/// <summary>
/// The <b>structural mechanism</b> that formed an edge, recorded by the engine <em>at formation</em>
/// (ADR-0004 §2/Stage 6, issue #34 — the edge-provenance seam). This is the coarse formation-path
/// classifier; for a <see cref="FlowArm"/>-formed edge the <em>fine</em> mechanism is
/// <see cref="PortEdge.Arm"/> (the specific arm <see cref="PortFlowMatcher.SelectArm"/> selected).
///
/// <para><b>Purely structural — it names no rule.</b> Each value is the honest answer to "which formation
/// path in <see cref="PortGraphEngine.Materialize"/> built this edge," derived from where the edge was
/// constructed, never a hand-typed rule id / gold id. The soundness half of §2's bijection joins <em>this
/// tag</em> (plus the endpoints' stems, already on the edge) against the gold-derived rollup — a
/// <c>structure ↔ structure</c> match — so no rule-id correspondence ever enters engine code. §2's
/// forbidden fix (a <c>[WitnessedRule("bridge:…")]</c> attribute) is exactly what this avoids: the tag
/// carries structure, and the golds supply the rule attribution, and the two meet on the stems.</para>
/// </summary>
public enum EdgeMechanism
{
  /// <summary>Intra-ability / created-object causality — <see cref="PortEdge.CardDefined"/> (the golds'
  /// <c>card-defined</c> connector). Self-certifying by the same-card / created-object witness.</summary>
  CardDefined,

  /// <summary>A structured flow arm fired — <see cref="PortFlowMatcher.Captures"/> selected an arm and its
  /// guard passed. <see cref="PortEdge.Arm"/> names <em>which</em> arm. Covers the golds' <c>subsumption</c>,
  /// <c>bridge</c>, <c>polarity</c> and <c>match_policy</c> connectors — the arm + the endpoints' stems are
  /// the structural key the rollup rule joins on.</summary>
  FlowArm,

  /// <summary>A replacement/doubler intercepts an emission (token emit → intercept) — the golds' <c>modifier</c>
  /// connector.</summary>
  Modifier,

  /// <summary>The explicit untapped-lands → <c>pay:mana</c> enabler (tiered AMBER; not a
  /// <see cref="PortFlowMatcher.SelectArm"/> arm because a scalar <c>pay:mana</c> consume has no Subject).
  /// The golds' <c>bridge:untap-land-to-mana</c>.</summary>
  UntapLandsToMana,

  /// <summary>A copy-graft synthesized closing edge that renews the copier's tap (an inherited untap/blink)
  /// — the golds' <c>bridge:untap-renews-tap</c> / <c>bridge:blink-renews-tap</c>.</summary>
  GraftClosing,
}

/// <summary>Certainty of an edge or cycle. Ordered worst-last so a cycle's tier is the max over its hops.</summary>
public enum CertaintyTier
{
  Green = 0, // certain
  Amber = 1, // closes only through an Unknown / conditional region
  Red = 2, // a dark hop — an expected edge that does not hold
}
