"""Embed oracle text with a BERT sentence-transformer, then reduce to 2D via UMAP.

Input:  DataFrame of FilteredCardCoreData rows — we only need `Id` and `OracleText`.
Output: DataFrame with columns [id, x, y, text_type].

The sentence-transformer model (all-MiniLM-L6-v2, ~90 MB) is downloaded on first run and
cached under ~/.cache/huggingface. UMAP fit+transform on ~30K 384-dim vectors takes a minute
on CPU.
"""
from __future__ import annotations

import logging
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


@step(inputs=["OracleInputs"], outputs="AtlasPoints")
def embed_and_reduce(cards: pd.DataFrame) -> pd.DataFrame:
    # Lazy imports — these pull in ~GB of torch/transformers so we don't want them
    # loaded when Flowthru is just inspecting the module at pipeline-graph time.
    from sentence_transformers import SentenceTransformer
    import umap

    logger.info("Input: %d cards", len(cards))

    texts = cards["oracle_text"].fillna("").astype(str).tolist()

    logger.info("Loading sentence-transformer (all-MiniLM-L6-v2)...")
    model = SentenceTransformer("all-MiniLM-L6-v2")

    logger.info("Encoding %d oracle texts...", len(texts))
    embeddings = model.encode(
        texts,
        batch_size=64,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    )
    logger.info("Embeddings shape: %s", embeddings.shape)

    logger.info("Running UMAP (n_neighbors=15, min_dist=0.1, n_components=2)...")
    reducer = umap.UMAP(
        n_components=2,
        n_neighbors=15,
        min_dist=0.1,
        metric="cosine",
        random_state=42,
    )
    coords = reducer.fit_transform(embeddings)
    logger.info("UMAP output shape: %s", coords.shape)

    return pd.DataFrame({
        "id": cards["id"],
        "x": coords[:, 0].astype(float),
        "y": coords[:, 1].astype(float),
        "text_type": "oracle",
    })
