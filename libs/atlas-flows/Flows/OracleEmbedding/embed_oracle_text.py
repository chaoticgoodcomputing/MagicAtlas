"""Encode each oracle-text fragment to a sentence-transformer vector using a catalog-managed model.

Inputs:
    fragments: DataFrame of OracleInput rows (point_id, card_id, text, text_type). Reminder-
               text parentheticals are already stripped upstream in ProjectOracleInputNode, so
               the model sees the bare mechanical text.
    model_ref: ModelArtifactRef record — { Path, RepoId, Variant }. The Python step loads
               directly from `Path` on disk; model bytes don't transit Flowthru's marshaller.
    config:    OracleEmbeddingConfig record — uses `EmbedBatchSize`.

Output: DataFrame [point_id, card_id, text_type, embedding] — embedding packed as a
        little-endian byte blob (see BertEmbedding.cs).

Two parallel @step entries differ only in input/output catalog item names — both delegate to
`_embed_impl`. Wiring is done in C# (OracleEmbeddingFlow.cs) which binds each entry to the
default-variant or fine-tuned-variant catalog item pair.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _embed_impl(fragments: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    variant = model_ref.get("Variant", "?")
    batch_size = int(config["EmbedBatchSize"])
    logger.info(
        "Input: %d fragments across %d cards; model=%s @ %s",
        len(fragments), fragments["card_id"].nunique(), variant, model_path,
    )

    model = SentenceTransformer(model_path)

    texts = fragments["text"].fillna("").astype(str).tolist()
    dim = model.get_embedding_dimension() if hasattr(model, "get_embedding_dimension") else model.get_sentence_embedding_dimension()
    logger.info("Encoding %d fragments (dim=%d, batch=%d)...", len(texts), dim, batch_size)
    embeddings = model.encode(
        texts,
        batch_size=batch_size,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float16)
    logger.info("Embeddings shape: %s (dtype %s)", embeddings.shape, embeddings.dtype)

    # Pack each row's float16s into a little-endian byte blob (2 bytes/elem). float16 halves
    # the embedding payload (critical for the 768-dim mpnet variant: float32 × 54k rows hits
    # System.Text.Json's value-length limit at the C# ↔ Python boundary, ~226 MB). Precision
    # loss is negligible for normalized embeddings used in similarity / clustering. Schema
    # change: byte[] vector blob is now (dim × 2) bytes per row instead of (dim × 4).
    blobs = [vec.astype("<f2").tobytes() for vec in embeddings]
    return pd.DataFrame({
        "point_id": fragments["point_id"],
        "card_id": fragments["card_id"],
        "text_type": fragments["text_type"],
        "embedding": blobs,
    })


@step(
    inputs=["OracleInputs", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="BertEmbeddings",
)
def embed_oracle_text(fragments: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    return _embed_impl(fragments, model_ref, config)


@step(
    inputs=["OracleInputs", "FineTunedEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="FineTunedBertEmbeddings",
)
def embed_oracle_text_finetuned(fragments: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    return _embed_impl(fragments, model_ref, config)
