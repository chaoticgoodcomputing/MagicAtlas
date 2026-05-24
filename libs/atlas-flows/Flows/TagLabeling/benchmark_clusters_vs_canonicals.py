"""Scores a candidate clustering (HDBSCAN) against the canonical line-attribution ground
truth. Per-cluster purity + recall + V-measure plus a corpus-wide row with homogeneity,
completeness, V-measure, and Adjusted Rand Index.

Treat canonicals as ground truth and clusters as the candidate; this is the scorecard for
future clustering work (HDBSCAN re-tunes, alternative algorithms).

Inputs:
    cluster_assignments: ClusterAssignments [line_id, cluster_id]
    line_primary:        LinePrimaryCanonicals [line_id, canonical_slug, canonical_family, …]

Output: DataFrame conforming to ClusterCanonicalBenchmark
        [cluster_id, dominant_canonical, n_member_lines, n_canonical_lines, purity,
         canonical_recall, v_measure, ari]

  Per-cluster rows: cluster_id >= 0
    purity            — homogeneity of the cluster (fraction matching dominant canonical)
    canonical_recall  — fraction of the dominant canonical's lines that landed here
    v_measure         — per-cluster V-measure (homogeneity ∩ completeness for this cluster)

  Noise bucket row:  cluster_id == -1, dominant_canonical == "(noise)"

  Overall row:       cluster_id == -2, dominant_canonical == "*"
    purity            — overall homogeneity
    canonical_recall  — overall completeness
    v_measure         — overall V-measure (harmonic mean of the above)
    ari               — Adjusted Rand Index
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


def _benchmark_impl(
    cluster_assignments: pd.DataFrame,
    line_primary: pd.DataFrame,
) -> pd.DataFrame:
    from sklearn import metrics

    # Normalize line_id on both sides.
    ca = cluster_assignments.copy()
    lp = line_primary.copy()
    ca["line_id"] = ca["line_id"].map(_normalize_guid)
    lp["line_id"] = lp["line_id"].map(_normalize_guid)

    # Inner join — only lines that have BOTH a cluster and a canonical participate.
    joined = ca.merge(lp[["line_id", "canonical_slug"]], on="line_id", how="inner", validate="one_to_one")
    logger.info(
        "Benchmark join: %d lines (cluster ∩ canonical) from %d cluster assignments × %d primary canonicals",
        len(joined), len(ca), len(lp),
    )

    if len(joined) == 0:
        raise ValueError("Empty join — cluster assignments and line primaries don't overlap")

    rows = []

    # ── Per-cluster purity + recall + V-measure ──
    # Pre-compute canonical-wide totals for recall.
    canonical_totals = joined.groupby("canonical_slug").size().to_dict()

    for cluster_id, group in joined.groupby("cluster_id"):
        n_members = len(group)
        canonical_counts = group["canonical_slug"].value_counts()
        dominant = canonical_counts.idxmax()
        dominant_count = int(canonical_counts.iloc[0])

        purity = dominant_count / n_members
        n_canonical_lines = canonical_totals.get(dominant, 0)
        recall = dominant_count / n_canonical_lines if n_canonical_lines > 0 else 0.0
        # Harmonic mean = per-cluster V-measure proxy.
        v = 2 * purity * recall / (purity + recall) if (purity + recall) > 0 else 0.0

        label = "(noise)" if cluster_id == -1 else dominant
        rows.append({
            "cluster_id": int(cluster_id),
            "dominant_canonical": label,
            "n_member_lines": n_members,
            "n_canonical_lines": n_canonical_lines,
            "purity": float(purity),
            "canonical_recall": float(recall),
            "v_measure": float(v),
            "ari": 0.0,
        })

    # ── Corpus-wide metrics ──
    # Exclude noise (-1) for the corpus metrics since noise has no canonical equivalent.
    non_noise = joined[joined["cluster_id"] != -1]
    if len(non_noise) > 0:
        labels_true = non_noise["canonical_slug"].astype("category").cat.codes.to_numpy()
        labels_pred = non_noise["cluster_id"].astype(int).to_numpy()
        homogeneity = float(metrics.homogeneity_score(labels_true, labels_pred))
        completeness = float(metrics.completeness_score(labels_true, labels_pred))
        v_measure = float(metrics.v_measure_score(labels_true, labels_pred))
        ari = float(metrics.adjusted_rand_score(labels_true, labels_pred))
    else:
        homogeneity = completeness = v_measure = ari = 0.0

    rows.append({
        "cluster_id": -2,
        "dominant_canonical": "*",
        "n_member_lines": len(non_noise),
        "n_canonical_lines": int(non_noise["canonical_slug"].nunique()),
        "purity": homogeneity,
        "canonical_recall": completeness,
        "v_measure": v_measure,
        "ari": ari,
    })

    out = pd.DataFrame(rows).sort_values(
        ["cluster_id"], key=lambda s: s.map(lambda v: (v == -2, v == -1, v)),
    ).reset_index(drop=True)

    logger.info(
        "Overall: homogeneity=%.3f, completeness=%.3f, V=%.3f, ARI=%.3f "
        "(over %d non-noise lines, %d clusters, %d canonicals)",
        homogeneity, completeness, v_measure, ari, len(non_noise),
        non_noise["cluster_id"].nunique(), non_noise["canonical_slug"].nunique(),
    )
    # Top-purity clusters preview
    per_cluster = out[(out["cluster_id"] != -2) & (out["cluster_id"] != -1)].nlargest(5, "purity")
    logger.info("Top 5 purest clusters:")
    for _, r in per_cluster.iterrows():
        logger.info("  cluster=%d size=%d → %s (purity=%.2f, recall=%.2f)",
                    int(r["cluster_id"]), int(r["n_member_lines"]),
                    r["dominant_canonical"], r["purity"], r["canonical_recall"])

    return out


@step(
    inputs=["ClusterAssignments", "LinePrimaryCanonicals"],
    outputs="ClusterCanonicalBenchmark",
    cacheable=True,
)
def benchmark_clusters_vs_canonicals(
    cluster_assignments: pd.DataFrame,
    line_primary: pd.DataFrame,
) -> pd.DataFrame:
    return _benchmark_impl(cluster_assignments, line_primary)
