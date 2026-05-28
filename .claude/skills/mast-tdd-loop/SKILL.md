---
name: mast-tdd-loop
description: Drives a TDD cycle for extending MagicAST (the Magic-the-Gathering oracle-text parser at libs/magic-ast/). Pick unparseable cards from triage, hand-parse the gold AST, run the NUnit suite to surface schema and parser gaps, then close each gap with a new AST node or parser rule. Every test must be green to land a batch — no ratchet tolerance. Use when extending MagicAST coverage, working on the MAST TDD loop, hand-parsing a card, adding a new AST node or ability/effect/cost type, adding an ability-kind parser, or when the user references issue #7, "mast-tdd-loop", "MAST round-trip", or "the MAST cycle".
---

# MAST TDD loop

Drives one round of extending MagicAST. Each round starts at parser gaps surfaced by triage, ends with new AST nodes and/or parser rules that close them, lands `nx run mast:test` at 100% green (vanilla NUnit, no ratchet tolerance), and rolls the corpus-wide triage forward.

**Default dispatch model: combined agents.** The orchestrator splits the corpus into *families* and dispatches one sub-agent per family. Each agent does the whole vertical slice in one session — creates any new AST type, writes the gold fixture(s), extends the parser, runs the tests green, commits on its own branch. The orchestrator merges, gates, and re-triages. This is the path documented below.

The older two-phase **helper/mech split** (separate agents for AST authoring vs. parser work) is a fallback for the rare batch dominated by genuinely novel doctrinal shapes. It lives in [PIPELINE.md](PIPELINE.md). Don't reach for it by default — combined agents have proven out across multiple large mega-batches with no structurally-wrong AST landing, and the two-phase barrier (wait for ALL AST authors before ANY parser work) serializes the batch for no benefit when most families follow an established AST shape.

If you are invoked directly by the user with no orchestrator above you, do every step yourself, single-threaded. The parallel dispatch collapses, but the discipline (rule lookup → gold → parser → judge gate → green) still stands.

## Invariants

These hold for every batch and every agent. They are stated once, here.

- **Gold AST = eventual truth, never a snapshot.** The hand-parsed JSON is what a fully-implemented parser *should* emit, not what the current parser emits. Never `"Kind": "unparsed"`, never embedded `Diagnostics[]`, never `Pattern` strings copied from `FallbackParser`. Getting this wrong inverts the TDD direction — the test "passes" by matching the parser's current limitations. (This is the test-overfit guard: fixtures are the committed failing test; workers extend the parser to meet them, never edit them to pass.)
- **Fixtures are immutable to parser work.** Whoever writes the gold owns it. An agent closing a parser gap must NOT edit a fixture to make a test pass. If a gold looks wrong, STOP and report — orchestrator-side fix.
- **GLOSSARY.md is orchestrator-only.** `libs/magic-ast/GLOSSARY.md` is the tracked, auto-generated AST index. Sub-agents **read it freely, never regenerate it.** `nx run magic-ast:glossary` runs on `main`, once, at the end of a batch. Any in-worktree regen is a guaranteed merge conflict for no benefit — no sub-agent's tests depend on the regenerated glossary.
- **All git uses `git -C "$WORKTREE_ROOT"`.** Capture `WORKTREE_ROOT="$(pwd)"` at session start. CWD-based git can land commits on the wrong branch.
- **MAST describes, it does not execute.** Model what oracle text *says*, not what the rules *do* at runtime. No turn-state, priority, stack ordering, or layering fields. (See memory `feedback_mast_describes_not_executes`.)
- **No ratchet tolerance.** The NUnit suite is 100% green to land a batch, full stop.

## Before you touch anything

Read these first — five minutes, saves hours.

1. **Agent memory:** `feedback_mast_describes_not_executes`, `reference_mtg_glossary_location`, `feedback_contributing_replaces_context` (in this workspace, library conventions live in `CONTRIBUTING.md`, not `CONTEXT.md`).
2. **`libs/magic-ast/GLOSSARY.md`** — every current AST node with discriminator strings and source links. Look here before inventing a node; many things already exist (`Quantity`, `ObjectFilter`, `TriggerCondition`, `UnlessClause`, the trait interfaces under `AST/Effects/Traits/`).
3. **`libs/magic-ast/CONTRIBUTING.md`** — terminology, AST styling, attribute conventions.
4. **`tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json`** + **`rules-structure.json`** — parsed MTG Comprehensive Rules. Gitignored Flowthru intermediates, but copied into every sub-agent worktree via `.worktreeinclude`, so sub-agents can `jq` them directly:
   ```bash
   jq '.terms["Deathtouch"]' tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json
   ```

