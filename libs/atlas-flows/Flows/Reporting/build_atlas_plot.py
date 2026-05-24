"""Render the atlas as a standalone Plotly HTML.

Post-restructure rendering choices:
- Points are colored by **canonical_family** (the root of the colon hierarchy: `tribal`, `hate`,
  `removal`, `evasion`, etc.), not WUBRG. WUBRG was useful when the atlas was an embedding-only
  view; now that supervised structure dominates, family colors directly visualize the canonical
  taxonomy.
- Hover details list **all** canonical attributions for a line (not just primary), with each
  attribution's confidence and source. Reveals when multiple inference paths agree, and when a
  line spans several mechanic domains.
- Annotations are placed at the **medoid** of each canonical's members — the member point that
  minimizes summed distance to the others — not the geometric mean. The mean is a mathematical
  fiction when clusters are non-spherical; the medoid is an actual member position and tends to
  land inside the densest region of the cluster.
- Parent→child colon-hierarchy relationships render as thin grey edges between annotation
  positions, producing a tag-skeleton overlay.

Inputs:
    points:       DataFrame [line_id, x, y]
    lines:        DataFrame [line_id, card_id, text] — provides per-line text + the line→card key
    hover:        DataFrame [card_id, name, mana_cost, cmc, type_line, color_identity, power,
                              toughness, oracle_text]
    primary:      DataFrame [line_id, canonical_slug, canonical_family, confidence, source]
                  — drives coloring + annotation placement.
    assignments:  DataFrame [line_id, canonical_slug, confidence, source]
                  — ALL attributions per line for the multi-tag hover list.
    curation:     DataFrame [canonical_slug, name, category, description, aliases]
                  — referenced only for parent slug existence in the hierarchy; annotation labels
                    use the raw colon slug (no display-name sanitisation).

Output:
    str — standalone HTML doc with Plotly.js inlined.
"""
from __future__ import annotations

import logging
import uuid
from typing import Dict, List, Tuple

import numpy as np
import pandas as pd
import plotly.express as px
import plotly.graph_objects as go
from flowthru import step

logger = logging.getLogger(__name__)

_UNCATEGORIZED = "(uncategorized)"
_UNCATEGORIZED_COLOR = "#9CA3AF"
_EDGE_COLOR = "rgba(120, 120, 120, 0.35)"
_EDGE_WIDTH = 0.4
# Cap members used in O(N²) medoid computation. Above this, sample deterministically; medoid
# location is robust to subsampling for large clusters.
_MEDOID_MAX_MEMBERS = 500
_RNG_SEED = 42


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


def _scaled_font(n: int, n_min: int, n_max: int) -> int:
    """Visual hierarchy: bigger canonicals get bigger labels (8–14pt range)."""
    if n_max <= n_min:
        return 10
    t = (n - n_min) / (n_max - n_min)
    return int(round(8 + t * 6))


def _build_family_palette(families: List[str]) -> Dict[str, str]:
    """Deterministic family→color mapping. Uncategorized lines always get grey; everything else
    cycles through Plotly's qualitative palettes (Plotly + Light24 + Dark24 = ~50 distinct colors)
    in alphabetical family order so the mapping is stable across runs."""
    palette = list(px.colors.qualitative.Plotly) + \
              list(px.colors.qualitative.Light24) + \
              list(px.colors.qualitative.Dark24)
    sorted_families = sorted(f for f in families if f != _UNCATEGORIZED)
    mapping: Dict[str, str] = {_UNCATEGORIZED: _UNCATEGORIZED_COLOR}
    for i, fam in enumerate(sorted_families):
        mapping[fam] = palette[i % len(palette)]
    return mapping


