"""Attribute oracle lines to canonical archetypes via long-short prototype margin scoring.

Architecture: each CanonicalArchetype is defined by a list of natural-language prototype clauses
(see CanonicalArchetype.cs for authoring guidance). This step:

  1. Reads pre-encoded prototype vectors from the EncodedPrototypes cache (produced by the
     sibling `embed_prototypes.py` step using the same model + `clustering:` prefix that
     produced EncodedTexts). Kept separate from EncodedTexts so archetype edits invalidate
     only this small cache, not the ~30k-row oracle-line cache.
  2. Stacks all prototypes (across all archetypes) into one (P, D) matrix with an owner-index
     per row — no per-archetype mean-pooling.
  3. For each oracle line, computes cosine against every prototype in one matmul, then per
     archetype A derives long(A) = max cosine to A's own prototypes (the attractor) and
     short(A) = max cosine to every OTHER archetype's prototypes (the implicit detractor — no
     hand-curated negatives needed; each archetype's negative set is just every other archetype's
     positives). Confidence = long - short.
  4. Emits the top-K archetypes per line whose `long ≥ LineAnchorThreshold`, ranked by margin.
  5. ALSO runs Pass 0 pattern matching — regex clauses on the archetype's `patterns` list
     deterministically emit `pattern` attributions at confidence=1.0.

Why long-short: solo-match lines get sharp positive margins on their true archetype only;
dual-match lines (e.g. a hexproof + indestructible creature) have ≈equal attractor and detractor
on both sides, so the margins cancel symmetrically and both archetypes still attribute; lines
that bleed into a wrong archetype's prototype space are suppressed because the correct archetype
wins the long-vs-short contest by a positive margin. Long-short investing analog: each archetype
is "long its own prototype basket, short every other basket" — pure factor isolation.

Replaces the deleted `build_canonical_line_assignments.py` (which had Pass 0 pattern + Pass 1
anchor + Pass 2a/2b inference). All Scryfall-tag-derived attribution logic is gone — archetype
identity is now defined entirely by its prototype clauses, not by which Scryfall otag points at it.

Inputs:
    archetypes: CanonicalArchetypes [slug, name, prototypes, patterns].
    lines:      OracleLines [line_id, card_id, oracle_id, text].
    encoded:    EncodedTexts [text, embedding] — encoder cache, used to lookup line embeddings
                AND to lookup prototype embeddings (prototypes are added to the encoder corpus
                upstream so their cached vectors are available here).
    config:     TagLabelingConfig — uses LineAnchorThreshold, TopKInferences.

Output: OracleLineCanonicalAssignment rows.
"""
from __future__ import annotations

import logging
import re
import uuid

import numpy as np
import pandas as pd
from flowthru import step

from Flows.TagLabeling import _sources

logger = logging.getLogger(__name__)


def _normalize_guid(v) -> str | None:
    """Coerce a Guid-typed value (bytes from parquet, string from JSON) to a UUID string."""
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


def _unit_norm(vec: np.ndarray) -> np.ndarray:
    n = float(np.linalg.norm(vec))
    return vec / n if n > 0 else vec


def _derive_synthetic_prototypes(
    archetypes: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    k_medoid: int,
) -> dict[str, list[bytes]]:
    """For each archetype with regex patterns, derive K medoid line-embedding blobs from the
    set of oracle lines its regex matches. The medoid set is the K texts whose HD embeddings
    are closest to the matched-set centroid — the "semantic core" of what the regex actually
    picks up. Used as synthetic prototypes in long-short scoring, giving pattern-only archetypes
    embedding presence (otherwise they can only match lines literally hit by the regex).

    Returns {archetype_slug: [embedding_blob, ...]}. Self-reference (a regex-matched line
    cos=1.0 with its own synthetic prototype) is benign — those lines are already attributed
    via PATTERN source at confidence=1.0, so the prototype attribution is deduplicated by the
    caller's already_emitted set; the real win is for lines that DON'T match the regex but sit
    near the medoid neighborhood.

    Skips archetypes with no patterns or no matches. k_medoid=0 disables synthesis entirely."""
    if k_medoid <= 0:
        return {}

    text_to_emb_bytes = dict(zip(encoded["text"].tolist(), encoded["embedding"].tolist()))
    line_text_arr = lines["text"].astype(str).tolist()
    unique_line_texts = list(dict.fromkeys(line_text_arr))

    out: dict[str, list[bytes]] = {}
    n_archetypes_synthesized = 0
    for _, row in archetypes.iterrows():
        slug = str(row["slug"])
        raw_patterns = row.get("patterns")
        if raw_patterns is None:
            continue
        if isinstance(raw_patterns, float) and pd.isna(raw_patterns):
            continue
        if len(raw_patterns) == 0:
            continue
        try:
            compiled = [re.compile(str(p)) for p in raw_patterns]
        except re.error:
            continue

        matched_texts: list[str] = []
        for t in unique_line_texts:
            if any(p.search(t) for p in compiled):
                matched_texts.append(t)
        if not matched_texts:
            continue

        blobs: list[bytes] = []
        for t in matched_texts:
            b = text_to_emb_bytes.get(t)
            if b is not None:
                blobs.append(b)
        if not blobs:
            continue

        mat = _decode_blobs(blobs)
        norms = np.linalg.norm(mat, axis=1, keepdims=True)
        norms[norms == 0] = 1.0
        mat = mat / norms
        centroid = mat.mean(axis=0)
        cn = np.linalg.norm(centroid)
        if cn > 0:
            centroid /= cn

        cos_to_centroid = mat @ centroid
        k = min(k_medoid, len(blobs))
        top_idx = np.argsort(-cos_to_centroid)[:k]
        out[slug] = [blobs[int(i)] for i in top_idx]
        n_archetypes_synthesized += 1

    if n_archetypes_synthesized:
        logger.info(
            "Derived synthetic prototypes for %d archetypes (k_medoid=%d, sourced from "
            "regex-matched oracle lines)", n_archetypes_synthesized, k_medoid,
        )
    return out


