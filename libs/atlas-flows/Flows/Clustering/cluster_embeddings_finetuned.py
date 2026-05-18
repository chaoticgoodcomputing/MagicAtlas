"""Fine-tuned-model counterpart to `cluster_embeddings.py`. Identical logic — only the catalog
item bindings differ. Split into its own file so Flowthru's Python source generator picks up
the `cacheable=True` opt-in (the generator only registers the first @step per .py file in
0.18.2).
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.Clustering.cluster_embeddings import _cluster_impl


@step(
    inputs=["FineTunedClusteringEmbeddings", "ClusteringConfig"],
    outputs="FineTunedClusterAssignments",
    cacheable=True,
)
def cluster_embeddings_finetuned(embeddings: pd.DataFrame, config: dict) -> pd.DataFrame:
    return _cluster_impl(embeddings, config)
