"""Encode each oracle-text fragment to a 384-dim BERT vector (sentence-transformers all-MiniLM-L6-v2).

Input:  DataFrame of OracleInput rows (point_id, card_id, text, text_type).
Output: DataFrame [point_id, card_id, text_type, embedding] — embedding is a numpy float32 array.

Materialized so both the 2D-UMAP display reduction and the Clustering flow's 5D-UMAP+HDBSCAN
reduction can read these vectors without re-running BERT.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["OracleInputs"], outputs="BertEmbeddings")
def embed_oracle_text(fragments: pd.DataFrame) -> pd.DataFrame:
    # Lazy import — pulls in ~GB of torch/transformers.
    from sentence_transformers import SentenceTransformer

    logger.info("Input: %d fragments across %d cards",
                len(fragments), fragments["card_id"].nunique())

    texts = fragments["text"].fillna("").astype(str).tolist()

    logger.info("Loading sentence-transformer (all-MiniLM-L6-v2)...")
    model = SentenceTransformer("all-MiniLM-L6-v2")

    logger.info("Encoding %d fragments...", len(texts))
    embeddings = model.encode(
        texts,
        batch_size=64,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float32)
    logger.info("Embeddings shape: %s (dtype %s)", embeddings.shape, embeddings.dtype)

    # Pack each row's 384 float32s into a little-endian byte blob. Reason: Flowthru's parquet
    # serializer requires IFlatSchema, and a typed float[] is classified as nested — `byte[]` is
    # the one array form considered flat (Tier 3 opaque blob). 1,536 bytes per row.
    blobs = [vec.astype("<f4").tobytes() for vec in embeddings]
    return pd.DataFrame({
        "point_id": fragments["point_id"],
        "card_id": fragments["card_id"],
        "text_type": fragments["text_type"],
        "embedding": blobs,
    })
