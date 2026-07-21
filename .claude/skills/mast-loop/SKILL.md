---
name: mast-loop
description: The single entry point for the MagicAtlas TDD loop — extending the parser (MagicAST) AND the interaction engine (ports, edges, the taxonomy) as one cohesive cycle. Every run starts at the Flowthru reporting layer, picks a TRACK by objective (parse coverage / interaction coverage & correctness), and closes a gold-gated slice. Use when driving any MAST/engine TDD round, picking high-value work from triage, authoring a parse gold OR an interaction gold, burning down topology holes, chasing a wrong port, or when the user references "the loop", "mast-loop", "the TDD loop", "the accretion loop", or issue #7.
---

# MAST loop — the unified parse + engine TDD cycle

One loop, two tracks. Every track is TDD: a **gold is a committed failing test**; you extend the system to meet it; nothing lands unless the gate is green. What differs per track is only three things — **entry report → gold artifact → gate**. This umbrella owns what's shared; each track's detail lives in a sub-doc.

**The entry is always the reporting layer.** You come in with an *objective* (raw coverage / product recall / taxonomy build-out / a wrong port), not a card. The `_08_Reporting` Flowthru artifacts are the triage surface; the objective picks the track, the track picks the report, the report picks the work. Never start from a card you have in mind — start from what the reports say is highest-value *for the current objective*.

## The two tracks

Both add coverage; the Interaction track also fixes what's wrong (false-positive ports, mis-attributed spans) as a standing part of its own cycle, not a separate pass you schedule later. Same spine for both: **entry report → artifact → gate**.

| Track | Objective | Entry report(s) | Gold artifact | Gate | Detail |
|---|---|---|---|---|---|
| **Parse** | Extend the AST — more cards/oracle text parse to structure | `triage-report.json` (`topYieldClusters` = L0, `topResidualClusters` = L1→L2) + `combo-anchor-report.json` (demand-first hubs) | `Fixtures/HandParsedCards/**` AST gold | `nx run mast:test` + `mast-judge` | [mast-tdd-loop/SKILL.md](../mast-tdd-loop/SKILL.md) |
| **Interaction** | Reconstruct more combos, keep reconstructions trustworthy, grow the port taxonomy, and catch wrong ports before they poison an edge | `combo-anchor-report.json` / `bench:recall` / `port-label-census.json` (`topProjectionGaps`) / `port-topology-demand.json` (value-ranked holes) / `span-witness-report.json` (wrong-port suspects) | PortWalk flow arm + `combo-axis-expectations.json` pin, OR interaction gold under `Fixtures/Interactions/golds/`, OR a span/gold correction | `nx run bench:recall` + `interaction-judge`, plus `nx run mast:test` + a clean `span-witness` run at round-end | [INTERACTION.md](INTERACTION.md) |

Pick by where the marginal return is highest. Raw coverage bottleneck → **Parse**. A popular combo reconstructs AMBER-that-should-be-GREEN, a false-GREEN poisons the product, a demand-ranked taxonomy hole is worth witnessing, or a port is lying about its own text → **Interaction** (an untrustworthy GREEN or a wrong port outranks any coverage gain — pick the specific currency inside the track per [INTERACTION.md](INTERACTION.md)).

## A cross-cutting fact: the parse-inside-engine bridge

An Interaction-track unit frequently **cannot be closed until a parse gap closes**: a port is an AST query, so a stem/attribute the parser doesn't yet emit is not derivable, and a flow arm or gold built around it isn't honest. The `manner` facet, the `Sacrificed` trigger event, per-cost spans, and the Deadeye blink slice are all exactly this shape — Interaction-track targets blocked on Parse work.

So the loop is unified: **an Interaction-track task that finds its port isn't AST-derivable spawns a Parse sub-slice inline** — dispatch a `mast-worker` against the parser gap (full [Parse track](../mast-tdd-loop/SKILL.md) discipline), land it, *then* return and finish the arm/fix/gold. Do not work around a missing parse; close the parse, then finish faithfully. This is the concrete reason parse and engine are one cohesive system rather than two skills that hand off.

## Shared machinery (stated once)

Every track reuses the same infrastructure — detailed in the Parse track's [SKILL.md](../mast-tdd-loop/SKILL.md) and [FANOUT.md](../mast-tdd-loop/FANOUT.md); summarized here so a track sub-doc need not repeat it:

