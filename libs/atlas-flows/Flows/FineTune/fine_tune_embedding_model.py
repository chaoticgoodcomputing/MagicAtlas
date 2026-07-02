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

# Reduces CUDA memory fragmentation during training — relevant for Nomic v1.5's larger
# activation memory (SwiGLU + RoPE) on consumer GPUs (~12 GB). Suggested in PyTorch's OOM
# error guidance.
os.environ.setdefault("PYTORCH_CUDA_ALLOC_CONF", "expandable_segments:True")


def _models_dir() -> Path:
    data_root = os.environ.get("MAGIC_ATLAS_DATA")
    if not data_root:
        raise RuntimeError(
            "MAGIC_ATLAS_DATA env var not set — the harness must set this in Program.cs."
        )
    return Path(data_root) / "_06_Models"


@step(
    inputs=["TrainingPairsMined", "FineTuneConfig"],
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
    # `trust_remote_code=True` is required for Nomic's custom modeling code; harmless for
    # other base models.
    model = SentenceTransformer(base_repo, device=device, trust_remote_code=True)

    # Mixed-loss training: split rows into positive-pairs and triplets, train each with its own
    # CachedMultipleNegativesRankingLoss. MNR with a 3-column dataset (anchor, positive, negative)
    # uses the explicit negative as an additional in-batch hard negative — exactly what swap-
    # triplets need to teach "Vampire ≠ Werewolf", "you ≠ your opponent", etc.
    #
    # Apply the `clustering:` task prefix to every training-pair string so the fine-tune
    # refines the same prefix-conditioned representation that downstream OracleEmbedding
    # steps use at inference. Training and inference must agree on the prefix — fine-tuning
    # unprefixed strings would refine a *different* representation than the one we later
    # infer against.
    pair_rows: list[dict] = []
    triplet_rows: list[dict] = []
    for _, row in pairs.iterrows():
        anchor = f"clustering: {row['anchor']}"
        positive = f"clustering: {row['positive']}"
        negative = row["negative"]
        if negative is None or (isinstance(negative, float) and pd.isna(negative)):
            pair_rows.append({"anchor": anchor, "positive": positive})
        else:
            triplet_rows.append({
                "anchor": anchor,
                "positive": positive,
                "negative": f"clustering: {negative}",
            })

    logger.info(
        "Training corpus: %d positive pairs + %d hard-negative triplets",
        len(pair_rows), len(triplet_rows),
    )
    if not pair_rows and not triplet_rows:
        raise ValueError("No training data — refusing to fine-tune from empty corpus.")

    # Build a dict of datasets + dict of losses; SentenceTransformerTrainer iterates them
    # together, applying the corresponding loss to each batch from each named dataset.
    #
    # CachedMultipleNegativesRankingLoss vs plain MultipleNegativesRankingLoss:
    # MNR's effectiveness scales with the number of in-batch negatives per anchor — sbert.net's
    # losses ref [1] and the Nomic Embed paper [2] both treat batch sizes < 16 as fundamentally
    # under-powered for MNR-style contrastive training (their training used a 16,384 global
    # batch). The Cached variant lets us run at the recommended effective batch size without
    # exceeding GPU memory: it splits the batch into smaller forward/backward chunks
    # (mini_batch_size below), caches the embeddings, and computes the contrastive loss
    # against the full cached batch as if it had all fit in one forward pass.
    #
    # mini_batch_size=4 keeps each forward+backward inside the cached loss under our GPU
    # memory ceiling (Nomic v1.5 at ~140M params + SwiGLU + RoPE activations on a 12 GB GPU
    # is tight — see PYTORCH_CUDA_ALLOC_CONF + gradient_checkpointing above). The effective
    # contrastive batch is still per_device_train_batch_size (set to 32 in appsettings).
    #
    # [1] https://sbert.net/docs/package_reference/sentence_transformer/losses.html
    # [2] Nomic Embed: Training a Reproducible Long Context Text Embedder,
    #     Nussbaum et al., arxiv:2402.01613 §3.1.
    _CACHED_MNR_MINI_BATCH = 4

    train_datasets: dict = {}
    losses_dict: dict = {}
    if pair_rows:
        train_datasets["pairs"] = Dataset.from_list(pair_rows)
        losses_dict["pairs"] = losses.CachedMultipleNegativesRankingLoss(
            model, mini_batch_size=_CACHED_MNR_MINI_BATCH,
        )
    if triplet_rows:
        train_datasets["triplets"] = Dataset.from_list(triplet_rows)
        losses_dict["triplets"] = losses.CachedMultipleNegativesRankingLoss(
            model, mini_batch_size=_CACHED_MNR_MINI_BATCH,
        )
    # When only one of the two is populated, unwrap so the trainer takes the simpler form.
    if len(train_datasets) == 1:
        only_key = next(iter(train_datasets))
        train_dataset = train_datasets[only_key]
        loss = losses_dict[only_key]
    else:
        train_dataset = train_datasets
        loss = losses_dict

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
        # Recomputes activations during backprop instead of caching them — trades ~30% extra
        # compute for ~half the activation memory. Needed for Nomic v1.5 on 12 GB-class GPUs;
        # safe to leave on for smaller bases too.
        gradient_checkpointing=True,
    )
    trainer = SentenceTransformerTrainer(
        model=model,
        args=args,
        train_dataset=train_dataset,
        loss=loss,
    )
    n_p = len(pair_rows)
    n_t = len(triplet_rows)
    logger.info(
        "Starting fit (%d epochs, MNR%s)...",
        config["TrainNumEpochs"],
        f" mixed: {n_p} pairs + {n_t} triplets" if n_p and n_t else
        f" pairs only ({n_p})" if n_p else f" triplets only ({n_t})",
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
