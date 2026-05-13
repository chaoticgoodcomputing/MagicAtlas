"""c-TF-IDF cluster labeling — the canonical BERTopic-style approach.

Inputs:
    assignments: DataFrame [point_id, cluster_id]
    fragments:   DataFrame [point_id, card_id, text, text_type]
Output:
    DataFrame conforming to the ClusterLabel schema:
        [cluster_id, label, description, keywords, size, source, source_version]

    `keywords` is a JSON-encoded string array, not a native list — see ClusterLabel.cs for the
    reasoning (Flowthru's Arrow marshaller in 0.17.4 only supports scalar property types).

Algorithm (BERTopic-style class-based TF-IDF):
  1. Join points to their fragment texts; group all texts in a cluster into one "class document".
  2. Token-count each class document.
  3. For each (cluster, term), score = tf(term, cluster) * log(avg_word_count / sum_count(term)).
     This penalises words that are common across all clusters (low IDF) and rewards words that
     are densely repeated inside one cluster.
  4. Take the top N terms per cluster as the cluster's keywords; the top-1 plus the next 2 as the
     primary display label.

The noise bucket (cluster_id == -1) gets a fixed sentinel label so reporting can render it as a
grey catch-all without skipping it.
"""
from __future__ import annotations

import json
import logging
from typing import List

import numpy as np
import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

_TOP_KEYWORDS = 8
_LABEL_HEAD = 3
_NOISE_LABEL = "(noise)"


@step(inputs=["ClusterAssignments", "OracleInputs"], outputs="ClusterLabels")
def generate_ctfidf_labels(
    assignments: pd.DataFrame, fragments: pd.DataFrame
) -> pd.DataFrame:
    import sys
    import traceback as _tb

    try:
        return _generate_impl(assignments, fragments)
    except Exception:
        # Flowthru's subprocess executor wraps the worker's traceback in an opaque .NET
        # message ("Exception has been thrown by the target of an invocation"); dump our own
        # traceback to stderr so the real failure shows up in the harness console.
        sys.stderr.write("[generate_ctfidf_labels] ERROR:\n")
        sys.stderr.write(_tb.format_exc())
        sys.stderr.flush()
        raise


def _generate_impl(
    assignments: pd.DataFrame, fragments: pd.DataFrame
) -> pd.DataFrame:
    import sklearn
    from sklearn.feature_extraction.text import CountVectorizer

    merged = assignments.merge(fragments, on="point_id", validate="one_to_one")
    logger.info(
        "Labeling %d clusters across %d points (%d noise)",
        merged["cluster_id"].nunique(),
        len(merged),
        int((merged["cluster_id"] == -1).sum()),
    )

    # Group fragment texts into one class-document per cluster (BERTopic terminology).
    docs_by_cluster = (
        merged.assign(text=merged["text"].fillna("").astype(str))
        .groupby("cluster_id")["text"]
        .apply(lambda s: " ".join(s))
        .reset_index()
    )
    sizes_by_cluster = merged.groupby("cluster_id").size().to_dict()

    # Vectorise as 1-3 grams of mostly-letter tokens. Only English function words are stopworded
    # — c-TF-IDF's IDF term already penalises MTG verbs (e.g. "creature", "spell") that appear
    # uniformly across clusters, so a hand-curated MTG stoplist would risk filtering actual
    # signal (a graveyard-recursion cluster *should* surface "graveyard" highly).
    vectorizer = CountVectorizer(
        ngram_range=(1, 3),
        stop_words=list(_english_stopwords()),
        min_df=2,
        token_pattern=r"(?u)\b[a-zA-Z][a-zA-Z\-']{1,}\b",
    )
    counts = vectorizer.fit_transform(docs_by_cluster["text"])
    vocab = vectorizer.get_feature_names_out()
    logger.info("Vocabulary: %d terms", len(vocab))

    # c-TF-IDF: TF (row-normalised) × IDF computed across cluster-documents (not docs).
    tf = counts.toarray().astype(float)
    row_sums = tf.sum(axis=1, keepdims=True)
    row_sums[row_sums == 0] = 1.0  # guard empty cluster docs
    tf = tf / row_sums

    total_counts_per_term = counts.sum(axis=0).A1  # 1-D ndarray of term totals
    avg_words_per_class = counts.sum(axis=1).mean()
    idf = np.log(np.maximum(avg_words_per_class / np.maximum(total_counts_per_term, 1), 1e-9))

    ctfidf = tf * idf

    rows = []
    for idx, cluster_id in enumerate(docs_by_cluster["cluster_id"].tolist()):
        cluster_id = int(cluster_id)
        size = int(sizes_by_cluster.get(cluster_id, 0))
        if cluster_id == -1:
            rows.append(
                {
                    "cluster_id": -1,
                    "label": _NOISE_LABEL,
                    "description": None,
                    "keywords": json.dumps([]),
                    "size": size,
                    "source": "c-tf-idf",
                    "source_version": f"sklearn-{sklearn.__version__}",
                }
            )
            continue

        scores = ctfidf[idx]
        # argsort ascending → take the last _TOP_KEYWORDS for descending.
        ranked = np.argsort(scores)[::-1]
        keywords: List[str] = []
        for term_idx in ranked:
            if scores[term_idx] <= 0:
                break
            keywords.append(str(vocab[term_idx]))
            if len(keywords) >= _TOP_KEYWORDS:
                break

        label = ", ".join(keywords[:_LABEL_HEAD]) if keywords else "(no terms)"
        rows.append(
            {
                "cluster_id": cluster_id,
                "label": label,
                "description": None,
                "keywords": json.dumps(keywords),
                "size": size,
                "source": "c-tf-idf",
                "source_version": f"sklearn-{sklearn.__version__}",
            }
        )

    rows.sort(key=lambda r: (r["cluster_id"] == -1, -r["size"]))
    logger.info("Top 5 cluster labels by size:")
    for r in rows[:5]:
        logger.info("  cluster %d (size %d): %s", r["cluster_id"], r["size"], r["label"])

    df = pd.DataFrame(rows)
    # The c-TF-IDF labeler never populates `description` (every row is None), which causes
    # pyarrow to infer column type `null` rather than nullable string — and C# can't merge
    # `null` into the `string?` schema field. Force the string-extension dtype so Arrow sees a
    # proper nullable string column even when every value is missing.
    df["description"] = df["description"].astype("string")
    return df


def _english_stopwords() -> frozenset:
    # Minimal embedded list — scikit-learn's full English list isn't exposed cleanly across
    # versions, and we want determinism. Captures the function-word noise that dominates
    # uninformative top-terms in MTG ability text.
    return frozenset(
        {
            "a", "an", "the", "and", "or", "but", "if", "then", "of", "on", "in", "to",
            "for", "from", "with", "without", "by", "at", "as", "is", "are", "was",
            "were", "be", "been", "being", "has", "have", "had", "do", "does", "did",
            "this", "that", "these", "those", "it", "its", "they", "them", "their",
            "you", "your", "may", "can", "could", "would", "should", "into", "than",
            "more", "less", "any", "all", "each", "no", "not", "so", "such", "until",
            "when", "while", "where", "who", "whom", "which", "what",
        }
    )


