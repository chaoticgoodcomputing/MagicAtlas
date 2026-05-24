"""Cross-level projection-quality scorecard.

Compares the same set of attributed lines as they appear at three levels of dimensionality:
HD (raw nomic encoder output, 768d), 5D (clustering UMAP), 2D (atlas UMAP). Reports both
**Exploration** camp metrics (does the projection reveal canonical structure?) and
**Exploitation** camp metrics (does the projection preserve HD's local neighborhoods?).

HD acts as the achievable ceiling — most camp-2 metrics are defined as preservation-relative-to-HD
and so trivially read 1.0 at HD; that's by design.

Inputs:
    lines:    OracleLines — line_id, card_id, text (broadcasts HD embeddings line-by-line).
    encoded:  EncodedTexts — text, embedding (float16 bytes).
    five_d:   ClusteringEmbeddings — line_id, 5d vector (float32 bytes).
    two_d:    AtlasPoints — line_id, x, y.
    primary:  LinePrimaryCanonicals — line_id, canonical_slug, canonical_family.

Output: ProjectionQualityMetric rows (level × camp × metric × value).
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step
from scipy.spatial.distance import cdist
from scipy.stats import spearmanr
from sklearn.cluster import MiniBatchKMeans
from sklearn.manifold import trustworthiness as sk_trustworthiness
from sklearn.metrics import normalized_mutual_info_score
from sklearn.neighbors import NearestNeighbors

logger = logging.getLogger(__name__)

_K = 10
# sklearn.manifold.trustworthiness materializes an N×N distance matrix. Cap N to keep RAM bounded
# while still giving a reliable estimate (sampling error << inter-level differences we care about).
_TRUST_SAMPLE = 5000
_NMI_KMEANS_BATCH = 2048
_RNG_SEED = 42


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


def _decode_bytes(byte_col, dtype: str) -> np.ndarray:
    """Stack a column of fixed-width byte blobs into (N, dim) float32."""
    return np.stack(
        [np.frombuffer(b, dtype=dtype) for b in byte_col]
    ).astype(np.float32)


def _load_aligned(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    five_d: pd.DataFrame,
    two_d: pd.DataFrame,
    primary: pd.DataFrame,
) -> tuple[pd.DataFrame, np.ndarray, np.ndarray, np.ndarray]:
    """Join all inputs on line_id, returning (meta_df, hd, fived, twod) all in the same row order.

    Filters to lines with a canonical assignment — same scope as the existing per-canonical
    scorecard so the two reports describe the same population.
    """
    lines = lines.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    five_d = five_d.copy()
    five_d["line_id"] = five_d["line_id"].map(_normalize_guid)
    two_d = two_d.copy()
    two_d["line_id"] = two_d["line_id"].map(_normalize_guid)
    primary = primary.copy()
    primary["line_id"] = primary["line_id"].map(_normalize_guid)

    merged = (
        lines[["line_id", "card_id", "text"]]
        .merge(
            primary[["line_id", "canonical_slug", "canonical_family"]],
            on="line_id", how="inner",
        )
        .merge(two_d[["line_id", "x", "y"]], on="line_id", how="inner",
               validate="one_to_one")
        .merge(five_d[["line_id", "vector"]], on="line_id", how="inner",
               validate="one_to_one")
        .merge(encoded[["text", "embedding"]], on="text", how="left",
               validate="many_to_one")
    )
    if merged["embedding"].isna().any():
        n_missing = int(merged["embedding"].isna().sum())
        raise RuntimeError(
            f"{n_missing} attributed lines have no HD embedding — encoder cache out of sync"
        )

    hd = _decode_bytes(merged["embedding"], "<f2")
    fived = _decode_bytes(merged["vector"], "<f4")
    twod = merged[["x", "y"]].to_numpy(dtype=np.float32)

    meta = merged[["line_id", "card_id", "canonical_slug", "canonical_family"]].reset_index(drop=True)
    logger.info(
        "Aligned %d attributed lines × %d canonicals × %d families. Dims: HD=%d, 5D=%d, 2D=%d",
        len(meta), meta["canonical_slug"].nunique(), meta["canonical_family"].nunique(),
        hd.shape[1], fived.shape[1], twod.shape[1],
    )
    return meta, hd, fived, twod


def _centroid_silhouette(vectors: np.ndarray, labels: np.ndarray) -> float:
    """Per-point (b - a) / max(a, b), averaged. b = distance to nearest *other* centroid.
    Uses euclidean across all levels for cross-level comparability — with normalized HD
    embeddings, euclidean and cosine are monotonically related so silhouette signs/ranks agree.
    """
    uniq = np.unique(labels)
    if len(uniq) < 2:
        return 0.0
    label_to_idx = {l: i for i, l in enumerate(uniq)}
    own = np.array([label_to_idx[l] for l in labels], dtype=np.int64)
    centroids = np.stack(
        [vectors[labels == l].mean(axis=0) for l in uniq]
    ).astype(np.float32)

    # cdist is C-implemented; output is (N, K) which is bounded by ~30k × 347 × 4B ≈ 40MB.
    dists = cdist(vectors, centroids, metric="euclidean")
    rows = np.arange(len(vectors))
    a = dists[rows, own]
    dists_other = dists.copy()
    dists_other[rows, own] = np.inf
    b = dists_other.min(axis=1)
    denom = np.maximum(a, b)
    denom = np.where(denom == 0, 1.0, denom)
    return float(((b - a) / denom).mean())


def _pairwise_centroid_spearman(
    vectors: np.ndarray, labels: np.ndarray, hd_centroid_distances: np.ndarray | None,
) -> tuple[float, np.ndarray]:
    """Spearman correlation of canonical-pair centroid distances vs HD. Pass `hd_centroid_distances=None`
    for HD itself (returns 1.0 trivially and the matrix for later reuse)."""
    uniq = np.unique(labels)
    centroids = np.stack([vectors[labels == l].mean(axis=0) for l in uniq]).astype(np.float32)
    dists = cdist(centroids, centroids, metric="euclidean")
    if hd_centroid_distances is None:
        return 1.0, dists
    iu = np.triu_indices(len(uniq), k=1)
    rho, _ = spearmanr(hd_centroid_distances[iu], dists[iu])
    return float(rho), dists


def _nmi_kmeans(vectors: np.ndarray, labels: np.ndarray) -> float:
    """NMI(unsupervised kmeans clustering, canonical labels). k = number of canonicals.
    MiniBatchKMeans handles HD (768d × 30k) without choking."""
    uniq, encoded = np.unique(labels, return_inverse=True)
    n_clusters = len(uniq)
    kmeans = MiniBatchKMeans(
        n_clusters=n_clusters,
        batch_size=_NMI_KMEANS_BATCH,
        random_state=_RNG_SEED,
        n_init=3,
        max_iter=100,
    )
    pred = kmeans.fit_predict(vectors)
    return float(normalized_mutual_info_score(encoded, pred))


def _knn_indices(X: np.ndarray, k: int, metric: str) -> np.ndarray:
    """Returns (N, k) integer indices of each point's k nearest neighbors (excluding self)."""
    nn = NearestNeighbors(n_neighbors=k + 1, metric=metric, n_jobs=-1).fit(X)
    _, idx = nn.kneighbors(X)
    return idx[:, 1:]


