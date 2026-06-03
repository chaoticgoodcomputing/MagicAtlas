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