def _build_all_tags_hover_html(
    assignments: pd.DataFrame,
) -> Dict[str, str]:
    """Returns line_id → pre-formatted hover snippet listing every attribution for that line,
    one per line of HTML, sorted by confidence descending."""
    out: Dict[str, str] = {}
    # Sort once globally; per-line ordering preserved.
    sorted_df = assignments.sort_values("confidence", ascending=False)
    for line_id, group in sorted_df.groupby("line_id", sort=False):
        rows = [
            f"  {slug}  ({conf:.2f}, {src})"
            for slug, conf, src in zip(
                group["canonical_slug"], group["confidence"], group["source"]
            )
        ]
        out[line_id] = "<br>".join(rows) if rows else ""
    return out


def _compute_medoid(coords: np.ndarray) -> Tuple[float, float]:
    """L1-medoid (member with smallest sum of distances to other members). Robust to outliers
    and lives at an actual member position — unlike the geometric mean which is a math fiction
    for non-spherical clusters. Subsample to keep the O(N²) cost bounded."""
    n = len(coords)
    if n == 0:
        return (0.0, 0.0)
    if n == 1:
        return (float(coords[0, 0]), float(coords[0, 1]))
    if n > _MEDOID_MAX_MEMBERS:
        rng = np.random.default_rng(_RNG_SEED)
        idx = rng.choice(n, size=_MEDOID_MAX_MEMBERS, replace=False)
        coords = coords[idx]
    diff = coords[:, None, :] - coords[None, :, :]
    dists = np.linalg.norm(diff, axis=2)
    sum_dists = dists.sum(axis=1)
    medoid = coords[int(np.argmin(sum_dists))]
    return (float(medoid[0]), float(medoid[1]))


