"""Rank tag candidates per cluster. For each HDBSCAN cluster, compute its centroid in encoded-
text space, then return the top-K tag centroids (from the unified exemplar + Scryfall pool) by
cosine similarity. Also emits a sample of representative oracle lines for the labeler prompt.

Inputs:
    assignments:        DataFrame [line_id, cluster_id] — HDBSCAN output.
    lines:              DataFrame [line_id, card_id, oracle_id, text] — OracleLines.
    encoded:            DataFrame [text, embedding] — persisted encoder cache.
    exemplar_centroids: DataFrame conforming to TagCentroid (curated archetypes).
    scryfall_centroids: DataFrame conforming to TagCentroid (otag-derived archetypes).
    config:             TagLabelingConfig — uses TopKAffinity and MaxSampleLines.

Output: DataFrame conforming to ClusterTagAffinity — one row per cluster (excluding -1 noise).
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _decode_embeddings(blobs: list[bytes]) -> np.ndarray:
    """Decode an iterable of little-endian float16 blobs into a (n, dim) float32 matrix."""
    if not blobs:
        return np.zeros((0, 0), dtype=np.float32)
    dim = len(np.frombuffer(blobs[0], dtype="<f2"))
    mat = np.empty((len(blobs), dim), dtype=np.float32)
    for i, b in enumerate(blobs):
        mat[i] = np.frombuffer(b, dtype="<f2").astype(np.float32)
    return mat


def _compute_impl(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    exemplar_centroids: pd.DataFrame,
    scryfall_centroids: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    top_k = int(config["TopKAffinity"])
    max_samples = int(config["MaxSampleLines"])
    exemplar_bonus = float(config.get("ExemplarBonus", 0.0))

    # ── Stack tag centroids into one matrix with parallel metadata arrays.
    combined = pd.concat([exemplar_centroids, scryfall_centroids], ignore_index=True)
    if len(combined) == 0:
        raise ValueError("No tag centroids in either pool — upstream pipeline is broken")
    tag_mat = _decode_embeddings(combined["embedding"].tolist())
    # Re-normalize defensively (centroids should already be unit-norm, but the f16 round-trip
    # introduces a tiny norm drift).
    norms = np.linalg.norm(tag_mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    tag_mat = tag_mat / norms

    tag_slugs = combined["slug"].tolist()
    tag_names = combined["name"].tolist()
    tag_sources = combined["source"].tolist()
    logger.info(
        "Tag candidate pool: %d total (%d exemplar + %d scryfall), dim=%d",
        len(combined), len(exemplar_centroids), len(scryfall_centroids), tag_mat.shape[1],
    )

    # ── Decode line embeddings and align with cluster assignments.
    lines_x_encoded = lines.merge(encoded, on="text", how="inner", validate="many_to_one")
    joined = lines_x_encoded.merge(assignments, on="line_id", how="inner", validate="one_to_one")
    logger.info(
        "Lines after join (encoded × assignments): %d", len(joined),
    )

    line_mat = _decode_embeddings(joined["embedding"].tolist())
    line_norms = np.linalg.norm(line_mat, axis=1, keepdims=True)
    line_norms[line_norms == 0] = 1.0
    line_mat_normed = line_mat / line_norms

    cluster_ids = joined["cluster_id"].astype(int).to_numpy()
    line_texts = joined["text"].tolist()

    rows = []
    for cid in sorted(set(cluster_ids.tolist())):
        if cid == -1:
            # Noise bucket — emit no affinity row. Reporting renders it with a sentinel label.
            continue
        member_idx = np.where(cluster_ids == cid)[0]
        if len(member_idx) == 0:
            continue
        sub = line_mat_normed[member_idx]                 # (n_members, dim)
        centroid = sub.mean(axis=0)
        norm = float(np.linalg.norm(centroid))
        if norm > 0:
            centroid = centroid / norm

        # Cosine sim against all tag centroids (since both sides are unit-norm, dot = cosine).
        scores = tag_mat @ centroid                       # (n_tags,)
        # Apply additive bonus to exemplar-source candidates — reflects the curated-intent
        # prior. See TagLabelingConfig.ExemplarBonus for rationale.
        if exemplar_bonus > 0:
            bonus_mask = np.array([1.0 if s == "exemplar" else 0.0 for s in tag_sources])
            scores = scores + exemplar_bonus * bonus_mask
        top_idx = np.argsort(-scores)[:top_k]
        cand_slugs = [tag_slugs[i] for i in top_idx]
        cand_names = [tag_names[i] for i in top_idx]
        cand_sources = [tag_sources[i] for i in top_idx]
        cand_scores = [float(scores[i]) for i in top_idx]

        # Sample lines = the members closest to the cluster centroid (most representative).
        # If a cluster is large, sub @ centroid is cheap (~hundreds of dot products).
        member_sims = sub @ centroid
        sample_pick = np.argsort(-member_sims)[:max_samples]
        sample_lines = [line_texts[member_idx[i]] for i in sample_pick]

        rows.append({
            "cluster_id": int(cid),
            "cluster_size": int(len(member_idx)),
            "candidate_slugs": cand_slugs,
            "candidate_names": cand_names,
            "candidate_sources": cand_sources,
            "candidate_scores": cand_scores,
            "sample_lines": sample_lines,
        })

    out = pd.DataFrame(rows).sort_values("cluster_size", ascending=False).reset_index(drop=True)
    logger.info("Emitted %d affinity rows (excluding noise bucket).", len(out))
    if len(out) > 0:
        logger.info(
            "Top cluster: id=%d, size=%d, top-3 candidates: %s",
            int(out.iloc[0]["cluster_id"]),
            int(out.iloc[0]["cluster_size"]),
            list(zip(out.iloc[0]["candidate_slugs"][:3], [f"{s:.3f}" for s in out.iloc[0]["candidate_scores"][:3]])),
        )
    return out


@step(
    inputs=[
        "ClusterAssignments",
        "OracleLines",
        "EncodedTexts",
        "ExemplarTagCentroids",
        "ScryfallTagCentroids",
        "TagLabelingConfig",
    ],
    outputs="ClusterTagAffinity",
    cacheable=True,
)
def compute_cluster_tag_affinity(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    exemplar_centroids: pd.DataFrame,
    scryfall_centroids: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _compute_impl(assignments, lines, encoded, exemplar_centroids, scryfall_centroids, config)
