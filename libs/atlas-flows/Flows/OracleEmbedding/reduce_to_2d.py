"""UNSUPERVISED UMAP from 5D ClusteringEmbeddings → 2D AtlasPoints.

Post-restructure architecture:

    HD (768d nomic)
        │  reduce_to_five_d  (SUPERVISED, target_weight)   ← supervision lives here
        ▼
    5D  (ClusteringEmbeddings)
        │  reduce_to_2d      (UNSUPERVISED, this file)     ← pure topology
        ▼
    2D  (AtlasPoints)

Why unsupervised: by the time data reaches 5D, the canonical structure is already shaped by the
upstream supervised step. This step's only job is to make that 5D shape visible in 2D without
re-fighting the topology/supervision trade-off. Supervision at 2D was geometrically infeasible
anyway — 280+ canonicals can't be cleanly separated in 2 dimensions.

Inputs:
    five_d:  ClusteringEmbeddings [line_id, vector] — 5D float32 byte blobs.
    config:  OracleEmbeddingConfig — uses Umap2DNNeighbors, Umap2DMinDist.

Output: AtlasPoints [line_id, x, y].

Jitter is NOT applied here — the upstream 5D step already jittered the HD vectors, so identical-
text lines arrive at 5D with slightly-different vectors.
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_N_COMPONENTS = 2
_METRIC = "euclidean"  # 5D outputs are in euclidean space, not cosine
_RANDOM_STATE = 42


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


# Kept exported for backward compatibility — reduce_to_five_d.py imports this from us.
def _broadcast_and_jitter(
    lines: pd.DataFrame, encoded: pd.DataFrame, jitter_sigma: float
) -> tuple[pd.Series, np.ndarray]:
    """Join lines × encoded on text, decode the byte-blob to float32, apply Gaussian jitter
    scaled per-row by the embedding norm. Returns (line_ids in input order, jittered vectors).

    Why jitter: with the encoder-dedup optimization, lines sharing identical text receive the
    SAME embedding vector. Without noise, UMAP maps duplicates to identical coordinates and the
    scatter plot collapses many cards to a single dot. Tiny noise (sigma ≪ vector norm) preserves
    the topology while spreading duplicates into a tight ball after UMAP.
    """
    merged = lines.merge(encoded, on="text", how="left", validate="many_to_one")
    missing = merged["embedding"].isna().sum()
    if missing:
        raise RuntimeError(
            f"{missing} oracle lines have no matching encoded text — encoder cache out of "
            f"sync with OracleLines. Re-run EmbedOracleText."
        )

    vectors = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in merged["embedding"]]
    ).astype(np.float32)

    if jitter_sigma > 0:
        rng = np.random.default_rng(_RANDOM_STATE)
        norms = np.linalg.norm(vectors, axis=1, keepdims=True)
        noise = rng.normal(0.0, jitter_sigma, vectors.shape).astype(np.float32) * norms
        vectors = vectors + noise

    return merged["line_id"], vectors


def _make_umap_reducer(n_neighbors: int, min_dist: float):
    """Returns (reducer, backend_name). Prefers cuML; falls back to umap-learn. Unsupervised
    only — supervision lives at the 5D step, not here."""
    try:
        from cuml.manifold import UMAP as CumlUMAP

        return (
            CumlUMAP(
                n_components=_N_COMPONENTS,
                n_neighbors=n_neighbors,
                min_dist=min_dist,
                metric=_METRIC,
                random_state=_RANDOM_STATE,
            ),
            "cuml",
        )
    except ImportError:
        import umap

        return (
            umap.UMAP(
                n_components=_N_COMPONENTS,
                n_neighbors=n_neighbors,
                min_dist=min_dist,
                metric=_METRIC,
                random_state=_RANDOM_STATE,
            ),
            "umap-learn",
        )


def _reduce_to_2d_impl(five_d: pd.DataFrame, config: dict) -> pd.DataFrame:
    n_neighbors = int(config["Umap2DNNeighbors"])
    min_dist = float(config["Umap2DMinDist"])

    line_ids = five_d["line_id"].copy()
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in five_d["vector"]]
    ).astype(np.float32)

    logger.info(
        "Input: %d × %dD vectors (n_neighbors=%d, min_dist=%g, metric=%s)",
        vectors.shape[0], vectors.shape[1], n_neighbors, min_dist, _METRIC,
    )

    reducer, backend = _make_umap_reducer(n_neighbors=n_neighbors, min_dist=min_dist)
    logger.info("Running unsupervised UMAP → 2D via %s...", backend)
    coords = reducer.fit_transform(vectors)
    if hasattr(coords, "get"):
        coords = coords.get()
    coords = np.asarray(coords)
    logger.info("UMAP output shape: %s", coords.shape)

    return pd.DataFrame({
        "line_id": line_ids.values,
        "x": coords[:, 0].astype(float),
        "y": coords[:, 1].astype(float),
    })


@step(
    inputs=["ClusteringEmbeddings", "OracleEmbeddingConfig"],
    outputs="AtlasPoints",
    cacheable=True,
)
def reduce_to_2d(five_d: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _reduce_to_2d_impl(five_d, config)
