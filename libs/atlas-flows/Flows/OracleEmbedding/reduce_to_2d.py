"""Unsupervised UMAP from HD encoded oracle text → 2D AtlasPoints.

Explorer-mode pipeline:

    HD (768d nomic, fine-tuned)
        │  reduce_to_2d  (UNSUPERVISED, pure topology)
        ▼
    2D  (AtlasPoints)

No intermediate 5D layer, no supervision — the atlas exists for browsing semantic neighborhoods,
not for separating archetype regions. Same metric as the encoder produces (cosine on
unit-normalized HD vectors).

Inputs:
    lines:    OracleLines [line_id, card_id, text].
    encoded:  EncodedTexts [text, embedding] — encoder cache, one row per unique text.
    config:   OracleEmbeddingConfig — uses Umap2DNNeighbors, Umap2DMinDist, UmapJitterSigma.

Output: AtlasPoints [line_id, x, y].

Jitter is applied per-row before UMAP because identical-text lines (the encoder dedup
optimization) share the same HD vector; without noise UMAP maps them to the same 2D point and
the scatter collapses many cards to one dot. Tiny noise (sigma ≪ vector norm) spreads them into
a tight ball while preserving topology.
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_N_COMPONENTS = 2
_METRIC = "cosine"
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


def _broadcast_and_jitter(
    lines: pd.DataFrame, encoded: pd.DataFrame, jitter_sigma: float
) -> tuple[pd.Series, np.ndarray]:
    """Join lines × encoded on text, decode the byte-blob to float32, apply Gaussian jitter
    scaled per-row by the embedding norm. Returns (line_ids in input order, jittered vectors)."""
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
    """Returns (reducer, backend_name). Prefers cuML; falls back to umap-learn."""
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


def _reduce_to_2d_impl(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict,
) -> pd.DataFrame:
    n_neighbors = int(config["Umap2DNNeighbors"])
    min_dist = float(config["Umap2DMinDist"])
    jitter_sigma = float(config["UmapJitterSigma"])

    line_ids, vectors = _broadcast_and_jitter(lines, encoded, jitter_sigma)
    logger.info(
        "Input: %d lines × %d unique texts → %d vectors of dim %d "
        "(jitter_sigma=%g, n_neighbors=%d, min_dist=%g, metric=%s)",
        len(lines), len(encoded), *vectors.shape, jitter_sigma,
        n_neighbors, min_dist, _METRIC,
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
    inputs=["OracleLines", "EncodedTexts", "OracleEmbeddingConfig"],
    outputs="AtlasPoints",
    cacheable=True,
)
def reduce_to_2d(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict,
) -> pd.DataFrame:
    return _reduce_to_2d_impl(lines, encoded, config)
