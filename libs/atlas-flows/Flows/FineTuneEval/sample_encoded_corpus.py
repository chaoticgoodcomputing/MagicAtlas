"""Sample EncodedTexts (full ~30k rows) down to a small, fixed-size sample for FineTuneEval's
geometry-tier metrics.

Why this step exists: the geometry-tier metrics (pairwise cosine spread, hubness) need only a
few thousand vectors to be statistically meaningful, but passing the full ~30k-row encoded
caches through the C#↔Python step input boundary blows the System.Text.Json marshalling size
limit. Sampling upstream keeps every per-step input under a few MB.

Same RNG seed used for both fine-tuned and base versions, and the row index in EncodedTexts /
EncodedTextsBase is deterministic (both produced by dedup of OracleLines in the same order),
so the same indices are sampled in each variant — geometry metrics describe the same lines on
both sides.

Inputs:
    encoded: EncodedTexts or EncodedTextsBase [text, embedding].

Output: EncodedText [text, embedding] — N=`_SAMPLE_SIZE` rows.
"""
from __future__ import annotations

import logging

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_SAMPLE_SIZE = 3000
_RNG_SEED = 42


def _sample_impl(encoded: pd.DataFrame) -> pd.DataFrame:
    if len(encoded) <= _SAMPLE_SIZE:
        logger.info("Input has %d rows ≤ sample size %d; passing through unchanged.",
                    len(encoded), _SAMPLE_SIZE)
        return encoded.reset_index(drop=True)

    rng = np.random.default_rng(_RNG_SEED)
    idx = rng.choice(len(encoded), size=_SAMPLE_SIZE, replace=False)
    idx.sort()  # stable order makes diffing the parquet across runs easier
    sample = encoded.iloc[idx].reset_index(drop=True)
    logger.info("Sampled %d rows from %d (seed=%d)", len(sample), len(encoded), _RNG_SEED)
    return sample


@step(
    inputs=["EncodedTexts"],
    outputs="EncodedTextsSampled",
    cacheable=True,
)
def sample_encoded_texts(encoded: pd.DataFrame) -> pd.DataFrame:
    return _sample_impl(encoded)


@step(
    inputs=["EncodedTextsBase"],
    outputs="EncodedTextsBaseSampled",
    cacheable=True,
)
def sample_encoded_texts_base(encoded: pd.DataFrame) -> pd.DataFrame:
    return _sample_impl(encoded)
