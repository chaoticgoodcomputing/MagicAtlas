"""Vocabulary of `source` values for OracleLineCanonicalAssignment.

Mirror of the C# `TagAttributionSource` static class. Kept as string constants (not a Python
Enum) so values flow through Flowthru's JSON/Arrow marshaller unchanged. Reference these
constants instead of inlining string literals.

Confidence semantics by source:
- ANCHOR (1.0): single-line card whose Scryfall tag uniquely identifies the line.
- PATTERN (1.0): deterministic regex match against the line text (Pass 0).
- SCRYFALL_INFERENCE (cosine): inference restricted to canonicals from the card's Scryfall tags.
- EMBEDDING_INFERENCE (cosine): inference against ALL canonical anchors, top-K cap, no card-tag
  restriction.
- FALLBACK_ALL (0.5): tagged card had no usable anchor; all lines were attributed.
"""
from __future__ import annotations

ANCHOR = "anchor"
PATTERN = "pattern"
SCRYFALL_INFERENCE = "scryfall-inference"
EMBEDDING_INFERENCE = "embedding-inference"
FALLBACK_ALL = "fallback-all"

ALL: tuple[str, ...] = (
    ANCHOR,
    PATTERN,
    SCRYFALL_INFERENCE,
    EMBEDDING_INFERENCE,
    FALLBACK_ALL,
)
