"""Fine-tuned-model counterpart to `generate_ctfidf_labels.py`. Identical logic — only the
catalog item bindings differ. Split into its own file so Flowthru's Python source generator
picks up the `cacheable=True` opt-in (the generator only registers the first @step per .py
file in 0.18.2).
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.Clustering.generate_ctfidf_labels import _safe_generate


@step(
    inputs=["FineTunedClusterAssignments", "OracleInputs", "ClusteringConfig"],
    outputs="FineTunedClusterLabels",
    cacheable=True,
)
def generate_ctfidf_labels_finetuned(
    assignments: pd.DataFrame, fragments: pd.DataFrame, config: dict
) -> pd.DataFrame:
    return _safe_generate(assignments, fragments, config)
