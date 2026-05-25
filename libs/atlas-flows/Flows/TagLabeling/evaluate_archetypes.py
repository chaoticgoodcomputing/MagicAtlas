"""Per-archetype + per-pair quality scorecard for the prototype-driven attribution pipeline.

Produces tidy ArchetypeQualityMetric rows of two shapes:

  Per-archetype (slug = canonical slug):
    - n_prototypes        — how many prototype clauses are authored
    - intra_coherence     — mean pairwise cosine between prototype embeddings (centroid sharpness)
    - n_attributions      — how many lines are attributed to this archetype via prototype source
    - mean_confidence     — mean cosine confidence among prototype-attribution rows
    - median_confidence

  Per-pair (slug = "slug_a|slug_b"):
    - centroid_pair_cosine — cosine between the two archetypes' prototype centroids
                              (high values indicate at-risk-of-confusion)
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _normalize_guid(v) -> str | None:
    if v is None:
        return None
    if isinstance(v, float) and pd.isna(v):
        return None
    if isinstance(v, (bytes, bytearray)):
        try:
            return str(uuid.UUID(bytes=bytes(v)))
        except ValueError:
            return None
    s = str(v)
    return s if s else None


def _decode_blobs(blobs: list[bytes]) -> np.ndarray:
    if not blobs:
        return np.zeros((0, 0), dtype=np.float32)
    dim = len(np.frombuffer(blobs[0], dtype="<f2"))
    mat = np.empty((len(blobs), dim), dtype=np.float32)
    for i, b in enumerate(blobs):
        mat[i] = np.frombuffer(b, dtype="<f2").astype(np.float32)
    return mat


def _unit_norm_rows(mat: np.ndarray) -> np.ndarray:
    norms = np.linalg.norm(mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    return mat / norms


def _eval_impl(
    archetypes: pd.DataFrame,
    encoded: pd.DataFrame,
    assignments: pd.DataFrame,
) -> pd.DataFrame:
    text_to_emb = dict(zip(encoded["text"].tolist(), encoded["embedding"].tolist()))

    rows: list[dict] = []
    slugs_with_centroid: list[str] = []
    centroids: list[np.ndarray] = []

    # ── Per-archetype intra-coherence + counts ──
    for _, arow in archetypes.iterrows():
        slug = str(arow["slug"])
        protos = arow.get("prototypes")
        if protos is None or (isinstance(protos, float) and pd.isna(protos)):
            n_proto = 0
        elif hasattr(protos, "__len__"):
            n_proto = len(protos)
        else:
            n_proto = 0
        rows.append({"slug": slug, "metric": "n_prototypes", "value": float(n_proto)})

        if n_proto < 2:
            # Coherence undefined for <2 prototypes.
            continue

        blobs = [text_to_emb.get(str(p)) for p in protos]
        valid_blobs = [b for b in blobs if b is not None]
        if len(valid_blobs) < 2:
            continue
        proto_mat = _unit_norm_rows(_decode_blobs(valid_blobs))
        # Pairwise cosine, take upper triangle mean
        pair_cos = proto_mat @ proto_mat.T
        iu = np.triu_indices(len(proto_mat), k=1)
        intra = float(np.mean(pair_cos[iu]))
        rows.append({"slug": slug, "metric": "intra_coherence", "value": intra})

        # Build the centroid for the pair-separation step.
        centroid = proto_mat.mean(axis=0)
        n = np.linalg.norm(centroid)
        if n > 0:
            centroid = centroid / n
        slugs_with_centroid.append(slug)
        centroids.append(centroid)

    # ── Per-archetype attribution counts (from prototype-source rows only) ──
    proto_rows = assignments[assignments["source"] == "prototype"]
    counts = proto_rows["canonical_slug"].value_counts().to_dict()
    confs_by_slug = proto_rows.groupby("canonical_slug")["confidence"]
    for slug in {a["slug"] for _, a in archetypes.iterrows()}:
        n = int(counts.get(slug, 0))
        rows.append({"slug": slug, "metric": "n_attributions", "value": float(n)})
        if n > 0:
            c = confs_by_slug.get_group(slug)
            rows.append({"slug": slug, "metric": "mean_confidence", "value": float(c.mean())})
            rows.append({"slug": slug, "metric": "median_confidence", "value": float(c.median())})

    # ── Per-pair centroid cosine (upper triangle only) ──
    if len(centroids) >= 2:
        cent_mat = np.stack(centroids)
        pair_cos = cent_mat @ cent_mat.T
        n_pairs = 0
        for i in range(len(slugs_with_centroid)):
            for j in range(i + 1, len(slugs_with_centroid)):
                rows.append({
                    "slug": f"{slugs_with_centroid[i]}|{slugs_with_centroid[j]}",
                    "metric": "centroid_pair_cosine",
                    "value": float(pair_cos[i, j]),
                })
                n_pairs += 1
        logger.info("Computed %d archetype pairs", n_pairs)

    # ── Logging summary: top intra-coherence + most confused pairs ──
    df = pd.DataFrame(rows)
    intra = df[df["metric"] == "intra_coherence"].sort_values("value")
    logger.info(
        "Intra-coherence: best=%.3f (%s), worst=%.3f (%s)",
        intra["value"].max(), intra.iloc[-1]["slug"] if len(intra) > 0 else "?",
        intra["value"].min(), intra.iloc[0]["slug"] if len(intra) > 0 else "?",
    )
    weakest = intra.head(5)
    if len(weakest) > 0:
        logger.info("5 weakest intra-coherence:")
        for _, r in weakest.iterrows():
            logger.info("  %-25s %.3f", r["slug"], r["value"])

    pairs = df[df["metric"] == "centroid_pair_cosine"].sort_values("value", ascending=False)
    if len(pairs) > 0:
        logger.info("10 most-confusable pairs (highest centroid cosine):")
        for _, r in pairs.head(10).iterrows():
            logger.info("  %-50s %.3f", r["slug"], r["value"])

    return df


@step(
    inputs=["CanonicalArchetypes", "EncodedTexts", "OracleLineCanonicalAssignments"],
    outputs="ArchetypeQualityMetrics",
    cacheable=True,
)
def evaluate_archetypes(
    archetypes: pd.DataFrame,
    encoded: pd.DataFrame,
    assignments: pd.DataFrame,
) -> pd.DataFrame:
    return _eval_impl(archetypes, encoded, assignments)
