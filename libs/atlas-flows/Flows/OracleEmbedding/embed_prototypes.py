"""Encode CanonicalArchetypes.prototypes with the FineTunedEmbeddingModel into a small,
archetype-only encoder cache.

Split off from `embed_oracle_text.py` so that editing `canonical-archetypes.json` invalidates
only this step (and downstream attribution) without forcing a re-encode of the ~30k-row
oracle-line corpus. Uses the same model load + the same `clustering:` task prefix so the
resulting vectors are directly comparable in AttributeLines' long-short scorer.

Inputs:
    archetypes: DataFrame of CanonicalArchetype rows. Their `prototypes` lists are flattened
                and de-duplicated into the encoder input.
    model_ref:  ModelArtifactRef record — { Path, RepoId, Variant }. The model bytes don't
                transit Flowthru's marshaller; the Python step loads from `Path` on disk.
    config:     OracleEmbeddingConfig record — uses `EmbedBatchSize`.

Output: DataFrame [text, embedding] — one row per unique prototype string, embedding packed as
        a little-endian float16 byte blob (see EncodedText.cs). Same shape as EncodedTexts.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _embed_prototypes_impl(
    archetypes: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    variant = model_ref.get("Variant", "?")
    batch_size = int(config["EmbedBatchSize"])

    proto_texts: list[str] = []
    if "prototypes" in archetypes.columns:
        for protos in archetypes["prototypes"]:
            if protos is None:
                continue
            if isinstance(protos, float) and pd.isna(protos):
                continue
            for p in protos:
                if p is None:
                    continue
                proto_texts.append(str(p))

    unique_texts = list(dict.fromkeys(proto_texts))
    logger.info(
        "Input: %d archetypes × ~%.1f prototypes avg = %d total / %d unique; model=%s @ %s",
        len(archetypes),
        len(proto_texts) / max(len(archetypes), 1),
        len(proto_texts), len(unique_texts), variant, model_path,
    )

    if not unique_texts:
        logger.warning("No prototype strings found in CanonicalArchetypes — emitting empty cache.")
        return pd.DataFrame({"text": [], "embedding": []})

    model = SentenceTransformer(model_path, trust_remote_code=True)
    prefixed = [f"clustering: {t}" for t in unique_texts]

    embeddings = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=False,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float16)
    logger.info("Encoded %d prototype vectors (dim=%d)", embeddings.shape[0], embeddings.shape[1])

    blobs = [vec.astype("<f2").tobytes() for vec in embeddings]
    return pd.DataFrame({"text": unique_texts, "embedding": blobs})


@step(
    inputs=["CanonicalArchetypes", "FineTunedEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="EncodedPrototypes",
    cacheable=True,
)
def embed_prototypes(
    archetypes: pd.DataFrame, model_ref: dict, config: dict,
) -> pd.DataFrame:
    return _embed_prototypes_impl(archetypes, model_ref, config)
