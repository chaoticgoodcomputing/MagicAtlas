"""Fine-tuned-model counterpart to `build_atlas_plot.py`. Identical logic — only the catalog
item bindings differ. Split into its own file so Flowthru's Python source generator picks up
the `cacheable=True` opt-in.
"""
from __future__ import annotations

import pandas as pd
from flowthru import step

from Flows.Reporting.build_atlas_plot import _build_atlas_plot_impl


@step(
    inputs=[
        "FineTunedAtlasReportingPoints",
        "OracleLines",
        "AtlasCardHoverInfo",
        "FineTunedClusterAssignments",
        "FineTunedClusterLabels",
        "ReportingConfig",
    ],
    outputs="FineTunedAtlasPlotHtml",
    cacheable=True,
)
def build_atlas_plot_finetuned(
    points: pd.DataFrame,
    lines: pd.DataFrame,
    hover: pd.DataFrame,
    assignments: pd.DataFrame,
    labels: pd.DataFrame,
    config: dict,
) -> str:
    return _build_atlas_plot_impl(points, lines, hover, assignments, labels, config)
