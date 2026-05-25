"""Render the explorer-mode atlas as a standalone Plotly HTML.

Explorer-mode rendering choices:
- Points are colored by **color identity** (WUBRG + colorless). Multi-color cards render as
  transparent-filled markers with a dark outline so the underlying neighborhood remains visible
  through them — multi-color regions tend to be dense, and opaque blended colors lose detail.
- No canonical/cluster overlay. The atlas is for browsing semantic neighborhoods (explorer
  mode); exploiter-mode categorical queries belong in MagicAST (libs/magic-ast/), not on this
  map.
- Hover details surface card name, mana cost, type line, P/T, this line's text, and the full
  oracle text.

Inputs:
    points:  DataFrame [line_id, x, y]
    lines:   DataFrame [line_id, card_id, text] — provides per-line text + the line→card key.
    hover:   DataFrame [card_id, name, mana_cost, cmc, type_line, color_identity, power,
                        toughness, oracle_text]
    config:  ReportingConfig — uses AnnotationTextLimit (unused now but kept for stability),
             MarkerSize, MarkerOpacity, OracleHoverTruncateLimit.

Output:
    str — standalone HTML doc with Plotly.js inlined.
"""
from __future__ import annotations

import logging
import uuid
from typing import Dict, List

import numpy as np
import pandas as pd
import plotly.graph_objects as go
from flowthru import step

logger = logging.getLogger(__name__)

# MTG color-identity palette. Picked for contrast against a white plot background — pure white
# would be invisible, so W is a warm gold. Black is a near-black grey so the marker outline is
# still visible at small sizes.
_COLORLESS = "colorless"
_MULTICOLOR = "multicolor"
_COLOR_PALETTE: Dict[str, str] = {
    "W": "#D9B65A",          # gold-cream
    "U": "#2A7FCB",          # blue
    "B": "#3A3A3A",          # near-black
    "R": "#D9453A",          # red
    "G": "#2EA158",          # green
    _COLORLESS: "#9CA3AF",   # neutral grey
}
# Multicolor cards render as transparent fill with a dark outline. The dark outline is the only
# visual signal — the empty center lets you see what's behind dense multicolor regions.
_MULTICOLOR_OUTLINE = "#1F2937"
_MULTICOLOR_OUTLINE_WIDTH = 1.0


def _normalize_guid(v) -> str | None:
    if v is None:
        return None
    if isinstance(v, float) and pd.isna(v):
        return None
    if isinstance(v, (bytes, bytearray)):
        try:
            return str(uuid.UUID(bytes=bytes(v)))
        except ValueError:
            return None
    s = str(v)
    return s if s else None


def _truncate(text: object, limit: int) -> str:
    if text is None or (isinstance(text, float) and pd.isna(text)):
        return ""
    s = str(text)
    if len(s) <= limit:
        return s
    return s[: limit - 1] + "…"


def _format_oracle_html(text: object, limit: int) -> str:
    if text is None or (isinstance(text, float) and pd.isna(text)):
        return ""
    s = str(text)
    if len(s) > limit:
        s = s[: limit - 1] + "…"
    return s.replace("\n", "<br>")


def _format_pt(power: object, toughness: object) -> str:
    p = "" if power is None or (isinstance(power, float) and pd.isna(power)) else str(power)
    t = "" if toughness is None or (isinstance(toughness, float) and pd.isna(toughness)) else str(toughness)
    if not p and not t:
        return ""
    return f"{p}/{t}"


def _color_identity_bucket(ci: object) -> str:
    """Classify a Scryfall color_identity string into one of the palette buckets.

    color_identity arrives as a string like 'W', 'WU', 'WUBRG', or '' (colorless). The Scryfall
    convention is single-letter color codes concatenated, no separator. We bucket as:
        - empty / None  → 'colorless'
        - one character → that character ('W' | 'U' | 'B' | 'R' | 'G')
        - 2+ characters → 'multicolor' (rendered as transparent fill + outline)
    """
    if ci is None or (isinstance(ci, float) and pd.isna(ci)):
        return _COLORLESS
    s = str(ci).strip().upper()
    if not s:
        return _COLORLESS
    # Strip any non-WUBRG chars defensively
    valid = "".join(c for c in s if c in "WUBRG")
    if not valid:
        return _COLORLESS
    if len(valid) == 1:
        return valid
    return _MULTICOLOR


