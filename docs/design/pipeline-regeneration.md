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

## Cache keying and the cold/warm cost (ADR 0004, issue #22)

Flowthru caches steps, but the framework's generated `CodeVersion` hashes **the
step class's own source text only** — its own XML docs say cross-assembly changes
"are not reflected". Nearly every expensive step in this repo is a thin wrapper
around a library in `libs/`: `ParseCorpusStep` is a handful of lines around
`MagicAST.OracleParser`. So a parser change left the step's key unchanged, the
plan called it FRESH, and the flow re-served the previous `parse-records.json`.
That is what the old `rm parse-records.json` + `--only ParseCorpus` recipe was
working around.

`tests/magic-ast-tests/Infrastructure/StepCodeIdentity.cs` closes it: at startup
every `[FlowthruStep]` class is re-registered under
`{generatedCodeVersion}+{closureDigest}`, where the digest covers the first-party
code the step actually reaches — same-assembly helpers at method-IL granularity,
foreign first-party assemblies at MVID granularity. Gated by
`Tests/Pipeline/StepCacheKeyingTests.cs`. The force-re-derivation recipe is
retired; `--no-cache` remains the escape hatch for out-of-band edits to input
*files* (see the mtime caveat below).

### Measured — `MagicAstTriage`, ~38k-card corpus

Wall clock is the whole `dotnet run`; flow time is Flowthru's own report.

| Scenario | Flow time | Wall |
| --- | --- | --- |
| Cold — no cache manifest, no derived artifacts | 48.8 s | 49.6 s |
| Warm — nothing changed | 0.14 s | 0.88 s |
| After a code change in `libs/magic-ast/` | 45.3 s | 46.1 s |
| After a full from-scratch rebuild, zero source changes | 0.13 s | 0.88 s |

Step breakdown on the cold run: `FetchScryfallBulk` 3.2 s, `ProjectToCardInput`
1.0 s, **`ParseCorpus` 42.6 s**, `AggregateTriageReport` 1.9 s. The corpus parse
is essentially the entire cold cost.

Two things that row 4 settles. First, **MVID does not over-invalidate**: .NET SDK
builds are deterministic, `MagicAST.dll` was verified byte-identical across an
incremental rebuild and a from-scratch rebuild, and every step key in
`.flowthru/cache.json` survived a full `rm -rf dist` + rebuild unchanged. Second,
the warm path is ~350x cheaper than the cold path, so the cache is worth having
correct rather than worth disabling.

Only the steps that reach the changed code re-run: on the code-change run
`FetchScryfallBulk` stayed FRESH. `ProjectToCardInput` did re-run, correctly — its
output schema is a MagicAST type, so it genuinely reaches the changed assembly.

### Caveat for generate-on-demand (#23): file fingerprints are `mtime:size`

Flowthru's `FileStorageMedium.Fingerprint()` hashes `{LastWriteTimeUtc.Ticks}:{Length}`,
not file content — a documented framework limitation. Measured consequence:
deleting and regenerating `card-inputs.json` produced a **byte-identical file**
(same SHA-256) with a **different** Flowthru fingerprint, which changed
`ParseCorpus`'s composite key and forced a full re-parse.

For #23 this is the binding constraint, not the code key: with artifacts generated
on demand, regenerating any upstream artifact cascades a full re-run of everything
below it even when nothing about the content changed. Scope the seed target
accordingly, or fix it upstream with a content-hashing fingerprint variant
(Flowthru's own docs name this as a planned "deep fingerprint").

### Not yet closed: `libs/atlas-flows` / `tests/atlas-flow-test`

The fix above is installed in the `tests/magic-ast-tests` harness only (the CORE
ring). `libs/atlas-flows` has the identical hole — `CorpusParseFlow`'s
`ParseCorpusStep` is the same `OracleParser` wrapper — and its own
`tests/atlas-flow-test/.flowthru/cache.json`. Closing it needs `StepCodeIdentity`
in a place both projects can compile (neither references the other today), or the
same idea pushed upstream into Flowthru's `StepMetadataGenerator`. Until then the
dump-regeneration path above is still code-blind: pass `--no-cache` when
regenerating dumps after a parser change.
