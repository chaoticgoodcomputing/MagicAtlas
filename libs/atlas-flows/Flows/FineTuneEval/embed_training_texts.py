"""Encode the union of (anchor, positive, negative) strings from TrainingPairs under each
model variant. Needed because many training-pair strings (glossary definitions, CR section
bodies, seed-triplet templates) never appear in the oracle-line corpus and therefore aren't
in EncodedTexts / EncodedTextsBase. The FineTuneEval scorecard's objective tier needs vectors
for ALL training-pair strings to compute positive/negative cosines and triplet margins.

Two sibling steps (one per model variant) share a single implementation; the @step decorator
pins which model and which output catalog item each function uses.

Inputs:
    pairs:     TrainingPairs [anchor, positive, negative, weight, source]. Cells may be None
               for tier-1/tier-2 pairs that don't have a negative.
    model_ref: FineTunedEmbeddingModel or DefaultEmbeddingModel.
    config:    OracleEmbeddingConfig — uses EmbedBatchSize.

Output: EncodedText [text, embedding] — one row per unique training-pair string.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _collect_unique_texts(pairs: pd.DataFrame) -> list[str]:
    """Walk every cell of (anchor, positive, negative), keep order-preserving unique non-empty
    string values. Returns the list to be encoded."""
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


def _embed_impl(
    pairs: pd.DataFrame, model_ref: dict, config: dict, variant_label: str,
) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    # Cap batch size for diagnostic encoding — see note in embed_oracle_text_base.py.
    batch_size = min(int(config["EmbedBatchSize"]), 16)

    unique_texts = _collect_unique_texts(pairs)
    logger.info(
        "Input (%s): %d training-pair rows → %d unique strings (model %s)",
        variant_label, len(pairs), len(unique_texts), model_path,
    )

    if not unique_texts:
        logger.warning("No training-pair texts to encode — emitting empty cache.")
        return pd.DataFrame({"text": [], "embedding": []})

    model = SentenceTransformer(model_path, trust_remote_code=True)
    prefixed = [f"clustering: {t}" for t in unique_texts]
    logger.info("Encoding %d strings (batch=%d)…", len(unique_texts), batch_size)
    embeddings = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=False,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float16)
    logger.info("Encoded shape: %s (dtype %s)", embeddings.shape, embeddings.dtype)

    blobs = [vec.astype("<f2").tobytes() for vec in embeddings]
    return pd.DataFrame({"text": unique_texts, "embedding": blobs})


@step(
    inputs=["TrainingPairs", "FineTunedEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="EncodedTrainingTextsFineTuned",
    cacheable=True,
)
def embed_training_texts_finetuned(
    pairs: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    return _embed_impl(pairs, model_ref, config, variant_label="fine-tuned")


@step(
    inputs=["TrainingPairs", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="EncodedTrainingTextsBase",
    cacheable=True,
)
def embed_training_texts_base(
    pairs: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    return _embed_impl(pairs, model_ref, config, variant_label="base")
