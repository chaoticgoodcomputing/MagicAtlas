"""Atlas fidelity scorecard — does the 2D projection preserve HD-space neighborhoods?

Five label-free metrics comparing the HD encoder output vs the 2D atlas:

  Local-topology metrics (regression detectors):
  - trustworthiness_k10:  do points that are k-NN in 2D appear in each other's k-NN in HD?
                          Low trustworthiness = 2D introduces "false neighbors" (semantic
                          strangers appearing close on the map).
  - continuity_k10:       do points that are k-NN in HD appear in each other's k-NN in 2D?
                          Low continuity = 2D "tears" real neighborhoods apart.
  - card_jaccard_k10:     per-card jaccard of the k-NN card sets at HD vs 2D — measures whether
                          a card's nearest-card neighborhood is preserved after projection.

  Explorer-experience metrics (UMAP-limitation honesty):
  - density_spearman_k10: per-line Spearman correlation between local density in HD (1 / mean
                          k=10-NN distance) and local density in 2D. UMAP defaults flatten
                          density (it assumes uniform manifold density), so this typically
                          sits near zero — values >0.3 mean the projection preserves enough
                          density signal that a visually-tight cluster actually corresponds
                          to semantic tightness. DensMAP can lift this dramatically.
  - scale_stability:      std-dev of trustworthiness across k=5, 10, 25, 50 — measures whether
                          the projection is honest at scales beyond the single k we tuned for.
                          Low std-dev (<0.03) = consistent neighborhood preservation across
                          zoom levels; high std-dev = optimized for one mental zoom level
                          and misleading at others.

All metrics are label-free; no canonical labels involved. The scorecard's purpose is to catch
regressions in projection geometry, not to prove explorer success (which is a UX question).

Inputs:
    lines:    OracleLines [line_id, card_id, text] — provides line_id ↔ card_id mapping.
    encoded:  EncodedTexts [text, embedding] — HD vectors (float16 bytes).
    two_d:    AtlasPoints [line_id, x, y].

Output: AtlasFidelityMetric rows [metric, value].
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step
from scipy.stats import spearmanr
from sklearn.manifold import trustworthiness as sk_trustworthiness
from sklearn.neighbors import NearestNeighbors

logger = logging.getLogger(__name__)

_K = 10
_K_SCALES = (5, 10, 25, 50)
# sklearn.manifold.trustworthiness materializes an N×N distance matrix. Cap N so RAM stays
# bounded; sampling error << the deltas we care about for regression detection. The same
# sub-sample is reused across trustworthiness, continuity, scale_stability, and
# density_spearman so the metrics describe the same population.
_TRUST_SAMPLE = 5000
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
    return np.stack(
        [np.frombuffer(b, dtype=dtype) for b in byte_col]
    ).astype(np.float32)


def _load_aligned(
    lines: pd.DataFrame, encoded: pd.DataFrame, two_d: pd.DataFrame,
) -> tuple[pd.DataFrame, np.ndarray, np.ndarray]:
    """Join all inputs on line_id, returning (meta_df with [line_id, card_id], hd, twod) all in
    the same row order."""
    lines = lines.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    two_d = two_d.copy()
    two_d["line_id"] = two_d["line_id"].map(_normalize_guid)

    line_hd = lines.merge(encoded, on="text", how="left", validate="many_to_one")
    if line_hd["embedding"].isna().any():
        n = int(line_hd["embedding"].isna().sum())
        raise RuntimeError(f"{n} lines have no HD embedding (encoder cache out of sync)")

    merged = line_hd[["line_id", "card_id", "embedding"]].merge(
        two_d[["line_id", "x", "y"]], on="line_id", how="inner", validate="one_to_one",
    )

    hd = _decode_bytes(merged["embedding"].tolist(), "<f2")
    twod = merged[["x", "y"]].to_numpy(dtype=np.float32)
    meta = merged[["line_id", "card_id"]].reset_index(drop=True)
    return meta, hd, twod


def _continuity(hd: np.ndarray, ld: np.ndarray, k: int) -> float:
    """Dual of trustworthiness: penalizes points that are k-NN in HD but NOT in LD.

    sklearn provides trustworthiness directly. We get continuity by swapping the arguments —
    sklearn computes 'how well low-D neighborhoods match high-D neighborhoods', so swapping
    computes 'how well high-D neighborhoods match low-D neighborhoods', which is continuity.
    """
    return float(sk_trustworthiness(ld, hd, n_neighbors=k))


def _density_spearman(hd: np.ndarray, ld: np.ndarray, k: int) -> float:
    """Per-row Spearman correlation of HD local density vs 2D local density.

    Local density at row i = 1 / mean(distance to k nearest neighbors of i). Higher density =
    tighter local neighborhood. Spearman (rather than Pearson) because UMAP's distance scales
    are not directly comparable to HD's — we care only about rank agreement.

    Returns a value in [-1, 1]. ~0 means UMAP has flattened the density signal (the default
    behavior — UMAP assumes uniform manifold density). >0.5 means density is mostly preserved.
    Negative would mean inversion (very unlikely; would indicate a buggy projection).
    """
    nn_hd = NearestNeighbors(n_neighbors=k + 1).fit(hd)
    dist_hd, _ = nn_hd.kneighbors(hd)
    # Drop the self-distance (column 0); mean over k actual neighbors.
    mean_dist_hd = dist_hd[:, 1:].mean(axis=1)
    density_hd = 1.0 / np.maximum(mean_dist_hd, 1e-12)

    nn_ld = NearestNeighbors(n_neighbors=k + 1).fit(ld)
    dist_ld, _ = nn_ld.kneighbors(ld)
    mean_dist_ld = dist_ld[:, 1:].mean(axis=1)
    density_ld = 1.0 / np.maximum(mean_dist_ld, 1e-12)

    corr, _ = spearmanr(density_hd, density_ld)
    return float(corr)


def _card_jaccard(meta: pd.DataFrame, hd: np.ndarray, ld: np.ndarray, k: int) -> float:
    """Per-card jaccard of the k-NN card sets at HD vs LD. Aggregate up from lines to cards,
    take k-NN over CARD centroids in each space, then per-card jaccard of those k-NN card sets.
    Returns mean jaccard across cards in [0, 1]."""
    df = meta.copy()
    df["_row"] = np.arange(len(df))

    card_groups = df.groupby("card_id")["_row"].apply(list)
    card_ids = card_groups.index.tolist()
    if len(card_ids) < k + 1:
        return float("nan")
    hd_centroids = np.stack([hd[rows].mean(axis=0) for rows in card_groups])
    ld_centroids = np.stack([ld[rows].mean(axis=0) for rows in card_groups])

    nn_hd = NearestNeighbors(n_neighbors=k + 1).fit(hd_centroids)
    nn_ld = NearestNeighbors(n_neighbors=k + 1).fit(ld_centroids)
    _, idx_hd = nn_hd.kneighbors(hd_centroids)
    _, idx_ld = nn_ld.kneighbors(ld_centroids)

    jaccards = []
    for i in range(len(card_ids)):
        hd_set = set(idx_hd[i, 1:].tolist())
        ld_set = set(idx_ld[i, 1:].tolist())
        union = hd_set | ld_set
        if not union:
            continue
        jaccards.append(len(hd_set & ld_set) / len(union))
    return float(np.mean(jaccards)) if jaccards else float("nan")


def _evaluate_impl(
    lines: pd.DataFrame, encoded: pd.DataFrame, two_d: pd.DataFrame,
) -> pd.DataFrame:
    meta, hd, twod = _load_aligned(lines, encoded, two_d)
    logger.info(
        "Inputs aligned: %d lines × HD-dim %d × 2D-dim %d  (across %d cards)",
        len(meta), hd.shape[1], twod.shape[1], meta["card_id"].nunique(),
    )

    # Single sub-sample shared across all sub-sampled metrics — so they describe the same
    # population and are directly comparable.
    n = len(meta)
    if n > _TRUST_SAMPLE:
        rng = np.random.default_rng(_RNG_SEED)
        sample_idx = rng.choice(n, size=_TRUST_SAMPLE, replace=False)
        hd_s = hd[sample_idx]
        twod_s = twod[sample_idx]
        logger.info("Sub-sampled to %d lines for trustworthiness/continuity/scale/density",
                    _TRUST_SAMPLE)
    else:
        hd_s, twod_s = hd, twod

    # Local-topology metrics at the canonical k.
    trust = float(sk_trustworthiness(hd_s, twod_s, n_neighbors=_K))
    cont = _continuity(hd_s, twod_s, k=_K)
    # card_jaccard runs on the full corpus (it aggregates lines→cards which already reduces
    # the dim and is cheap with sklearn's NN).
    card_jac = _card_jaccard(meta, hd, twod, k=_K)

    # Density honesty.
    density_corr = _density_spearman(hd_s, twod_s, k=_K)

    # Scale stability — trustworthiness at multiple k. Std-dev across is the stability number.
    trust_by_k = {k: float(sk_trustworthiness(hd_s, twod_s, n_neighbors=k)) for k in _K_SCALES}
    scale_std = float(np.std(list(trust_by_k.values())))
    logger.info(
        "Trustworthiness by k: %s  →  std=%.4f",
        {k: round(v, 4) for k, v in trust_by_k.items()}, scale_std,
    )

    logger.info(
        "Atlas fidelity: trust_k%d=%.4f  cont_k%d=%.4f  card_jaccard_k%d=%.4f  "
        "density_spearman_k%d=%.4f  scale_stability=%.4f",
        _K, trust, _K, cont, _K, card_jac, _K, density_corr, scale_std,
    )
    return pd.DataFrame([
        {"metric": f"trustworthiness_k{_K}",  "value": trust},
        {"metric": f"continuity_k{_K}",       "value": cont},
        {"metric": f"card_jaccard_k{_K}",     "value": card_jac},
        {"metric": f"density_spearman_k{_K}", "value": density_corr},
        {"metric": "scale_stability",         "value": scale_std},
    ])


@step(
    inputs=["OracleLines", "EncodedTexts", "AtlasPoints"],
    outputs="AtlasFidelityMetrics",
    cacheable=True,
)
def evaluate_atlas_fidelity(
    lines: pd.DataFrame, encoded: pd.DataFrame, two_d: pd.DataFrame,
) -> pd.DataFrame:
    return _evaluate_impl(lines, encoded, two_d)
