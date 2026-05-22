"""Fine-tuned-model counterpart to `keyword_cluster_report.py`. Identical logic — only the
catalog item bindings differ. Split into its own file so Flowthru's Python source generator
picks up the `cacheable=True` opt-in.
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.ModelEvaluations.keyword_cluster_report import _build_report_impl


@step(
    inputs=[
        "KeywordVocabulary",
        "OracleLines",
        "FineTunedAtlasPoints",
        "FineTunedClusterAssignments",
        "FineTunedClusterLabels",
        "AtlasCardHoverInfo",
        "ModelEvaluationsConfig",
    ],
    outputs="FineTunedKeywordClusterReport",
    cacheable=True,
)
def keyword_cluster_report_finetuned(
    vocabulary: dict,
    lines: pd.DataFrame,
    atlas_points: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
    hover: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _build_report_impl(vocabulary, lines, atlas_points, assignments, labels, hover)
