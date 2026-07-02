"""Sibling encoder to <c>OracleEmbedding.embed_oracle_text</c>, but using the BASE
(un-fine-tuned) sentence-transformer instead of the fine-tuned one.

Used by the FineTuneEval flow to A/B compare embedding geometry under each model variant.
Same dedup, same `clustering:` task prefix, same float16 byte-blob output shape — so the
result is directly comparable to <see cref="Catalog.EncodedTexts"/>.

Inputs:
    lines:     OracleLines [line_id, card_id, text].
    model_ref: DefaultEmbeddingModel (the base nomic-embed-text-v1.5 download).
    config:    OracleEmbeddingConfig — uses EmbedBatchSize.

Output: EncodedTextsBase [text, embedding].
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _embed_impl(lines: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    variant = model_ref.get("Variant", "?")
    # Cap batch size for diagnostic encoding — the FineTuneEval flow runs occasionally and
    # often shares the GPU with the main pipeline's fine-tuned encode. Smaller batches keep
    # us comfortably under the GPU memory ceiling even when other processes are present.
    batch_size = min(int(config["EmbedBatchSize"]), 16)

    unique_texts = (
        lines["text"].fillna("").astype(str).drop_duplicates().tolist()
    )
    logger.info(
        "Input (base model): %d lines across %d cards; %d unique texts; model=%s @ %s",
        len(lines), lines["card_id"].nunique(), len(unique_texts), variant, model_path,
    )

    model = SentenceTransformer(model_path, trust_remote_code=True)
    prefixed = [f"clustering: {t}" for t in unique_texts]
    logger.info("Encoding %d unique texts (batch=%d)…", len(unique_texts), batch_size)
    embeddings = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float16)
    logger.info("Encoded shape: %s (dtype %s)", embeddings.shape, embeddings.dtype)

    blobs = [vec.astype("<f2").tobytes() for vec in embeddings]
    return pd.DataFrame({"text": unique_texts, "embedding": blobs})


@step(
    inputs=["OracleLines", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="EncodedTextsBase",
    cacheable=True,
)
def embed_oracle_text_base(
    lines: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    return _embed_impl(lines, model_ref, config)
