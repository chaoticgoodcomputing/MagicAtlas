"""Sweep step for the 5D→2D UMAP. Inputs the (fixed) ClusteringEmbeddings and runs an
unsupervised cuML UMAP per (n_neighbors, min_dist) combo from the sweep grid, emitting
silhouette / knn_purity / trustworthiness / continuity per combo.

Output: tidy long DataFrame conforming to UmapSweepResult. Use the markdown rendering tooling
or grouped pivots to compare combos.

Runs on GPU via cuML (one matmul per ~30k point set is fast — full grid finishes in seconds).
"""
from __future__ import annotations

import itertools
import logging
import time
import uuid

import numpy as np
import pandas as pd
from flowthru import step

from Flows.Tuning._sweep_metrics import (
    centroid_silhouette,
    knn_purity,
    trustworthiness_continuity,
)

logger = logging.getLogger(__name__)

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


def _make_2d_reducer(n_neighbors: int, min_dist: float):
    try:
        from cuml.manifold import UMAP as CumlUMAP
        return CumlUMAP(
            n_components=2,
            n_neighbors=n_neighbors,
            min_dist=min_dist,
            metric="euclidean",
            random_state=_RANDOM_STATE,
        ), "cuml"
    except ImportError:
        import umap
        return umap.UMAP(
            n_components=2,
            n_neighbors=n_neighbors,
            min_dist=min_dist,
            metric="euclidean",
            random_state=_RANDOM_STATE,
        ), "umap-learn"


def _load_aligned(
    five_d: pd.DataFrame, lines: pd.DataFrame, encoded: pd.DataFrame, primary: pd.DataFrame,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """Returns (5d_vectors, hd_vectors, leaf_labels, parent_labels) row-aligned for attributed
    lines (those with a primary canonical)."""
    five_d = five_d.copy()
    lines = lines.copy()
    primary = primary.copy()
    five_d["line_id"] = five_d["line_id"].map(_normalize_guid)
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    primary["line_id"] = primary["line_id"].map(_normalize_guid)

    merged = (
        primary[["line_id", "canonical_slug", "canonical_family"]]
        .merge(lines[["line_id", "text"]], on="line_id", how="inner", validate="one_to_one")
        .merge(five_d[["line_id", "vector"]], on="line_id", how="inner", validate="one_to_one")
        .merge(encoded[["text", "embedding"]], on="text", how="left", validate="many_to_one")
    )
    if merged["embedding"].isna().any():
        raise RuntimeError(f"{merged['embedding'].isna().sum()} lines missing HD embedding")

    five_d_vec = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in merged["vector"]]
    ).astype(np.float32)
    hd_vec = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in merged["embedding"]]
    ).astype(np.float32)
    leaf = merged["canonical_slug"].to_numpy()
    parent = merged["canonical_family"].to_numpy()
    return five_d_vec, hd_vec, leaf, parent


def _sweep_impl(
    five_d: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    # Defensive: marshaller may surface either snake_case (SerializedLabel-keyed) or PascalCase
    # (C#-property-keyed). Use the schema-canonical snake_case first, fall back to PascalCase.
    def _g(*keys):
        for k in keys:
            if k in config:
                return config[k]
        raise KeyError(keys)
    n_neighbors_grid = list(_g("n_neighbors_grid", "NNeighborsGrid"))
    min_dist_grid = list(_g("min_dist_grid", "MinDistGrid"))
    knn_k = int(_g("knn_k", "KnnK") if "knn_k" in config or "KnnK" in config else 10)
    trust_sample_n = int(_g("trust_sample_n", "TrustSampleN") if "trust_sample_n" in config or "TrustSampleN" in config else 5000)

    five_d_vec, hd_vec, leaf, parent = _load_aligned(five_d, lines, encoded, primary)
    logger.info(
        "Sweep 2D: %d combos over %d aligned lines (5D dim=%d, HD dim=%d)",
        len(n_neighbors_grid) * len(min_dist_grid), len(leaf), five_d_vec.shape[1], hd_vec.shape[1],
    )

    rows: list[dict] = []
    for n_neighbors, min_dist in itertools.product(n_neighbors_grid, min_dist_grid):
        sweep_id = f"n={n_neighbors},d={min_dist}"
        t0 = time.time()
        reducer, backend = _make_2d_reducer(n_neighbors, min_dist)
        coords = reducer.fit_transform(five_d_vec)
        if hasattr(coords, "get"):
            coords = coords.get()
        coords = np.asarray(coords, dtype=np.float32)
        t_umap = time.time() - t0

        # Metrics
        t1 = time.time()
        sil_leaf = centroid_silhouette(coords, leaf)
        sil_parent = centroid_silhouette(coords, parent)
        kp = knn_purity(coords, leaf, knn_k)
        trust, cont = trustworthiness_continuity(hd_vec, coords, knn_k, trust_sample_n)
        t_eval = time.time() - t1

        runtime = t_umap + t_eval
        logger.info(
            "  %s (%s, umap=%.1fs eval=%.1fs): sil_leaf=%+.3f sil_parent=%+.3f kp=%+.3f trust=%+.3f cont=%+.3f",
            sweep_id, backend, t_umap, t_eval, sil_leaf, sil_parent, kp, trust, cont,
        )

        for metric_name, value in [
            ("silhouette_leaf", sil_leaf),
            ("silhouette_parent", sil_parent),
            (f"knn_purity_k{knn_k}", kp),
            (f"trustworthiness_k{knn_k}", trust),
            (f"continuity_k{knn_k}", cont),
        ]:
            rows.append({
                "sweep_id": sweep_id,
                "sweep_type": "2d",
                "n_neighbors": int(n_neighbors),
                "min_dist": float(min_dist),
                "supervision_weight": 0.0,
                "level": "2d",
                "metric": metric_name,
                "value": float(value),
                "runtime_seconds": float(runtime),
            })
    return pd.DataFrame(rows)


@step(
    inputs=[
        "ClusteringEmbeddings",
        "OracleLines",
        "EncodedTexts",
        "LinePrimaryCanonicals",
        "UmapSweep2DConfig",
    ],
    outputs="UmapSweep2DResults",
    cacheable=True,
)
def sweep_umap_2d(
    five_d: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _sweep_impl(five_d, lines, encoded, primary, config)
