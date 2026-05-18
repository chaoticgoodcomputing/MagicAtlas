"""Fine-tuned-model counterpart to `reduce_to_2d.py`. Identical logic — only the catalog item
bindings differ. Split into its own file so Flowthru's Python source generator picks up the
`cacheable=True` opt-in (the generator only registers the first @step per .py file in 0.18.2).
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.OracleEmbedding.reduce_to_2d import _reduce_to_2d_impl


@step(
    inputs=["FineTunedBertEmbeddings", "OracleEmbeddingConfig"],
    outputs="FineTunedAtlasPoints",
    cacheable=True,
)
def reduce_to_2d_finetuned(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _reduce_to_2d_impl(embeddings, config)
