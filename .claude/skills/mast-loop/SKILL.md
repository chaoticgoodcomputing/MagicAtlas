---
name: mast-loop
description: The single entry point for the MagicAtlas TDD loop — extending the parser (MagicAST) AND the interaction engine (ports, edges, the ADR-3 taxonomy) as one cohesive cycle. Every run starts at the Flowthru reporting layer, picks a TRACK by objective (parse coverage / ADR-2 engine recall / ADR-3 accretion), and closes a gold-gated slice. Use when driving any MAST/engine TDD round, picking high-value work from triage, authoring a parse gold OR an interaction gold, burning down topology holes, or when the user references "the loop", "mast-loop", "the TDD loop", "the accretion loop", or issue #7.
---

# MAST loop — the unified parse + engine TDD cycle

One loop, three tracks. Every track is TDD: a **gold is a committed failing test**; you extend the system to meet it; nothing lands unless the gate is green. What differs per track is only three things — **entry report → gold artifact → gate**. This umbrella owns what's shared; each track's detail lives in a sub-doc.

**The entry is always the reporting layer.** You come in with an *objective* (raw coverage / product recall / taxonomy build-out), not a card. The `_08_Reporting` Flowthru artifacts are the triage surface; the objective picks the track, the track picks the report, the report picks the work. Never start from a card you have in mind — start from what the reports say is highest-value *for the current objective*.

## The four tracks

Three tracks ADD coverage (Parse / Legacy-engine / Accretion); the fourth, **Error-check**, FIXES what's wrong (false positives, span mis-attributions) — a quality dimension, not a coverage one. Same spine for all four: **entry report → artifact → gate**.

