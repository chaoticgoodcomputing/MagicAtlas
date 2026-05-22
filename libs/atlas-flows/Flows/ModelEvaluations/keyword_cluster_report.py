"""Per-Scryfall-keyword cluster diagnostic. For every keyword, compute which cluster its
synthetic-line cohort fell into, the 2D centroid of those points, an outlier sample, and the
nearest other keywords by centroid distance. Eyeball pass for "did keyword X cluster sensibly?"

Inputs (default-variant entry point):
    vocabulary:   KeywordVocabulary record — { Keywords: List[str] }.
    lines:        DataFrame [line_id, card_id, text] — the line inventory. Synthetic-keyword
                  lines are identified by exact text match against the vocabulary.
    atlas_points: DataFrame [line_id, x, y] — 2D atlas coordinates.
    assignments:  DataFrame [line_id, cluster_id] — HDBSCAN cluster assignments.
    labels:       DataFrame [cluster_id, label, ...] — display labels for clusters.
    hover:        DataFrame [card_id, name, ...] — card-level metadata for outlier names.
    config:       ModelEvaluationsConfig — variant labels (currently unused in this report; the
                  catalog item name encodes the variant identity).

Output: DataFrame [keyword, anchor_cluster_id, anchor_cluster_label, centroid_x, centroid_y,
                   n_member_lines, n_outliers, top_neighbor_keywords, outlier_sample]
        — one row per keyword. `top_neighbor_keywords` is a JSON-encoded string array;
        `outlier_sample` is a JSON-encoded array of `{card_id, card_name, actual_cluster_id}`
        objects (same pattern as ClusterLabel.Keywords). Flat-tabular because Python step
        outputs can't carry nested POCOs through Flowthru's Arrow marshaller.

Two thin entry points share `_build_report_impl`; the only difference is which catalog items
they bind.
"""
from __future__ import annotations

import json
import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_OUTLIER_SAMPLE_SIZE = 10
_TOP_NEIGHBOR_COUNT = 5


def _build_report_impl(
    vocabulary: dict,
    lines: pd.DataFrame,
    atlas_points: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
    hover: pd.DataFrame,
) -> pd.DataFrame:
    keywords: list[str] = list(vocabulary["Keywords"])
    logger.info(
        "Building keyword-cluster report: %d keywords, %d lines, %d points, %d clusters",
        len(keywords), len(lines), len(atlas_points), labels["cluster_id"].nunique(),
    )

    # Join: line → (cluster, x, y, card_id). Synthetic-keyword lines have text equal to a
    # vocabulary entry; we filter on that to isolate keyword anchors from natural lines.
    joined = (
        lines[["line_id", "card_id", "text"]]
        .merge(atlas_points[["line_id", "x", "y"]], on="line_id", how="inner", validate="one_to_one")
        .merge(assignments[["line_id", "cluster_id"]], on="line_id", how="left", validate="one_to_one")
    )
    joined["cluster_id"] = joined["cluster_id"].fillna(-1).astype(int)

    label_by_cluster = dict(
        zip(labels["cluster_id"].astype(int), labels["label"].astype(str))
    )
    name_by_card = dict(zip(hover["card_id"], hover["name"].astype(str)))

    keyword_set = set(keywords)
    keyword_rows = joined[joined["text"].isin(keyword_set)]

    # Pre-compute keyword → (anchor_cluster_id, cx, cy, n) so we can rank neighbors after.
    anchor_by_keyword: dict[str, tuple[int, float, float, int]] = {}
    for keyword, group in keyword_rows.groupby("text"):
        n = len(group)
        if n == 0:
            continue
        cluster_counts = group["cluster_id"].value_counts()
        anchor_cluster_id = int(cluster_counts.idxmax())
        anchor_by_keyword[keyword] = (
            anchor_cluster_id, float(group["x"].mean()), float(group["y"].mean()), n,
        )

    # Rank neighbors per keyword by 2D centroid distance.
    keyword_names = list(anchor_by_keyword.keys())
    if keyword_names:
        centroids = np.array(
            [(anchor_by_keyword[k][1], anchor_by_keyword[k][2]) for k in keyword_names]
        )
    else:
        centroids = np.zeros((0, 2))
    neighbors_by_keyword: dict[str, list[str]] = {}
    for i, k in enumerate(keyword_names):
        if len(keyword_names) <= 1:
            neighbors_by_keyword[k] = []
            continue
        dists = np.linalg.norm(centroids - centroids[i], axis=1)
        order = np.argsort(dists)
        neighbors_by_keyword[k] = [keyword_names[j] for j in order if j != i][:_TOP_NEIGHBOR_COUNT]

    rows: list[dict] = []
    for keyword in keywords:
        if keyword not in anchor_by_keyword:
            continue
        anchor_cluster_id, cx, cy, n_member = anchor_by_keyword[keyword]
        group = keyword_rows[keyword_rows["text"] == keyword]

        outlier_mask = group["cluster_id"] != anchor_cluster_id
        n_outliers = int(outlier_mask.sum())
        outlier_rows = group[outlier_mask].head(_OUTLIER_SAMPLE_SIZE)
        outlier_sample = [
            {
                "card_id": str(row["card_id"]),
                "card_name": name_by_card.get(row["card_id"], "(unknown)"),
                "actual_cluster_id": int(row["cluster_id"]),
            }
            for _, row in outlier_rows.iterrows()
        ]

        rows.append(
            {
                "keyword": keyword,
                "anchor_cluster_id": anchor_cluster_id,
                "anchor_cluster_label": label_by_cluster.get(anchor_cluster_id, "(unlabeled)"),
                "centroid_x": cx,
                "centroid_y": cy,
                "n_member_lines": int(n_member),
                "n_outliers": n_outliers,
                "top_neighbor_keywords": json.dumps(neighbors_by_keyword.get(keyword, [])),
                "outlier_sample": json.dumps(outlier_sample),
            }
        )

    # Sort by member count desc — biggest keywords first for quick eyeballing.
    rows.sort(key=lambda r: -r["n_member_lines"])
    logger.info("Emitted %d keyword entries", len(rows))
    return pd.DataFrame(rows)


@step(
    inputs=[
        "KeywordVocabulary",
        "OracleLines",
        "AtlasPoints",
        "ClusterAssignments",
        "ClusterLabels",
        "AtlasCardHoverInfo",
        "ModelEvaluationsConfig",
    ],
    outputs="KeywordClusterReport",
    cacheable=True,
)
def keyword_cluster_report(
    vocabulary: dict,
    lines: pd.DataFrame,
    atlas_points: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
    hover: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _build_report_impl(vocabulary, lines, atlas_points, assignments, labels, hover)
