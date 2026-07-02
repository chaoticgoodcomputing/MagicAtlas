# Contributing to atlas-flows

Flowthru data pipelines (C# + Python) for the MagicAtlas explorer atlas.

## Scope

This library produces a single artifact: a 2D semantic map of MTG oracle text, browsable by an
**explorer**-mode user who wants to find cards by similarity ("show me things near this Lightning
Bolt"). Categorical / **exploiter**-mode questions ("show me all removal under 3 mana") are not
served by this library — they live in [`libs/magic-ast/`](../magic-ast/), where a parsed AST and
query language can answer them deterministically.

What's NOT in scope here:
- Cluster discovery (HDBSCAN, c-TF-IDF labeling)
- Supervised UMAP, archetype taxonomy, prototype attribution
- Cluster-vs-canonical scorecards, attribution margin tuning
- Any pipeline whose purpose is to bend embedding geometry around a user-authored taxonomy

These were prior research threads that have been pruned in favor of the explorer/exploiter
separation. See git history (~May 2026) for the deletions if you need archaeology.

## Tests

The test harness for this library lives at [`tests/atlas-flow-test/`](../../tests/atlas-flow-test/),
not in this directory. Run flows from there.

## Pipeline shape

Five flows, registered in [`Program.cs`](../../tests/atlas-flow-test/Program.cs):

1. **Ingest** — HTTP boundary; fetches Scryfall card and symbology bytes into `_01_Raw`. (The MTG
   comprehensive rules text moved to the standalone `mtg-rules` project, which publishes the
   structured rules + glossary + type ontology this project vendors.)
2. **CardProcessing** — typed Scryfall card parsing + commander-format filter.
3. **FineTune** — base-model download + MTG-corpus fine-tune. Training pairs are derived
   entirely from glossary/CR/oracle-text (no manual curated overrides); the glossary + rules
   inputs are vendored from the `mtg-rules` project.
4. **OracleEmbedding** — encode oracle lines via the fine-tuned model → unsupervised UMAP →
   2D atlas coordinates + a label-free fidelity scorecard.
5. **Reporting** — render the atlas as a standalone Plotly HTML, colored by MTG color identity.

## Language

### Catalog layers

The `Data/_NN_*` folders are not a filing convention — each layer is a strict semantic answer
to *"what role does this artifact play in the ML lifecycle?"* (Kedro-strict; see
[ADR-0001](docs/adr/0001-kedro-strict-layer-placement.md)).

**`_01_Raw`**:
Original source bytes — Scryfall bulk JSON. Never modified after fetch. (Comprehensive rules
text now lives in the standalone `mtg-rules` project.)
_Avoid_: "Bronze layer", "ingest output"

**`_02_Intermediate`**:
Typed, parsed representations of a single raw source. Same information content as `_01`, just
stronger types and structural breakdown.
_Avoid_: "Silver layer", "cleaned"

**`_03_Primary`**:
Domain-cleansed, business-keyed, model-agnostic tables. The single source of truth for
downstream consumers. Filtered, deduplicated, joined — but no ML has touched them yet.
`OracleLines` is the keystone primary table.
_Avoid_: "Gold layer", "main data", "primary keys"

**`_04_Feature`**:
Engineered features in ML-ready form — derived vocabularies, anything that exists to feed a
model but isn't yet a training corpus.
_Avoid_: "Derived data", "intermediate features"

**`_05_ModelInput`**:
Training and validation corpora — split, sampled, formatted for the trainer.
_Avoid_: "Training data" (when ambiguous with `_04_Feature`)

**`_06_Models`**:
Trained model artifacts. Sidecar JSONs (`ModelArtifactRef`) pointing to on-disk model
directories.

**`_07_ModelOutput`**:
Model *inference* outputs — encoded vectors, UMAP projections, plus the `AtlasFidelityMetric`
scorecard and the `BarrelDetectionReport` diagnostic. Strictly predictions and per-run
diagnostics.
_Avoid_: "Eval results" (which are reports — `_08`)

**`_08_Reporting`**:
Final published artifacts (Atlas HTML) plus the pre-joined render-shape intermediates
(`AtlasReportingPoints`, `AtlasCardHoverInfo`).
_Avoid_: "Output layer" (overlaps with `_07_ModelOutput`)

### Variants

**Model variant**:
A trained-model identity (currently `default` and `finetuned`). The `*FineTuned`-suffixed
items and steps are temporary scaffolding for the rollout — the default variant is being
deprecated and the suffix will be dropped once cutover completes.
_Avoid_: "Model version" (implies semver), "model type"

### User modes

**Explorer**: a user navigating by *similarity* — "show me things near this card." Served by
this library's 2D atlas. Map geometry should preserve HD-embedding neighborhoods (measured by
`AtlasFidelityMetric.trustworthiness_k10` and `continuity_k10`).
_Avoid confusing with_: the codebase's deprecated "exploration camp" metrics, which referred to
label-coherence scorecards from the pre-pruning era — they don't exist anymore.

**Exploiter**: a user navigating by *category* — "show me all cards with X ability." NOT served
by this library; lives in [`libs/magic-ast/`](../magic-ast/) (parsed-AST + query language).
Trying to answer exploiter questions with embedding statistics here was the prior dead end;
deterministic AST queries are the right tool.

## Conventions

### One layer per step (soft rule)

A step should default to writing outputs into a single catalog layer. When you find yourself
reaching across layers in one step's output set, ask: am I doing two lifecycle jobs at once?
If yes, split.

The principled carve-out is **atomic outputs**: when artifacts of different lifecycle roles
drop out of the same indivisible computation, keep them together. The canonical case is model
training — the trained model (`_06_Models`) and the training metrics / loss curves
(`_08_Reporting`) are produced atomically; splitting would require either re-running training
or materializing an "everything the trainer produced" intermediate item, both of which are
pure bookkeeping.

The test: *if I split this step, do I have to either (a) re-run the expensive computation, or
(b) invent an intermediate item that only exists to be fanned out?* If yes, the multi-layer
output is justified. If no, split.
