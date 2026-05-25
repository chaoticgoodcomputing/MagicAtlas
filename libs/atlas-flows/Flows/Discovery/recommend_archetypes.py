"""Discover candidate new archetypes via HDBSCAN clustering on the unsupervised 5D embedding.

For each cluster, compute:
  - Cohesion (mean cosine of members to HD centroid)
  - c-TF-IDF distinctive tokens
  - Closest existing-archetype match by HD-centroid cosine
  - Medoid line (most-central concrete example)
  - 10 sample lines (5 closest to centroid + 5 random) ready as seed prototypes
  - Verdict: NEW / REFINE / MERGE / COVERED

The output drives the discover→curate→validate loop: user reviews recommendations via the
review script, copies seed prototypes into canonical-archetypes.json for NEW/REFINE clusters,
re-runs the pipeline, and re-discovers in the next iteration.

Inputs:
    five_d:              ClusteringEmbeddings (5D, MUST be unsupervised — runs honest clustering).
    lines:               OracleLines (line_id, card_id, text).
    encoded:             EncodedTexts (text, embedding) — HD vectors for cluster member lines
                         (used to compute per-cluster centroids and cohesion).
    encoded_prototypes:  EncodedPrototypes (text, embedding) — HD vectors for archetype
                         prototype strings, used to build the existing-archetype centroids that
                         each cluster is compared against for the NEW/REFINE/MERGE/SPLIT verdict.
                         Split from EncodedTexts so editing canonical-archetypes.json doesn't
                         invalidate the ~30k-row line cache.
    archetypes:          CanonicalArchetypes (slug, prototypes) — for coverage analysis.
    config:              ClusteringConfig — uses HdbscanMinClusterSize, HdbscanMinSamples.
"""
from __future__ import annotations

import logging
import re
import uuid

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_RNG_SEED = 42
_N_SAMPLE_LINES = 10           # 5 centroid-closest + 5 random
_N_CTFIDF_TOKENS = 7
_VERDICT_NEW_THRESHOLD = 0.65
_VERDICT_REFINE_THRESHOLD = 0.85
# SPLIT detection thresholds.
# - SIBLINGS_MAX_PAIR_COSINE: sibling clusters claiming the same archetype must be mutually
#   distinct (centroid cosine BELOW this). At 0.85, we avoid spurious SPLITs from
#   near-duplicate clusters (HDBSCAN granularity noise).
# - SIBLINGS_MIN_COHESION_GAP: each sibling's cohesion must EXCEED its match to the shared
#   archetype by at least this much — same condition as REFINE applied per-sibling.
_SPLIT_SIBLINGS_MAX_PAIR_COSINE = 0.85
_SPLIT_MIN_COHESION_GAP = 0.05


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


def _decode_blobs(blobs: list[bytes], dtype: str) -> np.ndarray:
    if not blobs:
        return np.zeros((0, 0), dtype=np.float32)
    dim = len(np.frombuffer(blobs[0], dtype=dtype))
    mat = np.empty((len(blobs), dim), dtype=np.float32)
    for i, b in enumerate(blobs):
        mat[i] = np.frombuffer(b, dtype=dtype).astype(np.float32)
    return mat


def _unit_norm_rows(mat: np.ndarray) -> np.ndarray:
    norms = np.linalg.norm(mat, axis=1, keepdims=True)
    norms[norms == 0] = 1.0
    return mat / norms


def _slugify(token: str) -> str:
    """kebab-case from a single token (placeholder slug)."""
    s = re.sub(r"[^a-z0-9]+", "-", token.lower()).strip("-")
    return s or "cluster"


def _build_archetype_centroids(
    archetypes: pd.DataFrame, encoded_prototypes: pd.DataFrame,
) -> tuple[list[str], np.ndarray]:
    """HD prototype centroid per archetype, for coverage analysis. Reads from the prototype-only
    encoder cache (kept separate from EncodedTexts so archetype edits stay cheap)."""
    text_to_emb = dict(
        zip(encoded_prototypes["text"].tolist(), encoded_prototypes["embedding"].tolist())
    )
    slugs: list[str] = []
    centroids: list[np.ndarray] = []
    for _, row in archetypes.iterrows():
        slug = str(row["slug"])
        protos = row.get("prototypes")
        if protos is None or (isinstance(protos, float) and pd.isna(protos)):
            continue
        if hasattr(protos, "__len__") and len(protos) == 0:
            continue
        blobs = [text_to_emb.get(str(p)) for p in protos]
        blobs = [b for b in blobs if b is not None]
        if not blobs:
            continue
        proto_mat = _unit_norm_rows(_decode_blobs(blobs, "<f2"))
        centroid = proto_mat.mean(axis=0)
        n = np.linalg.norm(centroid)
        if n > 0:
            centroid /= n
        slugs.append(slug)
        centroids.append(centroid)
    if not slugs:
        raise RuntimeError("No archetype centroids — empty CanonicalArchetypes.prototypes?")
    return slugs, np.stack(centroids)