def _build_prototype_matrix(
    archetypes: pd.DataFrame,
    encoded_prototypes: pd.DataFrame,
    synthetic_by_slug: dict[str, list[bytes]],
) -> tuple[list[str], np.ndarray, np.ndarray]:
    """For each archetype, gather its prototype embeddings (declared + synthetic medoid) and
    emit them one-per-row (no mean-pooling — we keep individual prototypes so the long-short
    scorer can compute per-prototype max). An archetype is included in the matrix if it has
    EITHER declared prototypes OR synthetic prototypes (or both); pure stubs (no patterns and
    no prototypes) are skipped.

    Returns:
        slug_order:  unique archetype slugs in the order they appear (1 entry per archetype)
        proto_mat:   (n_prototypes_total, D) L2-normalized prototype embeddings
        proto_owner: (n_prototypes_total,) int array — index into slug_order for each row
    """
    # Build text → embedding lookup from the prototype-only encoder cache (small — ~hundreds
    # of rows). Kept separate from the oracle-line EncodedTexts cache so archetype edits don't
    # invalidate the ~30k-row line cache.
    text_to_emb_bytes = dict(
        zip(encoded_prototypes["text"].tolist(), encoded_prototypes["embedding"].tolist())
    )

    slug_order: list[str] = []
    all_blobs: list[bytes] = []
    all_owner: list[int] = []
    missing_by_slug: dict[str, int] = {}
    n_declared_total = 0
    n_synthetic_total = 0
    for _, row in archetypes.iterrows():
        slug = str(row["slug"])
        prototypes = row.get("prototypes")

        # Collect declared prototype blobs (may be empty for pattern-only archetypes).
        declared_blobs: list[bytes] = []
        if (
            prototypes is not None
            and not (isinstance(prototypes, float) and pd.isna(prototypes))
            and hasattr(prototypes, "__len__")
            and len(prototypes) > 0
        ):
            n_missing = 0
            for p in prototypes:
                blob = text_to_emb_bytes.get(str(p))
                if blob is None:
                    n_missing += 1
                else:
                    declared_blobs.append(blob)
            if n_missing > 0:
                missing_by_slug[slug] = n_missing

        # Add synthetic medoid prototypes from regex matches (may be empty).
        synthetic_blobs = synthetic_by_slug.get(slug, [])

        archetype_blobs = declared_blobs + synthetic_blobs
        if not archetype_blobs:
            continue

        owner_idx = len(slug_order)
        slug_order.append(slug)
        all_blobs.extend(archetype_blobs)
        all_owner.extend([owner_idx] * len(archetype_blobs))
        n_declared_total += len(declared_blobs)
        n_synthetic_total += len(synthetic_blobs)

    if not slug_order:
        raise RuntimeError("No archetype prototypes built — every archetype was empty/missing "
                           "and had no synthetic medoid prototypes either")

    proto_mat = _decode_blobs(all_blobs)
    # Per-prototype L2 normalize. Encoder caches store raw model outputs which are typically
    # near-unit-norm but f16 round-trip drifts slightly.
    norms = np.linalg.norm(proto_mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    proto_mat = proto_mat / norms

    proto_owner = np.asarray(all_owner, dtype=np.int32)
    logger.info(
        "Built prototype matrix: %d archetypes × %d total prototype rows "
        "(declared=%d, synthetic=%d). %d archetypes had at least one missing declared "
        "prototype: %s",
        len(slug_order), proto_mat.shape[0], n_declared_total, n_synthetic_total,
        len(missing_by_slug),
        dict(sorted(missing_by_slug.items(), key=lambda kv: -kv[1])[:10]),
    )
    return slug_order, proto_mat, proto_owner


def _attribute_impl(
    archetypes: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    encoded_prototypes: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    threshold = float(config["LineAnchorThreshold"])
    top_k = int(config.get("TopKInferences", 3))
    neg_weight = float(config.get("PrototypeNegativeWeight", 1.0))
    synthetic_k = int(config.get("SyntheticPrototypeMedoidCount", 10))

    # OracleLines line_ids arrive as bytes (parquet) or UUID strings (JSON) depending on the
    # catalog item's serialization. Normalize to strings here so the output_rows uniformly carry
    # string line_ids; final conversion to 16-byte UUID happens once at the end.
    lines = lines.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)

    output_rows: list[dict] = []

    # ── PASS 0: PATTERN (regex, deterministic) ──
    pattern_total = 0
    pattern_by_slug: dict[str, int] = {}
    line_id_arr = lines["line_id"].tolist()
    text_arr = lines["text"].tolist()
    for _, row in archetypes.iterrows():
        slug = str(row["slug"])
        raw_patterns = row.get("patterns")
        if raw_patterns is None:
            continue
        if isinstance(raw_patterns, float) and pd.isna(raw_patterns):
            continue
        if len(raw_patterns) == 0:
            continue
        try:
            compiled = [re.compile(str(p)) for p in raw_patterns]
        except re.error as exc:
            logger.warning("Skipping invalid pattern(s) for archetype %r: %s", slug, exc)
            continue
        matched: set[str] = set()
        for lid, text in zip(line_id_arr, text_arr):
            if not isinstance(text, str):
                continue
            if any(p.search(text) for p in compiled):
                matched.add(lid)
        if not matched:
            continue
        for lid in matched:
            output_rows.append({
                "line_id": lid,
                "canonical_slug": slug,
                "confidence": 1.0,
                "source": _sources.PATTERN,
            })
        pattern_by_slug[slug] = len(matched)
        pattern_total += len(matched)
    logger.info(
        "PASS 0 (pattern): %d attributions across %d archetypes. Per-archetype counts: %s",
        pattern_total, len(pattern_by_slug),
        dict(sorted(pattern_by_slug.items(), key=lambda kv: -kv[1])),
    )

    # ── Derive synthetic medoid prototypes from regex matches ──
    # For each pattern-bearing archetype, take the K oracle lines whose embeddings sit closest
    # to the matched-set HD centroid. Layered with declared prototypes so the long-short scorer
    # sees ALL archetypes (not just those with hand-curated prototypes).
    synthetic_by_slug = _derive_synthetic_prototypes(archetypes, lines, encoded, synthetic_k)

    # ── Build prototype matrix (one row per prototype, ownership index per row) ──
    slugs, proto_mat, proto_owner = _build_prototype_matrix(
        archetypes, encoded_prototypes, synthetic_by_slug,
    )
    n_archetypes = len(slugs)

    # ── Decode line embeddings ──
    # Lines store their text; we look up the embedding via the dedup'd encoder cache.
    merged = lines[["line_id", "text"]].merge(
        encoded[["text", "embedding"]], on="text", how="left", validate="many_to_one",
    )
    if merged["embedding"].isna().any():
        n_missing = int(merged["embedding"].isna().sum())
        raise RuntimeError(
            f"{n_missing} oracle lines have no matching encoded text — encoder cache out of "
            f"sync with OracleLines."
        )
    line_mat = _decode_blobs(merged["embedding"].tolist())
    # Renormalize after f16 round-trip.
    norms = np.linalg.norm(line_mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    line_mat = line_mat / norms

    # ── PASS 1: PROTOTYPE (long-short margin) ──
    # One matmul gets every (line, prototype) cosine: (N, D) @ (P, D).T → (N, P).
    # Then per archetype A we derive:
    #     long(A)  = max over A's prototype columns      (attractor)
    #     short(A) = max over every OTHER prototype col  (detractor — union of all sibling archetypes)
    #     margin(A) = long(A) - short(A)
    # Long-gate is preserved: a line must still positively match (long ≥ threshold) to attribute.
    # Margin is emitted as the confidence and used for top-K ranking. This way:
    #   - solo-match lines get sharp positive margins on their true archetype only;
    #   - dual-match lines (e.g. "hexproof + indestructible") have ≈equal long and short on both
    #     sides, so attractor/detractor cancel symmetrically and both archetypes still attribute;
    #   - bleed-through (high cosine to wrong archetype's prototypes) is suppressed because the
    #     correct archetype wins the long-vs-short contest by a positive margin.
    line_proto_cos = line_mat @ proto_mat.T  # (N, P)

    # Per-archetype long: max over each archetype's prototype columns.
    # Build by iterating archetypes (≤ a few dozen) rather than a fancy segment-max kernel.
    long_scores = np.empty((line_proto_cos.shape[0], n_archetypes), dtype=np.float32)
    for a_idx in range(n_archetypes):
        cols = np.where(proto_owner == a_idx)[0]
        long_scores[:, a_idx] = line_proto_cos[:, cols].max(axis=1)

    # Per-archetype short: max over columns NOT owned by this archetype. Computed as:
    #   global_max  = max over all columns (per line)
    #   second_max  = max over all columns excluding the column that achieved global_max
    # For archetype A, short(A) = global_max if argmax-owner ≠ A, else second_max.
    global_argmax = line_proto_cos.argmax(axis=1)              # (N,)
    global_max = line_proto_cos[np.arange(line_proto_cos.shape[0]), global_argmax]
    masked = line_proto_cos.copy()
    masked[np.arange(masked.shape[0]), global_argmax] = -np.inf
    second_max = masked.max(axis=1)
    argmax_owner = proto_owner[global_argmax]                  # (N,)

    short_scores = np.empty_like(long_scores)
    for a_idx in range(n_archetypes):
        own_mask = argmax_owner == a_idx
        short_scores[:, a_idx] = np.where(own_mask, second_max, global_max)

    # Apply softened-negative weight. 1.0 = symmetric long-short; lower values reduce the
    # detractor's pull when prototypes across archetypes are paraphrases of each other (cases
    # where max_short ≈ max_long and pure long-short would collapse every margin to zero).
    margin_scores = long_scores - neg_weight * short_scores

    k_eff = min(top_k, n_archetypes - 1) if n_archetypes > 1 else 1

    already_emitted: set[tuple[str, str]] = {
        (r["line_id"], r["canonical_slug"]) for r in output_rows
    }

    line_ids_aligned = merged["line_id"].tolist()
    n_emitted = n_below_threshold = 0
    for i, lid in enumerate(line_ids_aligned):
        row_long = long_scores[i]
        row_margin = margin_scores[i]
        # Rank by margin (long-short purity), but gate on long ≥ threshold.
        top_idxs = np.argpartition(-row_margin, k_eff)[: k_eff + 1]
        top_idxs = top_idxs[np.argsort(-row_margin[top_idxs])][:top_k]
        for k_idx in top_idxs:
            long_s = float(row_long[k_idx])
            if long_s < threshold:
                n_below_threshold += 1
                continue
            margin_s = float(row_margin[k_idx])
            slug = slugs[k_idx]
            if (lid, slug) in already_emitted:
                continue
            output_rows.append({
                "line_id": lid,
                "canonical_slug": slug,
                "confidence": margin_s,
                "source": _sources.PROTOTYPE,
            })
            already_emitted.add((lid, slug))
            n_emitted += 1
    logger.info(
        "PASS 1 (prototype long-short, top-K=%d, long-gate=%.2f, neg_weight=%.2f): %d "
        "attributions emitted, %d (line, slug) below threshold. Long stats: "
        "mean=%.3f p90=%.3f. Margin stats: mean=%.3f p50=%.3f p90=%.3f",
        top_k, threshold, neg_weight, n_emitted, n_below_threshold,
        float(long_scores.mean()), float(np.percentile(long_scores, 90)),
        float(margin_scores.mean()), float(np.median(margin_scores)),
        float(np.percentile(margin_scores, 90)),
    )

    out = pd.DataFrame(output_rows)
    if len(out) == 0:
        raise ValueError("No attributions produced — every line scored below threshold and no "
                         "patterns matched. Check that EncodedTexts contains prototype rows.")

    # line_id strings → 16-byte UUID binary for C# Guid schema.
    out["line_id"] = out["line_id"].map(lambda s: uuid.UUID(s).bytes)

    logger.info(
        "Emitted %d total attributions across %d archetypes. By source: %s",
        len(out), out["canonical_slug"].nunique(),
        out["source"].value_counts().to_dict(),
    )
    return out


@step(
    inputs=[
        "CanonicalArchetypes",
        "OracleLines",
        "EncodedTexts",
        "EncodedPrototypes",
        "TagLabelingConfig",
    ],
    outputs="OracleLineCanonicalAssignments",
    cacheable=True,
)
def attribute_lines(
    archetypes: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    encoded_prototypes: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _attribute_impl(archetypes, lines, encoded, encoded_prototypes, config)