def _build_atlas_plot_impl(
    points: pd.DataFrame,
    lines: pd.DataFrame,
    hover: pd.DataFrame,
    config: dict,
) -> str:
    annotation_text_limit = int(config.get("AnnotationTextLimit", 36))  # unused but tolerated
    marker_size = int(config["MarkerSize"])
    marker_opacity = float(config["MarkerOpacity"])
    oracle_truncate_limit = int(config["OracleHoverTruncateLimit"])
    logger.info(
        "Inputs: %d points, %d lines, %d hover rows",
        len(points), len(lines), len(hover),
    )

    # ── Normalize Guid columns across all sources so the join keys line up. ──
    points = points.copy()
    lines = lines.copy()
    points["line_id"] = points["line_id"].map(_normalize_guid)
    lines["line_id"] = lines["line_id"].map(_normalize_guid)

    line_with_text = lines[["line_id", "card_id", "text"]].rename(columns={"text": "line_text"})

    merged = (
        points.merge(line_with_text, on="line_id", how="left", validate="one_to_one")
              .merge(hover, on="card_id", how="left", validate="many_to_one")
    )
    merged["name"] = merged["name"].fillna("(unknown)")
    merged["_color_bucket"] = merged["color_identity"].map(_color_identity_bucket)
    merged["_hover_pt"] = [_format_pt(p, t) for p, t in zip(merged["power"], merged["toughness"])]
    merged["_hover_oracle"] = merged["oracle_text"].map(
        lambda t: _format_oracle_html(t, oracle_truncate_limit)
    )
    merged["_hover_line"] = merged["line_text"].map(
        lambda t: _truncate(t, oracle_truncate_limit).replace("\n", " · ")
    )
    merged["_hover_mana"] = merged["mana_cost"].fillna("")
    merged["_hover_ci"] = merged["color_identity"].fillna("(colorless)")

    bucket_counts = merged["_color_bucket"].value_counts()
    logger.info("Color-identity distribution: %s", bucket_counts.to_dict())

    # ── Trace order: monocolor + colorless render first (opaque), multicolor renders LAST so its
    # outlined hollow markers sit on top and remain visible. ──
    bucket_order = ["W", "U", "B", "R", "G", _COLORLESS, _MULTICOLOR]
    traces: List[go.Scattergl] = []
    for bucket in bucket_order:
        subset = merged[merged["_color_bucket"] == bucket]
        if len(subset) == 0:
            continue
        if bucket == _MULTICOLOR:
            marker = dict(
                color="rgba(0, 0, 0, 0)",  # fully transparent fill
                size=marker_size,
                line=dict(width=_MULTICOLOR_OUTLINE_WIDTH, color=_MULTICOLOR_OUTLINE),
            )
            legend_label = f"Multicolor ({len(subset)})"
        else:
            marker = dict(
                color=_COLOR_PALETTE[bucket],
                size=marker_size,
                opacity=marker_opacity,
                line=dict(width=0.3, color="#1F2937"),
            )
            display_name = {
                "W": "White", "U": "Blue", "B": "Black", "R": "Red", "G": "Green",
                _COLORLESS: "Colorless",
            }[bucket]
            legend_label = f"{display_name} ({len(subset)})"

        traces.append(go.Scattergl(
            x=subset["x"],
            y=subset["y"],
            mode="markers",
            name=legend_label,
            marker=marker,
            customdata=subset[
                [
                    "name",          # 0  card name
                    "_hover_mana",   # 1  mana cost
                    "type_line",     # 2  type line
                    "_hover_pt",     # 3  P/T
                    "_hover_ci",     # 4  color identity (raw string)
                    "_hover_line",   # 5  THIS line's text
                    "_hover_oracle", # 6  full card oracle text
                ]
            ].to_numpy(),
            hovertemplate=(
                "<b>%{customdata[0]}</b>"
                "<br>%{customdata[1]} · %{customdata[2]}"
                "<br>%{customdata[3]}  ·  Colors: %{customdata[4]}"
                "<br>"
                "<br><b>Line:</b> %{customdata[5]}"
                "<br>"
                "<br><b>Oracle text:</b>"
                "<br>%{customdata[6]}"
                "<extra></extra>"
            ),
        ))

    fig = go.Figure(data=traces)
    fig.update_layout(
        title=(
            f"MagicAtlas — UMAP of oracle-text embeddings ({len(merged):,} line points)"
        ),
        xaxis_title="UMAP-1",
        yaxis_title="UMAP-2",
        plot_bgcolor="white",
        legend_title_text="Color identity",
        margin=dict(l=60, r=20, t=80, b=60),
        hoverlabel=dict(bgcolor="white", font_size=12, align="left"),
    )
    fig.update_xaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")
    fig.update_yaxes(zeroline=False, showgrid=True, gridcolor="#E5E7EB")

    html = fig.to_html(include_plotlyjs="inline", full_html=True)
    logger.info("Produced %d-byte standalone HTML", len(html))
    return html


@step(
    inputs=[
        "AtlasReportingPoints",
        "OracleLines",
        "AtlasCardHoverInfo",
        "ReportingConfig",
    ],
    outputs="AtlasPlotHtml",
    cacheable=True,
)
def build_atlas_plot(
    points: pd.DataFrame,
    lines: pd.DataFrame,
    hover: pd.DataFrame,
    config: dict,
) -> str:
    return _build_atlas_plot_impl(points, lines, hover, config)
