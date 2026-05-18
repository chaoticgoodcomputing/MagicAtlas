"""HDBSCAN clustering over the 5D-UMAP-reduced embeddings.

Inputs:
    embeddings: DataFrame [point_id, vector] — 5D byte-packed embeddings produced by
                `reduce_to_five_d` (see ClusteringEmbedding.cs).
    config:     ClusteringConfig record — uses `Hdbscan.MinClusterSize` and `Hdbscan.MinSamples`.

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

# euclidean / eom aren't tuning knobs — they're algorithm-fixed by cuML's HDBSCAN support and
# the BERTopic-style methodology. Surfacing them to config would invite invalid combinations.
_METRIC = "euclidean"
_CLUSTER_SELECTION_METHOD = "eom"


def _make_hdbscan_clusterer(min_cluster_size: int, min_samples: int):
    try:
        from cuml.cluster import HDBSCAN as CumlHDBSCAN

        return (
            CumlHDBSCAN(
                min_cluster_size=min_cluster_size,
                min_samples=min_samples,
                metric=_METRIC,
                cluster_selection_method=_CLUSTER_SELECTION_METHOD,
            ),
            "cuml",
        )
    except ImportError:
        import hdbscan

        return (
            hdbscan.HDBSCAN(
                min_cluster_size=min_cluster_size,
                min_samples=min_samples,
                metric=_METRIC,
                cluster_selection_method=_CLUSTER_SELECTION_METHOD,
            ),
            "hdbscan",
        )


def _cluster_impl(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    min_cluster_size = int(config["HdbscanMinClusterSize"])
    min_samples = int(config["HdbscanMinSamples"])

    # Unpack the 5D byte-blob vectors (see ClusteringEmbedding.cs). 20 bytes per row = 5
    # little-endian float32s.
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in embeddings["vector"]]
    )
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    clusterer, backend = _make_hdbscan_clusterer(
        min_cluster_size=min_cluster_size, min_samples=min_samples
    )
    logger.info(
        "HDBSCAN via %s (min_cluster_size=%d, min_samples=%d, %s)...",
        backend, min_cluster_size, min_samples, _METRIC,
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


@step(inputs=["ClusteringEmbeddings", "ClusteringConfig"], outputs="ClusterAssignments")
def cluster_embeddings(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _cluster_impl(embeddings, config)


@step(
    inputs=["FineTunedClusteringEmbeddings", "ClusteringConfig"],
    outputs="FineTunedClusterAssignments",
)
def cluster_embeddings_finetuned(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _cluster_impl(embeddings, config)
