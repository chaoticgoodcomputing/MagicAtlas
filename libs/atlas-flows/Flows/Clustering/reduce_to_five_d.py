"""UMAP-reduce the encoded oracle texts to 5D for HDBSCAN clustering and model evaluation.
The fine-tuned variant lives in `reduce_to_five_d_finetuned.py`; both files share
`_reduce_to_five_d_impl` via import.

Inputs:
    lines:    DataFrame of OracleLine rows [line_id, card_id, text].
    encoded:  DataFrame of EncodedText rows [text, embedding] — the encoder cache.
    config:   ClusteringConfig record — uses `Umap5DNNeighbors`, `Umap5DMinDist`, and
              `UmapJitterSigma`.

Output: DataFrame [line_id, vector] — `vector` is the byte-blob form of the 5D UMAP coordinates
        (20 bytes = 5 little-endian float32s), see ClusteringEmbedding.cs.

Hoisted out of `cluster_embeddings.py` so the (slow) UMAP doesn't re-run every time clustering
parameters or the eval suite change, and so ModelEvaluations can read the same 5D coordinates the
clusterer saw without redundant work.

Joins lines × encoded on `text` and applies pre-UMAP Gaussian jitter (same rationale as
`reduce_to_2d.py`).

BERTopic-style note on min_dist: the recommended 0.0 (vs. 0.1 for the 2D atlas-display
reduction) produces tighter local structure that helps HDBSCAN separate dense regions.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

from Flows.OracleEmbedding.reduce_to_2d import _broadcast_and_jitter

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


def _reduce_to_five_d_impl(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict
) -> pd.DataFrame:
    n_neighbors = int(config["Umap5DNNeighbors"])
    min_dist = float(config["Umap5DMinDist"])
    jitter_sigma = float(config["UmapJitterSigma"])

    line_ids, vectors = _broadcast_and_jitter(lines, encoded, jitter_sigma)
    logger.info(
        "Input: %d lines × %d unique texts → %d vectors of dim %d (jitter_sigma=%g)",
        len(lines), len(encoded), *vectors.shape, jitter_sigma,
    )

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
    # rationale as EncodedText.Embedding — Flowthru's parquet serializer needs IFlatSchema,
    # and byte[] is the only flat-classified array form.
    blobs = [vec.astype("<f4").tobytes() for vec in reduced]
    return pd.DataFrame({
        "line_id": line_ids.values,
        "vector": blobs,
    })


@step(
    inputs=["OracleLines", "EncodedTexts", "ClusteringConfig"],
    outputs="ClusteringEmbeddings",
    cacheable=True,
)
def reduce_to_five_d(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict
) -> pd.DataFrame:
    return _reduce_to_five_d_impl(lines, encoded, config)