def _knn_purity(knn_idx: np.ndarray, labels: np.ndarray) -> float:
    """Mean fraction of a point's k-NN sharing its label. Local-neighborhood analog of silhouette."""
    label_arr = np.asarray(labels)
    neighbor_labels = label_arr[knn_idx]  # (N, k)
    matches = (neighbor_labels == label_arr[:, None]).mean(axis=1)
    return float(matches.mean())


def _trustworthiness_continuity(
    X_hd: np.ndarray, X_proj: np.ndarray, k: int, sample_n: int,
) -> tuple[float, float]:
    """sklearn.manifold.trustworthiness on a stratified-random sample (cap RAM).
    Continuity is computed via the standard swap trick: continuity(X, Y) ≈ trustworthiness(Y, X).
    """
    n = len(X_hd)
    if n > sample_n:
        rng = np.random.default_rng(_RNG_SEED)
        idx = rng.choice(n, size=sample_n, replace=False)
        X_hd = X_hd[idx]
        X_proj = X_proj[idx]
    trust = sk_trustworthiness(X_hd, X_proj, n_neighbors=k, metric="euclidean")
    cont = sk_trustworthiness(X_proj, X_hd, n_neighbors=k, metric="euclidean")
    return float(trust), float(cont)


def _card_jaccard(
    meta: pd.DataFrame, vectors_hd: np.ndarray, vectors_proj: np.ndarray, k: int,
) -> float:
    """For each card, aggregate its lines into a card vector (mean of line vectors), find top-k
    nearest cards in HD vs in projection, average Jaccard overlap.
    """
    df = meta.copy()
    df["row"] = np.arange(len(df))
    grouped = df.groupby("card_id", sort=False)["row"].apply(list)
    if len(grouped) <= k + 1:
        return 1.0
    cards = list(grouped.index)
    card_hd = np.stack([vectors_hd[rows].mean(axis=0) for rows in grouped]).astype(np.float32)
    card_proj = np.stack([vectors_proj[rows].mean(axis=0) for rows in grouped]).astype(np.float32)

    hd_knn = _knn_indices(card_hd, k, metric="euclidean")
    proj_knn = _knn_indices(card_proj, k, metric="euclidean")

    overlaps = np.empty(len(cards), dtype=np.float32)
    for i in range(len(cards)):
        inter = len(set(hd_knn[i]).intersection(proj_knn[i]))
        # |A| = |B| = k → union = 2k - inter
        overlaps[i] = inter / (2 * k - inter) if inter > 0 else 0.0
    return float(overlaps.mean())


