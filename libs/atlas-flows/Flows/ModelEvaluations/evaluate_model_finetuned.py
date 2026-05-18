"""Fine-tuned-model counterpart to `evaluate_model.py`. Identical logic — only the catalog
item bindings and the variant-label field differ. Split into its own file so Flowthru's Python
source generator picks up the `cacheable=True` opt-in (the generator only registers the first
@step per .py file in 0.18.2).
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.ModelEvaluations.evaluate_model import _evaluate_impl


@step(
    inputs=[
        "FineTunedClusteringEmbeddings",
        "OracleInputs",
        "ModelEvaluationAssertions",
        "ModelEvaluationsConfig",
    ],
    outputs="FineTunedModelEvaluation",
    cacheable=True,
)
def evaluate_finetuned(
    embeddings: pd.DataFrame,
    oracle: pd.DataFrame,
    assertions: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _evaluate_impl(embeddings, oracle, assertions, config["FineTunedVariantLabel"])
