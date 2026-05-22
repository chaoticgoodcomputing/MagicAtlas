"""Fine-tuned-model counterpart to `reduce_to_five_d.py`. Identical logic — only the catalog
item bindings differ. Split into its own file so Flowthru's Python source generator picks up
the `cacheable=True` opt-in.
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.Clustering.reduce_to_five_d import _reduce_to_five_d_impl


@step(
    inputs=["OracleLines", "FineTunedEncodedTexts", "ClusteringConfig"],
    outputs="FineTunedClusteringEmbeddings",
    cacheable=True,
)
def reduce_to_five_d_finetuned(
    lines: pd.DataFrame, encoded: pd.DataFrame, config: dict
) -> pd.DataFrame:
    return _reduce_to_five_d_impl(lines, encoded, config)
