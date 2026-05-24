"""Deduplicate oracle lines by Text, encode each unique string once with the FineTunedEmbeddingModel,
and emit an EncodedTexts row per unique input. This is the persisted encoder cache — the 5D
UMAP step consumes (OracleLines + EncodedTexts) and broadcasts the cached vectors back to per-line
rows just before jitter+UMAP.

Inputs:
    lines:     DataFrame of OracleLine rows (line_id, card_id, text). Used only for the unique
               Text values; the line-level id is reattached downstream in the UMAP step's join.
    model_ref: ModelArtifactRef record — { Path, RepoId, Variant }. The model bytes don't
               transit Flowthru's marshaller; the Python step loads from `Path` on disk.
    config:    OracleEmbeddingConfig record — uses `EmbedBatchSize`.

Output: DataFrame [text, embedding] — one row per unique text, embedding packed as a
        little-endian float16 byte blob (see EncodedText.cs).
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
    batch_size = int(config["EmbedBatchSize"])

    # Deduplicate by text. The encoder workload becomes O(unique_texts) instead of O(lines) —
    # for ~50k lines we typically see ~30k unique texts after stripping reminder parentheticals,
    # and once synthetic per-keyword lines land (Phase 2) the keyword strings dedupe down to a
    # few hundred. The cache is persisted, so warm runs short-circuit entirely via Flowthru's
    # step cache when the unique-text set is unchanged.
    unique_texts = (
        lines["text"].fillna("").astype(str).drop_duplicates().tolist()
    )
    logger.info(
        "Input: %d lines across %d cards; %d unique texts; model=%s @ %s",
        len(lines), lines["card_id"].nunique(), len(unique_texts), variant, model_path,
    )

    # Nomic models ship custom modeling code (`modeling_hf_nomic.py`) — `trust_remote_code=True`
    # is required for SentenceTransformer to wire that in at load time.
    model = SentenceTransformer(model_path, trust_remote_code=True)
    dim = (
        model.get_embedding_dimension()
        if hasattr(model, "get_embedding_dimension")
        else model.get_sentence_embedding_dimension()
    )

    # Nomic v1.5 is trained with task-prefix conditioning: `clustering: ...` selects the
    # representation tuned for embedding-space clustering (vs `search_query:`/`search_document:`
    # for retrieval). The prefix is applied to encoder input only; the output `text` column
    # stays raw so downstream joins on `OracleLines.text` work unchanged.
    prefixed = [f"clustering: {t}" for t in unique_texts]

    logger.info("Encoding %d unique texts (dim=%d, batch=%d)...", len(unique_texts), dim, batch_size)
    embeddings = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float16)
    logger.info("Encoded shape: %s (dtype %s)", embeddings.shape, embeddings.dtype)

    # Pack each row's float16s into a little-endian byte blob (2 bytes/elem). float16 halves
    # the embedding payload (critical for the 768-dim mpnet variant: float32 × ~30k unique
    # texts can hit System.Text.Json's value-length limit at the C# ↔ Python boundary).
    blobs = [vec.astype("<f2").tobytes() for vec in embeddings]
    return pd.DataFrame({"text": unique_texts, "embedding": blobs})


@step(
    inputs=["OracleLines", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="EncodedTexts",
    cacheable=True,
)
def embed_oracle_text(lines: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    return _embed_impl(lines, model_ref, config)
