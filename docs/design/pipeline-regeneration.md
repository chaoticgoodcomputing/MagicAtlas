# Regenerating the Atlas datasets from scratch

The analytics datasets that power the API + frontend (ports, families, edges,
combos, archetypes, anchors — the D1–D4 dumps) are produced by the Flowthru
pipeline in **`libs/atlas-flows`** and run through the **`tests/atlas-flow-test`**
harness. As of the W3 promotion, a fresh clone can regenerate everything
end-to-end from the shippable lib (no test-project-only step in the path).

The intermediate + output datasets are **gitignored** (`Data/**`), so a clean
checkout has none of them. Run the flows in order:

```bash
# from repo root — each writes into tests/atlas-flow-test/Data/… (gitignored)
dotnet run --project tests/atlas-flow-test -- --flow CorpusParse
#   Scryfall oracle-cards bulk → card-inputs.json + parse-records.json
#   (includes the ~38k-card MagicAST corpus parse — the slow step, minutes)

dotnet run --project tests/atlas-flow-test -- --flow FetchCombos
#   Commander Spellbook variants dump (~510 MB) → combos.json

dotnet run --project tests/atlas-flow-test -- --flow CardAtlas
#   → card-ports.json, card-meta.json, combo-instances.json,
#     resource-graph.json, archetype-catalog.json  (~20–30s; reads the above)

dotnet run --project tests/atlas-flow-test -- --flow ComboAnchors
#   → combo-anchor-report.json
```

Equivalently via nx: `nx run atlas-flow-test:run -- --flow <Name>`.

Raw fetches (Scryfall bulk, the CSB dump) are HTTP-cached (~weekly), so
re-running is cheap after the first pull. `CorpusParse` and `FetchCombos` are
the only steps that touch the network / do the heavy parse; `CardAtlas` and
`ComboAnchors` just read the cached intermediates.

## Getting the dumps into the API

The `atlas-api` seeder reads the dumps from a `dumps/` directory (config keys
under `Atlas:*Path` in `apps/atlas-api/appsettings.json`). Copy the produced
files there, then (re)seed:

```bash
cp tests/atlas-flow-test/Data/_08_Reporting/dumps/*.json dumps/   # path per harness output
# truncate the analytics tables (or DROP a table whose schema changed), then:
nx serve atlas-api      # idempotent-by-emptiness reseed on startup
```

See the per-table recipe (truncate vs drop) and the tier/span notes in the
memory file `atlas-upstream-phase1-2` and `docs/design/upstream-atlas-data-plan.md`.

## What is *not* in this path (still test-project-only)

By design — these are diagnostics/reporting, not CardAtlas inputs:

- `AggregateTriageReport` → `triage-report.json` (the mast-tdd-loop artifact).
- The rest of `InteractionTriage` (`ClassifyCombos`, `LabelEdges`,
  `MaterializeCardEdges/Cycles`, `PortNodes`, `PlotInteractionGraph`) — the
  reconstruction-engine diagnostics + interaction-graph viz.

Promoting those would additionally require `YieldClusterAnalyzer` +
`Clustering/*`, `CardComboValueLoader`, and the `TriageReport` schema family —
tracked but out of scope for the dump-regeneration path.
