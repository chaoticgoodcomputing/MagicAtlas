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
- **GLOSSARY.md is orchestrator-only.** `libs/magic-ast/GLOSSARY.md` is the tracked, auto-generated AST index. Sub-agents **read it freely, never regenerate it.** `nx run magic-ast:glossary` runs on the integration branch, once, at the end of a batch. Any in-worktree regen is a guaranteed merge conflict for no benefit — no sub-agent's tests depend on the regenerated glossary.
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
Step 7   Regenerate GLOSSARY.md once on the integration branch, commit.
Step 8   Re-run triage. Reap worktrees (nx run mast:worktree-clean). Report. Loop or stop.
```

### Step 0 — Pre-flight

- **Permission mode.** You (orchestrator) must be in an executing mode (`default`/`acceptEdits`), NOT plan mode, before dispatching. Sub-agents inherit the parent's permission mode and cannot escape it — a child dispatched from plan mode will *propose* edits instead of making them, no matter what its prompt says. This is the root cause behind stalled "zero-commit" agents; the prompt-level "do not enter plan mode" mandate (Step 3) is the second half of the fix, not the whole fix.
- **Worktree isolation + base — BOTH are required.** These are two independent settings, and getting either wrong corrupts the run (this is the root cause of the batch-1 base-contamination incident):
  1. **Every `Agent` spawn MUST pass `isolation: "worktree"`.** Without it the agent runs *in the orchestrator's own checkout* and its `git checkout`/commit moves the primary branch — that is exactly what hijacked the integration branch and stranded agents on a stale ancestor. No exceptions; a spawn missing it is a bug, not a shortcut.
  2. **`worktree.baseRef: "head"`** is set in `.claude/settings.json`, so each isolated worktree branches from the **current local HEAD** — whatever branch you have checked out (e.g. `feat/mast-improvements`), **not** `main`/`origin/HEAD`. This is correct; do not "fix" it toward `main`. The integration branch is wherever you are checked out, and you merge agent branches back into *that* — there is no `main` in this loop.
- **Canary the isolation, don't trust the config.** Before a large batch, spawn ONE agent whose prompt's first action reports `git rev-parse --show-toplevel` (must be under `.claude/worktrees/`, NOT the repo root) and `git log --oneline -1 HEAD` (must match your HEAD). If toplevel is the main repo, or HEAD is a stale base (e.g. the expected just-landed files are missing), STOP — isolation is broken; fix it before dispatching the rest.
- **Refresh triage:** `nx run mast:run`.

### Step 1 — Pick families

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. **`topYieldClusters[]` is the primary pick surface; the two `topGaps` lists are diagnostics.**

- **`topYieldClusters[]`** (PRIMARY — data-derived) — unparsed lines clustered by normalized lexical template, ranked by **`fractionalYield`** (proximity-weighted: each card contributes `1/(distinct templates on it)`, so a template ranks high when it's the last-or-near-last missing piece across many nearly-complete cards). Each cluster is a **buildable family** — one normalized template → one parser surface — and carries: `template`, `fractionalYield` (primary signal), `directYield` (whole-card flips, a hard floor), `dominantPattern` + `dominantLastAttemptedRule` (the "where it fails" navigation hint — which parser the template bails in), and `exemplars[]` (each with `input` DTO and `alreadyHandParsed`, ready to hand-parse). ~50 entries deep, so the long tail (small/partial-card families like specific triggered abilities) is visible, not just the top whole-card flips. **The template is the family unit; `dominantPattern`/`dominantLastAttemptedRule` are annotations, not the key** — this is what splits coarse buckets (e.g. "UnparsedTriggered" — proliferate-trigger, roll-a-d20, play-a-card-trigger each surface as their own cluster).
- **`topGaps[]`** / **`topGapsByLineFrequency[]`** (DIAGNOSTIC) — failures grouped by the coarse `(pattern, lastAttemptedRule)` key, ranked by fractional yield and by raw line frequency respectively. Use these to *see where the parser bails broadly* (e.g. "4747 cards bail in TriggeredAbilityParser.Parse"), NOT as a pick surface — a single entry usually spans several distinct families, so it's not a pickable unit. If a gap looks interesting, find the matching `topYieldClusters[]` entries to get the actual buildable families.

**Heuristic:** pick families off `topYieldClusters[]` top-down by `fractionalYield`. High `directYield` = flips whole cards now; high `fractionalYield` with low `directYield` = chips many cards toward done (good when coverage is high and most cards have several gaps). Cross-reference the diagnostic gap lists only to understand *where* in the parser the work lands.

For each family: select **1-3 fixtures** from the cluster's `exemplars[]` (already ranked cleanest-first by fewest other unparsed templates; skip `alreadyHandParsed: true`). The low count is deliberate — N agents × 5 fixtures is unsustainable merge overhead, and coverage-per-fixture is the optimization target. **Diversity check:** the 1-3 should vary the dimensions the parser surface must handle, not be near-duplicates. When exemplars are stacked with multi-keyword legendaries, pre-curate cleaner single-line non-legendaries from `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` with a `jq` regex on the cluster template.

Choose batch size by the number of **non-overlapping** families, constrained by hot-file caps (below). Two families targeting the same hot parser file serialize across batches.

### Step 2 — Brief each family

Write one batch briefing to `docs/judgments/briefing-{YYYY-MM-DD}.md` (suffix `-N` if it exists). One section per family, **~200 words, informative not prescriptive** — establish rules facts, don't dictate AST shape (agents own that). For each family: identify the MTG mechanic(s) and **pull the canonical rule data — the exact CR rule number(s) AND verbatim quoted text — from `rules-structure.json`** (`jq` it; do not paraphrase a number from memory). Write: failure signal, cards in family, **relevant rules (number + quoted text — this block is the ground truth the agent cites verbatim in doc-comments and the judge cross-references, so it must be accurate)**, AST types likely in scope (convenience pointer to GLOSSARY.md, not a whitelist), expected generalization, anti-patterns. The full briefing template is in [PIPELINE.md](PIPELINE.md#briefing-template). Pulling the rule data here, once, on the orchestrator side is what prevents agents from hallucinating rule numbers.

If a family's mechanic isn't in `glossary.json` at all (and is genuinely MTG-domain, not vernacular), don't dispatch — swap the family or escalate to the human.

### Step 3 — Dispatch combined agents

Spawn N sub-agents in parallel via `Agent` **with `isolation: "worktree"` on every single spawn** (Step 0 — this is non-negotiable; a spawn without it runs in your checkout and moves your branch), one per family (Opus for novel-shape or doctrinal-edge families; Sonnet for mechanical ones). Each agent does the full slice: new AST type if needed → gold fixture(s) → parser surface → tests green → commit. **Family contract:** make ALL the family's fixtures green via ONE consolidated parser surface (one new method, or one extension at `lastAttemptedRule`). If the agent finds itself writing N separate `TryParseX` methods, it has misread the family — bail with a sub-pattern breakdown (that bail refines the triage taxonomy; bailing is not failure).

**Every dispatch prompt MUST include all of these** (self-contained — the agent shares no context with you):

- **Isolation self-check, FIRST.** "Before any edit/branch/commit: run `git rev-parse --show-toplevel` and `git log --oneline -1 HEAD`. If toplevel is `<repo root>` (not under `.claude/worktrees/`), or the base is missing expected files, STOP — make NO changes — and report `ISOLATION FAILED`/`WRONG BASE` with the sha. Else capture `WORKTREE_ROOT="$(pwd)"` and proceed." (Belt-and-suspenders against a spawn that didn't isolate; it prevents an un-isolated agent from corrupting your checkout.)
- **Execute, don't plan.** "Do NOT enter plan mode. Make edits, run tests, and commit directly." Inline the relevant steps rather than referencing this skill by name, which can re-trigger plan mode. (Combined with the orchestrator's own execute mode from Step 0 — both halves are required.)
- **Hand the card data in-prompt — do NOT make the agent look it up.** Paste the chosen exemplar(s)' `Input` DTO **verbatim from `triage-report.json`** into the prompt: `Name`, `ManaCost`, `TypeLine`, `OracleText`, `Power`, `Toughness`, `Colors`, `ColorIdentity` (for DFCs, the `CardFaces` block). The agent writes the fixture's `Input` straight from this. **Hard rule in the prompt: "The local datasets are the only card source. NEVER hit the network or Scryfall."** If the agent needs a cleaner alternate exemplar it `jq`s the local `oracle-cards.json` — but if you handed it a clean DTO, it shouldn't need to. (Reaching for Scryfall is a HITL stall and a sign the DTO wasn't handed over — or that *your* curated value was wrong; copy from triage, don't retype from memory.)
- **Cite rules only from the briefing's data — never from memory.** "Use the CR rule number(s) + text the briefing's 'Relevant rules' section gives you (the orchestrator pulled these from `rules-structure.json`); cite them verbatim in doc-comments. Do NOT write a rule number the briefing didn't provide. If you believe a different rule applies, say so in your report — do not guess a number." This kills citation hallucination at the source; the judge (Step 4) cross-references against the same rules data.
- **Never touch GLOSSARY.md.** "Do NOT run `nx run magic-ast:glossary` and do NOT edit `libs/magic-ast/GLOSSARY.md`. Read it freely; the orchestrator regenerates once at the end."
- **No self-merge.** "Commit on your worktree branch. Do NOT merge — the orchestrator merges."
- **Use `git -C "$WORKTREE_ROOT"`** for every git command.
- **Duplicate-work guard.** When two agents might add the same keyword/rule: "If [X] already exists when you read GLOSSARY.md, SKIP it and pick [alternate scope]."
- **Scope facts:** family identity (`pattern`, `lastAttemptedRule`), all fixture paths, the briefing path, the gold-AST authoring rules (the Invariants above; full schema-gap reference in [PIPELINE.md](PIPELINE.md#authoring-reference)), and the branch name.
- **Sibling-shape note:** real cards are multi-ability; the agent may add a tight sibling parser surface only under the constraints in [PIPELINE.md](PIPELINE.md#sibling-shape-allowance), else bail on the multi-ability card.

Wait for all N to report before merging.

### Step 4 — Judge

Dispatch a `mast-judge` sub-agent to verify rules-accuracy. **Policy:** judge any branch carrying novel-shape work (new AST types, replacement effects, combo depth, architectural changes); **skip** pure established-pattern branches (keyword additions mirroring existing patterns). When the judge runs it is a hard binary gate — **any FAIL HALTs the batch.** Do not merge the offending branch; remediate inline or via a focused follow-up agent, then re-judge. There is no "concern" tier. See `.claude/skills/mast-judge/SKILL.md`. Judge novel-shape branches *before* merging them so the integration branch stays clean.

The judge verifies **doctrine** (`unparsed` in gold, describe-vs-execute, wrong AST shape/discriminator, missing required fields, free-text where structure exists) **and cross-references each cited CR rule** against `rules-structure.json`. This is cheap and reliable now that citations are orchestrator-sourced (Step 2) rather than agent-guessed: the judge confirms the cited rule exists and its text matches the modeling, and FAILs only on an absent-from-data or contradictory citation — not on subrule-letter precision.

### Steps 5-6 — Merge and gate

Merge in **file-affinity order**, NUnit-gating after each group. Two individually-green branches can be jointly-red — Step 6 catches that, and no-ratchet-tolerance means any red halts the batch (roll back the merges, investigate per Stop conditions).

1. Unique-file rule agents first (the overwhelming majority — every keyword and spell/static/triggered/activated rule is its own file; trivial auto-merge; `--ours` on any GLOSSARY conflict).
2. `AbilityClassifier.cs` agents sequentially — the one remaining hot file; routing entries are additive, keep both sides.
3. Parser-orchestration agents last (rare): an agent that edited a thin dispatcher body in `TriggeredAbilityParser.cs`/`ActivatedAbilityParser.cs` (timing/split/multi-sentence only — adding a *rule* never lands here).
4. `nx run mast:test` after each group; final joint run must be 100% green.

See [Hot files](#hot-files) for the conflict-resolution protocol.

### Steps 7-8 — Regenerate glossary, re-triage, loop

```bash
nx run magic-ast:glossary
git add libs/magic-ast/GLOSSARY.md
git commit -m "chore(mast): regenerate GLOSSARY after batch {date}"
nx run mast:run            # refresh corpus-wide triage
nx run mast:worktree-clean # reap this batch's worktrees + merged agent branches
```

**Reap worktrees every batch.** `nx run mast:worktree-clean` removes the batch's isolated worktrees (Claude only auto-removes *clean* ones; ours have commits) and deletes the now-**merged** `mast-tdd/*` + `worktree-agent-*` branches. Skipping this is how the pool reached 318 worktrees and forced the in-place-checkout fallback. For a **discarded** batch (branches unmerged), run `bash tools/clean-worktrees.sh --force` to also drop the unmerged branches.

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
