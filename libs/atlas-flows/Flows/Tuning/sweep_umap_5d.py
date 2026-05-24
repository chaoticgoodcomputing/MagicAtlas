"""Sweep step for the HD→5D supervised UMAP. For each (n_neighbors, min_dist, supervision_weight)
combo, runs cuML UMAP HD→5D supervised + a fixed-default 5D→2D unsupervised, then computes
silhouette / knn_purity at BOTH the 5D and 2D layers (so we can see how 5D-layer tuning
propagates to the 2D viewer's experience).

cuML supervised UMAP verified working on cuml-cu13 26.4 — runs fully on GPU.

Output: tidy long DataFrame conforming to UmapSweepResult.
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
# Default 2D hyperparams used after each 5D variant — gives the sweep an end-to-end view.
_DEFAULT_2D_N_NEIGHBORS = 15
_DEFAULT_2D_MIN_DIST = 0.1


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


def _make_5d_supervised_reducer(n_neighbors: int, min_dist: float, supervision_weight: float):
    try:
        from cuml.manifold import UMAP as CumlUMAP
        return CumlUMAP(
            n_components=5,
            n_neighbors=n_neighbors,
            min_dist=min_dist,
            metric="cosine",
            random_state=_RANDOM_STATE,
            target_metric="categorical",
            target_weight=supervision_weight,
        ), "cuml"
    except ImportError:
        import umap
        return umap.UMAP(
            n_components=5,
            n_neighbors=n_neighbors,
            min_dist=min_dist,
            metric="cosine",
            random_state=_RANDOM_STATE,
            target_metric="categorical",
            target_weight=supervision_weight,
        ), "umap-learn"


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
    lines: pd.DataFrame, encoded: pd.DataFrame, primary: pd.DataFrame,
) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
    """Returns (hd_vectors, y_int_labels, leaf_str_labels, parent_str_labels). Includes ALL
    lines (not just attributed) so UMAP sees the full distribution; y=-1 for unlabeled per
    UMAP convention."""
    lines = lines.copy()
    primary = primary.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    primary["line_id"] = primary["line_id"].map(_normalize_guid)

    merged = lines[["line_id", "text"]].merge(
        encoded[["text", "embedding"]], on="text", how="left", validate="many_to_one",
    )
    if merged["embedding"].isna().any():
        raise RuntimeError(f"{merged['embedding'].isna().sum()} lines missing HD embedding")

    hd = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in merged["embedding"]]
    ).astype(np.float32)

    # Build y label array aligned with merged row order; -1 for unlabeled
    slug_by_line = dict(zip(primary["line_id"], primary["canonical_slug"]))
    family_by_line = dict(zip(primary["line_id"], primary["canonical_family"]))
    seen: dict[str, int] = {}
    y = np.full(len(merged), -1, dtype=np.int32)
    leaf = np.empty(len(merged), dtype=object)
    parent = np.empty(len(merged), dtype=object)
    for i, lid in enumerate(merged["line_id"]):
        slug = slug_by_line.get(lid)
        leaf[i] = slug or ""
        parent[i] = family_by_line.get(lid) or ""
        if slug is None or (isinstance(slug, float) and pd.isna(slug)):
            continue
        slug = str(slug)
        if slug not in seen:
            seen[slug] = len(seen)
        y[i] = seen[slug]
    return hd, y, leaf, parent


def _sweep_impl(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    def _g(*keys):
        for k in keys:
            if k in config:
                return config[k]
        raise KeyError(keys)
    n_neighbors_grid = list(_g("n_neighbors_grid", "NNeighborsGrid"))
    min_dist_grid = list(_g("min_dist_grid", "MinDistGrid"))
    supervision_weight_grid = list(_g("supervision_weight_grid", "SupervisionWeightGrid"))
    knn_k = int(_g("knn_k", "KnnK") if "knn_k" in config or "KnnK" in config else 10)
    trust_sample_n = int(_g("trust_sample_n", "TrustSampleN") if "trust_sample_n" in config or "TrustSampleN" in config else 5000)

    hd_vec, y, leaf_all, parent_all = _load_aligned(lines, encoded, primary)
    labeled_mask = y != -1
    n_total = len(hd_vec)
    n_labeled = int(labeled_mask.sum())
    n_classes = int(y[labeled_mask].max() + 1) if n_labeled > 0 else 0

    n_combos = len(n_neighbors_grid) * len(min_dist_grid) * len(supervision_weight_grid)
    logger.info(
        "Sweep 5D: %d combos. %d total lines (%d labeled across %d canonicals). HD dim=%d.",
        n_combos, n_total, n_labeled, n_classes, hd_vec.shape[1],
    )

    # Restrict eval labels to attributed lines (metrics need real labels).
    leaf_labeled = leaf_all[labeled_mask]
    parent_labeled = parent_all[labeled_mask]
    hd_labeled = hd_vec[labeled_mask]

    rows: list[dict] = []
    for n_neighbors, min_dist, supervision_weight in itertools.product(
        n_neighbors_grid, min_dist_grid, supervision_weight_grid,
    ):
        sweep_id = f"n={n_neighbors},d={min_dist},sw={supervision_weight}"

        # HD → 5D supervised
        t0 = time.time()
        reducer_5d, backend_5d = _make_5d_supervised_reducer(n_neighbors, min_dist, supervision_weight)
        coords_5d = reducer_5d.fit_transform(hd_vec, y=y)
        if hasattr(coords_5d, "get"):
            coords_5d = coords_5d.get()
        coords_5d = np.asarray(coords_5d, dtype=np.float32)
        t_5d = time.time() - t0

        # 5D → 2D unsupervised (default 2D params)
        t1 = time.time()
        reducer_2d, _ = _make_2d_reducer(_DEFAULT_2D_N_NEIGHBORS, _DEFAULT_2D_MIN_DIST)
        coords_2d = reducer_2d.fit_transform(coords_5d)
        if hasattr(coords_2d, "get"):
            coords_2d = coords_2d.get()
        coords_2d = np.asarray(coords_2d, dtype=np.float32)
        t_2d = time.time() - t1

        # Metrics — labeled-only subsets
        t2 = time.time()
        coords_5d_lab = coords_5d[labeled_mask]
        coords_2d_lab = coords_2d[labeled_mask]
        m5_sil_leaf = centroid_silhouette(coords_5d_lab, leaf_labeled)
        m5_sil_parent = centroid_silhouette(coords_5d_lab, parent_labeled)
        m5_kp = knn_purity(coords_5d_lab, leaf_labeled, knn_k)
        m2_sil_leaf = centroid_silhouette(coords_2d_lab, leaf_labeled)
        m2_sil_parent = centroid_silhouette(coords_2d_lab, parent_labeled)
        m2_kp = knn_purity(coords_2d_lab, leaf_labeled, knn_k)
        trust, cont = trustworthiness_continuity(hd_labeled, coords_2d_lab, knn_k, trust_sample_n)
        t_eval = time.time() - t2

        runtime = t_5d + t_2d + t_eval
        logger.info(
            "  %s (%s, 5d=%.1fs 2d=%.1fs eval=%.1fs):",
            sweep_id, backend_5d, t_5d, t_2d, t_eval,
        )
        logger.info(
            "    5d: sil_leaf=%+.3f sil_parent=%+.3f kp=%+.3f",
            m5_sil_leaf, m5_sil_parent, m5_kp,
        )
        logger.info(
            "    2d: sil_leaf=%+.3f sil_parent=%+.3f kp=%+.3f trust=%+.3f cont=%+.3f",
            m2_sil_leaf, m2_sil_parent, m2_kp, trust, cont,
        )

        common = dict(
            sweep_id=sweep_id,
            sweep_type="5d",
            n_neighbors=int(n_neighbors),
            min_dist=float(min_dist),
            supervision_weight=float(supervision_weight),
            runtime_seconds=float(runtime),
        )
        for level, metric_name, value in [
            ("5d", "silhouette_leaf", m5_sil_leaf),
            ("5d", "silhouette_parent", m5_sil_parent),
            ("5d", f"knn_purity_k{knn_k}", m5_kp),
            ("2d", "silhouette_leaf", m2_sil_leaf),
            ("2d", "silhouette_parent", m2_sil_parent),
            ("2d", f"knn_purity_k{knn_k}", m2_kp),
            ("2d", f"trustworthiness_k{knn_k}", trust),
            ("2d", f"continuity_k{knn_k}", cont),
        ]:
            rows.append({**common, "level": level, "metric": metric_name, "value": float(value)})

    return pd.DataFrame(rows)


@step(
    inputs=[
        "OracleLines",
        "EncodedTexts",
        "LinePrimaryCanonicals",
        "UmapSweep5DConfig",
    ],
    outputs="UmapSweep5DResults",
    cacheable=True,
)
def sweep_umap_5d(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _sweep_impl(lines, encoded, primary, config)