def _evaluate_level(
    level: str,
    meta: pd.DataFrame,
    vectors: np.ndarray,
    hd_vectors: np.ndarray,
    hd_centroid_distances_leaf: np.ndarray,
    hd_centroid_distances_parent: np.ndarray,
) -> list[dict]:
    """Compute all 8 metrics for one level. Returns list of tidy rows."""
    leaf_labels = meta["canonical_slug"].to_numpy()
    parent_labels = meta["canonical_family"].to_numpy()
    rows: list[dict] = []

    logger.info("── %s ── Camp 1: Exploration", level.upper())
    sil_leaf = _centroid_silhouette(vectors, leaf_labels)
    rows.append(dict(level=level, camp="exploration", metric="silhouette_leaf", value=sil_leaf))
    logger.info("  silhouette_leaf      = %+.4f", sil_leaf)

    sil_parent = _centroid_silhouette(vectors, parent_labels)
    rows.append(dict(level=level, camp="exploration", metric="silhouette_parent", value=sil_parent))
    logger.info("  silhouette_parent    = %+.4f", sil_parent)

    if level == "hd":
        spearman_leaf, _ = _pairwise_centroid_spearman(vectors, leaf_labels, None)
        spearman_parent, _ = _pairwise_centroid_spearman(vectors, parent_labels, None)
    else:
        spearman_leaf, _ = _pairwise_centroid_spearman(vectors, leaf_labels, hd_centroid_distances_leaf)
        spearman_parent, _ = _pairwise_centroid_spearman(vectors, parent_labels, hd_centroid_distances_parent)
    rows.append(dict(level=level, camp="exploration", metric="pairwise_centroid_spearman_leaf",
                     value=spearman_leaf))
    rows.append(dict(level=level, camp="exploration", metric="pairwise_centroid_spearman_parent",
                     value=spearman_parent))
    logger.info("  centroid_spearman    = leaf %+.4f / parent %+.4f", spearman_leaf, spearman_parent)

    nmi = _nmi_kmeans(vectors, leaf_labels)
    rows.append(dict(level=level, camp="exploration", metric="nmi_kmeans", value=nmi))
    logger.info("  nmi_kmeans (k=%d)  = %+.4f", len(np.unique(leaf_labels)), nmi)

    logger.info("── %s ── Camp 2: Exploitation", level.upper())
    knn_idx = _knn_indices(vectors, _K, metric="euclidean")
    knn_pur = _knn_purity(knn_idx, leaf_labels)
    rows.append(dict(level=level, camp="exploitation", metric=f"knn_purity_k{_K}", value=knn_pur))
    logger.info("  knn_purity_k%d       = %+.4f", _K, knn_pur)

    if level == "hd":
        trust, cont, card_jac = 1.0, 1.0, 1.0
    else:
        trust, cont = _trustworthiness_continuity(hd_vectors, vectors, _K, _TRUST_SAMPLE)
        card_jac = _card_jaccard(meta, hd_vectors, vectors, _K)
    rows.append(dict(level=level, camp="exploitation", metric=f"trustworthiness_k{_K}", value=trust))
    rows.append(dict(level=level, camp="exploitation", metric=f"continuity_k{_K}", value=cont))
    rows.append(dict(level=level, camp="exploitation", metric=f"card_jaccard_k{_K}", value=card_jac))
    logger.info(
        "  trustworthiness_k%d  = %+.4f / continuity_k%d = %+.4f / card_jaccard_k%d = %+.4f",
        _K, trust, _K, cont, _K, card_jac,
    )
    return rows


def _evaluate_impl(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    five_d: pd.DataFrame,
    two_d: pd.DataFrame,
    primary: pd.DataFrame,
) -> pd.DataFrame:
    meta, hd, fived, twod = _load_aligned(lines, encoded, five_d, two_d, primary)

    # Pre-compute HD centroid distances once — reused by every non-HD level for Spearman
    leaf = meta["canonical_slug"].to_numpy()
    parent = meta["canonical_family"].to_numpy()
    _, hd_centroids_leaf = _pairwise_centroid_spearman(hd, leaf, None)
    _, hd_centroids_parent = _pairwise_centroid_spearman(hd, parent, None)

    all_rows: list[dict] = []
    for level, vectors in (("hd", hd), ("5d", fived), ("2d", twod)):
        all_rows.extend(_evaluate_level(
            level, meta, vectors, hd,
            hd_centroids_leaf, hd_centroids_parent,
        ))

    out = pd.DataFrame(all_rows)
    return out


@step(
    inputs=[
        "OracleLines",
        "EncodedTexts",
        "ClusteringEmbeddings",
        "AtlasPoints",
        "LinePrimaryCanonicals",
    ],
    outputs="ProjectionQualityMetrics",
    cacheable=True,
)
def evaluate_projection_quality(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    five_d: pd.DataFrame,
    two_d: pd.DataFrame,
    primary: pd.DataFrame,
) -> pd.DataFrame:
    return _evaluate_impl(lines, encoded, five_d, two_d, primary)
