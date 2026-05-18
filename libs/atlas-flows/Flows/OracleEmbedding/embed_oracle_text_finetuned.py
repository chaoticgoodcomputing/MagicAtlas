"""Fine-tuned-model counterpart to `embed_oracle_text.py`. Identical logic — the only
differences are the catalog item bindings on the @step decorator. Split into its own file so
Flowthru's Python source generator picks up the `cacheable=True` opt-in (the generator only
registers the first @step per .py file in 0.18.2).
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.OracleEmbedding.embed_oracle_text import _embed_impl


@step(
    inputs=["OracleInputs", "FineTunedEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="FineTunedBertEmbeddings",
    cacheable=True,
)
def embed_oracle_text_finetuned(
    fragments: pd.DataFrame, model_ref: dict, config: dict
) -> pd.DataFrame:
    return _embed_impl(fragments, model_ref, config)
