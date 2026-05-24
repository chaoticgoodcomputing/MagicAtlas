"""Per-canonical 2D-placement quality scorecard for the atlas.

For each curated canonical attributed to at least one line, compute:
  - centroid (x, y) — the annotation position
  - radii    (mean / median / p90) of member lines around the centroid
  - dispersion (std of radii) — tight vs. smeared
  - centroid-silhouette — per-line `(b − a) / max(a, b)` averaged over members
  - nearest other canonical's slug + centroid distance
  - overlap_rate — fraction of own members whose 2D nearest centroid is some OTHER canonical
                    (i.e. the line is in the wrong neighborhood)

Plus an overall corpus row (slug = "*") with mean silhouette, mean radius, mean overlap.

Inputs:
    points:  AtlasReportingPoints [line_id, x, y]
    primary: LinePrimaryCanonicals [line_id, canonical_slug, …]

Output: DataFrame conforming to CanonicalPlacementMetric.
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


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


def _evaluate_impl(
    points: pd.DataFrame,
    primary: pd.DataFrame,
) -> pd.DataFrame:
    pts = points.copy()
    pri = primary.copy()
    pts["line_id"] = pts["line_id"].map(_normalize_guid)
    pri["line_id"] = pri["line_id"].map(_normalize_guid)

    joined = pts.merge(pri[["line_id", "canonical_slug"]], on="line_id", how="inner",
                      validate="one_to_one")
    if len(joined) == 0:
        raise ValueError("Empty join — no attributed lines have atlas points")
    logger.info(
        "Evaluating %d attributed line points across %d canonicals",
        len(joined), joined["canonical_slug"].nunique(),
    )

    coords = joined[["x", "y"]].to_numpy(dtype=np.float64)
    slugs = joined["canonical_slug"].to_numpy()

    # ── Per-canonical centroids ──
    canonicals = sorted(set(slugs))
    slug_to_idx = {s: i for i, s in enumerate(canonicals)}
    n_canonicals = len(canonicals)
    member_idxs: list[np.ndarray] = [None] * n_canonicals
    centroids = np.empty((n_canonicals, 2), dtype=np.float64)
    for i, slug in enumerate(canonicals):
        mask = slugs == slug
        member_idxs[i] = np.where(mask)[0]
        centroids[i] = coords[mask].mean(axis=0)

    # ── Per-line distance to ALL centroids (vectorised) ──
    # dists[i, k] = distance from line i to centroid k
    dx = coords[:, 0:1] - centroids[:, 0:1].T  # (n_lines, n_canonicals)
    dy = coords[:, 1:2] - centroids[:, 1:2].T
    dists = np.sqrt(dx * dx + dy * dy)

    # Own-canonical index per line
    own_idx = np.array([slug_to_idx[s] for s in slugs], dtype=np.int64)
    rows = np.arange(len(joined))
    a_per_line = dists[rows, own_idx]                 # distance to own centroid

    # Distance to nearest other canonical
    dists_other = dists.copy()
    dists_other[rows, own_idx] = np.inf
    nearest_other_idx = dists_other.argmin(axis=1)
    b_per_line = dists_other[rows, nearest_other_idx]

    # Per-line silhouette
    sil_denom = np.maximum(a_per_line, b_per_line)
    sil_denom[sil_denom == 0] = 1.0  # guard
    sil_per_line = (b_per_line - a_per_line) / sil_denom

    # Per-line overlap (own > nearest_other)
    overlap_per_line = a_per_line > b_per_line

    # ── Pairwise centroid distances for "nearest_canonical" per row ──
    cx = centroids[:, 0:1]
    cy = centroids[:, 1:2]
    cdx = cx - cx.T
    cdy = cy - cy.T
    cdists = np.sqrt(cdx * cdx + cdy * cdy)
    np.fill_diagonal(cdists, np.inf)
    nearest_centroid_idx = cdists.argmin(axis=1)
    nearest_centroid_d = cdists[np.arange(n_canonicals), nearest_centroid_idx]

    # ── Per-canonical aggregates ──
    rows_out: list[dict] = []
    all_mean_r: list[float] = []
    all_disp: list[float] = []
    all_overlap: list[float] = []
    all_sil: list[float] = []
    for i, slug in enumerate(canonicals):
        idxs = member_idxs[i]
        n = len(idxs)
        radii = a_per_line[idxs]
        mean_r = float(radii.mean())
        median_r = float(np.median(radii))
        p90 = float(np.percentile(radii, 90)) if n > 0 else 0.0
        disp = float(radii.std()) if n > 1 else 0.0
        sil = float(sil_per_line[idxs].mean())
        overlap = float(overlap_per_line[idxs].mean())
        nearest = canonicals[nearest_centroid_idx[i]]
        nearest_d = float(nearest_centroid_d[i]) if np.isfinite(nearest_centroid_d[i]) else 0.0

        rows_out.append({
            "canonical_slug": slug,
            "n_lines": int(n),
            "centroid_x": float(centroids[i, 0]),
            "centroid_y": float(centroids[i, 1]),
            "mean_radius": mean_r,
            "median_radius": median_r,
            "radius_p90": p90,
            "dispersion": disp,
            "silhouette": sil,
            "nearest_canonical": nearest,
            "nearest_distance": nearest_d,
            "overlap_rate": overlap,
        })
        all_mean_r.append(mean_r)
        all_disp.append(disp)
        all_overlap.append(overlap)
        all_sil.append(sil)

    # Overall corpus row
    corpus_centroid = coords.mean(axis=0)
    rows_out.append({
        "canonical_slug": "*",
        "n_lines": int(len(joined)),
        "centroid_x": float(corpus_centroid[0]),
        "centroid_y": float(corpus_centroid[1]),
        "mean_radius": float(np.mean(all_mean_r)),
        "median_radius": float(np.median(all_mean_r)),
        "radius_p90": float(np.percentile(all_mean_r, 90)),
        "dispersion": float(np.mean(all_disp)),
        "silhouette": float(sil_per_line.mean()),
        "nearest_canonical": "",
        "nearest_distance": 0.0,
        "overlap_rate": float(overlap_per_line.mean()),
    })

    out = pd.DataFrame(rows_out).sort_values(
        by=["canonical_slug"],
        key=lambda s: s.map(lambda v: (v == "*", v)),
    ).reset_index(drop=True)

    # ── Quick logging summary ──
    summary = out[out["canonical_slug"] == "*"].iloc[0]
    logger.info(
        "Overall: silhouette=%.3f, mean_radius=%.3f, mean_overlap=%.3f over %d lines / %d canonicals",
        summary["silhouette"], summary["mean_radius"], summary["overlap_rate"],
        int(summary["n_lines"]), n_canonicals,
    )
    per_only = out[out["canonical_slug"] != "*"]
    logger.info(
        "Spread of canonical mean_radius: min=%.3f / median=%.3f / p90=%.3f / max=%.3f",
        float(per_only["mean_radius"].min()), float(per_only["mean_radius"].median()),
        float(per_only["mean_radius"].quantile(0.9)), float(per_only["mean_radius"].max()),
    )
    high_overlap = per_only[per_only["overlap_rate"] >= 0.5].sort_values("overlap_rate", ascending=False)
    logger.info("Canonicals with overlap_rate >= 0.5: %d", len(high_overlap))
    for _, r in high_overlap.head(8).iterrows():
        logger.info(
            "  %s n=%d overlap=%.2f sil=%.2f nearest=%s (%.2f)",
            r["canonical_slug"], int(r["n_lines"]), r["overlap_rate"], r["silhouette"],
            r["nearest_canonical"], r["nearest_distance"],
        )
    return out


@step(
    inputs=["AtlasReportingPoints", "LinePrimaryCanonicals"],
    outputs="CanonicalPlacementMetrics",
    cacheable=True,
)
def evaluate_canonical_placement(
    points: pd.DataFrame,
    primary: pd.DataFrame,
) -> pd.DataFrame:
    return _evaluate_impl(points, primary)
