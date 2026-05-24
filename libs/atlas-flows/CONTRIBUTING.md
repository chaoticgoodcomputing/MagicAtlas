# Contributing to atlas-flows

Flowthru data pipelines (C# + Python) for the MagicAtlas card catalog.

## Tests

The test harness for this library lives at [`tests/atlas-flow-test/`](../../tests/atlas-flow-test/), not in this directory. Run tests from there.

## Language

### Catalog layers

The `Data/_NN_*` folders are not a filing convention — each layer is a strict semantic answer to *"what role does this artifact play in the ML lifecycle?"* (Kedro-strict; see [ADR-0001](docs/adr/0001-kedro-strict-layer-placement.md)).

**`_01_Raw`**:
Original source bytes — Scryfall bulk JSON, Magic comprehensive rules text, otag assignments. Never modified after fetch.
_Avoid_: "Bronze layer", "ingest output"

**`_02_Intermediate`**:
Typed, parsed representations of a single raw source. Same information content as `_01`, just stronger types and structural breakdown (e.g. rules sections split out, cards parsed into faces).
_Avoid_: "Silver layer", "cleaned"

**`_03_Primary`**:
Domain-cleansed, business-keyed, model-agnostic tables. The single source of truth for downstream consumers. Filtered, deduplicated, joined — but no ML has touched them yet. `OracleLines` is the keystone primary table.
_Avoid_: "Gold layer", "main data", "primary keys"

**`_04_Feature`**:
Engineered features in ML-ready form — derived vocabularies, centroids in embedding space, anything that exists to feed a model but isn't yet a training corpus.
_Avoid_: "Derived data", "intermediate features"

**`_05_ModelInput`**:
Training and validation corpora — split, sampled, formatted for the trainer.
_Avoid_: "Training data" (when ambiguous with `_04_Feature`)

**`_06_Models`**:
Trained model artifacts. Sidecar JSONs (`ModelArtifactRef`) pointing to on-disk model directories.

**`_07_ModelOutput`**:
Model *inference* outputs — encoded vectors, UMAP projections, cluster assignments, cluster labels, per-line tag attributions. Strictly predictions; **diagnostics and benchmarks belong in `_08_Reporting`**.
_Avoid_: "Eval results" (which are reports — `_08`)

**`_08_Reporting`**:
Final published artifacts (Atlas HTML, Mermaid diagrams) plus all diagnostic and benchmark reports (`BarrelDetectionReport`, `KeywordClusterReport`, `ClusterCanonicalBenchmark`, `ModelEvaluation`).
_Avoid_: "Output layer" (overlaps with `_07_ModelOutput`)

### Variants

**Model variant**:
A trained-model identity (currently `default` and `finetuned`). The `*FineTuned`-suffixed items and steps are temporary scaffolding for the rollout — the default variant is being deprecated and the suffix will be dropped once cutover completes.
_Avoid_: "Model version" (implies semver), "model type"

## Conventions

### One layer per step (soft rule)

A step should default to writing outputs into a single catalog layer. When you find yourself reaching across layers in one step's output set, ask: am I doing two lifecycle jobs at once? If yes, split.

The principled carve-out is **atomic outputs**: when artifacts of different lifecycle roles drop out of the same indivisible computation, keep them together. The canonical case is model training — the trained model (`_06_Models`) and the training metrics / loss curves (`_08_Reporting`) are produced atomically; splitting would require either re-running training or materializing an "everything the trainer produced" intermediate item, both of which are pure bookkeeping.

The test: *if I split this step, do I have to either (a) re-run the expensive computation, or (b) invent an intermediate item that only exists to be fanned out?* If yes, the multi-layer output is justified. If no, split.
