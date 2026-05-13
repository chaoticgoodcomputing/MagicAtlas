"""Populate the DefaultEmbeddingModel catalog reference by downloading a sentence-transformer
checkpoint from HuggingFace into `_06_Models/<variant>/` and emitting a metadata ref.

Input:  BaseModelSpec record — uses `DefaultRepoId`.
Output: ModelArtifactRef record — { path, repo_id, variant } pointing to the on-disk model dir.

Why-not-bytes: Flowthru's subprocess marshaller pipes payloads through a JSON envelope, and a
400+ MB model exceeds System.Text.Json's max-value-length. Writing the model files directly
to disk and shipping only a small JSON ref bypasses that limit entirely.

Reads the data-dir root from `MAGIC_ATLAS_DATA` (set by the harness).
"""
from __future__ import annotations

import fnmatch
import logging
import os
import shutil
from pathlib import Path

from flowthru import step

logger = logging.getLogger(__name__)

# Only the files SentenceTransformer needs at inference time. Skipping ONNX / OpenVINO /
# pytorch_model.bin / TensorFlow weights keeps the model dir under ~100 MB for MiniLM and
# ~450 MB for mpnet.
_ALLOW_PATTERNS = (
    "*.json",
    "vocab.txt",
    "model.safetensors",
    "1_Pooling/*",
)

_VARIANT = "default-minilm-l6-v2"


def _models_dir() -> Path:
    data_root = os.environ.get("MAGIC_ATLAS_DATA")
    if not data_root:
        raise RuntimeError(
            "MAGIC_ATLAS_DATA env var not set — the harness must set this in Program.cs."
        )
    return Path(data_root) / "_06_Models"


@step(inputs=["BaseModelSpec"], outputs="DefaultEmbeddingModel")
def download_base_model(spec) -> dict:
    from huggingface_hub import snapshot_download

    # Scalar IItem<T> records arrive keyed by C# PascalCase property names.
    repo_id = spec["DefaultRepoId"]
    target = _models_dir() / _VARIANT

    logger.info("Downloading %s into %s ...", repo_id, target)
    snapshot_path = Path(snapshot_download(
        repo_id=repo_id,
        allow_patterns=list(_ALLOW_PATTERNS),
    ))

    # Wipe the target so stale files from a previous run don't linger.
    if target.exists():
        shutil.rmtree(target)
    target.mkdir(parents=True)

    # Copy (don't symlink — HF cache uses symlinks already, and we want a self-contained dir).
    n_files = 0
    for path in snapshot_path.rglob("*"):
        if not path.is_file():
            continue
        rel = path.relative_to(snapshot_path).as_posix()
        if not any(fnmatch.fnmatch(rel, p) for p in _ALLOW_PATTERNS):
            continue
        out = target / rel
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(path.read_bytes())
        n_files += 1

    logger.info("Materialized %d files at %s", n_files, target)
    # Scalar IItem<T> records are deserialized by C# property name (PascalCase), NOT the
    # [SerializedLabel] snake_case (which only applies to tabular columns).
    return {"Path": str(target), "RepoId": repo_id, "Variant": _VARIANT}
