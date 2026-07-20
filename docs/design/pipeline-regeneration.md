# Regenerating the Atlas datasets from scratch

The analytics datasets that power the API + frontend (ports, families, edges,
combos, archetypes, anchors — the D1–D4 dumps) are produced by the Flowthru
pipeline in **`libs/atlas-flows`** and run through the **`tests/atlas-flow-test`**
harness. As of the W3 promotion, a fresh clone can regenerate everything
end-to-end from the shippable lib (no test-project-only step in the path).

The intermediate + output datasets are **gitignored** (`Data/**`, `dumps/`), so a
clean checkout has none of them. That is deliberate — see *Generate on demand*
below.

## The one target: `nx run flowthru:dumps`

```bash
nx run flowthru:dumps
```

That is the whole recipe. It runs the four flows in dependency order and then
**publishes** the results to the repo-root `dumps/` directory the consumers read:

| Step | Produces |
| --- | --- |
| `--flow CorpusParse` | Scryfall oracle-cards bulk → `card-inputs.json` + `parse-records.json` (includes the ~38k-card MagicAST corpus parse — the slow step) |
| `--flow FetchCombos` | Commander Spellbook variants dump (~510 MB) → `combos.json` |
| `--flow CardAtlas` | `card-ports.json`, `card-meta.json`, `combo-instances.json`, `resource-graph.json`, `archetype-catalog.json`, `extended-recall-report.json` |
| `--flow ComboAnchors` | `combo-anchor-report.json` |
| publish | `cp Data/_08_Reporting/dumps/*.json` → `<repo>/dumps/` |

Any step can still be run alone —
`dotnet run --project tests/atlas-flow-test -- --flow <Name>` — when you want a
single dataset and know its inputs are already present.

Raw fetches (Scryfall bulk, the CSB dump) are HTTP-cached (~weekly), so
re-running is cheap after the first pull. `CorpusParse` and `FetchCombos` are
the only steps that touch the network / do the heavy parse; `CardAtlas` and
`ComboAnchors` just read the cached intermediates.

**The publish step copies leaves only.** It never writes back into
`_01_Raw`/`_02_Intermediate`/`_07_ModelOutput`, which matters more than it looks:
Flowthru fingerprints file items on `mtime:size` rather than content (see the
caveat below), so rewriting an upstream artifact — even byte-identically —
cascades a full re-run of everything downstream.

## Generate on demand (ADR 0004 §3, issue #23)

Derived artifacts are **build outputs**: gitignored, and reproduced by running
the pipeline. The derivation base is exactly three inputs — external source data
(Scryfall, the CSB snapshot), Evidence fixtures, and code — so nothing else needs
storing, and an artifact that does not exist in the repository cannot go stale in
it.

The consumers therefore **do not fall back to a committed copy**. They read the
known gitignored path and fail loudly, naming the target that fixes it:

- **`atlas-api` seeder** (`AtlasSeeder.RequireFile`) throws on startup rather
  than half-seeding the database:

  > `Card ports dataset not found at '…/dumps/card-ports.json'. This is a Derived artifact (ADR 0004 §3): it is gitignored and produced on demand by the Flowthru pipeline, so a clean checkout does not have it. Run `nx run flowthru:dumps` first, then start the API again. See docs/design/pipeline-regeneration.md.`

  (`card-edges.json` is the exception in provenance, not in shape: it comes from
  the MAST InteractionTriage flow, so its runbook line names
  `nx run mast:interaction-triage`.)

- **`atlas-diag`** exits 2 with the same shape, naming `nx run mast:run` /
  `nx run mast:recall-report` for the MAST-side datasets it reads.

The rationale, recorded in #23: if `f` is too slow or too fragile to run, that is
a defect in `f` worth surfacing — not a reason to cache its output in git. Making
the pipeline the required path keeps it load-bearing; a pipeline nobody runs rots.

**The invariant is gated.** `DerivedArtifactTrackingGateTests` (CORE ring) checks
the census's Derived set against git's index: a Derived artifact that gets
committed is red on the first run. The committed exceptions are named in that
file, each with the gate that re-derives it — the rollup (whose inter-run diff is
the point), the sentinel snapshots and `ast-schema.json` (whose committed copy
*is* the expectation their gates diff against), plus two carve-outs recorded
there in full.

## Getting the dumps into the API

`nx run flowthru:dumps` already lands them in `dumps/` (the config keys under
`Atlas:*Path` in `apps/atlas-api/appsettings.json` resolve there). Then:

```bash
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

### The cache manifest is no longer committed (#23)

`tests/atlas-flow-test/.flowthru/cache.json` was tracked in git — a leftover from
before the `.flowthru` ignore rule existed. It is *runtime state*, and committing
it is worse than committing a derived artifact: the manifest records which steps
are FRESH, so a clean checkout inherited a cache claiming steps were satisfied by
output files that clone did not have. `git rm --cached` removed it; the ignore
rule (`tests/atlas-flow-test/.gitignore:24`) already covered it.

### Not yet closed: `libs/atlas-flows` / `tests/atlas-flow-test`

The fix above is installed in the `tests/magic-ast-tests` harness only (the CORE
ring). `libs/atlas-flows` has the identical hole — `CorpusParseFlow`'s
`ParseCorpusStep` is the same `OracleParser` wrapper — and its own
`tests/atlas-flow-test/.flowthru/cache.json`. Closing it needs `StepCodeIdentity`
in a place both projects can compile (neither references the other today), or the
same idea pushed upstream into Flowthru's `StepMetadataGenerator`. Until then the
dump-regeneration path above is still code-blind: pass `--no-cache` when
regenerating dumps after a parser change.