## The cycle (orchestrator)

```
Step 0   Pre-flight: confirm execute mode + worktree base + refresh triage.
Step 1   Pick N families from triage. A family = (pattern, lastAttemptedRule) cluster
         with 1-3 fixtures sharing one parser failure point. Respect hot-file caps.
Step 2   Brief each family inline → docs/judgments/briefing-{date}.md (rules facts).
Step 3   Dispatch N combined agents in parallel (worktree isolation), one per family.
Step 4   Judge novel-shape branches (per policy). HALT on any FAIL.
Step 5   Merge by file-affinity order. NUnit gate after each merge group.
Step 6   NUnit 100% green required (joint regressions surface here).
Step 7   Regenerate GLOSSARY.md once on main, commit.
Step 8   Re-run triage. Report. Loop or stop.
```

### Step 0 — Pre-flight

- **Permission mode.** You (orchestrator) must be in an executing mode (`default`/`acceptEdits`), NOT plan mode, before dispatching. Sub-agents inherit the parent's permission mode and cannot escape it — a child dispatched from plan mode will *propose* edits instead of making them, no matter what its prompt says. This is the root cause behind stalled "zero-commit" agents; the prompt-level "do not enter plan mode" mandate (Step 3) is the second half of the fix, not the whole fix.
- **Worktree base.** The repo sets `worktree.baseRef: "head"` in `.claude/settings.json`, so each sub-agent worktree branches from current local `main` HEAD at spawn (not stale `origin/HEAD`). Sanity-check `git log --oneline -1 HEAD` matches `main` in any worktree. If a worktree references pre-consolidation paths or test counts disagree wildly with the briefing, STOP — its branch may predate the `baseRef` setting.
- **Refresh triage:** `nx run mast:run`.

### Step 1 — Pick families

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. **`topYieldClusters[]` is the primary pick surface; the two `topGaps` lists are diagnostics.**

