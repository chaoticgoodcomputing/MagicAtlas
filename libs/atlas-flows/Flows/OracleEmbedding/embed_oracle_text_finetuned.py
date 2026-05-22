"""Fine-tuned-model counterpart to `embed_oracle_text.py`. Identical logic — only the catalog
item bindings differ. Split into its own file so Flowthru's Python source generator picks up
the `cacheable=True` opt-in.
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.OracleEmbedding.embed_oracle_text import _embed_impl


@step(
    inputs=["OracleLines", "FineTunedEmbeddingModel", "OracleEmbeddingConfig"],
    outputs="FineTunedEncodedTexts",
    cacheable=True,
)
def embed_oracle_text_finetuned(
    lines: pd.DataFrame, model_ref: dict, config: dict
) -> pd.DataFrame:
    return _embed_impl(lines, model_ref, config)
