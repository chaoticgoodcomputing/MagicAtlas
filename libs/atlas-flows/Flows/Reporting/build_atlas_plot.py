"""Join points + hover info + cluster assignments + cluster labels and render a standalone Plotly
HTML scatter with a dropdown to switch between cluster and color-identity coloring.

Inputs:
    points:      DataFrame [point_id, card_id, x, y]
    hover:       DataFrame [card_id, name, mana_cost, cmc, type_line, color_identity, power,
                            toughness, oracle_text]
    assignments: DataFrame [point_id, cluster_id]
    labels:      DataFrame [cluster_id, label, description, keywords, size, source,
                            source_version]

Output:
    str — full standalone HTML doc with Plotly.js inlined.

Plot structure: one trace per group for each coloring (cluster_id and color_identity). Both trace
sets live in the figure simultaneously; a dropdown toggles `visible` on each set. Initial view is
clusters because it's the new analytical lens; color-identity is the secondary toggle.

Cluster-label data is attached to every point's hover (in both views), so users see "this point
is in cluster 'graveyard, exile, return'" regardless of which coloring is active.
"""
from __future__ import annotations

import logging
from typing import Dict, Iterable, List, Tuple

import numpy as np
import pandas as pd
import plotly.graph_objects as go
from flowthru import step

logger = logging.getLogger(__name__)

# Color-identity palette (unchanged from the prior pure color-id plot).
_COLOR_ID_PALETTE: Dict[str, str] = {
    "": "#9CA3AF",
    "W": "#F9E79F",
    "U": "#5DADE2",
    "B": "#566573",
    "R": "#E74C3C",
    "G": "#27AE60",
}
_GOLD = "#D4AC0D"

# Plotly's Alphabet palette (26 distinct colors) — cycled for >26 clusters.
_CLUSTER_PALETTE = [
    "#AA0DFE", "#3283FE", "#85660D", "#782AB6", "#565656", "#1C8356", "#16FF32",
    "#F7E1A0", "#E2E2E2", "#1CBE4F", "#C4451C", "#DEA0FD", "#FE00FA", "#325A9B",
    "#FEAF16", "#F8A19F", "#90AD1C", "#F6222E", "#1CFFCE", "#2ED9FF", "#B10DA1",
    "#C075A6", "#FC1CBF", "#B00068", "#FBE426", "#FA0087",
]
_NOISE_COLOR = "#D1D5DB"


def _color_id_color(identity: str) -> str:
    if identity in _COLOR_ID_PALETTE:
        return _COLOR_ID_PALETTE[identity]
    return _GOLD


def _cluster_color(cluster_id: int) -> str:
    if cluster_id < 0:
        return _NOISE_COLOR
    return _CLUSTER_PALETTE[cluster_id % len(_CLUSTER_PALETTE)]


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


def _cluster_legend(cluster_id: int, label: str, size: int) -> str:
    if cluster_id < 0:
        return f"(noise) ({size})"
    # Truncate label so the legend stays readable.
    short = label if len(label) <= 32 else label[:31] + "…"
    return f"{cluster_id}: {short} ({size})"