def _run_hdbscan(vectors: np.ndarray, min_cluster_size: int, min_samples: int) -> np.ndarray:
    """Returns cluster_id per row; -1 for HDBSCAN noise. Tries cuML, falls back to hdbscan."""
    try:
        from cuml.cluster.hdbscan import HDBSCAN as CumlHDBSCAN
        labels = CumlHDBSCAN(
            min_cluster_size=min_cluster_size,
            min_samples=min_samples,
        ).fit_predict(vectors)
        if hasattr(labels, "get"):
            labels = labels.get()
        logger.info("HDBSCAN backend: cuml")
        return np.asarray(labels, dtype=np.int64)
    except ImportError:
        from hdbscan import HDBSCAN
        labels = HDBSCAN(
            min_cluster_size=min_cluster_size,
            min_samples=min_samples,
            metric="euclidean",
        ).fit_predict(vectors)
        logger.info("HDBSCAN backend: hdbscan (CPU)")
        return np.asarray(labels, dtype=np.int64)


def _cluster_ctfidf(
    cluster_texts: list[list[str]], top_k: int = _N_CTFIDF_TOKENS,
) -> list[list[str]]:
    """Class-based TF-IDF (BERTopic-style): each cluster's pooled text vs the corpus average.
    Returns top-K tokens per cluster, in descending importance."""
    from sklearn.feature_extraction.text import CountVectorizer

    pooled = [" ".join(ts) for ts in cluster_texts]
    if not any(p.strip() for p in pooled):
        return [[] for _ in cluster_texts]

    cv = CountVectorizer(
        ngram_range=(1, 2),
        min_df=2,
        stop_words="english",
        token_pattern=r"(?u)\b[a-zA-Z][a-zA-Z]{2,}\b",
    )
    try:
        counts = cv.fit_transform(pooled)
    except ValueError:
        # Corpus too small or all-stopwords after filtering.
        return [[] for _ in cluster_texts]
    vocab = np.array(cv.get_feature_names_out())

    # c-TF-IDF: tf normalized per cluster, idf = log(avg-tf-per-cluster / cluster-tf)
    counts_arr = counts.toarray().astype(np.float32)
    n_clusters, _ = counts_arr.shape
    cluster_sums = counts_arr.sum(axis=1, keepdims=True)
    cluster_sums[cluster_sums == 0] = 1.0
    tf = counts_arr / cluster_sums
    avg_per_word = counts_arr.mean(axis=0)
    avg_per_word[avg_per_word == 0] = 1.0
    idf = np.log(1.0 + (counts_arr.sum(axis=0) / (n_clusters * avg_per_word + 1e-9)))
    ctfidf = tf * idf[None, :]

    out: list[list[str]] = []
    for i in range(n_clusters):
        if cluster_sums[i, 0] <= 1.0:
            out.append([])
            continue
        top_idx = np.argsort(-ctfidf[i])[:top_k]
        out.append([str(v) for v in vocab[top_idx]])
    return out


