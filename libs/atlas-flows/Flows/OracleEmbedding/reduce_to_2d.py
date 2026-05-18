"""UMAP-reduce the default-variant BERT vectors to 2D for the atlas display. The fine-tuned
variant lives in `reduce_to_2d_finetuned.py`; both files share `_reduce_to_2d_impl` via import.
Split for Flowthru's Python source generator, which only registers the first @step per .py
file in 0.18.2.

Inputs:
    embeddings: DataFrame [point_id, card_id, text_type, embedding] — embedding is the byte-blob
                form (see BertEmbedding.cs). Decoded here back to float32 vectors.
    config:     OracleEmbeddingConfig record — uses `Umap2DNNeighbors` and `Umap2DMinDist`.

Output: DataFrame [point_id, card_id, x, y, text_type] — one row per fragment.

A sibling step (`Flows.Clustering.reduce_to_five_d`) runs its own UMAP at a higher
target-dimensionality on the same input for HDBSCAN; this 2D reduction is purely for the scatter.

Uses RAPIDS cuML's UMAP on GPU when available, falling back to umap-learn on CPU otherwise. The
two implementations don't produce identical coordinates (cuML uses a different initialization
strategy under the hood), but the topology is equivalent.
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


def _reduce_to_2d_impl(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    n_neighbors = int(config["Umap2DNNeighbors"])
    min_dist = float(config["Umap2DMinDist"])

    # Embeddings packed as float16 — cast to float32 for UMAP compatibility. Each row is
    # `dim` little-endian float16s, where dim depends on the source model (384 for MiniLM,
    # 768 for mpnet).
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in embeddings["embedding"]]
    ).astype(np.float32)
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    reducer, backend = _make_umap_reducer(
        n_components=2, n_neighbors=n_neighbors, min_dist=min_dist
    )
    logger.info(
        "Running UMAP via %s (n_components=2, n_neighbors=%d, min_dist=%g, %s)...",
        backend, n_neighbors, min_dist, _METRIC,
    )
    coords = reducer.fit_transform(vectors)
    # cuML returns a cupy array; normalize to numpy for downstream pandas/json.
    if hasattr(coords, "get"):
        coords = coords.get()
    coords = np.asarray(coords)
    logger.info("UMAP output shape: %s", coords.shape)

    return pd.DataFrame({
        "point_id": embeddings["point_id"],
        "card_id": embeddings["card_id"],
        "x": coords[:, 0].astype(float),
        "y": coords[:, 1].astype(float),
        "text_type": embeddings["text_type"],
    })


@step(
    inputs=["BertEmbeddings", "OracleEmbeddingConfig"],
    outputs="AtlasPoints",
    cacheable=True,
)
def reduce_to_2d(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _reduce_to_2d_impl(embeddings, config)
