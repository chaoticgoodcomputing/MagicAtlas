"""Join points + hover info + cluster assignments + cluster labels and render a standalone Plotly
HTML scatter. Points are colored by WUBRG color identity; cluster context appears as text
annotations placed at each top-N cluster's 2D centroid (no cluster-based color encoding — the
position-density structure already shows the clusters, the annotations name them).

Inputs:
    points:      DataFrame [point_id, card_id, x, y]
    hover:       DataFrame [card_id, name, mana_cost, cmc, type_line, color_identity, power,
                            toughness, oracle_text]
    assignments: DataFrame [point_id, cluster_id]
    labels:      DataFrame [cluster_id, label, description, keywords, size, source,
                            source_version]

Output:
    str — full standalone HTML doc with Plotly.js inlined.

Hover still surfaces each point's full cluster label, so the per-point cluster identity is
recoverable even when a centroid annotation has been dropped (e.g. tiny clusters not in the
top-N).
"""
from __future__ import annotations

import json
import logging
from typing import Dict, List, Tuple

import pandas as pd
import plotly.graph_objects as go
from flowthru import step

logger = logging.getLogger(__name__)

# Top-N largest clusters get a centroid text annotation. The noise cluster is always excluded;
# adding annotations for every one of ~500 clusters produces an unreadable wall of text and
# defeats the point of the lens. 50 is a reasonable balance: covers the bulk of the points by
# size while leaving the visual breathable.
_MAX_ANNOTATIONS = 50

# Per-cluster annotation text is the first keyword from c-TF-IDF (or the LLM-equivalent),
# trimmed to keep the overlay legible. The full multi-keyword label remains on hover.
_ANNOTATION_TEXT_LIMIT = 36

_COLOR_ID_PALETTE: Dict[str, str] = {
    "": "#9CA3AF",
    "W": "#F9E79F",
    "U": "#5DADE2",
    "B": "#566573",
    "R": "#E74C3C",
    "G": "#27AE60",
}
_GOLD = "#D4AC0D"


def _color_id_color(identity: str) -> str:
    if identity in _COLOR_ID_PALETTE:
        return _COLOR_ID_PALETTE[identity]
    return _GOLD


def _truncate(text: object, limit: int = 220) -> str:
    if text is None or (isinstance(text, float) and pd.isna(text)):
        return ""
    s = str(text).replace("\n", " · ")
    if len(s) <= limit:
        return s
    return s[: limit - 1] + "…"


def _format_pt(power: object, toughness: object) -> str:
    p = "" if power is None or (isinstance(power, float) and pd.isna(power)) else str(power)
    t = "" if toughness is None or (isinstance(toughness, float) and pd.isna(toughness)) else str(toughness)
    if not p and not t:
        return ""
    return f"{p}/{t}"


def _color_id_legend(identity: str) -> str:
    if identity == "":
        return "Colorless"
    if len(identity) > 1:
        return f"Multi ({identity})"
    return {"W": "White", "U": "Blue", "B": "Black", "R": "Red", "G": "Green"}.get(identity, identity)


def _identity_sort_key(s: str) -> Tuple[int, str]:
    return (len(s), s)


def _annotation_text(label: str, keywords_json: str) -> str:
    """Prefer the first keyword (highest-c-TF-IDF n-gram); fall back to the label head."""
    try:
        kws = json.loads(keywords_json) if keywords_json else []
    except (TypeError, ValueError):
        kws = []
    head = kws[0] if kws else label
    if not head:
        return ""
    if len(head) <= _ANNOTATION_TEXT_LIMIT:
        return head
    return head[: _ANNOTATION_TEXT_LIMIT - 1] + "…"


