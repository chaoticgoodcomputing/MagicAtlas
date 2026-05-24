"""Compute hand-curated archetype centroids: embed each TagExemplar's description + examples
with the sentence-transformer model, mean-pool, and emit a TagCentroid row per archetype.

These centroids form the curated-intent track of the cluster-labeling pipeline. They live in
the same embedding space as the variant's EncodedTexts, so cosine similarity against cluster
centroids is meaningful for downstream tag arbitration.

Inputs:
    exemplars: DataFrame of TagExemplar rows (slug, name, description, examples[]).
               `examples` is shipped as a list-of-strings column — flat-schema compatible with
               the Arrow marshaller (the analogue of KeywordVocabulary.keywords).
    model_ref: ModelArtifactRef record — { Path, RepoId, Variant }.
    config:    OracleEmbeddingConfig — uses EmbedBatchSize.

Output: DataFrame [slug, name, description, source, n_inputs, embedding]
        — one row per exemplar tag. `source` is always "exemplar"; `embedding` is the same
        little-endian float16 packing as EncodedText.embedding so the consumer's decode is
        identical.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _compute_impl(exemplars: pd.DataFrame, model_ref: dict, config: dict) -> pd.DataFrame:
    from sentence_transformers import SentenceTransformer

    model_path = model_ref["Path"]
    variant = model_ref.get("Variant", "?")
    batch_size = int(config["EmbedBatchSize"])

    logger.info(
        "Computing exemplar centroids: %d archetypes, model=%s @ %s",
        len(exemplars), variant, model_path,
    )

    # Build (slug, sentence) pairs so we can encode every input in a single batched call and
    # group by slug for the mean-pool. Avoids one encode() per archetype (slow due to model
    # load amortization) and keeps the per-row work uniform.
    sentence_rows = []
    for _, row in exemplars.iterrows():
        slug = str(row["slug"])
        sentence_rows.append((slug, str(row["description"])))
        for ex in row["examples"]:
            sentence_rows.append((slug, str(ex)))

    if not sentence_rows:
        raise ValueError("TagExemplars is empty — nothing to embed")

    slugs_per_row = [s for s, _ in sentence_rows]
    sentences = [t for _, t in sentence_rows]
    logger.info("Encoding %d total sentences across %d archetypes", len(sentences), exemplars.shape[0])

    # Nomic models require trust_remote_code (custom modeling code) and the `clustering:`
    # task prefix to land in the same conditioned representation as the OracleLines pass.
    # The output centroid is keyed by raw slug; the prefix is encoder-internal only.
    model = SentenceTransformer(model_path, trust_remote_code=True)
    prefixed = [f"clustering: {t}" for t in sentences]
    vectors = model.encode(
        prefixed,
        batch_size=batch_size,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float32)  # keep f32 for the mean; cast to f16 for the blob below
    logger.info("Encoded matrix: %s", vectors.shape)

    # Mean-pool by slug, then re-normalise so the centroid is a unit vector in cosine space.
    centroid_rows = []
    by_slug = pd.DataFrame({"slug": slugs_per_row, "idx": range(len(slugs_per_row))})
    grouped = by_slug.groupby("slug")["idx"].apply(list)

    # Build a slug → exemplar metadata lookup for the output rows.
    meta_by_slug = exemplars.set_index("slug")[["name", "description"]].to_dict(orient="index")

    for slug, idxs in grouped.items():
        mat = vectors[idxs]                                # (n_inputs, D)
        mean = mat.mean(axis=0)                            # (D,)
        norm = np.linalg.norm(mean)
        if norm > 0:
            mean = mean / norm
        blob = mean.astype("<f2").tobytes()
        meta = meta_by_slug.get(slug, {"name": slug, "description": ""})
        centroid_rows.append({
            "slug": slug,
            "name": str(meta.get("name", slug)),
            "description": str(meta.get("description", "")),
            "source": "exemplar",
            "n_inputs": int(len(idxs)),
            "embedding": blob,
        })

    out = pd.DataFrame(centroid_rows).sort_values("slug").reset_index(drop=True)
    logger.info(
        "Emitted %d exemplar centroids (variant=%s). Sample: %s",
        len(out), variant, out["slug"].head(5).tolist(),
    )
    return out


@step(
    inputs=["TagExemplars", "DefaultEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="ExemplarTagCentroids",
    cacheable=True,
)
def compute_exemplar_centroids(
    exemplars: pd.DataFrame, model_ref: dict, config: dict
) -> pd.DataFrame:
    return _compute_impl(exemplars, model_ref, config)