| Track | Objective | Entry report(s) | Gold artifact | Gate | Vocabulary | Detail |
|---|---|---|---|---|---|---|
| **Parse** | Extend the AST — more cards/oracle text parse to structure | `triage-report.json` (`topYieldClusters` = L0, `topResidualClusters` = L1→L2) + `combo-anchor-report.json` (demand-first hubs) | `Fixtures/HandParsedCards/**` AST gold | `nx run mast:test` + `mast-judge` | **taxonomy-neutral** | [mast-tdd-loop/SKILL.md](../mast-tdd-loop/SKILL.md) |
| **Legacy-engine** | Lift ADR-2 ports/edges to GREEN; cover popular combos | `combo-anchor-report.json` / `bench:recall` / `port-label-census.json` (`topProjectionGaps`) | PortWalk flow arm + `combo-expected-tiers.json` pin | `nx run bench:recall` + `interaction-judge` | **ADR-2** (mechanism labels: `sac`/`ltb`/`etb`) | [LEGACY-ENGINE.md](LEGACY-ENGINE.md) |
| **Accretion** | Witness an ADR-3 stem / edge / hole; grow the taxonomy | **`port-topology-demand.json`** (value-ranked) + `port-topology.cited.json` (status) + `port-interactions.cited.json` (rules/ladder) | interaction gold under `Fixtures/Interactions/golds/` | `InteractionRollup` flow (conflict + ladder) + `interaction-judge` | **ADR-3** (event stems: `removal:creature`) | [ACCRETION.md](ACCRETION.md) |
| **Error-check** | Fix a WRONG port/gold — false positives + span mis-attributions (a port's span-witness contradicts its label) | **`span-witness-report.json`** (`nx run mast:span-witness`): suspects ranked, each routed to the golds witnessing its stem | the refined **parser slice** (span mint) OR **interaction gold** the suspect's stem routes to | `nx run mast:test` (span-provenance invariants) + re-run `mast:span-witness` (the suspect clears) | **cross-cutting** — routes via the ADR-3 `stem` | [ERROR-CHECK.md](ERROR-CHECK.md) |

Pick by where the marginal return is highest — the same triage discipline the Parse track already uses across its three currencies, lifted up a level. Raw coverage bottleneck → **Parse**. A popular combo reconstructs AMBER-that-should-be-GREEN, or a false-GREEN poisons the product → **Legacy-engine** (an untrustworthy GREEN outranks any coverage gain). Building out the ADR-3 taxonomy toward Migration cutover → **Accretion**, picking the highest-demand sought hole / most-witnessed stem from `port-topology-demand.json`. A port that lies about its own text — a false-positive edge or a chip on the wrong clause → **Error-check** (a wrong port outranks a missing one; the same "untrustworthy GREEN > coverage" logic, one layer down at the port).

## Two cross-cutting facts (true for every run)

### 1. The migration-vocabulary seam — know which universe you're in

The interaction engine still emits **ADR-2** ports (`sac`, `ltb`, `etb` — named by *mechanism/role*). ADR-0003 (the taxonomy redesign — name by the *resource/event that flows*: `removal:creature`, `deployment:artifact`) is **PROPOSED and only partially realized**: Migration Stages 1–5 are unstarted, so the live engine, and therefore **every report derived from it, is ADR-2**. The **only** ADR-3 surfaces are the four `InteractionRollup` artifacts + the `TopologyDemand` overlay — because they are built from hand-authored golds + the scaffold, *not* the engine.

**Consequence:** the two vocabularies coexist by design during migration, and they must not be conflated.
- **Legacy-engine** work keeps the *current product* alive in ADR-2 (the GREEN combos users see).
- **Accretion** work builds the *ADR-3 replacement* in shadow — golds accrete the target taxonomy; the engine does not yet consume them. Accretion does **not** move `bench:recall` today; its product is the topology + rule rollup + the sought-hole backlog burn-down.
- They converge only at **Stage 3** (shadow mode: engine emits ADR-3 ports, checked against the golds' assertions) → **Stage 4** (cutover: the census/anchor/recall reports re-derive in ADR-3 vocabulary). See `libs/mast-interaction/docs/adr/0003-taxonomy-redesign.md` Migration.

State the vocabulary in your batch report. An ADR-3 stem in a `bench:recall` context, or an `sac` label in a gold, is a category error.

### 2. The parse-inside-engine bridge — why this is one skill, not two

An engine/accretion slice frequently **cannot be witnessed until a parse gap closes**: a port is an AST query, so a stem/attribute that the parser doesn't yet emit is not derivable, and the gold cannot be honestly witnessed. The four known Stage-1 parser asks (`manner` facet, the `Sacrificed` trigger event, per-cost spans, the Deadeye blink slice) are exactly this — Accretion targets that are blocked on Parse work.

So the loop is unified: **an Accretion (or Legacy-engine) task that finds its port isn't AST-derivable spawns a Parse sub-slice inline** — dispatch a `mast-worker` against the parser gap (full [Parse track](../mast-tdd-loop/SKILL.md) discipline), land it, *then* return and witness the gold. Do not fork the gold around a missing parse; close the parse, then witness faithfully. This is the concrete reason parse and engine are one cohesive system rather than two skills that hand off.

## Shared machinery (stated once)

Every track reuses the same infrastructure — detailed in the Parse track's [SKILL.md](../mast-tdd-loop/SKILL.md) and [FANOUT.md](../mast-tdd-loop/FANOUT.md); summarized here so a track sub-doc need not repeat it:

- **Worktree-isolated workers → orchestrator serial-merge.** Workers run in `isolation: worktree` and never merge; the orchestrator merges serially with a rebuild + gate between groups. All git via `git -C "$WORKTREE_ROOT"`.
- **Two judges, both read-only, both Opus.** `mast-judge` (parser doctrine: AST shape, no-unparsed, describe-not-execute) for Parse; `interaction-judge` (CR-correctness of port→port edges and tiers, the false-GREEN guard) for Legacy-engine and Accretion. A judge FAIL halts the merge.
- **Deterministic gates.** Nonzero exit = unconditional HALT with the gate output quoted. The gate set differs per track (see each sub-doc); the discipline does not.
- **Golds are immutable to the worker that consumes them.** Whoever authors a gold owns it; a worker closing a gap never edits a gold to pass. Re-pointing other golds after a system change is orchestrator back-prop (gate + mandatory re-judge), never worker work.
- **Model policy.** Opus orchestrator + Opus judges, always. Sonnet workers only for clear-cut, well-briefed mechanical slices.
- **Fan-out.** Large sessions run as the deterministic Workflow harness ([FANOUT.md](../mast-tdd-loop/FANOUT.md)), not hand-managed spawns.

## The cycle (shared spine)

```
Step 0  Pre-flight: executing mode (not plan) + worktree base + refresh the reporting layer for the chosen track.
Step 1  Pick the TRACK by objective, then N units of work from that track's entry report (value-ranked).
Step 2  Brief each unit inline (rules facts from rules-structure.json; the CR number + verbatim text).
Step 3  Dispatch N isolated workers, one per unit. An engine/accretion unit blocked on parse spawns a Parse sub-slice first (bridge §2).
Step 4  Judge novel-shape work (mast-judge for parse; interaction-judge for edges) → verdict JSON → gate.
Step 5  Merge serially in file-affinity order; the track's CORE gate green after each group.
Step 6  Regenerate the track's reporting artifacts; loop or stop.
```

Each track specializes Steps 1 / 3-gold / 4-judge / 5-gate / 6-report; the spine is invariant. Follow the track sub-doc for the specialization.

## When invoked directly (no orchestrator)

Do every step yourself, single-threaded. The parallel dispatch collapses but the discipline stands: **reporting-layer pick → rules lookup → gold → gate → green → re-report**. Pick the track from the objective; if none is stated, default to the highest-value surface across all three entry reports (untrustworthy GREEN > raw coverage > taxonomy build-out, unless the user's objective says otherwise).

## File quick reference

| Concern | Path |
|---|---|
| Parse track (full doctrine) | [.claude/skills/mast-tdd-loop/SKILL.md](../mast-tdd-loop/SKILL.md) · [PIPELINE.md](../mast-tdd-loop/PIPELINE.md) · [FANOUT.md](../mast-tdd-loop/FANOUT.md) |
| Legacy-engine track | [LEGACY-ENGINE.md](LEGACY-ENGINE.md) |
| Accretion track (ADR-3) | [ACCRETION.md](ACCRETION.md) |
| Error-check track (span-witness QA) | [ERROR-CHECK.md](ERROR-CHECK.md) · `nx run mast:span-witness` · `docs/design/span-witness-triage.md` |
| The taxonomy ADR | `libs/mast-interaction/docs/adr/0003-taxonomy-redesign.md` |
| End-to-end pipeline topology | `docs/design/system-topology.md` |
| Interaction gold schema | `tests/magic-ast-tests/Fixtures/Interactions/golds/README.md` |
| Rollup flow | `tests/magic-ast-tests/Flows/InteractionRollup/` (`dotnet run -- --flow InteractionRollup`) |
| Demand overlay flow | `tests/magic-ast-tests/Flows/TopologyDemand/` (`dotnet run -- --flow TopologyDemand`) |
| Judges | [mast-judge](../mast-judge/SKILL.md) · `interaction-judge` (agent) |
