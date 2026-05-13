"""HDBSCAN clustering over a moderately-reduced UMAP projection of the BERT vectors.

Input:  DataFrame [point_id, card_id, text_type, embedding] — full-dim sentence-transformer output
        with each row's vector packed as a little-endian byte blob (see BertEmbedding.cs).
Output: DataFrame [point_id, cluster_id] — `cluster_id == -1` for HDBSCAN noise.

Two-stage reduction is the standard recipe for this stack:
  1. UMAP to ~5 dims preserves enough semantic structure for density-based clustering while
     killing the curse-of-dimensionality that hurts HDBSCAN on raw 384-dim vectors.
  2. HDBSCAN on the 5D output — variable-shape clusters, noise bucket, no fixed-k needed.
Display is handled separately by `Flows.OracleEmbedding.reduce_to_2d`, which runs its own UMAP
to 2D off the same `BertEmbeddings` input.

Uses RAPIDS cuML's UMAP + HDBSCAN on GPU when available, falling back to umap-learn + hdbscan on
CPU otherwise. cuML's HDBSCAN only supports euclidean metric, which is what we want post-UMAP
anyway. Both backends produce comparable cluster topologies; cluster IDs are not stable across
backend swaps (HDBSCAN doesn't guarantee stable cluster numbering even between runs of the same
backend).
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _make_umap_reducer():
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


@step(inputs=["BertEmbeddings"], outputs="ClusterAssignments")
def cluster_embeddings(embeddings: pd.DataFrame) -> pd.DataFrame:
    # Unpack the byte-blob embeddings (see BertEmbedding.cs). Each row is 384
    # little-endian float32s = 1,536 bytes.
    vectors = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in embeddings["embedding"]]
    )
    logger.info("Input: %d vectors of dim %d", *vectors.shape)

    reducer, umap_backend = _make_umap_reducer()
    logger.info(
        "UMAP -> 5D for clustering via %s (n_neighbors=15, min_dist=0.0, cosine)...",
        umap_backend,
    )
    reduced = reducer.fit_transform(vectors)
    if hasattr(reduced, "get"):
        reduced = reduced.get()
    reduced = np.asarray(reduced)
    logger.info("Reduced shape: %s", reduced.shape)

    clusterer, hdb_backend = _make_hdbscan_clusterer()
    logger.info(
        "HDBSCAN via %s (min_cluster_size=30, min_samples=5, euclidean)...",
        hdb_backend,
    )
    cluster_ids = clusterer.fit_predict(reduced)
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