def _build_atlas_plot_impl(
    points: pd.DataFrame,
    lines: pd.DataFrame,
    hover: pd.DataFrame,
    primary: pd.DataFrame,
    assignments: pd.DataFrame,
    curation: pd.DataFrame,
    config: dict,
) -> str:
    annotation_text_limit = int(config["AnnotationTextLimit"])
    marker_size = int(config["MarkerSize"])
    marker_opacity = float(config["MarkerOpacity"])
    oracle_truncate_limit = int(config["OracleHoverTruncateLimit"])
    logger.info(
        "Inputs: %d points, %d lines, %d hover rows, %d primary canonicals, "
        "%d all-attributions, %d curation entries",
        len(points), len(lines), len(hover), len(primary), len(assignments), len(curation),
    )

    # ── Normalize Guid columns across all sources so the join keys line up. ──
    points = points.copy()
    lines = lines.copy()
    primary = primary.copy()
    assignments = assignments.copy()
    points["line_id"] = points["line_id"].map(_normalize_guid)
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    primary["line_id"] = primary["line_id"].map(_normalize_guid)
    assignments["line_id"] = assignments["line_id"].map(_normalize_guid)

    line_with_text = lines[["line_id", "card_id", "text"]].rename(columns={"text": "line_text"})

    merged = (
        points.merge(primary[["line_id", "canonical_slug", "canonical_family", "confidence", "source"]],
                     on="line_id", how="left", validate="one_to_one")
        .merge(line_with_text, on="line_id", how="left", validate="one_to_one")
        .merge(hover, on="card_id", how="left", validate="many_to_one")
    )
    merged["canonical_slug"] = merged["canonical_slug"].fillna(_UNCATEGORIZED)
    merged["canonical_family"] = merged["canonical_family"].fillna(_UNCATEGORIZED)
    merged["confidence"] = merged["confidence"].fillna(0.0).astype(float)
    merged["source"] = merged["source"].fillna("")
    merged["name"] = merged["name"].fillna("(unknown)")

    merged["_hover_pt"] = [_format_pt(p, t) for p, t in zip(merged["power"], merged["toughness"])]
    merged["_hover_oracle"] = merged["oracle_text"].map(
        lambda t: _format_oracle_html(t, oracle_truncate_limit)
    )
    merged["_hover_line"] = merged["line_text"].map(
        lambda t: _truncate(t, oracle_truncate_limit).replace("\n", " · ")
    )
    merged["_hover_mana"] = merged["mana_cost"].fillna("")

    # All-tags hover snippets, keyed by line_id.
    all_tags_by_line = _build_all_tags_hover_html(assignments)
    merged["_hover_all_tags"] = merged["line_id"].map(
        lambda lid: all_tags_by_line.get(lid, "")
    ).fillna("")

    n_categorized = int((merged["canonical_slug"] != _UNCATEGORIZED).sum())
    n_multi_tag = sum(1 for v in all_tags_by_line.values() if v.count("<br>") >= 1)
    logger.info(
        "Categorized %d / %d points (%.0f%%) with a primary canonical. "
        "Lines with 2+ attributions: %d. Medoid placement (not mean centroid) for annotations.",
        n_categorized, len(merged), 100 * n_categorized / max(len(merged), 1), n_multi_tag,
    )

    # ── Build family palette + traces (one per family for the legend). ──
    families = sorted(merged["canonical_family"].unique())
    family_palette = _build_family_palette(families)
    family_traces = _build_family_traces(merged, family_palette, marker_size, marker_opacity)
    logger.info("Built %d family-colored traces", len(family_traces))

    # ── Per-canonical medoid over the actual rendered line positions. ──
    categorized = merged[merged["canonical_slug"] != _UNCATEGORIZED]
    per_canonical_medoid: List[dict] = []
    for slug, grp in categorized.groupby("canonical_slug"):
        coords = grp[["x", "y"]].to_numpy(dtype=np.float64)
        mx, my = _compute_medoid(coords)
        per_canonical_medoid.append({
            "canonical_slug": slug,
            "cx": mx,
            "cy": my,
            "n": len(grp),
        })
    per_canonical = pd.DataFrame(per_canonical_medoid)

    annotations, edge_shapes = _build_canonical_overlay(per_canonical, annotation_text_limit)
    logger.info(
        "Overlay: %d canonical annotations, %d parent→child edges",
        len(annotations), len(edge_shapes),
    )

    fig = go.Figure(data=family_traces)
    fig.update_layout(
        title=(
            f"MagicAtlas — UMAP of oracle-text embeddings "
            f"({len(merged):,} line points; {n_categorized:,} canonically-attributed; "
            f"{len(families) - 1} families)"
        ),
        xaxis_title="UMAP-1",
        yaxis_title="UMAP-2",
        plot_bgcolor="white",
        legend_title_text="Canonical family",
        margin=dict(l=60, r=20, t=80, b=60),
        hoverlabel=dict(bgcolor="white", font_size=12, align="left"),
        annotations=annotations,
        shapes=edge_shapes,
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
        "LinePrimaryCanonicals",
        "OracleLineCanonicalAssignments",
        "ScryfallTagCuration",
        "ReportingConfig",
    ],
    outputs="AtlasPlotHtml",
    cacheable=True,
)
def build_atlas_plot(
    points: pd.DataFrame,
    lines: pd.DataFrame,
    hover: pd.DataFrame,
    primary: pd.DataFrame,
    assignments: pd.DataFrame,
    curation: pd.DataFrame,
    config: dict,
) -> str:
    return _build_atlas_plot_impl(points, lines, hover, primary, assignments, curation, config)