- **`topYieldClusters[]`** (PRIMARY — data-derived) — unparsed lines clustered by normalized lexical template, ranked by **`fractionalYield`** (proximity-weighted: each card contributes `1/(distinct templates on it)`, so a template ranks high when it's the last-or-near-last missing piece across many nearly-complete cards). Each cluster is a **buildable family** — one normalized template → one parser surface — and carries: `template`, `fractionalYield` (primary signal), `directYield` (whole-card flips, a hard floor), `dominantPattern` + `dominantLastAttemptedRule` (the "where it fails" navigation hint — which parser the template bails in), and `exemplars[]` (each with `input` DTO and `alreadyHandParsed`, ready to hand-parse). ~50 entries deep, so the long tail (small/partial-card families like specific triggered abilities) is visible, not just the top whole-card flips. **The template is the family unit; `dominantPattern`/`dominantLastAttemptedRule` are annotations, not the key** — this is what splits coarse buckets (e.g. "UnparsedTriggered" — proliferate-trigger, roll-a-d20, play-a-card-trigger each surface as their own cluster).
- **`topGaps[]`** / **`topGapsByLineFrequency[]`** (DIAGNOSTIC) — failures grouped by the coarse `(pattern, lastAttemptedRule)` key, ranked by fractional yield and by raw line frequency respectively. Use these to *see where the parser bails broadly* (e.g. "4747 cards bail in TriggeredAbilityParser.Parse"), NOT as a pick surface — a single entry usually spans several distinct families, so it's not a pickable unit. If a gap looks interesting, find the matching `topYieldClusters[]` entries to get the actual buildable families.

**Heuristic:** pick families off `topYieldClusters[]` top-down by `fractionalYield`. High `directYield` = flips whole cards now; high `fractionalYield` with low `directYield` = chips many cards toward done (good when coverage is high and most cards have several gaps). Cross-reference the diagnostic gap lists only to understand *where* in the parser the work lands.

For each family: select **1-3 fixtures** from the cluster's `exemplars[]` (already ranked cleanest-first by fewest other unparsed templates; skip `alreadyHandParsed: true`). The low count is deliberate — N agents × 5 fixtures is unsustainable merge overhead, and coverage-per-fixture is the optimization target. **Diversity check:** the 1-3 should vary the dimensions the parser surface must handle, not be near-duplicates. When exemplars are stacked with multi-keyword legendaries, pre-curate cleaner single-line non-legendaries from `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` with a `jq` regex on the cluster template.

Choose batch size by the number of **non-overlapping** families, constrained by hot-file caps (below). Two families targeting the same hot parser file serialize across batches.

### Step 2 — Brief each family

Write one batch briefing to `docs/judgments/briefing-{YYYY-MM-DD}.md` (suffix `-N` if it exists). One section per family, **~200 words, informative not prescriptive** — establish rules facts, don't dictate AST shape (agents own that). For each family: identify the MTG mechanic(s), look them up in `glossary.json`/`rules-structure.json`, and write: failure signal, cards in family, relevant rules (quoted), AST types likely in scope (convenience pointer to GLOSSARY.md, not a whitelist), expected generalization, anti-patterns. The full briefing template is in [PIPELINE.md](PIPELINE.md#briefing-template).

If a family's mechanic isn't in `glossary.json` at all (and is genuinely MTG-domain, not vernacular), don't dispatch — swap the family or escalate to the human.

### Step 3 — Dispatch combined agents

Spawn N sub-agents in parallel via `Agent` with `isolation: "worktree"`, one per family (Opus for novel-shape or doctrinal-edge families; Sonnet for mechanical ones). Each agent does the full slice: new AST type if needed → gold fixture(s) → parser surface → tests green → commit. **Family contract:** make ALL the family's fixtures green via ONE consolidated parser surface (one new method, or one extension at `lastAttemptedRule`). If the agent finds itself writing N separate `TryParseX` methods, it has misread the family — bail with a sub-pattern breakdown (that bail refines the triage taxonomy; bailing is not failure).

**Every dispatch prompt MUST include all of these** (self-contained — the agent shares no context with you):

- **Execute, don't plan.** "Do NOT enter plan mode. Make edits, run tests, and commit directly." Inline the relevant steps rather than referencing this skill by name, which can re-trigger plan mode. (Combined with the orchestrator's own execute mode from Step 0 — both halves are required.)
- **Never touch GLOSSARY.md.** "Do NOT run `nx run magic-ast:glossary` and do NOT edit `libs/magic-ast/GLOSSARY.md`. Read it freely; the orchestrator regenerates on main once at the end."
- **No self-merge.** "Commit on your worktree branch. Do NOT merge to main — the orchestrator merges."
- **Use `git -C "$WORKTREE_ROOT"`** for every git command.
- **Duplicate-work guard.** When two agents might add the same keyword/rule: "If [X] already exists when you read GLOSSARY.md, SKIP it and pick [alternate scope]."
- **Scope facts:** family identity (`pattern`, `lastAttemptedRule`), all fixture paths, the briefing path, the gold-AST authoring rules (the Invariants above; full schema-gap reference in [PIPELINE.md](PIPELINE.md#authoring-reference)), and the branch name.
- **Sibling-shape note:** real cards are multi-ability; the agent may add a tight sibling parser surface only under the constraints in [PIPELINE.md](PIPELINE.md#sibling-shape-allowance), else bail on the multi-ability card.

Wait for all N to report before merging.

### Step 4 — Judge

Dispatch a `mast-judge` sub-agent to verify rules-accuracy. **Policy:** judge any branch carrying novel-shape work (new AST types, replacement effects, combo depth, architectural changes); **skip** pure established-pattern branches (keyword additions mirroring existing patterns). When the judge runs it is a hard binary gate — **any FAIL HALTs the batch.** Do not merge the offending branch; remediate inline or via a focused follow-up agent, then re-judge. There is no "concern" tier. See `.claude/skills/mast-judge/SKILL.md`. Judge novel-shape branches *before* merging them so `main` stays clean.

### Steps 5-6 — Merge and gate

Merge in **file-affinity order**, NUnit-gating after each group. Two individually-green branches can be jointly-red — Step 6 catches that, and no-ratchet-tolerance means any red halts the batch (roll back the merges, investigate per Stop conditions).

1. Unique-file agents first (trivial auto-merge; `--ours` on any GLOSSARY conflict).
2. Keyword-batch agents sequentially — `KeywordDefinitions.cs`/`OracleParsers.cs` conflicts are additive, keep both sides.
3. Hot-file parser agents sequentially: StaticAbilityParser → TriggeredAbilityParser → AbilityClassifier → ActivatedAbilityParser.
4. `nx run mast:test` after each group; final joint run must be 100% green.

See [Hot files](#hot-files) for the conflict-resolution protocol.

### Steps 7-8 — Regenerate glossary, re-triage, loop

```bash
nx run magic-ast:glossary
git add libs/magic-ast/GLOSSARY.md
git commit -m "chore(mast): regenerate GLOSSARY after batch {date}"
nx run mast:run   # refresh corpus-wide triage
```

If a batch has an intra-batch second wave that depends on new AST types, regenerate + commit GLOSSARY.md *between* waves so the second wave's briefing can cite accurate signatures. Then produce the batch report ([template in PIPELINE.md](PIPELINE.md#batch-report)) and loop to Step 1, or stop if returns are diminishing.

## Batch dispatch model

The orchestrator dispatches up to N sub-agents per batch (default N=20; mega-batches run 40–80). The binding constraint is **file affinity** — agents that create distinct files run fully parallel; agents that touch the same file merge sequentially.

| Group | Typical target files | Cap per batch |
|---|---|---|
| **Unique-file agents** (`Triggered/Rules/`, `Spell/Rules/`) | each creates a new reflection-discovered file | Unlimited — never collide |
| **Keyword-batch** | `KeywordDefinitions.cs` + `OracleParsers.cs` | ~4 (≈5 keywords each) — hot |
| **StaticAbilityParser** | `StaticAbilityParser.cs` (new private method each) | ~6 — hot |
| **TriggeredAbilityParser** | `TriggeredAbilityParser.cs` (new trigger conditions) | ~4 — hot |
| **AbilityClassifier** | `AbilityClassifier.cs` (new routing entries) | ~4 — hot |
| **ActivatedAbilityParser** | `ActivatedAbilityParser.cs` | 1–2 — hot |
| **Combo-depth** | various — user-requested cards to 100% coverage | orchestrator judgment |

### Hot files

Five files take concurrent edits and produce the bulk of merge conflicts: `KeywordDefinitions.cs`, `OracleParsers.cs`, `StaticAbilityParser.cs`, `TriggeredAbilityParser.cs`, `AbilityClassifier.cs`. The unique-file rule directories never collide — each rule is its own `[SpellRule]`/`[TriggeredRule]` file.

**Interim protocol:** cap each hot file per the table; serialize overflow. Resolve conflicts with a dedicated **resolver sub-agent** per hot file ("keep BOTH sides — these are additive registry entries / dispatch-chain extensions"), which is faster and less error-prone than inline orchestrator resolution and keeps conflict noise out of orchestrator context.

**Architectural fix (planned):** extend the reflection-discovered one-file-per-rule pattern to the hot files — one file per keyword, per static rule, per trigger condition, per classifier entry. Then every agent adds a *new* file and the hot-file collision class disappears. Until that lands, the caps and resolver protocol are the mitigation.

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
- Judge returns HALT — do not merge; surface the verdict + offending branches.
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
| MTG Comprehensive Rules glossary | `tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json` |
| Triage report | `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json` |
| AST nodes | `libs/magic-ast/AST/**/*.cs` |
| Effect trait interfaces | `libs/magic-ast/AST/Effects/Traits/` |
| Ability parsers | `libs/magic-ast/Parsing/Parsers/*.cs` |
| Failure-pattern inference | `libs/magic-ast/Parsing/Parsers/FallbackParser.cs` |
| Hand-parsed fixtures | `tests/magic-ast-tests/Data/HandParsedCards/{set}/*.json` |
| Test diff dumps (on failure) | `/tmp/mast-diffs/{set}_{card}.expected.json` + `.actual.json` |
| Test runner / triage runner / glossary | `nx run mast:test` / `nx run mast:run` / `nx run magic-ast:glossary` |
| Two-phase fallback + authoring reference | [PIPELINE.md](PIPELINE.md) |