def _build_atlas_plot_impl(
    points: pd.DataFrame,
    hover: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
) -> str:
    logger.info(
        "Inputs: %d points, %d hover rows, %d assignments, %d labels",
        len(points),
        len(hover),
        len(assignments),
        len(labels),
    )

    label_by_cluster = dict(zip(labels["cluster_id"].astype(int), labels["label"].astype(str)))

    merged = (
        points.merge(assignments, on="point_id", how="left", validate="one_to_one")
        .merge(hover, on="card_id", how="left", validate="many_to_one")
    )
    merged["cluster_id"] = merged["cluster_id"].fillna(-1).astype(int)
    merged["cluster_label"] = merged["cluster_id"].map(label_by_cluster).fillna("(unlabeled)")
    merged["color_identity"] = merged["color_identity"].fillna("")
    merged["name"] = merged["name"].fillna("(unknown)")

    merged["_hover_pt"] = [_format_pt(p, t) for p, t in zip(merged["power"], merged["toughness"])]
    merged["_hover_oracle"] = merged["oracle_text"].map(_truncate)
    merged["_hover_mana"] = merged["mana_cost"].fillna("")

    color_id_traces = _build_color_id_traces(merged)
    logger.info("Built %d color-identity traces", len(color_id_traces))

    annotations = _build_cluster_annotations(merged, labels)
    logger.info("Built %d cluster centroid annotations", len(annotations))

    fig = go.Figure(data=color_id_traces)
    fig.update_layout(
        title=f"MagicAtlas — UMAP of oracle-text embeddings ({len(merged):,} fragments)",
        xaxis_title="UMAP-1",
        yaxis_title="UMAP-2",
        plot_bgcolor="white",
        legend_title_text="Color identity",
        margin=dict(l=60, r=20, t=80, b=60),
        hoverlabel=dict(bgcolor="white", font_size=12),
        annotations=annotations,
    )
    fig.update_xaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")
    fig.update_yaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")

    html = fig.to_html(include_plotlyjs="inline", full_html=True)
    logger.info("Produced %d-byte standalone HTML", len(html))
    return html


@step(
    inputs=["AtlasReportingPoints", "AtlasCardHoverInfo", "ClusterAssignments", "ClusterLabels"],
    outputs="AtlasPlotHtml",
)
def build_atlas_plot(
    points: pd.DataFrame,
    hover: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
) -> str:
    return _build_atlas_plot_impl(points, hover, assignments, labels)


@step(
    inputs=[
        "FineTunedAtlasReportingPoints",
        "AtlasCardHoverInfo",
        "FineTunedClusterAssignments",
        "FineTunedClusterLabels",
    ],
    outputs="FineTunedAtlasPlotHtml",
)
def build_atlas_plot_finetuned(
    points: pd.DataFrame,
    hover: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
) -> str:
    return _build_atlas_plot_impl(points, hover, assignments, labels)


def _build_color_id_traces(merged: pd.DataFrame) -> List[go.Scattergl]:
    combos = sorted(merged["color_identity"].fillna("").unique(), key=_identity_sort_key)
    traces: List[go.Scattergl] = []
    for combo in combos:
        subset = merged[merged["color_identity"] == combo]
        if len(subset) == 0:
            continue
        traces.append(
            go.Scattergl(
                x=subset["x"],
                y=subset["y"],
                mode="markers",
                name=f"{_color_id_legend(combo)} ({len(subset)})",
                marker=dict(
                    color=_color_id_color(combo),
                    size=4,
                    opacity=0.7,
                    line=dict(width=0.3, color="#1f2937"),
                ),
                customdata=subset[
                    ["name", "_hover_mana", "type_line", "_hover_pt", "_hover_oracle", "cluster_label"]
                ].to_numpy(),
                hovertemplate=(
                    "<b>%{customdata[0]}</b>"
                    "<br>%{customdata[1]} · %{customdata[2]}"
                    "<br>%{customdata[3]}"
                    "<br>%{customdata[4]}"
                    "<br><i>cluster: %{customdata[5]}</i>"
                    "<extra></extra>"
                ),
            )
        )
    return traces


def _build_cluster_annotations(merged: pd.DataFrame, labels: pd.DataFrame) -> List[dict]:
    """Top-N largest clusters → text annotations at the per-cluster (x, y) centroid."""
    # Centroid per cluster from the actual rendered point positions.
    centroids = (
        merged[merged["cluster_id"] != -1]
        .groupby("cluster_id")
        .agg(cx=("x", "mean"), cy=("y", "mean"))
        .reset_index()
    )

    labels_indexed = (
        labels[labels["cluster_id"] != -1]
        .sort_values("size", ascending=False)
        .head(_MAX_ANNOTATIONS)
        .merge(centroids, on="cluster_id", how="inner")
    )

    annotations: List[dict] = []
    for row in labels_indexed.itertuples(index=False):
        text = _annotation_text(row.label, row.keywords)
        if not text:
            continue
        annotations.append(
            dict(
                x=float(row.cx),
                y=float(row.cy),
                xref="x",
                yref="y",
                text=text,
                showarrow=False,
                font=dict(size=11, color="#111827"),
                bgcolor="rgba(255, 255, 255, 0.78)",
                bordercolor="rgba(31, 41, 55, 0.35)",
                borderwidth=0.5,
                borderpad=2,
            )
        )
    return annotations
