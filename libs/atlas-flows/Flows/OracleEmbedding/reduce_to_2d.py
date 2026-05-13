"""UMAP-reduce the per-fragment BERT vectors to 2D for the atlas display.

Input:  DataFrame [point_id, card_id, text_type, embedding] — embedding is the byte-blob form
        (see BertEmbedding.cs). Decoded here back to float32 vectors.
Output: DataFrame [point_id, card_id, x, y, text_type] — one row per fragment.

A sibling step (`Flows.Clustering.cluster_embeddings`) runs its own UMAP at a higher
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


def _make_umap_reducer(n_components: int):
    """Returns (reducer, backend_name). Prefers cuML; falls back to umap-learn."""
    try:
        from cuml.manifold import UMAP as CumlUMAP

        return (
            CumlUMAP(
                n_components=n_components,
                n_neighbors=15,
                min_dist=0.1,
                metric="cosine",
                random_state=42,
            ),
            "cuml",
        )
    except ImportError:
        import umap

        return (
            umap.UMAP(
                n_components=n_components,
                n_neighbors=15,
                min_dist=0.1,
                metric="cosine",
                random_state=42,
            ),
            "umap-learn",
        )


@step(inputs=["BertEmbeddings"], outputs="AtlasPoints")
def reduce_to_2d(embeddings: pd.DataFrame) -> pd.DataFrame:
    # Unpack the byte-blob embeddings (see BertEmbedding.cs remarks). Each row is 384
    # little-endian float32s = 1,536 bytes.
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in embeddings["embedding"]]
    )
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    reducer, backend = _make_umap_reducer(n_components=2)
    logger.info(
        "Running UMAP via %s (n_components=2, n_neighbors=15, min_dist=0.1, cosine)...",
        backend,
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
