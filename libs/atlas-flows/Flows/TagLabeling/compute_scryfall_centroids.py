"""Compute canonical-archetype centroids from line-level attributions.

Pass 3 of the bootstrap-then-infer pipeline (see `build_canonical_line_assignments.py` for
the upstream pass 1 anchor + pass 2 inference). This step is a pure mean-pool over the
already-resolved (line → canonical) assignments — no card-level granularity issues here.

Inputs:
    assignments:  OracleLineCanonicalAssignments [line_id, canonical_slug, confidence, source]
    lines:        OracleLines [line_id, card_id, oracle_id, text] — for the line→text lookup
    encoded:      EncodedTexts [text, embedding]
    curation:     ScryfallTagCuration [canonical_slug, name, description, …] — for output naming

Output: DataFrame conforming to TagCentroid:
    [slug, name, description, source, n_inputs, embedding]
    — one row per canonical that received any line attribution.

`source` here is always "scryfall" (the centroid's provenance is the Scryfall track);
provenance per line (`anchor` / `inferred` / `fallback-exemplar` / `fallback-all`) lives on
the OracleLineCanonicalAssignments rows.
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


def _compute_impl(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    curation: pd.DataFrame,
) -> pd.DataFrame:
    # ── Build canonical → display-meta lookup ──
    canonical_meta: dict[str, dict] = {}
    for _, row in curation.iterrows():
        slug = str(row["canonical_slug"])
        name_val = row.get("name")
        desc_val = row.get("description")
        canonical_meta[slug] = {
            "name": str(name_val) if name_val is not None and not (isinstance(name_val, float) and pd.isna(name_val)) else slug,
            "description": str(desc_val) if desc_val is not None and not (isinstance(desc_val, float) and pd.isna(desc_val)) else "",
        }

    # ── Normalize GUIDs on both sides for the join ──
    assignments = assignments.copy()
    assignments["line_id"] = assignments["line_id"].map(_normalize_guid)
    lines = lines.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)

    # ── Join: assignment → line → text → embedding ──
    a_x_l = assignments.merge(lines[["line_id", "text"]], on="line_id", how="inner", validate="many_to_one")
    a_x_l_x_e = a_x_l.merge(encoded, on="text", how="inner", validate="many_to_one")
    logger.info(
        "Joined assignments × lines × encoded: %d rows (from %d assignments). "
        "Drop ratio: %.1f%%",
        len(a_x_l_x_e), len(assignments),
        100 * (1 - len(a_x_l_x_e) / max(len(assignments), 1)),
    )

    if len(a_x_l_x_e) == 0:
        raise ValueError("Empty join — pipeline upstream is broken")

    # ── Decode embeddings ──
    blobs = a_x_l_x_e["embedding"].tolist()
    dim = len(np.frombuffer(blobs[0], dtype="<f2"))
    mat = np.empty((len(blobs), dim), dtype=np.float32)
    for i, b in enumerate(blobs):
        mat[i] = np.frombuffer(b, dtype="<f2").astype(np.float32)

    # ── Mean-pool per canonical, weighted by confidence (anchor=1.0, inferred=cosine, fallback=0.5) ──
    canonical_to_row_idxs: dict[str, list[int]] = {}
    canonical_to_weights: dict[str, list[float]] = {}
    for i, (slug, conf) in enumerate(zip(a_x_l_x_e["canonical_slug"].tolist(),
                                          a_x_l_x_e["confidence"].tolist())):
        canonical_to_row_idxs.setdefault(slug, []).append(i)
        canonical_to_weights.setdefault(slug, []).append(float(conf))

    centroid_rows = []
    for canonical, idxs in canonical_to_row_idxs.items():
        weights = np.array(canonical_to_weights[canonical], dtype=np.float32)
        sub = mat[idxs]
        # Confidence-weighted mean — anchored lines pull harder than fallback ones.
        weighted_sum = (sub * weights[:, None]).sum(axis=0)
        weight_total = float(weights.sum())
        if weight_total <= 0:
            continue
        mean = weighted_sum / weight_total
        norm = float(np.linalg.norm(mean))
        if norm > 0:
            mean = mean / norm
        meta = canonical_meta.get(canonical, {"name": canonical, "description": ""})
        centroid_rows.append({
            "slug": canonical,
            "name": meta["name"],
            "description": meta["description"],
            "source": "scryfall",
            "n_inputs": len(idxs),
            "embedding": mean.astype("<f2").tobytes(),
        })

    out = pd.DataFrame(centroid_rows).sort_values("n_inputs", ascending=False).reset_index(drop=True)
    logger.info(
        "Emitted %d canonical centroids. Top 5 by n_inputs: %s",
        len(out),
        list(zip(out["slug"].head(5).tolist(), out["n_inputs"].head(5).tolist())),
    )
    return out


@step(
    inputs=[
        "OracleLineCanonicalAssignments",
        "OracleLines",
        "EncodedTexts",
        "ScryfallTagCuration",
    ],
    outputs="ScryfallTagCentroids",
    cacheable=True,
)
def compute_scryfall_centroids(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    curation: pd.DataFrame,
) -> pd.DataFrame:
    return _compute_impl(assignments, lines, encoded, curation)
