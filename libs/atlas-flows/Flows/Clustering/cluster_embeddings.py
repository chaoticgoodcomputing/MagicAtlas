"""HDBSCAN clustering over the 5D-UMAP-reduced embeddings.

Input:  DataFrame [point_id, vector] — 5D byte-packed embeddings produced by
        `reduce_to_five_d` (see ClusteringEmbedding.cs).
Output: DataFrame [point_id, cluster_id] — `cluster_id == -1` for HDBSCAN noise.

Pure clustering — UMAP lives in its own step (`reduce_to_five_d`) so the reduction can be reused
by the ModelEvaluations flow and re-tuned independently of HDBSCAN parameters.

Uses RAPIDS cuML's HDBSCAN on GPU when available, falling back to hdbscan on CPU otherwise.
cuML's HDBSCAN only supports euclidean metric, which is what we want post-UMAP anyway. Both
backends produce comparable cluster topologies; cluster IDs are not stable across backend swaps
(HDBSCAN doesn't guarantee stable cluster numbering even between runs of the same backend).
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _make_hdbscan_clusterer():
    try:
        from cuml.cluster import HDBSCAN as CumlHDBSCAN

        return (
            CumlHDBSCAN(
                min_cluster_size=30,
                min_samples=5,
                metric="euclidean",
                cluster_selection_method="eom",
            ),
            "cuml",
        )
    except ImportError:
        import hdbscan

        return (
            hdbscan.HDBSCAN(
                min_cluster_size=30,
                min_samples=5,
                metric="euclidean",
                cluster_selection_method="eom",
            ),
            "hdbscan",
        )


def _cluster_impl(embeddings: pd.DataFrame) -> pd.DataFrame:
    # Unpack the 5D byte-blob vectors (see ClusteringEmbedding.cs). 20 bytes per row = 5
    # little-endian float32s.
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in embeddings["vector"]]
    )
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    clusterer, backend = _make_hdbscan_clusterer()
    logger.info(
        "HDBSCAN via %s (min_cluster_size=30, min_samples=5, euclidean)...",
        backend,
    )
    cluster_ids = clusterer.fit_predict(vectors)
    if hasattr(cluster_ids, "get"):
        cluster_ids = cluster_ids.get()
    cluster_ids = np.asarray(cluster_ids).astype(int)

    n_clusters = int(cluster_ids.max()) + 1 if cluster_ids.max() >= 0 else 0
    n_noise = int((cluster_ids == -1).sum())
    logger.info(
        "Found %d clusters; %d points (%.1f%%) classified as noise",
        n_clusters,
        n_noise,
        100 * n_noise / len(cluster_ids) if len(cluster_ids) else 0,
    )

    return pd.DataFrame({
        "point_id": embeddings["point_id"],
        "cluster_id": cluster_ids,
    })


@step(inputs=["ClusteringEmbeddings"], outputs="ClusterAssignments")
def cluster_embeddings(embeddings: pd.DataFrame) -> pd.DataFrame:
    return _cluster_impl(embeddings)


@step(inputs=["FineTunedClusteringEmbeddings"], outputs="FineTunedClusterAssignments")
def cluster_embeddings_finetuned(embeddings: pd.DataFrame) -> pd.DataFrame:
    return _cluster_impl(embeddings)
