# Kedro-strict ML-lifecycle layering for the atlas-flows catalog

## Context

The atlas-flows catalog is folder-numbered Kedro-style (`_01_Raw` … `_08_Reporting`), but item placement had drifted to a *lineage-stability* principle: any artifact stable enough to be referenced by multiple downstream consumers landed in `_03_Primary`, regardless of its role in the ML lifecycle. As a result `_03_Primary` was a four-role grab-bag (cleansed data, features, model inputs, model predictions) and `_07_ModelOutput` had become a mixed bag of predictions and diagnostic reports. The layer numbers carried Kedro's conventions but the placement didn't follow them — paying the cost of the convention without getting the payoff.

## Decision

Adopt **Kedro-strict ML-lifecycle layering**. Each layer answers exactly one question — *"what role does this artifact play in the ML lifecycle?"*:

- `_01_Raw` — original source data, never modified
- `_02_Intermediate` — typed raw, single-source transformation
- `_03_Primary` — domain-cleansed, business-keyed; the single-source-of-truth tables. Model-agnostic.
- `_04_Feature` — engineered features ready for ML (vocabularies, centroids, derived columns)
- `_05_ModelInput` — training/validation splits, model-ready inputs
- `_06_Models` — trained model artifacts
- `_07_ModelOutput` — model *inference* outputs (predictions, encoded vectors, cluster assignments). Not diagnostics.
- `_08_Reporting` — final published artifacts plus all diagnostic / benchmark reports

Migration is **big-bang**, not incremental: `_03_Primary` items that are features, model inputs, or model outputs are relocated to their correct layer in one pass; `_07_ModelOutput` diagnostic reports are promoted to `_08_Reporting`. Flowthru's catalog abstraction localizes the change to item factories — flow code references items by symbol, not by path.

## Considered alternatives

- **Lineage-stability (de facto status quo).** Formalize the current placement: items land in the most stable layer they reach. Rejected — it abandons the Kedro numbering's information content and leaves the predictions-vs-reports confusion in `_07` unresolved.
- **Audience-driven layering** (atlas-api / Reporting flow / next step / human reader). Rejected — audience changes more often than role, so items would migrate frequently and the layer signal would degrade.
- **Defer / incremental migration.** Rejected — partial layering is worse than either pure rule. New items would follow new rules, existing items would not, and the grab-bag would persist indefinitely.

## Consequences

- ~20 catalog items move; `_04_Feature` and `_05_ModelInput` are introduced.
- Any consumer that reads items by **disk path** rather than via the catalog symbol will break (e.g. the atlas-api's `Atlas:AtlasPointsPath` setting). Coordinate path changes with `atlas-api`, `atlas-site`, and any runbook that uses `--from`/`--to` slicing.
- The `*FineTuned` shadow items relocate together; deletion of the default-variant siblings (see project memory `project_base_model_deprecation`) is a separate follow-up and should happen after the migration so the renames don't interleave with deletions.
