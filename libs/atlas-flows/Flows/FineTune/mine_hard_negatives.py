"""Augment TrainingPairs with hard negatives mined via base-model k-NN.

The previous fine-tune iteration (CachedMNR, batch=32, 1 epoch) reduced but did not eliminate
positive-pair regression on the glossary tier. The published guidance for the remaining gap is
explicit hard-negative mining instead of relying on MultipleNegativesRankingLoss's random
in-batch negatives [1][2][3] — because in a lexically-uniform domain like MTG oracle text, the
in-batch "negatives" are themselves likely to be semantically related to the positive, which
sends gradient in the wrong direction.

Procedure (per [1] GPL §3.2 and [2] sbert.net's HN-mining docs):
  1. Collect every unique string appearing in TrainingPairs (anchor / positive / negative).
  2. Encode them with the BASE (un-fine-tuned) model. Base model is the stable semantic prior;
     using the fine-tuned model would be circular ("mine negatives against the model we're about
     to train, then train against those mined negatives").
  3. For each pair that currently has no explicit negative, find K nearest non-positive strings
     to the anchor in base-model cosine space. "Non-positive" excludes:
        (a) the pair's own positive
        (b) every other anchor's positive in the full TrainingPairs set  — a string that's
            someone-else's positive is a false negative if used as our negative
        (c) the anchor itself
  4. Emit one (anchor, positive, mined_negative) triplet per nearest neighbor — turning each
     pair into K triplet rows that carry the same source/weight tags. Existing triplets pass
     through unchanged.

K is set conservatively to 5 — the Nomic Embed paper §3.1 finds that increasing in-batch
negatives above ~7 doesn't meaningfully help [3], and mined negatives saturate faster than
in-batch ones because they're already-hard rather than random.

Inputs:
    pairs:     TrainingPairs [anchor, positive, negative, weight, source].
    model_ref: DefaultEmbeddingModel — the BASE model (deliberately not FineTunedEmbeddingModel).
    config:    OracleEmbeddingConfig — uses EmbedBatchSize.

Output: TrainingPairsMined [anchor, positive, negative, weight, source] — original triplets
        pass-through; original pairs explode into K triplet rows each.

[1] Wang et al. "GPL: Generative Pseudo Labeling for Unsupervised Domain Adaptation of Dense
    Retrieval", arxiv:2112.07577 §3.2 ("Hard Negative Mining").
[2] sbert.net training overview, "Hard Negatives" section,
    https://sbert.net/docs/sentence_transformer/training_overview.html
[3] Nomic Embed: Training a Reproducible Long Context Text Embedder, arxiv:2402.01613 §3.1
    ("increasing the number of negatives above 7 does not meaningfully improve performance").
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

# Conservative K — see citations in module docstring. Increase only with scorecard evidence.
_K_PER_ANCHOR = 5

# Cap batch size on the mining encoder pass to fit on a 12 GB GPU with the fine-tuned model
# potentially co-resident from a recent FineTune training run. See note in
# Flows/FineTuneEval/embed_oracle_text_base.py for the same caution.
_ENCODER_BATCH_CAP = 16


def _collect_unique_texts(pairs: pd.DataFrame) -> list[str]:
    seen: dict[str, None] = {}
    for col in ("anchor", "positive", "negative"):
        if col not in pairs.columns:
            continue
        for value in pairs[col].tolist():
            if value is None:
                continue
            if isinstance(value, float) and pd.isna(value):
                continue
            s = str(value)
            if not s:
                continue
            if s not in seen:
                seen[s] = None
    return list(seen.keys())


def _build_positives_index(pairs: pd.DataFrame) -> dict[str, set[str]]:
    """Returns {anchor → set(all positives for this anchor across the full TrainingPairs)}.
    Used to exclude false-negatives (a positive for ANY anchor is not a valid mined negative
    for THIS anchor — it'd just be a labeling overlap we should respect)."""
    out: dict[str, set[str]] = {}
    for _, row in pairs.iterrows():
        a = str(row["anchor"])
        p = str(row["positive"])
        out.setdefault(a, set()).add(p)
    return out


def _mine_impl(pairs: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    batch_size = min(int(config["EmbedBatchSize"]), _ENCODER_BATCH_CAP)
    logger.info(
        "Hard-negative mining: %d input pairs, base model %s, K=%d",
        len(pairs), model_path, _K_PER_ANCHOR,
    )

    # ── 1. Encode the unique string pool with the BASE model ──
    unique_texts = _collect_unique_texts(pairs)
    if not unique_texts:
        logger.warning("Empty TrainingPairs — emitting empty output.")
        return pairs.iloc[0:0].copy()

    model = SentenceTransformer(model_path, trust_remote_code=True)
    # Same `clustering:` prefix as the rest of the pipeline — keeps the mining
    # neighborhood structure consistent with what the encoder sees at inference / training.
    prefixed = [f"clustering: {t}" for t in unique_texts]
    logger.info("Encoding %d unique strings (batch=%d)…", len(unique_texts), batch_size)
    vecs = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=False,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float32)
    logger.info("Encoded shape: %s", vecs.shape)
    text_to_idx = {t: i for i, t in enumerate(unique_texts)}

    # Pool of candidate negatives = the same encoded matrix (we mine FROM the training-pair
    # string pool, not from the wider oracle corpus — keeps the procedure self-contained and
    # makes false-negative exclusion simpler).
    # Free the model — we're done with it. Mining itself is pure numpy.
    del model

    # ── 2. Build the false-negative exclusion index ──
    positives_by_anchor = _build_positives_index(pairs)
    logger.info(
        "Built exclusion index: %d distinct anchors with positives",
        len(positives_by_anchor),
    )

    # ── 3. Walk every row, pass triplets through, mine pairs ──
    rows: list[dict] = []
    n_passthrough = 0
    n_mined_rows = 0
    n_skipped_no_anchor_vec = 0
    n_skipped_no_negatives_available = 0

    for _, row in pairs.iterrows():
        anchor = str(row["anchor"])
        positive = str(row["positive"])
        negative = row["negative"]
        weight = float(row.get("weight", 1.0))
        source = str(row.get("source", "unknown"))

        # Existing triplet → pass through unchanged.
        if negative is not None and not (isinstance(negative, float) and pd.isna(negative)):
            rows.append({
                "anchor": anchor,
                "positive": positive,
                "negative": str(negative),
                "weight": weight,
                "source": source,
            })
            n_passthrough += 1
            continue

        # Mine K hard negatives for this (anchor, positive) pair.
        anchor_idx = text_to_idx.get(anchor)
        if anchor_idx is None:
            n_skipped_no_anchor_vec += 1
            continue

        # Cosine similarity from the anchor vector to all candidates (the row order matches
        # unique_texts). vecs are L2-normalized so dot product = cosine.
        anchor_vec = vecs[anchor_idx]
        sims = vecs @ anchor_vec  # (N,)

        # Exclude self + any string that's a positive for THIS or ANY anchor (false negatives).
        true_positive_strs = positives_by_anchor.get(anchor, set()) | {anchor}
        # Build the exclusion set of indices.
        excluded_idxs = {text_to_idx[s] for s in true_positive_strs if s in text_to_idx}
        # Also exclude any string that appears as a positive ANYWHERE in pairs (cross-anchor
        # false-negative guard).
        for other_positives in positives_by_anchor.values():
            for ps in other_positives:
                if ps in text_to_idx:
                    excluded_idxs.add(text_to_idx[ps])

        # Argsort descending — sims is already a 1-D array of cosines, biggest first.
        # We need top-K after exclusion; partition then sort the top-K candidates only.
        # Cheap shortcut: mask excluded indices to -inf, then take top-K.
        masked = sims.copy()
        masked[list(excluded_idxs)] = -np.inf
        top_k_idx = np.argpartition(-masked, min(_K_PER_ANCHOR, len(masked) - 1))[:_K_PER_ANCHOR]
        top_k_idx = top_k_idx[np.argsort(-masked[top_k_idx])]
        # Drop any -inf (would mean we exhausted candidates).
        top_k_idx = [int(i) for i in top_k_idx if np.isfinite(masked[i])]

        if not top_k_idx:
            n_skipped_no_negatives_available += 1
            continue

        for neg_idx in top_k_idx:
            rows.append({
                "anchor": anchor,
                "positive": positive,
                "negative": unique_texts[neg_idx],
                "weight": weight,
                "source": f"{source}+mined",
            })
            n_mined_rows += 1

    out = pd.DataFrame(rows)
    logger.info(
        "Mined output: %d total rows (%d passthrough triplets + %d mined triplets from "
        "%d pair anchors). Skipped: %d anchors with no vector, %d with no viable negatives.",
        len(out), n_passthrough, n_mined_rows,
        len(pairs) - n_passthrough - n_skipped_no_anchor_vec - n_skipped_no_negatives_available,
        n_skipped_no_anchor_vec, n_skipped_no_negatives_available,
    )
    return out


@step(
    inputs=["TrainingPairs", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="TrainingPairsMined",
    cacheable=True,
)
def mine_hard_negatives(
    pairs: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    return _mine_impl(pairs, model_ref, config)