def _build_family_traces(
    merged: pd.DataFrame,
    family_palette: Dict[str, str],
    marker_size: int,
    marker_opacity: float,
) -> List[go.Scattergl]:
    """One Scattergl trace per canonical family, ordered by member count so big families paint
    first (small families on top remain visible)."""
    counts = merged["canonical_family"].value_counts()
    # Render uncategorized last so it sits underneath the meaningful families in z-order.
    ordered_families = [
        f for f in counts.index if f != _UNCATEGORIZED
    ]
    if _UNCATEGORIZED in counts.index:
        ordered_families = [_UNCATEGORIZED] + ordered_families
    traces: List[go.Scattergl] = []
    for fam in ordered_families:
        subset = merged[merged["canonical_family"] == fam]
        if len(subset) == 0:
            continue
        traces.append(
            go.Scattergl(
                x=subset["x"],
                y=subset["y"],
                mode="markers",
                name=f"{fam} ({len(subset)})",
                marker=dict(
                    color=family_palette.get(fam, _UNCATEGORIZED_COLOR),
                    size=marker_size,
                    opacity=marker_opacity,
                    line=dict(width=0.3, color="#1f2937"),
                ),
                customdata=subset[
                    [
                        "name",            # 0  card name
                        "_hover_mana",     # 1  mana cost
                        "type_line",       # 2  type line
                        "_hover_pt",       # 3  P/T
                        "_hover_line",     # 4  THIS line's text (the point's actual encoding)
                        "_hover_oracle",   # 5  full card oracle text (with newlines)
                        "_hover_all_tags", # 6  ALL canonical attributions: slug (conf, source)
                    ]
                ].to_numpy(),
                hovertemplate=(
                    "<b>%{customdata[0]}</b>"
                    "<br>%{customdata[1]} · %{customdata[2]}"
                    "<br>%{customdata[3]}"
                    "<br>"
                    "<br><b>Line:</b> %{customdata[4]}"
                    "<br>"
                    "<br><b>Oracle text:</b>"
                    "<br>%{customdata[5]}"
                    "<br>"
                    "<br><b>Tags:</b>"
                    "<br>%{customdata[6]}"
                    "<extra></extra>"
                ),
            )
        )
    return traces


def _build_canonical_overlay(
    per_canonical: pd.DataFrame,
    annotation_text_limit: int,
) -> Tuple[List[dict], List[dict]]:
    """All-tier overlay: every canonical gets an annotation (font scaled by member count),
    plus parent→child edges drawn from the colon hierarchy. Annotation positions come from the
    medoid computation in _build_atlas_plot_impl."""
    if len(per_canonical) == 0:
        return [], []

    centroid_by_slug: Dict[str, Tuple[float, float]] = {
        row.canonical_slug: (float(row.cx), float(row.cy))
        for row in per_canonical.itertuples(index=False)
    }
    n_by_slug: Dict[str, int] = {
        row.canonical_slug: int(row.n)
        for row in per_canonical.itertuples(index=False)
    }
    n_max = max(n_by_slug.values())
    n_min = min(n_by_slug.values())

    # ── Edges: each canonical with a colon → its immediate parent (if that parent has a medoid). ──
    edge_shapes: List[dict] = []
    for slug, (cx, cy) in centroid_by_slug.items():
        if ":" not in slug:
            continue
        parent = slug.rsplit(":", 1)[0]
        if parent not in centroid_by_slug:
            continue
        px, py = centroid_by_slug[parent]
        edge_shapes.append(dict(
            type="line",
            xref="x", yref="y",
            x0=px, y0=py,
            x1=cx, y1=cy,
            line=dict(color=_EDGE_COLOR, width=_EDGE_WIDTH),
            layer="below",
        ))

    # ── Annotations: every canonical (no top-N cap), font size scaled by member count. ──
    annotations: List[dict] = []
    # Render larger canonicals last so their labels paint on top of smaller-canonical labels.
    for slug, n in sorted(n_by_slug.items(), key=lambda kv: kv[1]):
        cx, cy = centroid_by_slug[slug]
        text = slug if len(slug) <= annotation_text_limit else slug[: annotation_text_limit - 1] + "…"
        annotations.append(dict(
            x=cx,
            y=cy,
            xref="x",
            yref="y",
            text=text,
            showarrow=False,
            font=dict(size=_scaled_font(n, n_min, n_max), color="#111827"),
            bgcolor="rgba(255, 255, 255, 0.78)",
            bordercolor="rgba(31, 41, 55, 0.30)",
            borderwidth=0.4,
            borderpad=2,
        ))
    return annotations, edge_shapes
