"""Per-line attribution of OracleLines to curated canonicals — bootstrap-then-infer.

Resolves the card→line granularity gap in Scryfall's tag taxonomy: Scryfall tags an oracle_id,
but a multi-line card may only have one of its lines actually representing the tagged archetype
(e.g. Wood Elves is tagged `ramp` but its `Reach` line shouldn't pollute the ramp centroid).

Algorithm:

  PASS 0 — PATTERN (deterministic regex attribution)
    For each canonical T with non-empty `patterns` field in curation:
      For every line L (natural OR synthetic) whose text matches ANY pattern of T:
        emit (L, T, confidence=1.0, source='pattern')
    These are additive — a line can be pattern-attributed to one canonical AND
    anchor/inferred to others. The hierarchy applies: `evasion:flying` membership flows
    into `evasion` via canonical_family downstream.

  PASS 1 — ANCHOR
    For each canonical T:
      anchor_pool_T = lines from cards that
        (a) are tagged with some alias of T,
        (b) have exactly one NATURAL oracle line (synthetic-keyword lines excluded).
      if |anchor_pool_T| >= AnchorFloor:
        anchor_T = mean(anchor_pool_T)        — assignments tagged source='anchor'
      else if T has an exemplar centroid:
        anchor_T = exemplar centroid          — fallback, only used as anchor (not directly emitted)
      else:
        anchor_T = mean of ALL lines on ALL tagged cards (current behavior — least confident)
                                              — assignments tagged source='fallback-all'

  PASS 2 — INFER (only for canonicals self-anchored or exemplar-anchored)
    For each multi-line card C tagged with one or more canonicals {T1...Tn}:
      For each natural line L of C:
        score(L, Ti) = cosine(emb(L), anchor_Ti)  for each Ti in C's tags
        Pick winner = argmax(score)
        If score(winner) >= LineAnchorThreshold:
          emit (L, winner, score, source='inferred')

Inputs:
    assignments:  ScryfallTagAssignments [oracle_id, tag_slug]
    lines:        OracleLines [line_id, card_id, oracle_id, text]
    encoded:      EncodedTexts [text, embedding]
    curation:     ScryfallTagCuration [canonical_slug, aliases, ...]
    vocabulary:   KeywordVocabulary { keywords: List[str] } — used to flag synthetic lines
    exemplars:    ExemplarTagCentroids — fallback anchors when single-line pool is too thin
    config:       TagLabelingConfig — uses AnchorFloor, LineAnchorThreshold

Output: DataFrame conforming to OracleLineCanonicalAssignment
        [line_id, canonical_slug, confidence, source]
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


def _build_impl(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    curation: pd.DataFrame,
    vocabulary: dict,
    exemplars: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    anchor_floor = int(config["AnchorFloor"])
    threshold = float(config["LineAnchorThreshold"])
    top_k = int(config.get("TopKInferences", 3))

    # ── Curation → alias→canonical map + canonical→meta ──
    alias_to_canonical: dict[str, str] = {}
    canonical_meta: dict[str, dict] = {}
    for _, row in curation.iterrows():
        slug = str(row["canonical_slug"])
        canonical_meta[slug] = {"name": str(row.get("name") or slug)}
        aliases = row.get("aliases")
        if aliases is None:
            continue
        for alias in list(aliases):
            alias_to_canonical[str(alias)] = slug

    # ── Filter assignments to curated aliases only ──
    assignments = assignments.copy()
    assignments["oracle_id"] = assignments["oracle_id"].astype(str)
    assignments["canonical_slug"] = assignments["tag_slug"].map(alias_to_canonical)
    assignments = assignments.dropna(subset=["canonical_slug"]).reset_index(drop=True)
    n_canonicals_with_cards = assignments["canonical_slug"].nunique()
    logger.info(
        "Curation maps %d aliases → %d canonicals. After alias filter: %d assignments across "
        "%d canonicals", len(alias_to_canonical), len(canonical_meta),
        len(assignments), n_canonicals_with_cards,
    )

    # ── Identify synthetic lines (text matches a Scryfall keyword vocabulary entry). ──
    # The vocabulary arrives as a dict from the marshaller (the KeywordVocabulary scalar schema).
    keyword_set = set(vocabulary.get("Keywords", []) or vocabulary.get("keywords", []))
    logger.info("Keyword vocabulary: %d entries (used to mark synthetic lines)", len(keyword_set))

    lines = lines.copy()
    lines["oracle_id"] = lines["oracle_id"].map(_normalize_guid)
    lines["line_id"] = lines["line_id"].map(_normalize_guid)
    lines["is_synthetic"] = lines["text"].isin(keyword_set)

    natural_lines_all = lines[~lines["is_synthetic"]].reset_index(drop=True)
    # Natural-line count per card (used to identify single-line anchor cards).
    natural_per_card = natural_lines_all.groupby("card_id").size()
    logger.info(
        "Lines: %d total, %d natural, %d synthetic. Cards with exactly 1 natural line: %d",
        len(lines), len(natural_lines_all), int(lines["is_synthetic"].sum()),
        int((natural_per_card == 1).sum()),
    )

    # ── Attach embeddings to ALL lines (natural + synthetic) ──
    # Synthetic lines need embeddings so that pattern-anchored canonicals (e.g. evasion:flying,
    # whose anchor pool is the synthetic "Flying" lines) can build a centroid for Pass 2.
    all_with_vec = lines.merge(encoded, on="text", how="inner", validate="many_to_one")
    all_mat = _decode_blobs(all_with_vec["embedding"].tolist())
    # Renormalize (f16 round-trip can drift slightly).
    norms = np.linalg.norm(all_mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    all_mat = all_mat / norms
    line_id_to_idx: dict[str, int] = {
        lid: i for i, lid in enumerate(all_with_vec["line_id"].tolist())
    }

    # Natural subset used by single-line-card anchor selection + Pass 2a (scryfall-inference).
    natural_with_vec = all_with_vec[~all_with_vec["is_synthetic"]].reset_index(drop=True)
    oid_to_line_ids: dict[str, list[str]] = {}
    for lid, oid in zip(natural_with_vec["line_id"].tolist(), natural_with_vec["oracle_id"].tolist()):
        if oid:
            oid_to_line_ids.setdefault(oid, []).append(lid)

    # Natural line ids in matrix order — Pass 2b skips synthetic lines (they're already
    # pattern-attributed where appropriate; inferring them adds noise).
    natural_line_ids_ordered = natural_with_vec["line_id"].tolist()

    # Index oracle_id → card_id (single oracle_id may map to >1 card_id across printings; pick first).
    oid_to_card_ids: dict[str, list[str]] = {}
    for cid, oid in zip(natural_lines_all["card_id"].tolist(), natural_lines_all["oracle_id"].tolist()):
        if oid:
            oid_to_card_ids.setdefault(oid, []).append(cid)
    # natural_per_card is keyed by card_id; pick any card_id for the oracle_id and check count.
    def _has_single_natural_line(oid: str) -> bool:
        for cid in oid_to_card_ids.get(oid, []):
            if natural_per_card.get(cid, 0) == 1:
                return True
        return False

    # ── Exemplar centroid lookup for fallback ──
    exemplar_centroid_by_slug: dict[str, np.ndarray] = {}
    if len(exemplars) > 0:
        ex_mat = _decode_blobs(exemplars["embedding"].tolist())
        ex_norms = np.linalg.norm(ex_mat, axis=1, keepdims=True)
        ex_norms[ex_norms == 0] = 1.0
        ex_mat = ex_mat / ex_norms
        for slug, vec in zip(exemplars["slug"].tolist(), ex_mat):
            exemplar_centroid_by_slug[str(slug)] = vec
    logger.info("Exemplar centroids available as fallback anchors: %d", len(exemplar_centroid_by_slug))

    output_rows: list[dict] = []

    # ── PASS 0: PATTERN (deterministic regex attribution) ──
    # Runs over ALL lines (natural AND synthetic) so e.g. `^Flying$` against the synthetic-keyword
    # line gives every flying creature an `evasion:flying` attribution. Pattern attributions are
    # additive — they don't suppress anchor/infer for OTHER canonicals on the same line; downstream
    # LinePrimaryCanonical picks by confidence so the conf=1.0 pattern wins for that (line, canonical).
    pattern_total = 0
    pattern_by_canonical: dict[str, int] = {}
    line_id_arr = lines["line_id"].tolist()
    text_arr = lines["text"].tolist()
    for _, row in curation.iterrows():
        slug = str(row["canonical_slug"])
        raw_patterns = row.get("patterns")
        # Defensive: missing column, None, NaN, or empty list all mean "no patterns for this canonical".
        if raw_patterns is None:
            continue
        if isinstance(raw_patterns, float) and pd.isna(raw_patterns):
            continue
        if len(raw_patterns) == 0:
            continue
        try:
            compiled = [re.compile(str(p)) for p in raw_patterns]
        except re.error as exc:
            logger.warning("Skipping invalid pattern(s) for canonical %r: %s", slug, exc)
            continue
        matched_line_ids: set[str] = set()
        for lid, text in zip(line_id_arr, text_arr):
            if not isinstance(text, str):
                continue
            if any(p.search(text) for p in compiled):
                matched_line_ids.add(lid)
        if not matched_line_ids:
            continue
        for lid in matched_line_ids:
            output_rows.append({
                "line_id": lid,
                "canonical_slug": slug,
                "confidence": 1.0,
                "source": _sources.PATTERN,
            })
        pattern_by_canonical[slug] = len(matched_line_ids)
        pattern_total += len(matched_line_ids)
    logger.info(
        "PASS 0 (pattern): %d attributions across %d canonicals. Per-canonical counts: %s",
        pattern_total, len(pattern_by_canonical),
        dict(sorted(pattern_by_canonical.items(), key=lambda kv: -kv[1])),
    )

    # Index Pass-0 outputs by canonical for Pass 1's anchor pool.
    pattern_attributed_by_canonical: dict[str, list[str]] = {}
    for row in output_rows:
        if row["source"] == _sources.PATTERN:
            pattern_attributed_by_canonical.setdefault(row["canonical_slug"], []).append(row["line_id"])

    # ── PASS 1: ANCHOR (pool = single-line-card lines ∪ pattern-attributed lines) ──
    # Patterns count toward the anchor pool so canonicals like evasion:flying — which have no
    # Scryfall aliases but match the synthetic "Flying" keyword line — still get a usable
    # centroid for downstream Pass 2 scoring.
    anchor_by_canonical: dict[str, np.ndarray] = {}
    anchor_source_by_canonical: dict[str, str] = {}
    n_self_anchored = n_pattern_anchored = n_exemplar_fallback = n_all_lines_fallback = 0

    all_anchor_candidates = (
        set(assignments["canonical_slug"].unique()) | set(pattern_attributed_by_canonical.keys())
    )

    for canonical in all_anchor_candidates:
        scryfall_group = assignments[assignments["canonical_slug"] == canonical]
        tagged_oids = scryfall_group["oracle_id"].drop_duplicates().tolist()

        # Single-line anchor pool: line_ids of single-natural-line cards Scryfall-tagged here.
        single_line_ids: list[str] = []
        for oid in tagged_oids:
            if _has_single_natural_line(oid):
                single_line_ids.extend(oid_to_line_ids.get(oid, []))

        # Pattern-attributed pool (from Pass 0).
        pattern_ids = pattern_attributed_by_canonical.get(canonical, [])

        combined_pool = list(set(single_line_ids) | set(pattern_ids))

        if len(combined_pool) >= anchor_floor:
            idxs = [line_id_to_idx[lid] for lid in combined_pool if lid in line_id_to_idx]
            if idxs:
                sub = all_mat[idxs]
                centroid = _unit_norm(sub.mean(axis=0))
                anchor_by_canonical[canonical] = centroid
                # Source label: distinguish pure-pattern anchors (no Scryfall single-line cards
                # contributed) for diagnostics; downstream consumers treat both as "trusted anchor".
                if not single_line_ids:
                    anchor_source_by_canonical[canonical] = "anchor-pattern"
                    n_pattern_anchored += 1
                else:
                    anchor_source_by_canonical[canonical] = "anchor"
                    n_self_anchored += 1
                # Emit anchor rows for single-line-card lines; pattern lines were already
                # emitted by Pass 0.
                for lid in set(single_line_ids):
                    if lid in line_id_to_idx:
                        output_rows.append({
                            "line_id": lid,
                            "canonical_slug": canonical,
                            "confidence": 1.0,
                            "source": _sources.ANCHOR,
                        })
                continue

        # Below floor — try exemplar fallback.
        if canonical in exemplar_centroid_by_slug:
            anchor_by_canonical[canonical] = exemplar_centroid_by_slug[canonical]
            anchor_source_by_canonical[canonical] = "fallback-exemplar"
            n_exemplar_fallback += 1
            continue

        # Final fallback: all lines on Scryfall-tagged cards (noisy).
        all_card_line_ids: list[str] = []
        for oid in tagged_oids:
            all_card_line_ids.extend(oid_to_line_ids.get(oid, []))
        idxs = [line_id_to_idx[lid] for lid in all_card_line_ids if lid in line_id_to_idx]
        if not idxs:
            continue
        sub = all_mat[idxs]
        centroid = _unit_norm(sub.mean(axis=0))
        anchor_by_canonical[canonical] = centroid
        anchor_source_by_canonical[canonical] = "fallback-all"
        n_all_lines_fallback += 1
        for lid in {lid for lid in all_card_line_ids if lid in line_id_to_idx}:
            output_rows.append({
                "line_id": lid,
                "canonical_slug": canonical,
                "confidence": 0.5,
                "source": _sources.FALLBACK_ALL,
            })

    logger.info(
        "PASS 1 (anchor): %d Scryfall-anchored, %d pattern-only anchored, "
        "%d exemplar-fallback, %d all-lines-fallback",
        n_self_anchored, n_pattern_anchored, n_exemplar_fallback, n_all_lines_fallback,
    )

    # ── Build the candidate set for both Pass 2a and 2b: only TRUSTED anchors. ──
    # We deliberately exclude fallback-all anchors as inference targets — their centroids are
    # mean(all lines on tagged cards), which propagates noise rather than signal.
    trusted_anchor_sources = {"anchor", "anchor-pattern", "fallback-exemplar"}
    trusted_canonicals = [
        c for c, src in anchor_source_by_canonical.items() if src in trusted_anchor_sources
    ]
    if not trusted_canonicals:
        raise RuntimeError("No trusted anchors built — Pass 2 has nothing to score against")
    trusted_anchor_matrix = np.stack([anchor_by_canonical[c] for c in trusted_canonicals])
    logger.info(
        "Pass 2 candidate set: %d trusted canonicals (anchor + anchor-pattern + fallback-exemplar)",
        len(trusted_canonicals),
    )

    already_emitted: set[tuple[str, str]] = {(r["line_id"], r["canonical_slug"]) for r in output_rows}

    # ── PASS 2a: SCRYFALL-INFERENCE (legacy; restricted to card's Scryfall tag canonicals) ──
    # For each multi-line card, score each natural line against the card's tag canonicals
    # (intersected with the trusted set). Top-K above threshold emit as scryfall-inference.
    canonical_to_oids: dict[str, set[str]] = {}
    for canonical, group in assignments.groupby("canonical_slug"):
        canonical_to_oids[canonical] = set(group["oracle_id"].drop_duplicates().tolist())
    oid_to_canonicals: dict[str, list[str]] = {}
    for canonical, oids in canonical_to_oids.items():
        for oid in oids:
            oid_to_canonicals.setdefault(oid, []).append(canonical)

    n_2a_emitted = n_2a_skipped_no_anchor = n_2a_below_threshold = 0
    trusted_set = set(trusted_canonicals)

    for oid, tag_canonicals in oid_to_canonicals.items():
        if _has_single_natural_line(oid):
            continue  # handled by anchor in Pass 1
        usable_tags = [t for t in tag_canonicals if t in trusted_set]
        if not usable_tags:
            n_2a_skipped_no_anchor += 1
            continue
        line_ids = oid_to_line_ids.get(oid, [])
        for lid in line_ids:
            li = line_id_to_idx.get(lid)
            if li is None:
                continue
            vec = all_mat[li]
            scores = [(t, float(vec @ anchor_by_canonical[t])) for t in usable_tags]
            scores.sort(key=lambda kv: -kv[1])
            for t, s in scores[:top_k]:
                if s < threshold:
                    n_2a_below_threshold += 1
                    continue
                if (lid, t) in already_emitted:
                    continue
                output_rows.append({
                    "line_id": lid,
                    "canonical_slug": t,
                    "confidence": s,
                    "source": _sources.SCRYFALL_INFERENCE,
                })
                already_emitted.add((lid, t))
                n_2a_emitted += 1
    logger.info(
        "PASS 2a (scryfall-inference, top-K=%d): %d emitted, %d cards skipped (no usable anchor in tag set), "
        "%d (line, canonical) below threshold τ=%.2f",
        top_k, n_2a_emitted, n_2a_skipped_no_anchor, n_2a_below_threshold, threshold,
    )

    # ── PASS 2b: EMBEDDING-INFERENCE (global; no card-tag restriction) ──
    # For every natural line, score against EVERY trusted canonical anchor. Keep top-K above
    # threshold. This is what catches leveler-style bleed: the line "{3}{U}: Level 3" scores low
    # against evasion regardless of the card's Scryfall tags; embedding-inference picks whatever
    # canonicals it's actually semantically close to (e.g. mana-sink, activated-ability),
    # leaving evasion out of its top-K.
    natural_indices = [
        line_id_to_idx[lid] for lid in natural_line_ids_ordered if lid in line_id_to_idx
    ]
    aligned_line_ids = [lid for lid in natural_line_ids_ordered if lid in line_id_to_idx]
    natural_mat_subset = all_mat[natural_indices]  # (N_natural, D)

    # Single matmul gives every (line, canonical) cosine score.
    scores_matrix = natural_mat_subset @ trusted_anchor_matrix.T  # (N_natural, K_trusted)
    k_effective = min(top_k, scores_matrix.shape[1] - 1)
    n_2b_emitted = n_2b_below_threshold = 0
    for i, lid in enumerate(aligned_line_ids):
        row_scores = scores_matrix[i]
        # Partition for the top-K largest scores, then sort just those.
        top_idxs = np.argpartition(-row_scores, k_effective)[: k_effective + 1]
        top_idxs = top_idxs[np.argsort(-row_scores[top_idxs])][:top_k]
        for k_idx in top_idxs:
            s = float(row_scores[k_idx])
            if s < threshold:
                n_2b_below_threshold += 1
                continue
            c = trusted_canonicals[k_idx]
            if (lid, c) in already_emitted:
                continue
            output_rows.append({
                "line_id": lid,
                "canonical_slug": c,
                "confidence": s,
                "source": _sources.EMBEDDING_INFERENCE,
            })
            already_emitted.add((lid, c))
            n_2b_emitted += 1
    logger.info(
        "PASS 2b (embedding-inference, top-K=%d): %d emitted, %d (line, canonical) below threshold τ=%.2f",
        top_k, n_2b_emitted, n_2b_below_threshold, threshold,
    )

    out = pd.DataFrame(output_rows)
    if len(out) == 0:
        raise ValueError("No line assignments produced — pipeline upstream is broken")

    # Convert line_id strings → 16-byte UUID binary to satisfy the C# Guid schema.
    out["line_id"] = out["line_id"].map(lambda s: uuid.UUID(s).bytes)

    logger.info(
        "Emitted %d total assignments across %d canonicals. By source: %s",
        len(out), out["canonical_slug"].nunique(),
        out["source"].value_counts().to_dict(),
    )
    return out


@step(
    inputs=[
        "ScryfallTagAssignments",
        "OracleLines",
        "EncodedTexts",
        "ScryfallTagCuration",
        "KeywordVocabulary",
        "ExemplarTagCentroids",
        "TagLabelingConfig",
    ],
    outputs="OracleLineCanonicalAssignments",
    cacheable=True,
)
def build_canonical_line_assignments(
    assignments: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    curation: pd.DataFrame,
    vocabulary: dict,
    exemplars: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _build_impl(assignments, lines, encoded, curation, vocabulary, exemplars, config)
