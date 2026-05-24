"""SUPERVISED UMAP-reduce of encoded oracle texts to 5D — the structured intermediate that
feeds the downstream unsupervised 2D atlas projection.

This is where canonical-label supervision LIVES post-restructure. The architecture is:

    HD (768d nomic)
        │  reduce_to_five_d  (SUPERVISED, categorical, target_weight)
        ▼
    5D  (ClusteringEmbeddings)
        │  reduce_to_2d      (UNSUPERVISED, pure topology)
        ▼
    2D  (AtlasPoints)

Why supervision moved here: 5 dimensions have room to separate ~280 canonical leaves; 2 do not.
By the time we get to 2D, the canonical structure is already shaped in 5D — the 2D step's only
job is to make that shape visible without re-fighting the topology/supervision trade-off.

Inputs:
    lines:    OracleLines [line_id, card_id, text].
    encoded:  EncodedTexts [text, embedding] — encoder cache, one row per unique text.
    primary:  LinePrimaryCanonicals [line_id, canonical_slug, ...] — supervised target. Lines
              without a row get y = -1 (UMAP unlabeled).
    config:   ClusteringConfig — uses Umap5DNNeighbors, Umap5DMinDist, Umap5DSupervisionWeight,
              UmapJitterSigma.

Output: ClusteringEmbeddings [line_id, vector] — 5D float32 coords packed as little-endian bytes
        (20 bytes per row). See ClusteringEmbedding.cs.

Verified that cuml-cu13 26.4 honors `target_metric='categorical'` + `target_weight`: passing
y= produces output that differs distinctly from None and from random-y, with centroid separation
matching y's structure. Both supervised and unsupervised modes run on GPU.
"""
from __future__ import annotations

import logging
import uuid

import numpy as np
import pandas as pd
from flowthru import step

from Flows.OracleEmbedding.reduce_to_2d import _broadcast_and_jitter

logger = logging.getLogger(__name__)

_N_COMPONENTS = 5
_METRIC = "cosine"
_RANDOM_STATE = 42


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


def _make_umap_reducer(
    n_neighbors: int, min_dist: float, supervised: bool, supervision_weight: float,
):
    """Returns (reducer, backend_name). cuML supports supervised UMAP via target_metric +
    target_weight (verified on cuml-cu13 26.4: passing y= produces distinctly different output
    from None and is centroid-separation-correlated with y's structure). Prefer cuML always."""
    try:
        from cuml.manifold import UMAP as CumlUMAP

        kwargs = dict(
            n_components=_N_COMPONENTS,
            n_neighbors=n_neighbors,
            min_dist=min_dist,
            metric=_METRIC,
            random_state=_RANDOM_STATE,
        )
        if supervised:
            kwargs.update(target_metric="categorical", target_weight=supervision_weight)
        return CumlUMAP(**kwargs), "cuml"
    except ImportError:
        pass

    import umap

    kwargs = dict(
        n_components=_N_COMPONENTS,
        n_neighbors=n_neighbors,
        min_dist=min_dist,
        metric=_METRIC,
        random_state=_RANDOM_STATE,
    )
    if supervised:
        kwargs.update(target_metric="categorical", target_weight=supervision_weight)
    return umap.UMAP(**kwargs), "umap-learn"


def _build_supervision_labels(
    line_ids: pd.Series, primary: pd.DataFrame
) -> tuple[np.ndarray, int]:
    """Integer label per line aligned with line_ids; -1 for unlabeled (UMAP convention)."""
    pri = primary.copy()
    pri["line_id"] = pri["line_id"].map(_normalize_guid)
    line_id_strs = pd.Series(line_ids).map(_normalize_guid).tolist()

    slug_by_line = dict(zip(pri["line_id"], pri["canonical_slug"]))
    seen: dict[str, int] = {}
    y = np.full(len(line_id_strs), -1, dtype=np.int32)
    for i, lid in enumerate(line_id_strs):
        slug = slug_by_line.get(lid)
        if slug is None or (isinstance(slug, float) and pd.isna(slug)):
            continue
        slug = str(slug)
        idx = seen.get(slug)
        if idx is None:
            idx = len(seen)
            seen[slug] = idx
        y[i] = idx
    return y, len(seen)


def _reduce_to_five_d_impl(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    n_neighbors = int(config["Umap5DNNeighbors"])
    min_dist = float(config["Umap5DMinDist"])
    jitter_sigma = float(config["UmapJitterSigma"])
    supervision_enabled = bool(config.get("Umap5DSupervised", True))
    supervision_weight = float(config.get("Umap5DSupervisionWeight", 0.7))

    line_ids, vectors = _broadcast_and_jitter(lines, encoded, jitter_sigma)
    logger.info(
        "Input: %d lines × %d unique texts → %d vectors of dim %d (jitter_sigma=%g)",
        len(lines), len(encoded), *vectors.shape, jitter_sigma,
    )

    if supervision_enabled:
        y, n_classes = _build_supervision_labels(line_ids, primary)
        n_labeled = int((y != -1).sum())
        logger.info(
            "Supervised HD→5D UMAP: %d / %d lines labeled across %d canonicals "
            "(unlabeled lines project without supervision)",
            n_labeled, len(y), n_classes,
        )
        supervised = n_labeled > 0
    else:
        y = None
        supervised = False
        logger.info(
            "Umap5DSupervised=false — running unsupervised HD→5D UMAP "
            "(canonical labels ignored even though %d are available)",
            len(primary),
        )

    reducer, backend = _make_umap_reducer(
        n_neighbors=n_neighbors, min_dist=min_dist,
        supervised=supervised, supervision_weight=supervision_weight,
    )
    logger.info(
        "Running %s UMAP → 5D via %s (n_neighbors=%d, min_dist=%g, %s, supervision_weight=%g)...",
        "supervised" if supervised else "unsupervised",
        backend, n_neighbors, min_dist, _METRIC, supervision_weight,
    )
    reduced = reducer.fit_transform(vectors, y=y if supervised else None)
    if hasattr(reduced, "get"):
        reduced = reduced.get()
    reduced = np.asarray(reduced, dtype=np.float32)
    logger.info("Reduced shape: %s (dtype %s)", reduced.shape, reduced.dtype)

    # Pack each row's 5 float32s into a little-endian byte blob (20 bytes per row). Same
    # rationale as EncodedText.Embedding — Flowthru's parquet serializer needs IFlatSchema,
    # and byte[] is the only flat-classified array form.
    blobs = [vec.astype("<f4").tobytes() for vec in reduced]
    return pd.DataFrame({
        "line_id": line_ids.values,
        "vector": blobs,
    })


@step(
    inputs=["OracleLines", "EncodedTexts", "LinePrimaryCanonicals", "ClusteringConfig"],
    outputs="ClusteringEmbeddings",
    cacheable=True,
)
def reduce_to_five_d(
    lines: pd.DataFrame,
    encoded: pd.DataFrame,
    primary: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _reduce_to_five_d_impl(lines, encoded, primary, config)
