"""UMAP-reduce the encoded oracle texts to 2D for the atlas display. The fine-tuned variant
lives in `reduce_to_2d_finetuned.py`; both files share `_reduce_to_2d_impl` via import.

Inputs:
    lines:    DataFrame of OracleLine rows [line_id, card_id, text] — the per-line inventory.
    encoded:  DataFrame of EncodedText rows [text, embedding] — the encoder cache, one row per
              unique text. Embedding is little-endian float16 bytes (decoded here to float32).
    config:   OracleEmbeddingConfig record — uses `Umap2DNNeighbors`, `Umap2DMinDist`, and
              `UmapJitterSigma`.

Output: DataFrame [line_id, x, y] — one row per OracleLine.

Joins lines × encoded on `text`, applies pre-UMAP Gaussian jitter (sigma scaled per-vector by
embedding norm so identical-text lines from many cards don't collapse to one atlas dot), then
runs UMAP. A sibling step (`Flows.Clustering.reduce_to_five_d`) runs its own UMAP at higher
target-dimensionality on the same join for HDBSCAN; this 2D reduction is purely for the scatter.

Uses RAPIDS cuML's UMAP on GPU when available, falling back to umap-learn on CPU otherwise. The
two implementations don't produce identical coordinates (different initialization under the
hood), but the topology is equivalent.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

# Algorithm-fixed knobs — not surfaced to config because changing them isn't a tuning operation
# but a topology change.
_METRIC = "cosine"
_RANDOM_STATE = 42


def _make_umap_reducer(n_components: int, n_neighbors: int, min_dist: float):
    """Returns (reducer, backend_name). Prefers cuML; falls back to umap-learn."""
    try:
        from cuml.manifold import UMAP as CumlUMAP

        return (
            CumlUMAP(
                n_components=n_components,
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
                n_components=n_components,
                n_neighbors=n_neighbors,
                min_dist=min_dist,
                metric=_METRIC,
                random_state=_RANDOM_STATE,
            ),
            "umap-learn",
        )


def _broadcast_and_jitter(
    lines: pd.DataFrame, encoded: pd.DataFrame, jitter_sigma: float
) -> tuple[pd.Series, np.ndarray]:
    """Join lines × encoded on text, decode the byte-blob to float32, apply Gaussian jitter
    scaled per-row by the embedding norm. Returns (line_ids in input order, jittered vectors).

    Why jitter: with the encoder-dedup optimization, lines sharing identical text receive the
    SAME embedding vector. Without noise, UMAP maps duplicates to identical (x, y) and the
    scatter plot collapses many cards to a single dot. Tiny noise (sigma ≪ vector norm)
    preserves the topology while spreading duplicates into a tight ball after UMAP.
    """
    merged = lines.merge(encoded, on="text", how="left", validate="many_to_one")
    missing = merged["embedding"].isna().sum()
    if missing:
        # Should never happen: encoder runs over the full unique-text set. Loud failure beats
        # silent NaN downstream.
        raise RuntimeError(
            f"{missing} oracle lines have no matching encoded text — encoder cache out of "
            f"sync with OracleLines. Re-run EmbedOracleText."
        )

    vectors = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in merged["embedding"]]
    ).astype(np.float32)

    if jitter_sigma > 0:
        rng = np.random.default_rng(_RANDOM_STATE)
        # Per-row norm so jitter scales with the embedding magnitude — for normalized embeddings
        # (||v|| = 1) this is just `jitter_sigma`, but the formulation generalizes if upstream
        # ever stops normalizing.
        norms = np.linalg.norm(vectors, axis=1, keepdims=True)
        noise = rng.normal(0.0, jitter_sigma, vectors.shape).astype(np.float32) * norms
        vectors = vectors + noise

    return merged["line_id"], vectors


def _reduce_to_2d_impl(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict
) -> pd.DataFrame:
    n_neighbors = int(config["Umap2DNNeighbors"])
    min_dist = float(config["Umap2DMinDist"])
    jitter_sigma = float(config["UmapJitterSigma"])

    line_ids, vectors = _broadcast_and_jitter(lines, encoded, jitter_sigma)
    logger.info(
        "Input: %d lines × %d unique texts → %d vectors of dim %d (jitter_sigma=%g)",
        len(lines), len(encoded), *vectors.shape, jitter_sigma,
    )

    reducer, backend = _make_umap_reducer(
        n_components=2, n_neighbors=n_neighbors, min_dist=min_dist
    )
    logger.info(
        "Running UMAP via %s (n_components=2, n_neighbors=%d, min_dist=%g, %s)...",
        backend, n_neighbors, min_dist, _METRIC,
    )
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
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict
) -> pd.DataFrame:
    return _reduce_to_2d_impl(lines, encoded, config)
