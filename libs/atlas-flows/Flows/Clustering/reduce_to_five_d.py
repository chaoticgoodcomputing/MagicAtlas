"""UMAP-reduce the per-fragment BERT vectors to 5D for HDBSCAN clustering and model evaluation.

Inputs:
    embeddings: DataFrame [point_id, card_id, text_type, embedding] — `embedding` is the
                byte-blob form produced by `embed_oracle_text` (little-endian float16, 2 bytes
                per element; row width depends on the source model).
    config:     ClusteringConfig record — uses `Umap5D.NNeighbors` and `Umap5D.MinDist`.

Output: DataFrame [point_id, vector] — `vector` is the byte-blob form of the 5D UMAP coordinates
        (20 bytes = 5 little-endian float32s), see ClusteringEmbedding.cs.

Hoisted out of `cluster_embeddings.py` so the (slow) UMAP doesn't re-run every time clustering
parameters or the eval suite change, and so ModelEvaluations can read the same 5D coordinates the
clusterer saw without redundant work.

Uses RAPIDS cuML's UMAP on GPU when available, falling back to umap-learn on CPU otherwise. The
two implementations don't produce identical coordinates (different initialization under the hood),
but the topology is equivalent and downstream HDBSCAN parameters are stable across them.

BERTopic-style note on min_dist: the recommended 0.0 (vs. 0.1 for the 2D atlas-display reduction)
produces tighter local structure that helps HDBSCAN separate dense regions.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_N_COMPONENTS = 5
_METRIC = "cosine"
_RANDOM_STATE = 42


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


def _reduce_to_five_d_impl(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    n_neighbors = int(config["Umap5DNNeighbors"])
    min_dist = float(config["Umap5DMinDist"])

    # Embeddings are packed as little-endian float16 (2 bytes/elem) — see embed_oracle_text.py.
    # Cast to float32 for UMAP (cuML's UMAP doesn't accept float16 directly).
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in embeddings["embedding"]]
    ).astype(np.float32)
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    reducer, backend = _make_umap_reducer(n_neighbors=n_neighbors, min_dist=min_dist)
    logger.info(
        "UMAP -> 5D via %s (n_neighbors=%d, min_dist=%g, %s)...",
        backend, n_neighbors, min_dist, _METRIC,
    )
    reduced = reducer.fit_transform(vectors)
    if hasattr(reduced, "get"):
        reduced = reduced.get()
    reduced = np.asarray(reduced, dtype=np.float32)
    logger.info("Reduced shape: %s (dtype %s)", reduced.shape, reduced.dtype)

    # Pack each row's 5 float32s into a little-endian byte blob (20 bytes per row). Same
    # rationale as BertEmbedding.Embedding — Flowthru's parquet serializer needs IFlatSchema,
    # and byte[] is the only flat-classified array form.
    blobs = [vec.astype("<f4").tobytes() for vec in reduced]
    return pd.DataFrame({
        "point_id": embeddings["point_id"],
        "vector": blobs,
    })


@step(inputs=["BertEmbeddings", "ClusteringConfig"], outputs="ClusteringEmbeddings")
def reduce_to_five_d(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _reduce_to_five_d_impl(embeddings, config)


@step(
    inputs=["FineTunedBertEmbeddings", "ClusteringConfig"],
    outputs="FineTunedClusteringEmbeddings",
)
def reduce_to_five_d_finetuned(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _reduce_to_five_d_impl(embeddings, config)