def _ring_vs_core_tokens(
    ring_texts: list[str], core_texts: list[str], top_k: int = _N_CTFIDF_TOKENS,
) -> list[str]:
    """Tokens overrepresented in `ring_texts` relative to `core_texts`. Diagnostic for false
    positives: outer rings of a cluster often pick up tokens that the cluster's own core (top
    25% by cosine to centroid) doesn't carry — those are the "what's this cluster picking up
    that it ought not to" signals.

    Score is ring_relative_freq / (core_relative_freq + ε), gated by an absolute presence
    floor in the ring (≥2 occurrences) so single-line oddities don't pollute the top-K."""
    from sklearn.feature_extraction.text import CountVectorizer

    if not ring_texts or not core_texts:
        return []
    ring_doc = " ".join(ring_texts).strip()
    core_doc = " ".join(core_texts).strip()
    if not ring_doc or not core_doc:
        return []

    cv = CountVectorizer(
        ngram_range=(1, 2),
        min_df=1,
        stop_words="english",
        token_pattern=r"(?u)\b[a-zA-Z][a-zA-Z]{2,}\b",
    )
    try:
        counts = cv.fit_transform([ring_doc, core_doc]).toarray()
    except ValueError:
        return []
    vocab = cv.get_feature_names_out()
    ring_total = max(counts[0].sum(), 1)
    core_total = max(counts[1].sum(), 1)
    ring_tf = counts[0] / ring_total
    core_tf = counts[1] / core_total
    boost = ring_tf / (core_tf + 1e-3)
    # Require ≥2 occurrences in the ring; otherwise a single freak hit dominates the boost.
    presence_mask = counts[0] >= 2
    if not presence_mask.any():
        return []
    scored = np.where(presence_mask, boost, -np.inf)
    top_idx = np.argsort(-scored)[:top_k]
    return [str(vocab[i]) for i in top_idx if np.isfinite(scored[i])]


def _classify_verdict(
    cluster_hd_centroid: np.ndarray, archetype_centroid_mat: np.ndarray,
    cluster_cohesion: float,
) -> tuple[str, str, float]:
    """Returns (verdict, closest_slug_index, closest_cosine)."""
    cosines = archetype_centroid_mat @ cluster_hd_centroid
    closest_idx = int(np.argmax(cosines))
    closest_cos = float(cosines[closest_idx])

    if closest_cos < _VERDICT_NEW_THRESHOLD:
        verdict = "NEW"
    elif closest_cos < _VERDICT_REFINE_THRESHOLD:
        # Cluster cohesion vs. coverage: if internal tightness exceeds match strength,
        # the cluster is more coherent than its closest archetype — a refinement opportunity.
        verdict = "REFINE" if cluster_cohesion > closest_cos else "COVERED"
    else:
        # Well-covered. Check if the cluster spans multiple archetypes equally — MERGE candidate.
        top2 = np.partition(-cosines, 2)[:2] * -1.0  # top 2 cosines
        if len(top2) == 2 and abs(top2[0] - top2[1]) < 0.03:
            verdict = "MERGE"
        else:
            verdict = "COVERED"
    return verdict, closest_idx, closest_cos


