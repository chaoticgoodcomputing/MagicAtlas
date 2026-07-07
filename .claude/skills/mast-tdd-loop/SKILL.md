---
name: mast-tdd-loop
description: Drives a TDD cycle for extending MagicAST (the Magic-the-Gathering oracle-text parser at libs/magic-ast/). Pick unparseable cards from triage, hand-parse the gold AST, run the NUnit suite to surface schema and parser gaps, then close each gap with a new AST node or parser rule. Every test must be green to land a batch — no ratchet tolerance. Use when extending MagicAST coverage, working on the MAST TDD loop, hand-parsing a card, adding a new AST node or ability/effect/cost type, adding an ability-kind parser, or when the user references issue #7, "mast-tdd-loop", "MAST round-trip", or "the MAST cycle".
---

# MAST TDD loop

Drives one round of extending MagicAST. Each round starts at parser gaps surfaced by triage, ends with new AST nodes and/or parser rules that close them, lands `nx run mast:test` at 100% green (vanilla NUnit, no ratchet tolerance), and rolls the corpus-wide triage forward.

**Default dispatch model: combined agents.** The orchestrator splits the corpus into *families* and dispatches one sub-agent per family. Each agent does the whole vertical slice in one session — creates any new AST type, writes the gold fixture(s), extends the parser, runs the tests green, commits on its own branch. The orchestrator merges, gates, and re-triages. This is the path documented below.

The older two-phase **helper/mech split** (separate agents for AST authoring vs. parser work) is a fallback for the rare batch dominated by genuinely novel doctrinal shapes. It lives in [PIPELINE.md](PIPELINE.md). Don't reach for it by default — combined agents have proven out across multiple large mega-batches with no structurally-wrong AST landing, and the two-phase barrier (wait for ALL AST authors before ANY parser work) serializes the batch for no benefit when most families follow an established AST shape.

If you are invoked directly by the user with no orchestrator above you, do every step yourself, single-threaded. The parallel dispatch collapses, but the discipline (rule lookup → gold → parser → judge gate → green) still stands.

## Three kinds of effort (what a batch can close)

The loop's real objective is **trustworthy reconstructed interactions** (GREEN combos), which sit at the end of a four-stage funnel: **corpus scope → parse → projection → operator precision**. A card only contributes a reconstructable edge once it is *in scope*, *parses*, *projects* to a real port label, and *closes* through the certainty operators at GREEN. Parser coverage is only the second stage — so a batch can buy progress in three different currencies, and the right mix depends on where the marginal return is highest:

