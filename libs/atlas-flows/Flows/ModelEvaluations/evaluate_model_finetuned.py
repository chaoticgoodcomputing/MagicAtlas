"""Fine-tuned-model counterpart to `evaluate_model.py`. Identical logic — only the catalog
item bindings and the variant-label field differ. Split into its own file so Flowthru's Python
source generator picks up the `cacheable=True` opt-in.
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.ModelEvaluations.evaluate_model import _evaluate_impl


@step(
    inputs=[
        "FineTunedClusteringEmbeddings",
        "OracleLines",
        "ModelEvaluationAssertions",
        "ModelEvaluationsConfig",
    ],
    outputs="FineTunedModelEvaluation",
    cacheable=True,
)
def evaluate_finetuned(
    embeddings: pd.DataFrame,
    lines: pd.DataFrame,
    assertions: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _evaluate_impl(embeddings, lines, assertions, config["FineTunedVariantLabel"])
