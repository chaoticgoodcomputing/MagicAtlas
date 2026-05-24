"""Derives one primary canonical per oracle line from the multi-canonical attributions in
OracleLineCanonicalAssignments. The primary canonical is the highest-confidence assignment
for that line, with source-priority tie-breaking.

Source priority (higher = preferred when confidence ties):
    anchor              4   (single-line card, unambiguous)
    inferred            3   (multi-line card, cosine ≥ threshold)
    fallback-exemplar   2   (curated exemplar fallback, no Scryfall anchor)
    fallback-all        1   (least confident, no anchor at all)

The output also extracts the colon-prefix as `canonical_family` for coarser visual grouping
downstream (everything in `tribal:*` collapses to `tribal`, `removal:*` to `removal`, etc.).

Inputs:
    assignments: OracleLineCanonicalAssignments [line_id, canonical_slug, confidence, source]

Output: DataFrame conforming to LinePrimaryCanonical
        [line_id, canonical_slug, canonical_family, confidence, source]
"""
from __future__ import annotations

import logging

import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_SOURCE_PRIORITY = {
    "anchor": 4,
    "inferred": 3,
    "fallback-exemplar": 2,
    "fallback-all": 1,
}


def _derive_impl(assignments: pd.DataFrame) -> pd.DataFrame:
    df = assignments.copy()
    df["source_priority"] = df["source"].map(_SOURCE_PRIORITY).fillna(0).astype(int)

    # Sort so the first row per line_id is the winner: confidence desc, then source priority desc.
    df = df.sort_values(["line_id", "confidence", "source_priority"], ascending=[True, False, False])
    primary = df.drop_duplicates(subset=["line_id"], keep="first").reset_index(drop=True)

    primary["canonical_family"] = primary["canonical_slug"].map(
        lambda s: str(s).split(":", 1)[0]
    )

    out = primary[["line_id", "canonical_slug", "canonical_family", "confidence", "source"]]
    logger.info(
        "Primary canonical per line: %d lines covered (from %d total attributions). "
        "Source breakdown: %s. Top 5 canonicals by line count: %s",
        len(out), len(df),
        out["source"].value_counts().to_dict(),
        out["canonical_slug"].value_counts().head(5).to_dict(),
    )
    return out


@step(
    inputs=["OracleLineCanonicalAssignments"],
    outputs="LinePrimaryCanonicals",
    cacheable=True,
)
def derive_line_primary_canonical(assignments: pd.DataFrame) -> pd.DataFrame:
    return _derive_impl(assignments)