def _recommend_impl(
    five_d: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    encoded_prototypes: pd.DataFrame,
    archetypes: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    min_cluster_size = int(config["HdbscanMinClusterSize"])
    min_samples = int(config["HdbscanMinSamples"])
    rng = np.random.default_rng(_RNG_SEED)

    # ── Decode 5D for clustering + HD for centroid math ──
    five_d = five_d.copy()
    five_d["line_id"] = five_d["line_id"].map(_normalize_guid)
    lines = lines.copy()
    lines["line_id"] = lines["line_id"].map(_normalize_guid)

    five_d_vec = np.stack(
        [np.frombuffer(b, dtype="<f4") for b in five_d["vector"]]
    ).astype(np.float32)
    logger.info("Clustering input: %d × %dD vectors", *five_d_vec.shape)

    # Join HD embeddings via the text column on lines.
    merged = five_d[["line_id"]].merge(
        lines[["line_id", "card_id", "text"]], on="line_id", how="inner", validate="one_to_one",
    ).merge(encoded[["text", "embedding"]], on="text", how="left", validate="many_to_one")
    if merged["embedding"].isna().any():
        n = int(merged["embedding"].isna().sum())
        raise RuntimeError(f"{n} lines have no HD embedding (encoder cache out of sync)")
    hd_vec = _unit_norm_rows(_decode_blobs(merged["embedding"].tolist(), "<f2"))

    # ── HDBSCAN on the 5D space ──
    labels = _run_hdbscan(five_d_vec, min_cluster_size, min_samples)
    unique_labels = sorted(set(labels) - {-1})
    n_noise = int((labels == -1).sum())
    logger.info(
        "HDBSCAN: %d clusters + %d noise points (%.0f%% of input)",
        len(unique_labels), n_noise, 100 * n_noise / len(labels),
    )
    if not unique_labels:
        raise RuntimeError("HDBSCAN found 0 clusters at the configured parameters")

    # ── Archetype centroids in HD for coverage scoring ──
    archetype_slugs, archetype_mat = _build_archetype_centroids(archetypes, encoded_prototypes)
    logger.info("Coverage baseline: %d archetype centroids", len(archetype_slugs))

    # ── Pool cluster texts for c-TF-IDF ──
    texts = merged["text"].astype(str).tolist()
    cluster_texts: list[list[str]] = [[] for _ in unique_labels]
    label_to_pos = {lbl: i for i, lbl in enumerate(unique_labels)}
    for i, lbl in enumerate(labels):
        if lbl == -1:
            continue
        cluster_texts[label_to_pos[lbl]].append(texts[i])
    ctfidf_per_cluster = _cluster_ctfidf(cluster_texts)

    # ── Per-cluster characterization ──
    rows: list[dict] = []
    # Keep HD centroids per cluster for the SPLIT post-pass (pairwise centroid cosines
    # between sibling clusters sharing the same closest_archetype).
    cluster_hd_centroids: dict[int, np.ndarray] = {}
    for pos, cluster_id in enumerate(unique_labels):
        member_mask = labels == cluster_id
        member_idxs = np.where(member_mask)[0]
        if len(member_idxs) == 0:
            continue

        member_hd = hd_vec[member_idxs]
        hd_centroid = member_hd.mean(axis=0)
        n = np.linalg.norm(hd_centroid)
        if n > 0:
            hd_centroid /= n

        # Cohesion = mean cosine to centroid (in HD, since we want semantic tightness)
        member_cos = member_hd @ hd_centroid
        cohesion = float(member_cos.mean())

        # Medoid: row with smallest sum of pairwise distances ~= row closest to centroid
        # (using cosine here since vectors are unit-normed).
        medoid_local = int(np.argmax(member_cos))
        medoid_global = int(member_idxs[medoid_local])
        medoid_text = str(texts[medoid_global])

        # Sample lines: top-5 closest to centroid + 5 random (avoid duplicate samples).
        top_local = np.argsort(-member_cos)[:5]
        rng_pool = [i for i in range(len(member_idxs)) if i not in set(top_local.tolist())]
        random_local = rng.choice(rng_pool, size=min(5, len(rng_pool)), replace=False) if rng_pool else np.array([], dtype=int)
        sample_locals = list(top_local) + list(random_local)
        sample_texts = [str(texts[int(member_idxs[s])]) for s in sample_locals]
        sample_joined = " | ".join(sample_texts)

        # c-TF-IDF tokens (cluster-vs-corpus — the cluster's overall identity)
        tokens = ctfidf_per_cluster[pos]
        tokens_joined = " · ".join(tokens) if tokens else "(no distinctive tokens)"
        suggested_slug = _slugify(tokens[0]) if tokens else f"cluster-{cluster_id}"

        # Ring analysis — concentric bands by distance to centroid. Bands are quartiles of
        # cosine-to-centroid (high cosine = close), so the "core" is the top 25% and the
        # "periphery" is the bottom 25%. Each non-core ring's tokens are computed vs the core,
        # surfacing what the ring picked up that the cluster's identity center doesn't carry —
        # the false-positive diagnostic.
        member_texts = [str(texts[int(member_idxs[j])]) for j in range(len(member_idxs))]
        # Sort indices by cosine descending: position 0 = closest to centroid, last = furthest.
        sort_order = np.argsort(-member_cos)
        n_mem = len(sort_order)
        # 4 quartile bands; integer split rounded down. Edge case: very small clusters with
        # n_mem < 4 yield empty outer bands → ring tokens are empty strings, handled below.
        q1 = n_mem // 4
        q2 = n_mem // 2
        q3 = (3 * n_mem) // 4
        band_indices = {
            "p0_25":  sort_order[:q1],          # core (closest to centroid)
            "p25_50": sort_order[q1:q2],
            "p50_75": sort_order[q2:q3],
            "p75_99": sort_order[q3:],          # periphery (furthest from centroid)
        }
        core_texts = [member_texts[int(j)] for j in band_indices["p0_25"]]
        ring_tokens: dict[str, str] = {}
        for band_name in ("p25_50", "p50_75", "p75_99"):
            band_texts = [member_texts[int(j)] for j in band_indices[band_name]]
            ring_toks = _ring_vs_core_tokens(band_texts, core_texts)
            ring_tokens[band_name] = " · ".join(ring_toks) if ring_toks else ""

        # Verdict
        verdict, closest_idx, closest_cos = _classify_verdict(
            hd_centroid, archetype_mat, cohesion,
        )
        closest_slug = archetype_slugs[closest_idx]

        cluster_hd_centroids[int(cluster_id)] = hd_centroid
        rows.append({
            "cluster_id": int(cluster_id),
            "n_members": int(len(member_idxs)),
            "cohesion": cohesion,
            "verdict": verdict,
            "ctfidf_tokens": tokens_joined,
            "closest_archetype_slug": closest_slug,
            "closest_archetype_cosine": closest_cos,
            "suggested_slug": suggested_slug,
            "medoid_line_text": medoid_text,
            "sample_lines_joined": sample_joined,
            "split_sibling_cluster_ids": "",
            "ring_p25_50_tokens": ring_tokens["p25_50"],
            "ring_p50_75_tokens": ring_tokens["p50_75"],
            "ring_p75_99_tokens": ring_tokens["p75_99"],
        })

    # ── SPLIT post-pass ──
    # Group clusters by closest_archetype_slug; for any archetype claimed by ≥2 clusters,
    # check that (a) the clusters are mutually distinct (pairwise centroid cosine below
    # threshold) and (b) each cluster's cohesion exceeds its closest_archetype_cosine by the
    # min gap. If both hold, override each qualifying cluster's verdict to SPLIT and record
    # the sibling cluster ids on each row.
    rows_by_archetype: dict[str, list[dict]] = {}
    for r in rows:
        rows_by_archetype.setdefault(r["closest_archetype_slug"], []).append(r)

    n_split_promoted = 0
    for archetype_slug, sibling_rows in rows_by_archetype.items():
        if len(sibling_rows) < 2:
            continue
        # Filter to siblings whose cohesion-vs-coverage gap qualifies them individually
        qualified = [
            r for r in sibling_rows
            if r["cohesion"] > r["closest_archetype_cosine"] + _SPLIT_MIN_COHESION_GAP
        ]
        if len(qualified) < 2:
            continue
        # Pairwise distinctness check
        ids = [r["cluster_id"] for r in qualified]
        cent_mat = np.stack([cluster_hd_centroids[cid] for cid in ids])
        pair_cos = cent_mat @ cent_mat.T
        iu = np.triu_indices(len(ids), k=1)
        if pair_cos[iu].max() >= _SPLIT_SIBLINGS_MAX_PAIR_COSINE:
            # At least one pair too similar; this looks like HDBSCAN granularity noise rather
            # than genuine sub-region structure. Skip.
            continue
        # Promote all qualified siblings to SPLIT and cross-link them. Rewrite each
        # suggested_slug as `<parent>:<distinctive-term>` so the proposed children inherit
        # the parent archetype's hierarchy (matches the colon-delimited canonical_slug
        # convention — e.g. `removal:creature`, `tribal:elf`).
        id_set = set(ids)
        for r in qualified:
            r["verdict"] = "SPLIT"
            r["split_sibling_cluster_ids"] = "|".join(
                str(other) for other in ids if other != r["cluster_id"]
            )
            child_term = r["suggested_slug"]
            r["suggested_slug"] = f"{archetype_slug}:{child_term}"
            n_split_promoted += 1
        logger.info(
            "SPLIT promoted archetype %r: %d sibling clusters (max pair cosine %.3f)",
            archetype_slug, len(qualified), float(pair_cos[iu].max()),
        )

    out = pd.DataFrame(rows).sort_values(
        ["verdict", "n_members"], ascending=[True, False],
    ).reset_index(drop=True)

    counts = out["verdict"].value_counts().to_dict()
    logger.info(
        "Recommendation verdicts: %s (over %d clusters; %d promoted to SPLIT)",
        counts, len(out), n_split_promoted,
    )
    return out


@step(
    inputs=[
        "ClusteringEmbeddings",
        "OracleLines",
        "EncodedTexts",
        "EncodedPrototypes",
        "CanonicalArchetypes",
        "ClusteringConfig",
    ],
    outputs="ArchetypeRecommendations",
    cacheable=True,
)
def recommend_archetypes(
    five_d: pd.DataFrame,
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    encoded_prototypes: pd.DataFrame,
    archetypes: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _recommend_impl(five_d, lines, encoded, encoded_prototypes, archetypes, config)