def _identity_sort_key(s: str) -> Tuple[int, str]:
    return (len(s), s)


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
    logger.info(
        "Inputs: %d points, %d hover rows, %d assignments, %d labels",
        len(points),
        len(hover),
        len(assignments),
        len(labels),
    )

    # Build a {cluster_id: label} lookup; sort cluster traces by size desc with noise last.
    label_by_cluster = dict(zip(labels["cluster_id"].astype(int), labels["label"].astype(str)))
    size_by_cluster = dict(zip(labels["cluster_id"].astype(int), labels["size"].astype(int)))

    merged = (
        points.merge(assignments, on="point_id", how="left", validate="one_to_one")
        .merge(hover, on="card_id", how="left", validate="many_to_one")
    )
    # Clusters with no row in `labels` shouldn't happen in normal runs but fall back gracefully.
    merged["cluster_id"] = merged["cluster_id"].fillna(-1).astype(int)
    merged["cluster_label"] = merged["cluster_id"].map(label_by_cluster).fillna("(unlabeled)")
    merged["color_identity"] = merged["color_identity"].fillna("")
    merged["name"] = merged["name"].fillna("(unknown)")

    # Precompute hover fields once.
    merged["_hover_pt"] = [_format_pt(p, t) for p, t in zip(merged["power"], merged["toughness"])]
    merged["_hover_oracle"] = merged["oracle_text"].map(_truncate)
    merged["_hover_mana"] = merged["mana_cost"].fillna("")

    cluster_traces, n_cluster_traces = _build_cluster_traces(merged, size_by_cluster)
    color_id_traces, n_color_id_traces = _build_color_id_traces(merged)

    logger.info(
        "Built %d cluster traces + %d color-identity traces",
        n_cluster_traces,
        n_color_id_traces,
    )

    # Cluster view visible by default; color-id traces hidden.
    cluster_visible = [True] * n_cluster_traces + [False] * n_color_id_traces
    color_id_visible = [False] * n_cluster_traces + [True] * n_color_id_traces

    fig = go.Figure(data=cluster_traces + color_id_traces)

    fig.update_layout(
        title=f"MagicAtlas — UMAP of oracle-text embeddings ({len(merged):,} fragments)",
        xaxis_title="UMAP-1",
        yaxis_title="UMAP-2",
        plot_bgcolor="white",
        legend_title_text="Cluster",
        margin=dict(l=60, r=20, t=80, b=60),
        hoverlabel=dict(bgcolor="white", font_size=12),
        updatemenus=[
            dict(
                type="dropdown",
                buttons=[
                    dict(
                        label="Color: Clusters",
                        method="update",
                        args=[
                            {"visible": cluster_visible},
                            {"legend.title.text": "Cluster"},
                        ],
                    ),
                    dict(
                        label="Color: Color identity",
                        method="update",
                        args=[
                            {"visible": color_id_visible},
                            {"legend.title.text": "Color identity"},
                        ],
                    ),
                ],
                direction="down",
                showactive=True,
                x=0.0,
                xanchor="left",
                y=1.08,
                yanchor="top",
            )
        ],
    )
    fig.update_xaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")
    fig.update_yaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")

    html = fig.to_html(include_plotlyjs="inline", full_html=True)
    logger.info("Produced %d-byte standalone HTML", len(html))
    return html


def _build_cluster_traces(
    merged: pd.DataFrame, size_by_cluster: Dict[int, int]
) -> Tuple[List[go.Scattergl], int]:
    """One trace per cluster, sorted by size descending with noise last."""
    cluster_ids: Iterable[int] = sorted(
        merged["cluster_id"].unique(),
        key=lambda cid: (cid == -1, -size_by_cluster.get(int(cid), 0), int(cid)),
    )
    traces: List[go.Scattergl] = []
    for cid in cluster_ids:
        cid = int(cid)
        subset = merged[merged["cluster_id"] == cid]
        if len(subset) == 0:
            continue
        label = subset["cluster_label"].iloc[0]
        traces.append(
            _scatter_trace(
                subset,
                name=_cluster_legend(cid, label, len(subset)),
                color=_cluster_color(cid),
                visible=True,
                legendgroup="clusters",
            )
        )
    return traces, len(traces)


def _build_color_id_traces(merged: pd.DataFrame) -> Tuple[List[go.Scattergl], int]:
    combos = sorted(merged["color_identity"].fillna("").unique(), key=_identity_sort_key)
    traces: List[go.Scattergl] = []
    for combo in combos:
        subset = merged[merged["color_identity"] == combo]
        if len(subset) == 0:
            continue
        traces.append(
            _scatter_trace(
                subset,
                name=f"{_color_id_legend(combo)} ({len(subset)})",
                color=_color_id_color(combo),
                visible=False,
                legendgroup="color_id",
            )
        )
    return traces, len(traces)


def _scatter_trace(
    subset: pd.DataFrame, name: str, color: str, visible: bool, legendgroup: str
) -> go.Scattergl:
    return go.Scattergl(
        x=subset["x"],
        y=subset["y"],
        mode="markers",
        name=name,
        visible=visible,
        legendgroup=legendgroup,
        showlegend=True,
        marker=dict(
            color=color,
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
