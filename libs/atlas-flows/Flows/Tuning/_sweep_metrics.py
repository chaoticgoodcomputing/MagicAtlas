"""Shared metric helpers for the UMAP hyperparameter sweep steps. Lightweight versions of the
metrics in evaluate_projection_quality.py — same definitions, but kept here to avoid pulling
the full eval module's imports into the sweep loop.
"""
from __future__ import annotations

import numpy as np
import pandas as pd
from scipy.spatial.distance import cdist
from sklearn.manifold import trustworthiness as sk_trustworthiness
from sklearn.neighbors import NearestNeighbors

_RNG_SEED = 42


def centroid_silhouette(vectors: np.ndarray, labels: np.ndarray) -> float:
    """Per-point (b − a) / max(a, b) using euclidean distance to canonical centroids."""
    uniq = np.unique(labels)
    if len(uniq) < 2:
        return 0.0
    label_to_idx = {l: i for i, l in enumerate(uniq)}
    own = np.array([label_to_idx[l] for l in labels], dtype=np.int64)
    centroids = np.stack(
        [vectors[labels == l].mean(axis=0) for l in uniq]
    ).astype(np.float32)
    dists = cdist(vectors, centroids, metric="euclidean")
    rows = np.arange(len(vectors))
    a = dists[rows, own]
    dists_other = dists.copy()
    dists_other[rows, own] = np.inf
    b = dists_other.min(axis=1)
    denom = np.maximum(a, b)
    denom = np.where(denom == 0, 1.0, denom)
    return float(((b - a) / denom).mean())


def knn_purity(vectors: np.ndarray, labels: np.ndarray, k: int) -> float:
    """Mean fraction of a point's k nearest neighbors that share its label."""
    nn = NearestNeighbors(n_neighbors=k + 1, metric="euclidean", n_jobs=-1).fit(vectors)
    _, idx = nn.kneighbors(vectors)
    neighbor_labels = labels[idx[:, 1:]]
    matches = (neighbor_labels == labels[:, None]).mean(axis=1)
    return float(matches.mean())


def trustworthiness_continuity(
    X_hd: np.ndarray, X_proj: np.ndarray, k: int, sample_n: int,
) -> tuple[float, float]:
    """sklearn.manifold.trustworthiness on a stratified-random sample (cap N×N RAM)."""
    n = len(X_hd)
    if n > sample_n:
        rng = np.random.default_rng(_RNG_SEED)
        idx = rng.choice(n, size=sample_n, replace=False)
        X_hd = X_hd[idx]
        X_proj = X_proj[idx]
    trust = sk_trustworthiness(X_hd, X_proj, n_neighbors=k, metric="euclidean")
    cont = sk_trustworthiness(X_proj, X_hd, n_neighbors=k, metric="euclidean")
    return float(trust), float(cont)