- **Worktree-isolated workers → orchestrator serial-merge.** Workers run in `isolation: worktree` and never merge; the orchestrator merges serially with a rebuild + gate between groups. All git via `git -C "$WORKTREE_ROOT"`.
- **Two judges, both read-only, both Opus.** `mast-judge` (parser doctrine: AST shape, no-unparsed, describe-not-execute) for Parse; `interaction-judge` (CR-correctness of port→port edges and tiers, the false-GREEN guard) for the Interaction track. A judge FAIL halts the merge.
- **Deterministic gates.** Nonzero exit = unconditional HALT with the gate output quoted. The gate set differs per track (see each sub-doc); the discipline does not.
- **Golds are immutable to the worker that consumes them.** Whoever authors a gold owns it; a worker closing a gap never edits a gold to pass. Re-pointing other golds after a system change is orchestrator back-prop (gate + mandatory re-judge), never worker work.
- **Model policy.** Opus orchestrator + Opus judges, always. Sonnet workers only for clear-cut, well-briefed mechanical slices.
- **Fan-out.** Large sessions run as the deterministic Workflow harness ([FANOUT.md](../mast-tdd-loop/FANOUT.md)), not hand-managed spawns.
- **Orchestration-layer costs are cheap; per-subagent costs are not.** A corpus-wide refresh (regenerating `card-ports.json`, a full report re-run) run once by the orchestrator between rounds is a fine cost to pay (tens of seconds). The same refresh run inside every worker, or once per subagent in a fan-out, is not — it multiplies. Keep whole-corpus operations at the orchestrator/triage layer; brief workers with pre-computed facts instead of having each one re-derive them.

## The cycle (shared spine)

```
Step 0  Pre-flight: executing mode (not plan) + worktree base + refresh the reporting layer for the chosen track.
Step 1  Pick the TRACK by objective, then N units of work from that track's entry report (value-ranked).
Step 2  Brief each unit inline (rules facts from rules-structure.json; the CR number + verbatim text).
Step 3  Dispatch N isolated workers, one per unit. An Interaction-track unit blocked on parse spawns a Parse sub-slice first (the bridge above).
Step 4  Judge novel-shape work (mast-judge for parse; interaction-judge for edges) → verdict JSON → gate.
Step 5  Merge serially in file-affinity order; the track's CORE gate green after each group.
Step 6  Regenerate the track's reporting artifacts; for the Interaction track, also run the round-end span-witness check (INTERACTION.md Step 6); loop or stop.
```

Each track specializes Steps 1 / 3-gold / 4-judge / 5-gate / 6-report; the spine is invariant. Follow the track sub-doc for the specialization.

## When invoked directly (no orchestrator)

Do every step yourself, single-threaded. The parallel dispatch collapses but the discipline stands: **reporting-layer pick → rules lookup → gold → gate → green → re-report**. Pick the track from the objective; if none is stated, default to the highest-value surface across both entry reports (untrustworthy GREEN or a wrong port > raw coverage > taxonomy build-out, unless the user's objective says otherwise).

## File quick reference

| Concern | Path |
|---|---|
| Parse track (full doctrine) | [.claude/skills/mast-tdd-loop/SKILL.md](../mast-tdd-loop/SKILL.md) · [PIPELINE.md](../mast-tdd-loop/PIPELINE.md) · [FANOUT.md](../mast-tdd-loop/FANOUT.md) |
| Interaction track (coverage, correctness, taxonomy) | [INTERACTION.md](INTERACTION.md) |
| Span-witness QA mechanics (used by Interaction Step 6, also standalone) | [ERROR-CHECK.md](ERROR-CHECK.md) · `nx run mast:span-witness` · `docs/design/span-witness-triage.md` |
| The taxonomy ADR | `libs/mast-interaction/docs/adr/0003-taxonomy-redesign.md` |
| End-to-end pipeline topology | `docs/design/system-topology.md` |
| Interaction gold schema | `tests/magic-ast-tests/Fixtures/Interactions/golds/README.md` |
| Rollup flow | `tests/magic-ast-tests/Flows/InteractionRollup/` (`dotnet run -- --flow InteractionRollup`) |
| Demand overlay flow | `tests/magic-ast-tests/Flows/TopologyDemand/` (`dotnet run -- --flow TopologyDemand`) |
| Judges | [mast-judge](../mast-judge/SKILL.md) · `interaction-judge` (agent) |
