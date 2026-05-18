"""Score an embedding model against declarative pairwise-distance assertions in the 5D UMAP space.

Inputs:
    embeddings: DataFrame [point_id, vector] — 5D byte-packed clustering embeddings.
    oracle:     DataFrame [point_id, card_id, text, text_type] — oracle fragments.
    assertions: DataFrame [name, group_a_pattern, group_b_pattern, expect, baseline_group_pattern].

Output: DataFrame of ModelEvaluationResult rows.

Centroid distance metric is squared L2 (same metric HDBSCAN saw), so eval numbers stay coupled
to what the clusterer actually used. Pattern matching is case-insensitive `re.search()` against
each fragment's text — use word boundaries (`\\b`) in patterns to avoid substring false positives.

Two thin entry points share the same impl: `evaluate_default` for the default-variant catalog
items, `evaluate_finetuned` for the fine-tuned-variant ones. The model variant label is the
only differentiator and is stamped into each output row's `model_variant` field.
"""
from __future__ import annotations

import logging
import re

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


def _evaluate_impl(
    embeddings: pd.DataFrame,
    oracle: pd.DataFrame,
    assertions: pd.DataFrame,
    variant_label: str,
) -> pd.DataFrame:
    logger.info(
        "Inputs: %d embedded fragments, %d oracle rows, %d assertions; variant=%s",
        len(embeddings), len(oracle), len(assertions), variant_label,
    )

    vectors = np.stack([np.frombuffer(b, dtype="<f4") for b in embeddings["vector"]])
    idx_by_point = {pid: i for i, pid in enumerate(embeddings["point_id"])}

    oracle = oracle.copy()
    oracle["vec_idx"] = oracle["point_id"].map(idx_by_point)
    missing = oracle["vec_idx"].isna().sum()
    if missing:
        logger.warning("Dropping %d oracle rows with no matching embedding", missing)
    oracle = oracle.dropna(subset=["vec_idx"])
    oracle["vec_idx"] = oracle["vec_idx"].astype(int)

    results: list[dict] = []
    for _, row in assertions.iterrows():
        name = row["name"]
        try:
            pat_a = re.compile(row["group_a_pattern"], re.IGNORECASE)
            pat_b = re.compile(row["group_b_pattern"], re.IGNORECASE)
            pat_base = re.compile(row["baseline_group_pattern"], re.IGNORECASE)
        except re.error as e:
            logger.warning("Skipping assertion %s: invalid regex (%s)", name, e)
            continue

        mask_a = oracle["text"].str.contains(pat_a, na=False, regex=True)
        mask_b = oracle["text"].str.contains(pat_b, na=False, regex=True)
        mask_base = oracle["text"].str.contains(pat_base, na=False, regex=True)

        n_a, n_b, n_base = int(mask_a.sum()), int(mask_b.sum()), int(mask_base.sum())

        if n_a == 0 or n_b == 0 or n_base == 0:
            logger.warning(
                "Assertion %s: empty group (n_a=%d, n_b=%d, n_base=%d) — skipping",
                name, n_a, n_b, n_base,
            )
            continue

        centroid_a = vectors[oracle.loc[mask_a, "vec_idx"].values].mean(axis=0)
        centroid_b = vectors[oracle.loc[mask_b, "vec_idx"].values].mean(axis=0)
        centroid_base = vectors[oracle.loc[mask_base, "vec_idx"].values].mean(axis=0)

        d_ab = float(np.sum((centroid_a - centroid_b) ** 2))
        d_a_base = float(np.sum((centroid_a - centroid_base) ** 2))

        expect = row["expect"]
        if expect == "closer_than":
            passed = d_ab < d_a_base
        elif expect == "farther_than":
            passed = d_ab > d_a_base
        else:
            logger.warning("Assertion %s: unknown expect=%r — failing", name, expect)
            passed = False

        logger.info(
            "%s [%s]: d(A,B)=%.4f vs d(A,baseline)=%.4f expect=%s -> %s (n_a=%d, n_b=%d, n_base=%d)",
            name, variant_label, d_ab, d_a_base, expect,
            "PASS" if passed else "FAIL", n_a, n_b, n_base,
        )

        results.append({
            "model_variant": variant_label,
            "assertion_name": name,
            "distance_a_b": d_ab,
            "distance_a_baseline": d_a_base,
            "pass": passed,
            "n_a": n_a,
            "n_b": n_b,
            "n_baseline": n_base,
        })

    return pd.DataFrame(results)


@step(
    inputs=[
        "ClusteringEmbeddings",
        "OracleInputs",
        "ModelEvaluationAssertions",
        "ModelEvaluationsConfig",
    ],
    outputs="ModelEvaluation",
    cacheable=True,
)
def evaluate_default(
    embeddings: pd.DataFrame,
    oracle: pd.DataFrame,
    assertions: pd.DataFrame,
    config: dict,
) -> pd.DataFrame:
    return _evaluate_impl(embeddings, oracle, assertions, config["DefaultVariantLabel"])


