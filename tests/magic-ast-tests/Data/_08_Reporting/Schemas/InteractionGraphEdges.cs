using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// A flat edge of the abstract <b>label-level</b> interaction graph — one authored family-grammar
/// edge (port label → port label on a resource/family). Feeds the left subplot of the Plotly viz.
/// </summary>
/// <remarks>
/// Flat scalar properties only: this crosses the Flowthru → Python (Arrow) boundary, where nested
/// POCOs fail (FT2009). Arrives in Python keyed by these PascalCase names.
/// </remarks>
[FlowthruSchema]
public partial record LabelEdgeRow
{
  [SerializedLabel("from")]
  public string From { get; init; } = "";

  [SerializedLabel("to")]
  public string To { get; init; } = "";

  [SerializedLabel("resource")]
  public string Resource { get; init; } = "";

  [SerializedLabel("family")]
  public string Family { get; init; } = "";
}

/// <summary>
/// A flat edge of the materialized <b>card-level union graph</b> — one engine-produced
/// <c>InteractionEdge</c> between two real card ports, flattened (no nested <c>Port</c>) and tagged
/// with its certainty tier. Ports are deduped across combos (a port is a card property, not a combo
/// property), so cycles can form from any closed loop among them. Feeds the Plotly viz.
/// </summary>
[FlowthruSchema]
public partial record CardEdgeRow
{
  [SerializedLabel("fromCard")]
  public string FromCard { get; init; } = "";

  [SerializedLabel("fromLabel")]
  public string FromLabel { get; init; } = "";

  [SerializedLabel("toCard")]
  public string ToCard { get; init; } = "";

  [SerializedLabel("toLabel")]
  public string ToLabel { get; init; } = "";

  [SerializedLabel("resource")]
  public string Resource { get; init; } = "";

  [SerializedLabel("family")]
  public string Family { get; init; } = "";

  /// <summary>Certainty tier — <c>Green</c> / <c>Amber</c> / <c>Red</c> — drives edge color.</summary>
  [SerializedLabel("tier")]
  public string Tier { get; init; } = "";

  [SerializedLabel("reason")]
  public string Reason { get; init; } = "";
}

/// <summary>
/// A flat hop of a <b>reconstructed cycle</b> — one edge of one loop the
/// <see cref="MagicAST.Interaction.PortGraphEngine"/> found (cycles computed in C# via the direct MAST
/// APIs, not re-derived in Python). One row per hop; <see cref="Cycle"/> groups the hops of a loop,
/// ordered by <see cref="Hop"/>. Carries both the per-edge tier and the <b>cycle-level verdict</b>
/// (<see cref="CycleTier"/>) — the worst hop floored by firability (§8) and the multi-cost conjunction
/// (an unfed co-cost) — so the viz renders the engine's certainty, not just edge colors. Flat scalars
/// only (crosses the Python/Arrow boundary).
/// </summary>
[FlowthruSchema]
public partial record CycleEdgeRow
{
  /// <summary>Cycle index (groups the hops of one loop); ranked GREEN-verdict-first then shortest.</summary>
  [SerializedLabel("cycle")]
  public int Cycle { get; init; }

  /// <summary>Hop order within the cycle (for layout).</summary>
  [SerializedLabel("hop")]
  public int Hop { get; init; }

  [SerializedLabel("fromCard")]
  public string FromCard { get; init; } = "";

  [SerializedLabel("fromLabel")]
  public string FromLabel { get; init; } = "";

  [SerializedLabel("toCard")]
  public string ToCard { get; init; } = "";

  [SerializedLabel("toLabel")]
  public string ToLabel { get; init; } = "";

  /// <summary>This hop's own certainty tier.</summary>
  [SerializedLabel("edgeTier")]
  public string EdgeTier { get; init; } = "";

  /// <summary>The CYCLE's verdict — worst hop floored by firability + the multi-cost conjunction (§8). Drives edge color.</summary>
  [SerializedLabel("cycleTier")]
  public string CycleTier { get; init; } = "";

  /// <summary>Firability (§8): no hop is gated.</summary>
  [SerializedLabel("firable")]
  public bool Firable { get; init; }

  /// <summary>Multi-cost conjunction (§8): every co-cost of the loop's abilities is fed.</summary>
  [SerializedLabel("coCostsSatisfied")]
  public bool CoCostsSatisfied { get; init; }

  /// <summary>Why the cycle isn't certified-GREEN (hover): the floor reason, or the worst hop's operator reason.</summary>
  [SerializedLabel("limitingReason")]
  public string LimitingReason { get; init; } = "";

  /// <summary>How the loop relates to the Commander Spellbook corpus: <c>verified</c> (cards EXACTLY a CSB
  /// combo, cycle.cards == a combo) · <c>partial</c> (cards ⊆ a combo — a partial reconstruction of a
  /// known combo) · <c>derived</c> (cards span no single combo — a genuinely novel loop). The viz tiers them.</summary>
  [SerializedLabel("match")]
  public string Match { get; init; } = "derived";

  /// <summary>The id of the matched CSB combo (empty for derived loops) — for hover / drill-through.</summary>
  [SerializedLabel("comboId")]
  public string ComboId { get; init; } = "";

  /// <summary>Total cycles found pre-cap (carried on every row, so the viz can show "showing N of T").</summary>
  [SerializedLabel("total")]
  public int Total { get; init; }
}

/// <summary>
/// Per-card node metadata for the viz — the card's oracle text, surfaced on node hover. Kept
/// separate from <see cref="CardEdgeRow"/> so the (large) oracle text isn't duplicated per edge.
/// <see cref="OracleText"/> preserves the original newlines; the Python viz renders them as line
/// breaks. Flat scalars only (crosses the Python/Arrow boundary).
/// </summary>
[FlowthruSchema]
public partial record PortNodeRow
{
  [SerializedLabel("card")]
  public string Card { get; init; } = "";

  [SerializedLabel("oracleText")]
  public string OracleText { get; init; } = "";
}
