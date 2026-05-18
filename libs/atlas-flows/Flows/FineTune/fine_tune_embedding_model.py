"""Fine-tune the embedding model on the MTG-domain training corpus.

Inputs:
    pairs:  DataFrame[anchor, positive, negative?, weight, source] — output of
            build_training_pairs. Currently trains over (anchor, positive) only with
            MultipleNegativesRankingLoss; the small handful of seed triplets is dropped into
            the same dataset as plain pairs (the explicit negative becomes an extra in-batch
            datum). Once the curated-triplet count grows, split into a second dataset and use
            mixed-loss training.
    config: FineTuneConfig record — `FineTuneBaseRepoId`, `FineTuneVariant`, and nested
            `TrainingArgs` (epochs/batch/warmup/lr/logging-steps/fp16). Sourced from
            `Flowthru:Flows:FineTune` in appsettings.json via the harness sidecar.

Output: ModelArtifactRef — { path, repo_id, variant } pointing to the on-disk fine-tuned
        model dir under `_06_Models/`.

Model bytes go directly to disk (`_06_Models/<variant>/`) rather than through Flowthru's
marshaller, sidestepping System.Text.Json's max-value-length on the C# decode side.
"""
from __future__ import annotations

import logging
import os
import shutil
from pathlib import Path

import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)

# Pin to a single GPU before torch imports. HuggingFace's Trainer auto-wraps multi-GPU with
# `torch.nn.parallel.DataParallel`, and MPNet hits a `StopIteration` in replicas (incompatibility
# between MPNet's parameter detection and DataParallel's replica handling).
os.environ.setdefault("CUDA_VISIBLE_DEVICES", "0")


def _models_dir() -> Path:
    data_root = os.environ.get("MAGIC_ATLAS_DATA")
    if not data_root:
        raise RuntimeError(
            "MAGIC_ATLAS_DATA env var not set — the harness must set this in Program.cs."
        )
    return Path(data_root) / "_06_Models"


@step(
    inputs=["TrainingPairs", "FineTuneConfig"],
    outputs="FineTunedEmbeddingModel",
    cacheable=True,
)
def fine_tune_embedding_model(pairs: pd.DataFrame, config: dict) -> dict:
    import torch
    from datasets import Dataset
    from sentence_transformers import (
        SentenceTransformer,
        SentenceTransformerTrainer,
        SentenceTransformerTrainingArguments,
        losses,
    )

    logger.info(
        "CUDA_VISIBLE_DEVICES=%r, torch sees %d device(s)",
        os.environ.get("CUDA_VISIBLE_DEVICES"),
        torch.cuda.device_count(),
    )

    base_repo = config["FineTuneBaseRepoId"]
    variant = config["FineTuneVariant"]
    target = _models_dir() / variant

    # In-step idempotency: if the fine-tuned model dir already has the inference-time files,
    # emit the existing ref and skip training. Not `cacheable=True` on the @step decorator
    # because the model dir is an un-tracked side effect — Flowthru's cache plan only sees
    # the ModelArtifactRef sidecar, so a cache hit could serve a stale ref to a deleted dir.
    # Wipe the dir manually (or run `--no-cache`) to force a fresh train; corpus or
    # hyperparameter changes still need explicit re-training too — the input fingerprints
    # don't reach into this short-circuit.
    if (target / "config.json").exists() and (target / "1_Pooling" / "config.json").exists():
        logger.info("Fine-tuned model dir %s already exists; skipping training.", target)
        return {"Path": str(target), "RepoId": base_repo, "Variant": variant}

    device = "cuda" if torch.cuda.is_available() else "cpu"
    logger.info("Loading fine-tune base %s on %s...", base_repo, device)
    model = SentenceTransformer(base_repo, device=device)

    # Treat triplets as pairs (drop the explicit negative). With only ~4 triplets in the
    # corpus today, the signal is dominated by MNR's in-batch negatives anyway. When the
    # curated-triplet count grows, split into a separate dataset and use mixed-loss training.
    pair_rows = []
    n_pos = n_trip_demoted = 0
    for _, row in pairs.iterrows():
        anchor = str(row["anchor"])
        positive = str(row["positive"])
        negative = row["negative"]
        pair_rows.append({"anchor": anchor, "positive": positive})
        if negative is None or (isinstance(negative, float) and pd.isna(negative)):
            n_pos += 1
        else:
            n_trip_demoted += 1

    logger.info(
        "Training corpus: %d positive pairs + %d triplets-as-pairs = %d rows",
        n_pos, n_trip_demoted, len(pair_rows),
    )
    if not pair_rows:
        raise ValueError("No training pairs — refusing to fine-tune from empty corpus.")

    train_dataset = Dataset.from_list(pair_rows)
    loss = losses.MultipleNegativesRankingLoss(model)

    if target.exists():
        shutil.rmtree(target)
    target.mkdir(parents=True)

    args = SentenceTransformerTrainingArguments(
        output_dir=str(target / "_trainer"),
        num_train_epochs=config["TrainNumEpochs"],
        per_device_train_batch_size=config["TrainPerDeviceBatchSize"],
        warmup_ratio=config["TrainWarmupRatio"],
        learning_rate=config["TrainLearningRate"],
        logging_steps=config["TrainLoggingSteps"],
        save_strategy="no",
        report_to="none",
        dataloader_drop_last=False,
        max_steps=-1,
        fp16=config["TrainFp16"],
    )
    trainer = SentenceTransformerTrainer(
        model=model,
        args=args,
        train_dataset=train_dataset,
        loss=loss,
    )
    logger.info(
        "Starting fit (%d epochs, MNR over pairs+triplets-as-pairs)...",
        config["TrainNumEpochs"],
    )
    trainer.train()
    logger.info("Fit complete; writing model to %s", target)

    model.save(str(target))
    # Clean up the trainer's checkpoint scratch directory if any was created.
    scratch = target / "_trainer"
    if scratch.exists():
        shutil.rmtree(scratch)

    # Scalar record output uses C# PascalCase property names on deserialization.
    return {"Path": str(target), "RepoId": base_repo, "Variant": variant}