1. **Parse-family** (the workhorse) — close a parser gap so cards that don't parse now do. **Pick surface:** `triage-report.json` → `topYieldClusters[]`, ranked by `fusedScore` (Step 1). **Cost:** a gold fixture + parser rule per family. **Signal:** card/line coverage ↑.
2. **Projection-slice** (the cheapest edge inventory) — a card that **already parses** can still project to a coarse `emit:<x>` / `emit:unparsed` label that no flow arm reads, so it forms **no interaction edge** — ~3,600+ such cards exist, paid-for parse work sitting dark. Closing one means adding a **PortWalk flow arm** (`libs/mast-interaction/docs/adding-a-flow-arm.md`), usually **no parser or gold work at all** — and one arm lights up *every* card carrying that label, so the leverage is far higher than one-card-at-a-time parsing. **Pick surface:** `port-label-census.json` → `topProjectionGaps[]` (coarse emit labels ranked by the combo-popularity mass of the cards behind them — same value axis as `fusedScore`; regenerate with `nx run mast:run` for a fresh corpus, then `dotnet run -- --flow PortLabelCensus`). **Gates:** `PortWalkExhaustivenessTests` (the new arm must project or be whitelisted), the Step-8 edge-diff (a new arm SHOULD change edges — the target labels are the expected footprint), and `bench:recall`. **Signal:** ports/edges ↑, `emit:unparsed` card count ↓.
3. **Precision-fix** (multiplies the value of all coverage) — lift a reconstructed edge from **AMBER → GREEN** (usually a `Subsumes` type-proof the operator can't yet make), or kill a **false-GREEN** (an over-approximating port that fabricates an "infinite combo" — e.g. the death-payoff arm conflating self-death and other-death). Until GREENs are trustworthy the novel-combo product is worthless, so precision work retroactively raises the value of every parse and projection unit. **Pick surface:** the `bench:recall` AMBER/missed combos (`tools/bench/MagicAtlas.Bench/bench-report.json` + `combo-expected-tiers.json`) and the `interaction-judge` findings under `docs/judgments/`. **Gates:** `bench:recall` + the `interaction-judge` (the false-GREEN guard). **Signal:** RecallAtGreen ↑, judge-PASS rate ↑.

**Default mix.** Parse-family remains the default when raw coverage is the bottleneck. But when `topProjectionGaps[]` shows high-mass coarse labels, a projection-slice batch buys more *edge* coverage per unit than parsing new cards (it monetizes cards already parsed); and whenever `bench:recall` shows AMBER combos that *should* be GREEN or the judge flags a false-GREEN, a precision batch comes first — an untrustworthy GREEN poisons the product regardless of how many cards parse. Weigh all three surfaces at Step 1, not just `topYieldClusters[]`.

## Invariants

These hold for every batch and every agent. They are stated once, here.

- **Gold AST = eventual truth, never a snapshot.** The hand-parsed JSON is what a fully-implemented parser *should* emit, not what the current parser emits. Never *any* `IUnparsed` node — neither `"Kind": "unparsed"` (UnparsedAbility) **nor `"EffectType": "unparsed"`** (UnparsedEffect, at any nesting depth) — never embedded `Diagnostics[]`, never `Pattern` strings copied from `FallbackParser`. Getting this wrong inverts the TDD direction — the test "passes" by matching the parser's current limitations. (This is the test-overfit guard: fixtures are the committed failing test; workers extend the parser to meet them, never edit them to pass.) **This is now machine-enforced (de-ratcheted: stateless invariant + explicit named whitelist):** `GoldFixtureUnparsedTests` (ADR 0001 goal b) fails any gold fixture carrying an `IUnparsed` node. The only escape is an explicit, NAMED whitelist FILE — `tests/magic-ast-tests/Fixtures/whitelist-unparsed.json` (each entry `{card, tag, reason}`), no longer an in-test HashSet. When your batch closes one of those cards' parser gap, the gold loses its unparsed node and the test tells you to **remove that card's entry** (the list only shrinks; a new entry must be a justified `irreducible` carve-out, never silent debt). This is the project-wide de-ratcheting philosophy (memory `tests-stateless-whitelist-over-ratchet`): an absolute invariant, a loud per-card failure, named/justified carve-outs — never a moving count baseline.
- **Fixtures are immutable to parser work.** Whoever writes the gold owns it. An agent closing a parser gap must NOT edit a fixture to make a test pass. If a gold looks wrong, STOP and report — orchestrator-side fix. **Machine-enforced:** `tools/gate-fixture-immutability.sh <base> <branch>` halts the batch if a worker branch modifies/deletes any existing gold (additions only). Legitimately re-pointing other cards' golds after a parser change is *orchestrator* back-prop (core-green + mandatory re-judge), never worker work — see Step 4's "Back-propagation".
- **Gold `Input` is seeded from the corpus, never hand-composed — pre-dispatch fidelity gate.** The gold's `Input` (OracleText + Name/ManaCost/TypeLine/P-T/Colors/ColorIdentity) is the *authoritative card text*, and it MUST be sourced verbatim from the corpus (`card-inputs.json`, or the Scryfall bulk for cards filtered out of the commander-legal corpus) — NEVER paraphrased, NEVER composed from memory, NEVER given a reminder-text the real oracle lacks. Run `tools/seed-gold-input.py "<Card>" …` (on the orchestrator/main, where the corpus lives) to emit each card's authoritative `Input`; embed it **verbatim** in the worker brief. **Why this is an invariant, not a nicety:** `GoldOracleTextFidelityTests` (gold `Input.OracleText` == corpus) is the check that catches drift — but it **SKIPS in worker worktrees** (the gitignored corpus is absent), so a mis-transcribed Input is invisible to the worker *and* the judge and only surfaces at the orchestrator's post-merge CORE gate, *after* the worker built a parser against the wrong text. So the fidelity check runs **between transcription and delegation**: the orchestrator seeds + validates Input on main, then dispatches. The worker treats `Input` as fixed/authoritative and authors only the `Output` AST + parser. (This is the Maddening-Cacophony lesson — an orchestrator-composed kicker reminder the real card lacks — and the Peregrin-Took lesson — a drifted 2/3→1/2 P/T.)
- **GLOSSARY.md is orchestrator-only.** `libs/magic-ast/GLOSSARY.md` is the tracked, auto-generated AST index. Sub-agents **read it freely, never regenerate it.** `nx run magic-ast:glossary` runs on the integration branch, once, at the end of a batch. Any in-worktree regen is a guaranteed merge conflict for no benefit — no sub-agent's tests depend on the regenerated glossary.
- **All git uses `git -C "$WORKTREE_ROOT"`.** Capture `WORKTREE_ROOT="$(pwd)"` at session start. CWD-based git can land commits on the wrong branch.
- **MAST describes, it does not execute.** Model what oracle text *says*, not what the rules *do* at runtime. No turn-state, priority, stack ordering, or layering fields. (See memory `feedback_mast_describes_not_executes`.)
- **No ratchet tolerance.** The NUnit suite is 100% green to land a batch, full stop.

## Before you touch anything

Read these first — five minutes, saves hours.

1. **Agent memory:** `feedback_mast_describes_not_executes`, `reference_mtg_glossary_location`, `feedback_contributing_replaces_context` (in this workspace, library conventions live in `CONTRIBUTING.md`, not `CONTEXT.md`).
2. **`libs/magic-ast/GLOSSARY.md`** — every current AST node with discriminator strings and source links. Look here before inventing a node; many things already exist (`Quantity`, `ObjectFilter`, `TriggerCondition`, `UnlessClause`, the trait interfaces under `AST/Effects/Traits/`).
3. **`libs/magic-ast/CONTRIBUTING.md`** — terminology, AST styling, attribute conventions.
4. **`libs/mtg-rules/Data/_03_Primary/Datasets/glossary.json`** + **`rules-structure.json`** — parsed MTG Comprehensive Rules. Gitignored Flowthru intermediates, but copied into every sub-agent worktree via `.worktreeinclude`, so sub-agents can `jq` them directly:
   ```bash
   jq '.terms["Deathtouch"]' libs/mtg-rules/Data/_03_Primary/Datasets/glossary.json
   ```

## The cycle (orchestrator)

```
Step 0   Pre-flight: confirm execute mode + worktree base + refresh triage (nx run mast:interaction-triage).
         GATE: bash tools/gate-preflight.sh — HALT on nonzero.
         BASELINE: snapshot the corpus-edge signatures of the just-triaged tree —
         tools/corpus-edge-signatures.py tests/.../Data/_08_Reporting/card-edges.json > /tmp/edge-base-{batch}.json
         (the Step-8 overfit gate diffs against this).
Step 1   Pick N families from triage. A family = (pattern, lastAttemptedRule) cluster
         with 1-3 fixtures sharing one parser failure point. Respect hot-file caps.
Step 2   Brief each family inline → docs/judgments/briefing-{date}.md (rules facts).
         SEED + GATE Input: `tools/seed-gold-input.py "<Card>" …` emits each card's AUTHORITATIVE
         gold Input from the corpus; embed it verbatim in the brief (never hand-compose oracle text).
         Cards it flags MISSING are not dispatched. This is the pre-dispatch oracle-fidelity gate.
Step 2.5 Assignment matrix (family | model | anticipated updates) → run the collision pre-check.
Step 3   Dispatch N combined agents in parallel (worktree isolation), one per family.
         Each worker self-gates isolation: bash tools/gate-isolation.sh <base>.
Step 4   Judge novel-shape branches (per policy) → verdict JSON.
         GATE: bash tools/gate-judge-verdict.sh <verdict.json>      — HALT on any FAIL.
         GATE: bash tools/gate-fixture-immutability.sh <base> <br>  — per branch; HALT on illicit gold edit.
Step 5   Merge by file-affinity order. NUnit gate after each merge group.
Step 6   NUnit 100% green required (joint regressions surface here).  [CORE merge gate]
Step 7   Regenerate GLOSSARY.md once on the integration branch, commit.
Step 8   Re-run triage: nx run mast:interaction-triage (fast slice — worklist + card-edges + port-graph-metrics; NO slow viz tail).
         GATE: bash tools/gate-corpus-edge-diff.sh /tmp/edge-base-{batch}.json tests/.../Data/_08_Reporting/card-edges.json "<dispatched-card-names,csv>"
               — HALT if a NON-target card's interaction footprint changed (the OVERFIT/sibling-mislabel class — the #1 recurring FAIL, else invisible to the worker suite + the 33-combo bench). A legitimate cross-card reprojection is a named entry in tests/magic-ast-tests/Fixtures/edge-diff-expected.json, never silent.
         GATE: nx run bench:recall (per-combo expected-tier gate — HALT if any combo's tier drifts from its pin).
         Reap worktrees (nx run mast:worktree-clean). Report (incl. recall numbers). Loop or stop.
```

**Deterministic gates (the loop's safety floor).** Five meta-gates convert former agent *promises*
into nonzero exit codes; a nonzero exit is an **unconditional HALT** with the gate's output quoted
in the batch report. The fifth, `gate-corpus-edge-diff.sh` (Step 8), is the overfit defense: it diffs
the per-card port-projection signatures of `card-edges.json` (the ~2,900-card union graph) between the
batch base and the merged tip, HALTing if any NON-dispatched card's interaction footprint changed —
mechanizing the sibling-mislabel sweep the judge did by hand (the #1 recurring FAIL class, invisible to
the worker suite — siblings have no golds — and to the 33-combo bench). It complements, never replaces,
the mast-judge. They run only inside the live loop (they need ephemeral state — a base sha, a
branch, the worktree pool, a verdict file), so they are bash, not NUnit, and never run in CI. The
*core* ring (`nx run mast:test` — gold fidelity, no-unparsed, round-trip) is the snapshot check and
is what CI runs; the meta-gates are the transition check. See
[01_deterministic-loop-gates.md](../../../docs/scratch/alignment-session/01_deterministic-loop-gates.md)
for the full two-ring model. Self-test the gates with `nx run mast:gate-test`.

### Step 0 — Pre-flight

- **Permission mode.** You (orchestrator) must be in an executing mode (`default`/`acceptEdits`), NOT plan mode, before dispatching. Sub-agents inherit the parent's permission mode and cannot escape it — a child dispatched from plan mode will *propose* edits instead of making them, no matter what its prompt says. This is the root cause behind stalled "zero-commit" agents; the prompt-level "do not enter plan mode" mandate (Step 3) is the second half of the fix, not the whole fix.
- **Worktree isolation + base — BOTH are required.** These are two independent settings, and getting either wrong corrupts the run (this is the root cause of the batch-1 base-contamination incident):
  1. **Every `Agent` spawn MUST pass `isolation: "worktree"`.** Without it the agent runs *in the orchestrator's own checkout* and its `git checkout`/commit moves the primary branch — that is exactly what hijacked the integration branch and stranded agents on a stale ancestor. No exceptions; a spawn missing it is a bug, not a shortcut.
  2. **`worktree.baseRef: "head"`** is set in `.claude/settings.json`, so each isolated worktree branches from the **current local HEAD** — whatever branch you have checked out (e.g. `feat/mast-improvements`), **not** `main`/`origin/HEAD`. This is correct; do not "fix" it toward `main`. The integration branch is wherever you are checked out, and you merge agent branches back into *that* — there is no `main` in this loop.
- **Canary the isolation, don't trust the config.** Before a large batch, spawn ONE agent whose prompt's first action runs `bash tools/gate-isolation.sh <base sha>` (the same gate workers run). If it exits nonzero — toplevel is the main repo, or HEAD is a stale base — STOP; isolation is broken; fix it before dispatching the rest.
- **GATE — preflight hygiene:** `bash tools/gate-preflight.sh`. Nonzero exit (too many `mast-tdd/*` branches, too many agent worktrees, or a dirty tree) is a HALT: clean up (`nx run mast:worktree-clean`, commit/stash) and re-run before dispatching. This folds the old "remember to reap" reminder into a hard gate. Thresholds are tunable via `MAST_MAX_TDD_BRANCHES` / `MAST_MAX_WORKTREES`.
- **Refresh triage — ORDER MATTERS for the fused pick surface.** Run `nx run mast:interaction-triage` **before** `nx run mast:run`. The interaction slice regenerates `interaction-triage-report.json` (the `allComboBlockingCards` value map); `mast:run`'s `AggregateTriageReport` then reads that file and folds each card's combo-popularity mass into the cluster `fusedScore`. Run them the other way (or skip the interaction slice) and the aggregate falls back to the *previous* cycle's value map — still valid (combo popularity is stable batch-to-batch, and the fusion degrades to plain `fractionalYield` if the file is absent), just one refresh stale. The value map lags the corpus parse by at most one refresh either way, which is negligible for ranking. (Downstream, Step 8's `mast:interaction-triage` re-run keeps the map current for the *next* cycle's Step 0.)

### Step 1 — Pick families

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. **`topYieldClusters[]` is the primary pick surface; the two `topGaps` lists are diagnostics.**

- **`topYieldClusters[]`** (PRIMARY — data-derived) — unparsed lines clustered by normalized lexical template, ranked by **`fusedScore`** — parse-proximity **weighted by the downstream combo value each surface unblocks**. Each cluster is a **buildable family** — one normalized template → one parser surface — and carries: `template`; **`fusedScore` (the primary ranking key = `fractionalYield × (1 + interactionValueScore)`)**; `fractionalYield` (proximity-weighted parse yield: each card contributes `1/(distinct templates on it)`, so it ranks high when it's the last-or-near-last missing piece across many nearly-complete cards); `directYield` (whole-card flips, a hard floor); **`comboBlockedCount` + `comboPopularityMass` (the combo-value axis — how many CSB combos, and how much popularity mass, this surface's cards gate, joined from the InteractionTriage `allComboBlockingCards` overlay with the same `1/N` attribution as `fractionalYield`)**; **`interactionValueScore` (`log10(1 + comboPopularityMass)` — the bounded value boost, 0 when the surface unblocks no known combo)**; `dominantPattern` + `dominantLastAttemptedRule` (the "where it fails" navigation hint — which parser the template bails in); `dominantShare` (diagnostic-spread homogeneity in [0,1] — see the gate below); and `exemplars[]` (each with `input` DTO and `alreadyHandParsed`, ready to hand-parse). ~50 entries deep, so the long tail (small/partial-card families like specific triggered abilities) is visible, not just the top whole-card flips. **The template is the family unit; `dominantPattern`/`dominantLastAttemptedRule` are annotations, not the key** — this is what splits coarse buckets (e.g. "UnparsedTriggered" — proliferate-trigger, roll-a-d20, play-a-card-trigger each surface as their own cluster). **Fusion is graceful:** with no InteractionTriage value map present, every `interactionValueScore` is 0, `fusedScore` collapses to `fractionalYield`, and the surface ranks exactly as the pre-fusion loop did — so the pick surface is never *worse* than parse-yield-only, only better-informed when the overlay is fresh.
- **`topGaps[]`** / **`topGapsByLineFrequency[]`** (DIAGNOSTIC) — failures grouped by the coarse `(pattern, lastAttemptedRule)` key, ranked by fractional yield and by raw line frequency respectively. Use these to *see where the parser bails broadly* (e.g. "4747 cards bail in TriggeredAbilityParser.Parse"), NOT as a pick surface — a single entry usually spans several distinct families, so it's not a pickable unit. If a gap looks interesting, find the matching `topYieldClusters[]` entries to get the actual buildable families.

**Heuristic:** pick families off `topYieldClusters[]` top-down by `fusedScore` (the default sort). The three sub-signals let you read *why* a cluster ranks where it does: high `directYield` = flips whole cards now; high `fractionalYield` with low `directYield` = chips many cards toward done (good when coverage is high and most cards have several gaps); high `comboPopularityMass` / `interactionValueScore` = the surface gates popular combos, so closing it converts directly into reconstruction coverage (the loop's actual objective — see the bench gate at Step 8). A cluster with strong `fractionalYield` but near-zero `comboPopularityMass` is pure parse-hygiene: still worth doing when coverage is the goal, but it will (correctly) rank below a combo-unblocking surface of comparable parse yield. Cross-reference the diagnostic gap lists only to understand *where* in the parser the work lands.

For each family: select **1-3 fixtures** from the cluster's `exemplars[]` (already ranked cleanest-first by fewest other unparsed templates; skip `alreadyHandParsed: true`). The low count is deliberate — N agents × 5 fixtures is unsustainable merge overhead, and coverage-per-fixture is the optimization target. (`OtherUnparsedClusters: 0` is low-risk, not zero-risk — a multi-clause non-target line can still fail and force a worker STOP; improving that signal is a triage concern, not a per-batch worry.) **Diversity check:** the 1-3 should vary the dimensions the parser surface must handle, not be near-duplicates. When exemplars are stacked with multi-keyword legendaries, pre-curate cleaner single-line non-legendaries from `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` with a `jq` regex on the cluster template.

Choose batch size by the number of **non-overlapping** families, constrained by hot-file caps (below). Two families targeting the same hot parser file serialize across batches.

**GATE — cluster homogeneity (initiative 02).** Clusters are grouped by *exact* template, so members share the template by construction; the residual risk is template **over-collapse** — one template lumping lines that bail in *different* parsers (e.g. `<SUBTYPE> <TYPE>` covering "Enchant land" and unrelated lines). A worker handed such a "family" lands a rule correct for only part of it. Each cluster carries `dominantShare` (the fraction of its failure signals that are the single dominant `(pattern, rule)`); below ~0.85 the cluster is heterogeneous. Before dispatch, guard your intended picks:

```bash
bash tools/gate-triage-cluster.sh tests/magic-ast-tests/Data/_08_Reporting/triage-report.json <rank> [<rank>...]   # nonzero → that pick is heterogeneous; HALT it
bash tools/gate-triage-cluster.sh tests/magic-ast-tests/Data/_08_Reporting/triage-report.json                       # no ranks → audit: list all heterogeneous clusters
```

A heterogeneous cluster is **excluded from dispatch** (pick a different one, or hand-curate cleaner exemplars from `oracle-cards.json` and treat them as their own narrower family) — it is not a batch HALT, it's a pick exclusion. Tune via `MAST_MIN_HOMOGENEITY`.

**2-strike rule — a twice-failed card is a dedicated-surface card, not a batch-card.** A card that FAILs the judge or merge gate **twice across batches for different root causes** (e.g. Rings of Brighthearth: unanchored-regex overfit, then a free-text residual) needs bespoke parser-surface design — it is not closeable by re-dispatch. STOP re-dispatching it: promote it to [`libs/magic-ast/docs/dedicated-surfaces-design.md`](../../../libs/magic-ast/docs/dedicated-surfaces-design.md) (with the exact gold + the missing surfaces) and exclude it from the *per-batch* slate. Strike count is mechanical from the dated `docs/judgments/verdict-{date}-{batch}.json` artifacts — not memory. (A card that FAILs twice for the SAME, simple, well-specified reason is a normal carry-forward, not a 2-strike defer.)

  **The dedicated-surfaces backlog is a value-ranked queue, NOT a graveyard.** The 2-strike defer removes a card from the *batch slate* (it can't be fanned out) — it must NOT quietly park high-value cards forever, and by construction the hard-to-parse cards are often the high-combo-value ones (Rings of Brighthearth alone gates **124 combos**). So rank the backlog by each card's combo value (its `blockedComboCount` × popularity mass in `interaction-triage-report.json`'s `allComboBlockingCards` — the same value axis as `fusedScore`; annotate each `dedicated-surfaces-design.md` entry with its count) and periodically pull the **highest-value** entry into a **dedicated single-card effort** — a batch of one, on Opus, with the design budget a bespoke surface needs (the spec is already written in that doc). A top-of-backlog dedicated-surface card outranks a routine parse-family pick; do one such focused card whenever the backlog's top entry out-values the batch's `fusedScore` leaders. Deferral is a scheduling decision (fan-out won't close it), never a value judgment (it's still worth closing).

### Step 2 — Brief each family

Write one batch briefing to `docs/judgments/briefing-{YYYY-MM-DD}.md` (suffix `-N` if it exists). One section per family, **~200 words, informative not prescriptive** — establish rules facts, don't dictate AST shape (agents own that). For each family: identify the MTG mechanic(s) and **pull the canonical rule data — the exact CR rule number(s) AND verbatim quoted text — from `rules-structure.json`** (`jq` it; do not paraphrase a number from memory). Write: failure signal, cards in family, **relevant rules (number + quoted text — this block is the ground truth the agent cites verbatim in doc-comments and the judge cross-references, so it must be accurate)**, AST types likely in scope (convenience pointer to GLOSSARY.md, not a whitelist), expected generalization, anti-patterns. The full briefing template is in [PIPELINE.md](PIPELINE.md#briefing-template). Pulling the rule data here, once, on the orchestrator side is what prevents agents from hallucinating rule numbers.

If a family's mechanic isn't in `glossary.json` at all (and is genuinely MTG-domain, not vernacular), don't dispatch — swap the family or escalate to the human.

### Step 2.5 — Assignment matrix (pre-dispatch checkpoint)

Before spawning anything, lay out an **assignment matrix** inline in the session — in your response, **not** written to a file (keep it out of the briefing doc; the briefing is for rules facts, the matrix is ephemeral dispatch reasoning) — one row per family, three columns:

| Family | Model | Anticipated updates |
|---|---|---|
| `{id} — short name (cluster it closes)` | `opus` / `sonnet` + one-word why (novel-shape vs mechanical) | new node? which existing rule(s) extended? which **shared** AST primitive touched (`ObjectFilter`, `Duration`, an effect node, `AbilityClassifier.cs`)? |

This surfaces the dispatch reasoning — which until now lived only in the orchestrator's head — in the session *before* N agents run, where you (and the user) can review it. It exists to force three checks while they're still cheap:

1. **Collision pre-check (the main payoff).** Read down the *Anticipated updates* column for two families that would write the **same file** — a shared AST node (`AddManaEffect`, `PreventDamageEffect`, `ObjectFilter`, `Duration`, …) or `AbilityClassifier.cs`. New one-file-per-rule surfaces never collide; **shared-file writes do.** If two rows target one file, serialize them or re-scope one **now** — this is the cheap, pre-dispatch version of the Steps 5-6 joint-regression catch.
2. **Model commitment.** The *Model* column is the single source for Step 3's per-spawn `model:` override — decided once, with rationale visible, not ad hoc at spawn time.
3. **Judge scope.** A row whose *Anticipated updates* names a new node, new effect-trait, replacement effect, chosen-variable, or architectural change is a novel-shape row → it will be judged in Step 4. Mark those rows so the judge dispatch is pre-scoped.

**The *Anticipated updates* column is a hypothesis, not a contract.** It's your best read from the Step-1 recon + GLOSSARY + a source-tree grep, used to find collisions and size the batch — agents still own their final AST shape. When reality diverges (a "mechanical" family turns out to need a new node; a "no new node" family adds one), that's signal for the *next* matrix, not a deviation to police, and **never** hand the matrix to the workers as a spec. (Corollary from the field: a card reading `OtherUnparsedClusters: 0` is only *necessary*, not sufficient, for "its non-target lines are faithfully parsed" — a greedy rule can yield a clean-looking but lossy parse. Treat the *Anticipated updates* of a multi-ability exemplar with that caution.)

### Step 3 — Dispatch combined agents

Spawn N agents in parallel, **one per family**, via `Agent` with **`subagent_type: "mast-worker"`** — the checked-in `.claude/agents/mast-worker.md` definition carries `isolation: worktree` (so isolation is the *default*, not a per-call param you can forget) plus the standing worker contract: the isolation self-check, path hygiene (never `cd`; relative paths only for Read/Write/Edit; `git -C "$WORKTREE_ROOT"` only for git), execute-don't-plan, the gold-AST authoring rules, targeted-test discipline, the trigger≠effect rule, never-touch-GLOSSARY, and no-self-merge. **Override the model per spawn** (`model: "opus"` for novel-shape/doctrinal-edge families; `model: "sonnet"` for mechanical ones) since the agent def leaves model inherited. Each agent does the full slice: new AST type if needed → gold fixture(s) → parser surface → targeted test green → commit. **Family contract:** prefer ONE consolidated parser surface, but **the goal is a green card — if closing it takes a paired condition+effect rule, or a second rule, that's fine; do what the card needs.** The contract is a guard against *misclassification*, not a hard cap: only bail with a sub-pattern breakdown when the family is genuinely heterogeneous (its cards fail for *different* reasons, demanding several unrelated `TryParse` shapes). A bail refines the triage taxonomy; it is not failure — but neither is adding a second rule to finish a coherent family.

Because the standing contract lives in `mast-worker.md`, **each dispatch prompt only needs the family-SPECIFIC payload** (self-contained — the agent shares no conversational context with you):

- **Base sha + branch name.** State the exact base sha the worktree must show (so its `WRONG BASE` check is meaningful) and the branch to create: **`mast-tdd/<YYYY-MM-DD>-<slug>`**, where the date is the dispatch date. The date prefix makes batch age visible at a glance — `git branch --list 'mast-tdd/*'` (which sorts by refname) surfaces severely out-of-date leftovers immediately. **The separator is a hyphen, NOT a slash, deliberately:** `clean-worktrees.sh` reaps via `git for-each-ref 'refs/heads/mast-tdd/*'`, and `for-each-ref`'s `*` does *not* cross a `/` — a `mast-tdd/<date>/<slug>` branch would be invisible to the reaper and silently accumulate (the worktree-pool-bloat failure mode). Keep the whole `<date>-<slug>` as one path component. (Belt-and-suspenders: the worker re-verifies isolation itself, and Step 0's canary verifies it before the batch — but a wrong base sha is the one thing only you know.)
- **Hand the card data in-prompt — do NOT make the agent look it up.** Paste the chosen exemplar(s)' `Input` DTO **verbatim from `triage-report.json`**: `Name`, `ManaCost`, `TypeLine`, `OracleText`, `Power`, `Toughness`, `Colors`, `ColorIdentity` (for DFCs, the `CardFaces` block). The agent writes the fixture's `Input` straight from this. (The agent def already bans network/Scryfall and points at the local `oracle-cards.json` for alternates — but if you handed it a clean DTO it shouldn't need one. Reaching for Scryfall is a sign the DTO wasn't handed over, or *your* curated value was wrong; copy from triage, don't retype from memory.)
- **The CR rule(s): number + verbatim text**, pulled by you from `rules-structure.json` in Step 2. The worker cites only these.
- **Fixture path(s)**, family identity (`pattern`, `lastAttemptedRule`), and the briefing path.
- **Duplicate-work guard.** When two agents might add the same keyword/rule: "If [X] already exists when you read GLOSSARY.md, SKIP it and pick [alternate scope]."
- **Sibling-shape note** (when relevant): real cards are multi-ability; the agent may add a tight sibling parser surface only under the constraints in [PIPELINE.md](PIPELINE.md#sibling-shape-allowance), else bail on the multi-ability card.

(For the rare in-place, non-isolated task, dispatch `general-purpose` explicitly — that is now the exception, not the default.)

Wait for all N to report before merging.

### Step 4 — Judge

Dispatch the judge via `Agent` with **`subagent_type: "mast-judge"`** — the checked-in `.claude/agents/mast-judge.md` definition is READ-ONLY by construction (no `Write`/`Edit` tools) and runs **non-isolated in your checkout** (it needs to see un-merged branch refs via `git`; do NOT give it `isolation: worktree`). **The judge — like the orchestrator — MUST run on Opus** (high-effort, never downgraded): pass `model: "opus"` on the judge spawn. Only the mechanical *workers* may be Sonnet; the load-bearing judgment (orchestration, judging, serial merges) stays Opus. The judges have repeatedly caught silent semantic errors a Sonnet pass would wave through (dropped sibling effects, lost concepts, false GREENs) — that rigor is the whole point of the gate. It defers to `.claude/skills/mast-judge/SKILL.md` for doctrine. Your dispatch prompt names the specific branches + files + cited CR rules to judge, the base sha for `git diff <baseSha>..<branch>`, and the verdict JSON output path (`docs/judgments/verdict-{date}-{batch}.json`). **Policy:** judge any branch carrying novel-shape work (new AST types, replacement effects, combo depth, architectural changes, trigger/effect-separation or chosen-variable concerns); **skip** pure established-pattern branches (keyword additions mirroring existing patterns). **Always-judge override:** any branch (or back-prop commit) that *modifies an existing gold* is judged on those golds regardless of the skip policy — a changed gold is exactly the thing that must be re-blessed.

**Shard the judge — judging is read-only, so it is NOT on the serial-merge critical path.** The judge has no `Write`/`Edit` tools: it reads branch diffs + golds and emits a verdict. Nothing it does mutates shared state, so judging N branches does **not** need to be serialized — only the **merges** (Step 5, which move the integration branch and run the CORE ring) are serial. For a batch with many novel-shape branches, do NOT feed them all to one Opus judge in sequence (that is the throughput ceiling the loop kept hitting). Instead **partition the to-be-judged branches into disjoint shards and dispatch one `mast-judge` agent per shard, in parallel**, each on Opus, each writing its own verdict file `docs/judgments/verdict-{date}-{batch}-{shard}.json`. The natural partition already exists — reuse the Step-2.5 collision matrix: branches that touch independent families/files are independent to judge, so one shard per non-colliding group (or simply a fixed fan-out of ~3–5 branches per judge) keeps each judge's context small and focused, which *improves* rigor rather than trading it away. **Rigor is unchanged:** every branch still gets a full Opus judgment with CR citations; the parallelism is across shards, never a model downgrade and never a skipped branch. The verdict gate then runs over the union of shard files (below), and Step-5 merges stay strictly serial and gated on ALL shards PASSing. Net: the judge wall-time drops ~N× and stops being the batch bottleneck, while the serial part (merges) — which was always going to be serial — is unaffected.

The judge emits a machine-readable verdict JSON (`{ items: [{ target, verdict, citations, reason }] }`) alongside its prose. **The halt decision is a script, not a reading:**

```bash
bash tools/gate-judge-verdict.sh docs/judgments/verdict-{date}-{batch}*.json   # one arg per shard (glob); nonzero = any non-PASS / malformed / missing → HALT
```

A nonzero exit is an unconditional HALT — do NOT merge the offending branch; quote the gate output, remediate inline or via a focused follow-up agent, then re-judge and re-run the gate. There is no "concern" tier; the gate, not your judgment of the prose, decides.

**GATE — fixture immutability (per worker branch, before merge):**

```bash
bash tools/gate-fixture-immutability.sh <baseSha> <branch>   # nonzero = branch edits/deletes an existing gold → HALT
```

Run this on **every** worker branch (not only judged ones). Workers may only ADD fixtures; a worker that edited a gold to make its own test pass is the self-confirmation drift vector, and a nonzero exit halts the batch. (Legitimate back-prop is *your* job, off the worker path — see "Back-propagation" below.)

The judge verifies **doctrine** (`unparsed` in gold, describe-vs-execute, wrong AST shape/discriminator, missing required fields, free-text where structure exists) **and cross-references each cited CR rule** against `rules-structure.json`. This is cheap and reliable now that citations are orchestrator-sourced (Step 2) rather than agent-guessed: the judge confirms the cited rule exists and its text matches the modeling, and FAILs only on an absent-from-data or contradictory citation — not on subrule-letter precision.

### Back-propagation (orchestrator-only; off the worker path)

When a parser change legitimately re-points *other* cards' golds to a new eventual-truth (history shows this is bimodal — one card, or a systematic sweep; the largest single event re-pointed 154 golds), that is **your** action, never a worker's. The worker whose test is being made green is the wrong actor to also redefine other golds (the same self-confirmation vector, spread across cards). So the immutability gate is worker-scoped, and back-prop is governed by the **core ring + a mandatory re-judge** instead of an allowlist:

1. Edit the affected golds yourself on the integration branch.
2. `nx run mast:test` stays 100% green (core: gold fidelity, no-unparsed, round-trip).
3. **Mandatory re-judge** of the changed golds → verdict JSON → `bash tools/gate-judge-verdict.sh` passes.
4. Commit with the count + rationale in the message (e.g. "re-point 12 judge-verified golds — §6 self-binding"); that message is the audit trail.

The immutability gate does not run against your back-prop commit (it is worker-scoped, run on worker branches `<base>..<branch>`); the re-judge gate is what blesses a back-propped Output AST.

### Steps 5-6 — Merge and gate

Merge in **file-affinity order**, NUnit-gating after each group. **`nx run mast:test` is the CORE ring** — the same snapshot suite CI runs (gold oracle-text fidelity, no-unparsed-in-gold, round-trip); a red here is a corpus-correctness failure, distinct from the meta-gates above. Two individually-green branches can be jointly-red — Step 6 catches that, and no-ratchet-tolerance means any red halts the batch (roll back the merges, investigate per Stop conditions).

1. Unique-file rule agents first (the overwhelming majority — every keyword and spell/static/triggered/activated rule is its own file; trivial auto-merge; `--ours` on any GLOSSARY conflict).
2. `AbilityClassifier.cs` agents sequentially — the one remaining hot file; routing entries are additive, keep both sides.
3. Parser-orchestration agents last (rare): an agent that edited a thin dispatcher body in `TriggeredAbilityParser.cs`/`ActivatedAbilityParser.cs` (timing/split/multi-sentence only — adding a *rule* never lands here).
4. `nx run mast:test` after each group; final joint run must be 100% green.

**GATE — discriminator lint, after EACH merge group (initiative 02):** HALT on nonzero, then advance the baseline:

```bash
nx run magic-ast:lint-discriminators            # per-family duplicate (hard) + new near-dup w/o justification (soft)
nx run magic-ast:lint-discriminators:baseline   # advance schema/discriminator-baseline.json so "new" stays well-defined
git add libs/magic-ast/schema/discriminator-baseline.json && git commit -m "chore(mast): advance discriminator baseline after merge group"
```

Per-merge-group (not once at batch end) is the point: the lint reads the **merged source** directly, so it surfaces a concurrent duplicate/near-dup at the **first** merge that introduces it, not after the whole batch has landed (closing the concurrent-duplicate hole — initiative 02 #2). A hard fail (duplicate within a family) or an unexplained near-dup is an unconditional HALT — rename, or add a judge-reviewed entry to `libs/magic-ast/schema/discriminator-justifications.json` (`{name, near, reason}`). The same invariant is belt-and-braces in the core ring (`DiscriminatorUniquenessTests` in `nx run mast:test`) so anything that slips past the script still fails the suite. Uniqueness is **per-base, not global** — cross-base reuse (`untap` as Effect+Cost+ReplacementEvent) is legitimate.

**Projection exhaustiveness (initiative 03) rides the core ring too.** `PortWalkExhaustivenessTests` (in `nx run mast:test`) fails if a new discriminator is neither projected by PortWalk nor named in the explicit whitelist `libs/mast-interaction/known-coarse-projections.json` (each coarse discriminator carries a justification) — so a batch that adds an effect/cost/trigger/restriction discriminator must make a projection decision (worker contract). `PortWalkSentinelSnapshotTest` snapshots the full pipeline (parse → ports → flow edges → cycle tiers) over ~56 sentinels; a cross-pillar regression (a node-shape change silently dropping a port) fails it — regenerate via its `[Explicit]` test and justify the diff in the commit.

(`glossary:check` is deliberately NOT run per merge group: workers never regenerate `GLOSSARY.md`, so it is legitimately stale mid-batch and a `--check` would false-fail. GLOSSARY regen stays once at Step 7. The lint reads source, not the glossary, so it needs no fresh glossary to catch collisions.)

See [Hot files](#hot-files) for the conflict-resolution protocol.

### Steps 7-8 — Regenerate glossary, re-triage, loop

```bash
nx run magic-ast:glossary
git add libs/magic-ast/GLOSSARY.md
git commit -m "chore(mast): regenerate GLOSSARY after batch {date}"
nx run mast:run            # refresh corpus-wide triage
nx run bench:recall        # GATE: per-combo expected-tier whitelist (initiative 04, de-ratcheted) — HALT if any combo's tier drifts from combo-expected-tiers.json
git add tools/bench/MagicAtlas.Bench/bench-report.json  # commit if the gate advanced the baseline (a recall gain)
nx run mast:worktree-clean # reap this batch's worktrees + merged agent branches
```

**GATE — combo expected-tier (initiative 04, de-ratcheted), Step 8.** `nx run bench:recall` runs the interaction engine over the eligible Commander Spellbook combos; **`ComboExpectedTierTest` fails if ANY eligible combo's reconstruction tier drifts from its explicit pin in `tools/bench/MagicAtlas.Bench/combo-expected-tiers.json`** — a loud, per-combo gate (the end-product measure; parser-green ≠ product-green). A REGRESSION (e.g. Green→Amber) is an unconditional HALT: investigate before looping. An IMPROVEMENT (e.g. Missed→Amber) also fails — **update that combo's pin** to lock the gain (a named edit, never a silent baseline rewrite). `bench-report.json` is now a derived report, not the gate. **Put the tier summary (`Green / Amber / missed`) in the batch report**; the missed combos are the aligned worklist for the next pick (Step 1). **The gold-33 is the GATE, not the scoreboard** — its denominator is too small to show a batch's progress (a batch can unblock dozens of combos and move it by zero). For the batch SCOREBOARD, regenerate and quote the **wide measurement tier**: `dotnet run -- --flow CardAtlas` emits `Data/_08_Reporting/extended-recall-report.json` (schema `ExtendedRecallReport`) — reconstruction recall over EVERY projection-ready combo (thousands), with `green / amber / missed`, `recallAtGreen`, `recallAtAmber`, and a **popularity-weighted recall** (are we reconstructing the *popular* combos?). It has no pins and is not a gate (it needs the corpus, so main-only); it is the number that actually moves per batch. Report BOTH: the gold-33 tier line (regression gate) and the wide recall deltas (progress). (Free-text cleanliness — `GoldFreeTextWhitelistTests`, the de-ratcheted replacement for the old `DestringSinkRatchetTests` *count* baseline — rides the core ring in `nx run mast:test`: no gold may carry a free-text residual sink unless its exact `(card, sink)` pair is named on `tests/magic-ast-tests/Fixtures/whitelist-freetext.json`. Gates at Steps 5-6 automatically; no separate Step-8 call.)

**Reap worktrees every batch.** `nx run mast:worktree-clean` removes the batch's isolated worktrees (Claude only auto-removes *clean* ones; ours have commits) and deletes the now-**merged** `mast-tdd/*` + `worktree-agent-*` branches. Skipping this is how the pool reached 318 worktrees and forced the in-place-checkout fallback. For a **discarded** batch (branches unmerged), run `bash tools/clean-worktrees.sh --force` to also drop the unmerged branches.

**Sweep stale leftovers.** The reaper only drops *merged* branches (absent `--force`), so an unmerged branch abandoned by a prior batch lingers. The `<YYYY-MM-DD>` prefix makes these obvious — `git branch --list 'mast-tdd/*'` lists them date-first. When a `mast-tdd/<old-date>-*` branch is several batches stale, confirm it's abandoned (`git log -1` + `git merge-base --is-ancestor` to check it isn't merged), then drop it with `git branch -D`. Don't let undated/severely-stale branches accumulate — they're the slow path back to pool bloat.

If a batch has an intra-batch second wave that depends on new AST types, regenerate + commit GLOSSARY.md *between* waves so the second wave's briefing can cite accurate signatures. Then produce the batch report ([template in PIPELINE.md](PIPELINE.md#batch-report)) and loop to Step 1, or stop if returns are diminishing.

## Batch dispatch model

The orchestrator dispatches up to N sub-agents per batch (default N=20; mega-batches run 40–80). The binding constraint is **file affinity** — agents that create distinct files run fully parallel; agents that touch the same file merge sequentially.

| Group | Typical target files | Cap per batch |
|---|---|---|
| **Unique-file rule agents** (`Spell/Rules/`, `Static/Rules/`, `Triggered/Rules/`, `Activated/Rules/`, `Tokens/Keywords/`) | each creates a new reflection-discovered file (`[SpellRule]`, `[StaticRule]`, `[TriggeredRule]`, `[TriggerConditionRule]`, `[ActivatedEffectRule]`, `[ActivatedCostRule]`, `[StructuralKeyword]`) | Unlimited — never collide |
| **AbilityClassifier** | `AbilityClassifier.cs` (new routing entries) | ~4 — the one remaining hot file |
| **Parser orchestration** | a thin dispatcher body in `TriggeredAbilityParser.cs` / `ActivatedAbilityParser.cs` (timing/split/multi-sentence pre-pass only — adding a *rule* never lands here) | 1 per file — rare |
| **Combo-depth** | various — user-requested cards to 100% coverage | orchestrator judgment |

### Hot files

The one-file-per-rule reflection registry now covers keywords, spell rules, static rules, triggered conditions, triggered effects, and activated costs/effects — adding any of these is dropping a *new* file under the relevant `Rules/` (or `Tokens/Keywords/`) directory, so those agents never collide. **`AbilityClassifier.cs` is the only remaining hot monolith** (routing entries are still edited in-place).

**Resolver protocol (AbilityClassifier overflow):** cap it per the table and serialize overflow. If two agents must both edit `AbilityClassifier.cs`, resolve conflicts with a dedicated **resolver sub-agent** ("keep BOTH sides — these are additive routing entries"), which is faster and less error-prone than inline orchestrator resolution and keeps conflict noise out of orchestrator context. The same applies in the rare case two agents edit one parser's orchestration body.

**Architectural fix (mostly landed):** the merge-conflict-elimination refactor is essentially complete. Keywords (`Tokens/Keywords/`) and spell/static/triggered/activated rules each live one-file-per-rule and are reflection-discovered by descending `Priority`. The legacy `KeywordDefinitions.cs`, the monolithic `Parsing/OracleParsers.cs`, and the hand-ordered `TryParse*` dispatch chains inside the static/triggered/activated parsers are deleted — those parsers are now thin registry dispatchers (`Parsing/Combinators/OracleParsers.cs` survives only as a ~40-line shim onto the keyword registry). **Only `AbilityClassifier.cs` remains to convert** (one file per routing entry) to close the hot-file class entirely.

## Scaling out: the fan-out harness (v3) + delta-judge

For large sessions (10–20+ workers) the dispatch above runs as a deterministic **Workflow** harness, not hand-managed `Agent` spawns. The ratified protocol is [FANOUT.md](FANOUT.md); the runnable harnesses are [`scripts/tdd-fanout-harness-v3.js`](scripts/tdd-fanout-harness-v3.js) (parser/interaction TDD) and [`scripts/gold-burndown-execute-v2.js`](scripts/gold-burndown-execute-v2.js) (the delta-judge gold burndown). It encodes:

- **Reflection-first decomposition.** `RuleRegistry.Discover` auto-discovers ~300+ one-file-per-rule classes across the `[SpellRule]`/`[StaticRule]`/`[TriggeredRule]`/`[TriggerConditionRule]`/`[ActivatedEffectRule]`/`[ActivatedCostRule]`/`[Keyword]`/`[StructuralKeyword]` families — so a new rule/keyword is a **collision-free new file**. Prefer those; the shared-edit hazards (the `*RuleHelpers`, `ObjectFilter.cs`, `Characteristic.cs` `FromLabel`/discriminator, `AbilityClassifier.cs`, `ConditionParser.cs`, the flow-arm 3-layer dance) are the only things that must serialize.
- **Soft disjoint-touch-set waves.** The orchestrator declares each task's touched files and graph-colors the conflict graph into waves where no two workers share a file.
- **Worktree-isolated workers → orchestrator serial-merge (HYBRID throughput, ratified 2026-06-16).** Workers run in worktree isolation and never merge; the orchestrator merges serially with rebuild + gate between each. Provably-disjoint new-file branches get a targeted gate + a batched merge; shared-edit/interaction branches are serialized one-at-a-time under the full CORE ring; one full-ring consolidation runs at wave end. Revert-and-defer on red — every exit leaves a clean tree.
- **DELTA-JUDGE + partial-commit.** The judge verifies the slice structured *its* target residual correctly and introduced **no new** residual/regression — NOT whole-gold purity — so a multi-residual gold is cleaned incrementally across the slices that own its residuals (a leftover other-axis residual is expected, keeps its named whitelist entry, and does not fail). This is what resolved the coupled-gold failures (e.g. Mentor cards carrying both `attacking` and `lesser power`).
- **Model policy:** Opus orchestrator + Opus judges (always); Sonnet workers for clear-cut, well-briefed parser slices.

## Stop conditions

Bail and escalate if any of these hold.

**`[sub]`** — write the reason into your manifest's "Stop / handoff" line and exit; do not retry. Conditions:
- A trait-boundary decision (new `Effect` trait interface beyond the existing three) — HITL per `feedback_mast_describes_not_executes`.
- Oracle text drags in mechanics that challenge the descriptive/engine boundary (layering, replacement-effect ordering, priority). Surface the tension; don't paper over it.
- More than 3 consecutive reds without forward progress — likely misclassification or a deeper architectural gap.
- You need to edit infrastructure: `OracleParser.cs`, `AbilityParserRegistry.cs`, `PolymorphicReflectionConverter.cs`, or any `[PolymorphicBase]` base class (Ability, Effect, Duration, Cost, Quantity, ReplacementEvent, CardAttribute, PowerToughnessValue). That's a separate architectural ticket.
- An MTG term in oracle text isn't in `glossary.json`. Surface the gap; don't guess.
- The family is too coarse (cards fail for genuinely different reasons) — bail with a sub-pattern breakdown.

**`[main]`** — when a sub-agent reports a stop, route to human with its manifest; don't silently re-dispatch. Conditions:
- Post-merge NUnit isn't 100% green (semantic conflict between individually-green branches) — roll back, re-dispatch serially or escalate.
- `gate-judge-verdict.sh` exits nonzero (any non-PASS verdict) — do not merge; surface the gate output + offending branches, remediate, re-judge, re-run the gate.
- `gate-fixture-immutability.sh` exits nonzero (a worker edited an existing gold) — do not merge that branch; the worker should have STOPped. Investigate; if the gold genuinely needs changing, that is orchestrator back-prop, not worker work.
- `gate-preflight.sh` exits nonzero before dispatch — clean up (`nx run mast:worktree-clean`) and re-run; do not dispatch into a polluted environment.
- `ComboExpectedTierTest` fails (an eligible combo's tier drifted from its pin in `combo-expected-tiers.json`) — a REGRESSION means a batch silently lost interaction reconstruction. HALT; find which merged branch's node-shape/projection change dropped the cycle (the sentinel snapshot diff from initiative 03 usually localizes it); re-pin only genuine improvements, never a regression (that would paper over a real loss).
- Two agents claim the same `AbilityKind` or discriminator string — serialize: land one, re-dispatch the other against the post-merge tree.
- Two agents target the same hot parser file beyond its cap — serialize across batches.
- Post-batch triage shows fewer total successes than pre-batch — roll back and investigate.
- A pattern bucket fails to shrink across multiple batches — file a `FallbackParser.InferFailurePattern` refinement ticket (out of scope here).
- **Stalled agent:** completed with only Step 0 done and no parser/fixture work. Do not re-dispatch in the same mega-batch (scope may overlap with landing work); queue for the next batch.

## File quick reference

| Concern | Path |
|---|---|
| Current AST types (auto-generated) | `libs/magic-ast/GLOSSARY.md` |
| MAST conventions | `libs/magic-ast/CONTRIBUTING.md` |
| MTG Comprehensive Rules glossary | `libs/mtg-rules/Data/_03_Primary/Datasets/glossary.json` |
| Triage report | `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json` |
| AST nodes | `libs/magic-ast/AST/**/*.cs` |
| Effect trait interfaces | `libs/magic-ast/AST/Effects/Traits/` |
| Ability parsers | `libs/magic-ast/Parsing/Parsers/*.cs` |
| Failure-pattern inference | `libs/magic-ast/Parsing/Parsers/FallbackParser.cs` |
| Hand-parsed fixtures | `tests/magic-ast-tests/Fixtures/HandParsedCards/{set}/*.json` |
| Test diff dumps (on failure) | `/tmp/mast-diffs/{set}_{card}.expected.json` + `.actual.json` |
| Orchestrator merge-gate / triage / glossary (main checkout, `nx` available) | `nx run mast:test` / `nx run mast:run` / `nx run magic-ast:glossary` |
| Meta-gates (bash; HALT on nonzero) | `tools/gate-preflight.sh` / `tools/gate-isolation.sh` / `tools/gate-fixture-immutability.sh` / `tools/gate-judge-verdict.sh` |
| Gate self-tests (CI-safe) | `nx run mast:gate-test` (`tools/test/gates/run.sh`) |
| Combo-recall bench + per-combo expected-tier gate (end-product, Step 8) | `nx run bench:recall` (`tools/bench/MagicAtlas.Bench/`; gate = `combo-expected-tiers.json`; `bench-report.json` = derived report) |
| **Flow-arm reference pattern** (close a missed-combo family: project faithfully + connect in the engine) | [`libs/mast-interaction/docs/adding-a-flow-arm.md`](../../../libs/mast-interaction/docs/adding-a-flow-arm.md) |
| Worker targeted test (worktree — `nx` UNavailable, no `node_modules`) | `dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --filter "FullyQualifiedName~<CardNameNoSpaces>" --nologo` |
| Worker subagent / judge subagent definitions | [`.claude/agents/mast-worker.md`](../../agents/mast-worker.md) / [`.claude/agents/mast-judge.md`](../../agents/mast-judge.md) |
| Two-phase fallback + authoring reference | [PIPELINE.md](PIPELINE.md) |
| **Fan-out protocol (10–20 workers) + harnesses** | [FANOUT.md](FANOUT.md) · [scripts/tdd-fanout-harness-v3.js](scripts/tdd-fanout-harness-v3.js) · [scripts/gold-burndown-execute-v2.js](scripts/gold-burndown-execute-v2.js) |
| **Stateless cleanliness whitelists** (de-ratcheted; named carve-outs, not count baselines) | `tests/magic-ast-tests/Fixtures/whitelist-unparsed.json` · `whitelist-freetext.json` · `oracle-text-quarantine.json` (drift) |
| Gold burndown plan + long-tail disposition | `libs/magic-ast/docs/gold-burndown-plan.md` |
