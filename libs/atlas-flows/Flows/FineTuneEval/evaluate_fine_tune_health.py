"""Compare the BASE vs FINE-TUNED embedding model along two diagnostic tiers:

  Geometry tier (does fine-tuning preserve embedding spread, or collapse it uniformly?):
    pair_cos_median        — median pairwise cosine over a sample of the encoded corpus.
                              Higher = tighter / more collapsed.
    pair_cos_std           — std-dev of pairwise cosines. Higher = more discrimination
                              (similar pairs distinguishable from dissimilar ones).
    pair_cos_p10           — bottom-decile pair cosine. Tracks the dissimilar tail.
    pair_cos_p90           — top-decile pair cosine. Tracks the similar tail.
    frac_dissimilar_lt_0_3 — fraction of pairs with cosine < 0.3. Collapse pushes this to ~0.
    hubness_gini           — Gini coefficient of "how often am I someone's k=10 NN?" counts.
                              Higher = a few vectors dominate as nearest-neighbor attractors,
                              a classic dense-center pathology.

  Objective tier (did fine-tuning learn what we taught it on each training source?):
    positive_cos_mean       — mean cosine of (anchor, positive) pairs.
    negative_cos_mean       — mean cosine of (anchor, negative) pairs.
    margin_mean             — mean of (positive_cos − negative_cos) per triplet — the single
                              most important number. Fine-tune should make this substantially
                              positive; if it's near zero, the model learned nothing.
    margin_std              — variance of the margin across triplets.

  Per-source breakdown for objective tier: we partition TrainingPairs by `source` (glossary,
  glossary_cr, reminder_text, template:seed, etc.) and emit each metric per source so you can
  see which training tier is doing the work.

Inputs:
    pairs:                 TrainingPairs [anchor, positive, negative, weight, source].
    encoded_ft:            EncodedTextsSampled (corpus sample, fine-tuned).
    encoded_base:          EncodedTextsBaseSampled (corpus sample, base).
    encoded_train_ft:      EncodedTrainingTextsFineTuned (training-pair texts, fine-tuned).
    encoded_train_base:    EncodedTrainingTextsBase (training-pair texts, base).

Note: the corpus inputs are pre-sampled upstream (see <c>sample_encoded_corpus.py</c>) — full
~30k-row caches blow the C#↔Python step input JSON marshaller limit. The training-text caches
are small (~2k rows) and pass through full.

Output: FineTuneHealthMetric rows [tier, metric, source, base_value, finetuned_value, n].
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step
from sklearn.neighbors import NearestNeighbors

logger = logging.getLogger(__name__)

_GEOMETRY_SAMPLE = 3000
_HUBNESS_K = 10
_RNG_SEED = 42


def _decode_to_matrix(encoded: pd.DataFrame) -> np.ndarray:
    """Decode + L2-normalize the EncodedText byte-blob column → (N, D) float32 matrix."""
    if len(encoded) == 0:
        return np.zeros((0, 0), dtype=np.float32)
    mat = np.stack(
        [np.frombuffer(b, dtype="<f2") for b in encoded["embedding"]]
    ).astype(np.float32)
    norms = np.linalg.norm(mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    return mat / norms


def _gini(values: np.ndarray) -> float:
    """Gini coefficient of a non-negative array. 0 = perfectly equal; 1 = one entry owns
    everything."""
    if len(values) == 0:
        return 0.0
    sorted_vals = np.sort(values.astype(np.float64))
    n = len(sorted_vals)
    total = sorted_vals.sum()
    if total <= 0:
        return 0.0
    cumsum = np.cumsum(sorted_vals)
    # Gini = (n + 1 - 2 * sum_i (cumsum_i / total)) / n
    return float((n + 1 - 2 * (cumsum / total).sum()) / n)


def _geometry_metrics(vecs: np.ndarray) -> dict:
    """Sample + compute geometry diagnostics on a single embedding matrix."""
    n_total = len(vecs)
    if n_total > _GEOMETRY_SAMPLE:
        rng = np.random.default_rng(_RNG_SEED)
        idx = rng.choice(n_total, size=_GEOMETRY_SAMPLE, replace=False)
        sample = vecs[idx]
    else:
        sample = vecs

    cos = sample @ sample.T
    iu = np.triu_indices(len(sample), k=1)
    pair_cos = cos[iu]

    # Hubness: k-NN attractor count distribution, computed on the SAME sample so geometry
    # metrics and hubness describe the same population.
    if len(sample) > _HUBNESS_K + 1:
        nn = NearestNeighbors(n_neighbors=_HUBNESS_K + 1, metric="euclidean").fit(sample)
        _, nbr_idx = nn.kneighbors(sample)
        # Drop self-neighbor (column 0).
        neighbors = nbr_idx[:, 1:].flatten()
        counts = np.bincount(neighbors, minlength=len(sample))
        hubness = _gini(counts)
    else:
        hubness = 0.0

    return {
        "pair_cos_median":        (float(np.median(pair_cos)), len(sample)),
        "pair_cos_std":           (float(np.std(pair_cos)),    len(sample)),
        "pair_cos_p10":           (float(np.percentile(pair_cos, 10)), len(sample)),
        "pair_cos_p90":           (float(np.percentile(pair_cos, 90)), len(sample)),
        "frac_dissimilar_lt_0_3": (float((pair_cos < 0.3).mean()),     len(sample)),
        "hubness_gini":           (float(hubness),             len(sample)),
    }


def _build_lookup(*encoded_dfs: pd.DataFrame) -> dict[str, np.ndarray]:
    """Merge multiple EncodedText caches into one text → unit-normed vector dict. Later
    duplicates overwrite earlier ones — order shouldn't matter since the same string under
    the same model should produce the same vector regardless of source cache."""
    lookup: dict[str, np.ndarray] = {}
    for df in encoded_dfs:
        if len(df) == 0:
            continue
        for text, blob in zip(df["text"].tolist(), df["embedding"].tolist()):
            vec = np.frombuffer(blob, dtype="<f2").astype(np.float32)
            n = np.linalg.norm(vec)
            if n > 0:
                vec = vec / n
            lookup[str(text)] = vec
    return lookup


def _objective_metrics_for_source(
    pairs: pd.DataFrame, lookup: dict[str, np.ndarray],
) -> dict:
    """Compute per-source training-objective metrics from a TrainingPairs sub-dataframe and a
    text-lookup. Returns {metric_name: (value, n)} dict; some metrics only emit if there are
    triplets (negative present)."""
    pos_cos: list[float] = []
    neg_cos: list[float] = []
    margins: list[float] = []
    for _, row in pairs.iterrows():
        anchor = str(row["anchor"]) if row.get("anchor") is not None else None
        positive = str(row["positive"]) if row.get("positive") is not None else None
        if not anchor or not positive:
            continue
        a_vec = lookup.get(anchor)
        p_vec = lookup.get(positive)
        if a_vec is None or p_vec is None:
            continue
        pos = float(a_vec @ p_vec)
        pos_cos.append(pos)

        neg_text = row.get("negative")
        if neg_text is None or (isinstance(neg_text, float) and pd.isna(neg_text)):
            continue
        n_str = str(neg_text)
        if not n_str:
            continue
        n_vec = lookup.get(n_str)
        if n_vec is None:
            continue
        neg = float(a_vec @ n_vec)
        neg_cos.append(neg)
        margins.append(pos - neg)

    out: dict[str, tuple[float, int]] = {}
    if pos_cos:
        out["positive_cos_mean"] = (float(np.mean(pos_cos)), len(pos_cos))
        out["positive_cos_std"]  = (float(np.std(pos_cos)),  len(pos_cos))
    if neg_cos:
        out["negative_cos_mean"] = (float(np.mean(neg_cos)), len(neg_cos))
        out["margin_mean"]       = (float(np.mean(margins)), len(margins))
        out["margin_std"]        = (float(np.std(margins)),  len(margins))
    return out


def _evaluate_impl(
    pairs: pd.DataFrame,
    encoded_ft: pd.DataFrame,
    encoded_base: pd.DataFrame,
    encoded_train_ft: pd.DataFrame,
    encoded_train_base: pd.DataFrame,
) -> pd.DataFrame:
    logger.info(
        "Inputs: %d training pairs · EncodedTexts FT/base sizes %d/%d · "
        "EncodedTrainingTexts FT/base sizes %d/%d",
        len(pairs), len(encoded_ft), len(encoded_base),
        len(encoded_train_ft), len(encoded_train_base),
    )

    rows: list[dict] = []

    # ── Geometry tier (corpus-wide) ─────────────────────────────────────────
    ft_corpus = _decode_to_matrix(encoded_ft)
    base_corpus = _decode_to_matrix(encoded_base)
    logger.info("Computing geometry metrics on corpus samples…")
    g_ft = _geometry_metrics(ft_corpus)
    g_base = _geometry_metrics(base_corpus)
    for metric_name in g_ft:
        ft_val, ft_n = g_ft[metric_name]
        base_val, base_n = g_base.get(metric_name, (float("nan"), 0))
        rows.append({
            "tier": "geometry",
            "metric": metric_name,
            "source": "*",
            "base_value": base_val,
            "finetuned_value": ft_val,
            "n": min(ft_n, base_n) if base_n else ft_n,
        })
    logger.info("Geometry tier: %d rows", len(g_ft))

    # ── Objective tier (per-source) ─────────────────────────────────────────
    # For the lookup, prefer the dedicated training-text cache but fall back to the
    # corpus cache for any anchor/positive/negative strings that happen to overlap with
    # oracle lines (keywords typically do — "Flying" is in both).
    ft_lookup = _build_lookup(encoded_ft, encoded_train_ft)
    base_lookup = _build_lookup(encoded_base, encoded_train_base)

    sources = sorted(pairs["source"].dropna().unique().tolist())
    logger.info(
        "Objective tier across %d training-pair sources: %s",
        len(sources), sources,
    )
    n_obj_rows = 0
    for src in sources:
        sub = pairs[pairs["source"] == src]
        ft_m = _objective_metrics_for_source(sub, ft_lookup)
        base_m = _objective_metrics_for_source(sub, base_lookup)
        for metric_name in ft_m:
            ft_val, ft_n = ft_m[metric_name]
            if metric_name not in base_m:
                continue
            base_val, base_n = base_m[metric_name]
            rows.append({
                "tier": "objective",
                "metric": metric_name,
                "source": src,
                "base_value": base_val,
                "finetuned_value": ft_val,
                "n": min(ft_n, base_n),
            })
            n_obj_rows += 1
    logger.info("Objective tier: %d rows across %d sources", n_obj_rows, len(sources))

    df = pd.DataFrame(rows)
    # Surface the headline numbers in the log so you don't have to open the JSON to see them.
    margins = df[(df.tier == "objective") & (df.metric == "margin_mean")]
    if len(margins):
        for _, row in margins.iterrows():
            delta = row.finetuned_value - row.base_value
            logger.info(
                "  margin_mean[%s]: base=%+.3f  finetuned=%+.3f  Δ=%+.3f  (n=%d)",
                row.source, row.base_value, row.finetuned_value, delta, int(row.n),
            )
    return df


@step(
    inputs=[
        "TrainingPairs",
        "EncodedTextsSampled",
        "EncodedTextsBaseSampled",
        "EncodedTrainingTextsFineTuned",
        "EncodedTrainingTextsBase",
    ],
    outputs="FineTuneHealthMetrics",
    cacheable=True,
)
def evaluate_fine_tune_health(
    pairs: pd.DataFrame,
    encoded_ft: pd.DataFrame,
    encoded_base: pd.DataFrame,
    encoded_train_ft: pd.DataFrame,
    encoded_train_base: pd.DataFrame,
) -> pd.DataFrame:
    return _evaluate_impl(pairs, encoded_ft, encoded_base, encoded_train_ft, encoded_train_base)
