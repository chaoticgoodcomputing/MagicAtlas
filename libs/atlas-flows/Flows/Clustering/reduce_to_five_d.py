"""UMAP-reduce the per-fragment BERT vectors to 5D for HDBSCAN clustering and model evaluation.

Input:  DataFrame [point_id, card_id, text_type, embedding] — `embedding` is the byte-blob form
        produced by `embed_oracle_text` (1,536 bytes = 384 little-endian float32s).
Output: DataFrame [point_id, vector] — `vector` is the byte-blob form of the 5D UMAP coordinates
        (20 bytes = 5 little-endian float32s), see ClusteringEmbedding.cs.

Hoisted out of `cluster_embeddings.py` so the (slow) UMAP doesn't re-run every time clustering
parameters or the eval suite change, and so ModelEvaluations can read the same 5D coordinates the
clusterer saw without redundant work.

Uses RAPIDS cuML's UMAP on GPU when available, falling back to umap-learn on CPU otherwise. The
two implementations don't produce identical coordinates (different initialization under the hood),
but the topology is equivalent and downstream HDBSCAN parameters are stable across them.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _make_umap_reducer():
    """Returns (reducer, backend_name). Prefers cuML; falls back to umap-learn."""
    try:
        from cuml.manifold import UMAP as CumlUMAP

        return (
            CumlUMAP(
                n_components=5,
                n_neighbors=15,
                # BERTopic recommendation for clustering-target UMAPs — tighter local structure
                # helps HDBSCAN separate dense regions.
                min_dist=0.0,
                metric="cosine",
                random_state=42,
            ),
            "cuml",
        )
    except ImportError:
        import umap

        return (
            umap.UMAP(
                n_components=5,
                n_neighbors=15,
                min_dist=0.0,
                metric="cosine",
                random_state=42,
            ),
            "umap-learn",
        )


def _reduce_to_five_d_impl(embeddings: pd.DataFrame) -> pd.DataFrame:
    # Embeddings are packed as little-endian float16 (2 bytes/elem) — see embed_oracle_text.py.
    # Cast to float32 for UMAP (cuML's UMAP doesn't accept float16 directly).
    # Unpack the byte-blob embeddings (see BertEmbedding.cs). Each row is 384
    # little-endian float32s = 1,536 bytes.
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in embeddings["embedding"]]
    ).astype(np.float32)
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    reducer, backend = _make_umap_reducer()
    logger.info(
        "UMAP -> 5D via %s (n_neighbors=15, min_dist=0.0, cosine)...",
        backend,
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


@step(inputs=["BertEmbeddings"], outputs="ClusteringEmbeddings")
def reduce_to_five_d(embeddings: pd.DataFrame) -> pd.DataFrame:
    return _reduce_to_five_d_impl(embeddings)


@step(inputs=["FineTunedBertEmbeddings"], outputs="FineTunedClusteringEmbeddings")
def reduce_to_five_d_finetuned(embeddings: pd.DataFrame) -> pd.DataFrame:
    return _reduce_to_five_d_impl(embeddings)
