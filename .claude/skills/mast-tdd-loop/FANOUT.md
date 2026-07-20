<!-- Design doc for the parallel-fan-out MAST TDD orchestration protocol (v3 harness).
     DESIGN + SCRIPTING ONLY. Authored read-only against the repo at 2026-06-16.
     Companion script: tdd-fanout-harness-v3.js (same dir). -->

# Parallel-fan-out TDD orchestration protocol (MAST parser + interaction)

A protocol for running the MagicAST parser + interaction TDD loop across **10–20 worker subagents in
one wave**, encoding the project owner's three-part strategy in priority order:

1. **Reflection-first** — bias the work decomposition so each worker *adds a new file* the registry
   auto-discovers, instead of *editing a shared dispatcher/helper*. New-file work never collides.
2. **Soft non-colliding assignment** — when a shared edit is unavoidable, the orchestrator predicts
   each task's *touch-set* and batches waves so no two workers in one wave write the same shared file.
3. **Orchestrator-managed serial merges** — workers run in **git-worktree isolation**, each produces a
   self-contained *green + judged* change, and the **orchestrator merges them back one at a time**,
   rebuilding and running the gates between every merge, resolving the unavoidable shared-file conflicts
   at the merge boundary (never concurrently in the workers).

Model assignment (owner decision): **orchestrator = Opus, all judges = Opus, workers = Sonnet.** The
worker job is engineered so a Sonnet executes a clear-cut, pre-AST'd plan; Opus does the planning,
judging, and serial merges.

This builds directly on `gold-burndown-execute-v2.js` (delta-judge, stateless-whitelist partial-commit,
revert-on-failure, defer-and-continue) and the `mast-tdd-loop` skill (worktree isolation, deterministic
gates, file-affinity merge). v3's new contribution is **safe fan-out width** (10–20 in one wave instead
of a serial slice loop) achieved by the reflection-first seam map + touch-set wave batching.

---

## 0. The two registries that make fan-out safe

Everything below rests on the fact that MAST already has a mature **reflection/attribute discovery
backbone**. v3 does not introduce it — it *steers work toward it*.

### Parser side — `RuleRegistry.Discover<TRule,TAttr>` (`libs/magic-ast/Parsing/Parsers/RuleRegistry.cs`)

A single generic scanner: for each `[TAttr]`-decorated type in the assembly, instantiate (parameterless
ctor) and order by descending `Priority` then ordinal name. Seven attribute families ride it, each a
**one-file-per-rule** directory that is *append-only* under parallel dispatch:

| Attribute | Interface | Directory | Files today |
|---|---|---|---|
| `[SpellRule]` | `ISpellRule` | `Parsing/Parsers/Spell/Rules/` | 109 |
| `[StaticRule]` | `IStaticRule` | `Parsing/Parsers/Static/Rules/` | 95 |
| `[TriggeredRule]` | `ITriggeredRule` | `Parsing/Parsers/Triggered/Rules/` | 112 |
| `[TriggerConditionRule]` | `ITriggerConditionRule` | `Parsing/Parsers/Triggered/Rules/` | (above) |
| `[ActivatedEffectRule]` / `[ActivatedCostRule]` | `IActivated*Rule` | `Parsing/Parsers/Activated/Rules/` | 52 |
| `[Keyword]` | `IKeyword` | `Keywords/Definitions/` | 163 |
| `[StructuralKeyword]` | (structural) | `Parsing/Tokens/Keywords/` | 23 |

All attributes share `IPrioritizedRuleAttribute { int Priority }` (default 50). A new rule is **a new
file with a `[TAttr]` and a parameterless ctor** — zero edits to any shared file. ~654 such files
exist; they have never merge-conflicted under batch dispatch (per the skill). This is the collision-free
ideal.

### Interaction side — `PortWalkProjection` registries + `PortGraph` dispatch

Asymmetric to the parser. Adding interaction coverage ("a flow arm") is a **three-layer shared edit**,
not a new file (see §1.3): a registry-set line in `PortWalkProjection.cs` **plus** a `case` in
`PortGraph.Effects()/Trigger()/Costs()` **plus** a clause in `PortGraphEngine.FlowFeasible()`. The
`PortWalkExhaustivenessTests` ratchet + `known-coarse-projections.json` allowlist force a projection
*decision* for any new discriminator but do not make the arm a new file. **The interaction layer is the
structurally collision-prone half of the codebase** and must be treated differently from the parser
half by the orchestrator (it gets at most one worker per wave — see §2.4).

---

## 1. Reflection-first seam map (priority 1)

Goal: convert as much TDD work as possible into **new-file, collision-free** additions. Below is the
taxonomy the orchestrator uses to classify every candidate task, plus concrete new seams that would
widen the new-file column.

### 1.1 NEW-FILE (collision-free) — the ideal; unlimited workers per wave

A task is new-file iff closing it is *only* dropping `[TAttr]`-decorated file(s) (+ a gold fixture,
which is itself a new file under `HandParsedCards/`). No shared file is touched.

- A **brand-new spell/static/triggered/activated rule** — the overwhelming majority of parser coverage
  work. e.g. `unparsed-triggered (A) Hylderblade` (widen-into-new-`AttachTriggeredRule` shape),
  `Chorale arm 2` (new `SacrificeSelfUnlessConditionTriggeredRule.cs`), `WhirlerRogue`'s new
  `TapPermanentsCostRule.cs`.
- A **brand-new keyword** under `Keywords/Definitions/` (`[Keyword]`, parameterless ctor).
- A **new structural keyword** under `Tokens/Keywords/`.
- A **new gold fixture** under `tests/.../HandParsedCards/` — always a new file; the immutability gate
  *requires* additions-only.

Worker contract for these is the cleanest: "here is the card DTO, the CR rule text, and the exact rule
interface to implement; drop one new file + one gold; make the targeted test green." A Sonnet executes
this against clear guidance with no shared-state reasoning.

### 1.2 SHARED-EDIT (collision-prone) — minimize, partition, or convert

A task is shared-edit iff it must write a file >1 worker could also need. The known hot shared files:

| Shared file | Why it is hot | Burndown example |
|---|---|---|
| `Parsing/Parsers/*/[*]RuleHelpers.cs` (4 files) | the duplicated **qualifier→axis mapping** every rule routes through | **PB-3** (the canonical hazard) |
| `AST/References/ObjectFilter.cs` | every new filter axis is a field here | PB-1 `IsEnchanted`, PB-3 `ExcludedColors`, PB-2 `Comparison.RelativeTo` |
| `AST/References/Characteristic.cs` | new variants + the `FromLabel` switch + the `[CharacteristicKind]` discriminator | PB-3 `TappedStateCharacteristic`, `CounterCharacteristic` |
| `Parsing/AbilityClassifier.cs` (~54 KB) | the **last hot monolith**; routing entries edited in place | Slices 4/7/8/9 classifier-touchers |
| `Parsing/Tokens/.../ConditionParser.cs` | additive arms before the `OtherCondition` fallback | Slice 7 buckets |
| `libs/mast-interaction/PortGraph.cs` + `PortGraphEngine.cs` + `PortWalkProjection.cs` | the three-layer flow-arm dance + the `CharacteristicKind` exhaustive switches | Slice 6 carve-out; PB-3 schema kinds |

PB-3 is the textbook hazard: it edits **all four `*RuleHelpers`**, adds two `Characteristic` variants
(touching `FromLabel` + the polymorphic discriminator), adds an `ObjectFilter` axis, mutates the
`Comparison` record (~12 literal-int consumers to keep byte-identical), and propagates two new schema
kinds into every `CharacteristicKind` switch in `mast-interaction`. No amount of seam-work converts
PB-3 into new-file work; it is an **atomic single-worker shared edit** and the orchestrator treats it as
a wave of one (see §2.3).

### 1.3 Interaction flow arms — shared-edit by construction

Per `libs/mast-interaction/docs/adding-a-flow-arm.md`, a flow arm is a *rules-defined cross-card edge*
("an emit of kind X satisfies a consume of role Y"). Adding one is the three-layer edit: faithful label
in `PortGraph.cs` → discriminator into `PortWalkProjection` (+ remove from
`known-coarse-projections.json`) → arm clause in `PortGraphEngine.FlowFeasible()`. **It is not a new
file.** It is the interaction half's analogue of PB-3: serialize, one per wave.

### 1.4 Recommended new reflection seams (convert shared → new-file)

These are concrete extension-point recommendations that would shrink the shared-edit column. They are
**design proposals for the human, not work this protocol performs** (read-only run).

1. **`[CharacteristicLabel("tapped")]` registry replacing `Characteristic.FromLabel`'s switch.**
   `FromLabel` is a hand-maintained `switch` in the hot `Characteristic.cs`; every new label is a shared
   edit there. A small reflection registry (mirroring `RuleRegistry`) keyed on the label string would
   make a new characteristic a **new file** carrying its own label→variant mapping. This converts the
   PB-3-style "extend `FromLabel`" work into new-file work for all *future* characteristics. (The
   variant `record` itself still lands in `Characteristic.cs`, but the *mapping* — the part that
   collides — moves to a discovered file.)

2. **A `[QualifierAxis]` registry to dissolve the four `*RuleHelpers` duplication.** The single worst
   hazard is that the qualifier→axis mapping is *copy-pasted* across `SpellRuleHelpers`,
   `StaticRuleHelpers`, `TriggeredRuleHelpers`, `ActivatedRuleHelpers`. PB-3's prescribed first step is
   "extract ONE shared helper, then route all call sites." Going one step further — a reflection
   registry of `IQualifierAxis` matchers (one file per qualifier: `tapped`, `noncreature`, `enchanted`,
   `other`…) discovered like rules — would make *every future qualifier* a new file instead of a
   four-file edit. This is the highest-leverage seam: it is exactly the PB-1/PB-3/PB-6/Slice-6 collision
   nexus.

3. **`[FlowArm]` registry for `PortGraphEngine.FlowFeasible()`.** The `FlowFeasible` switch over
   `(ResourceKind, Role)` is the interaction-side monolith. An `IFlowArm { bool Feasible(emit, consume) }`
   registry (one file per arm: token-sac, life-trigger, blink-etb…) would make a new arm a new file. The
   projection-set + label half still needs the registry-line + `PortGraph` case, but the *feasibility
   clause* — the part most likely to collide and the part the judge scrutinizes — becomes new-file.

4. **`AbilityClassifier` routing-entry registry** — the skill already names this as "the only remaining
   hot monolith to convert (one file per routing entry)." Endorsed: a `[ClassifierRoute]` registry
   removes the last in-place-edited parser dispatcher, making *every* classifier change new-file.

**Net recommendation:** seams (2) and (4) retire the two biggest parser collision sources; seam (3)
retires the interaction one. Until they exist, the protocol leans on §2 (soft assignment) to keep the
unavoidable shared edits disjoint per wave.

---

## 2. Soft non-colliding assignment + touch-set wave batching (priority 2)

When new-file framing is impossible, the orchestrator guarantees **disjoint file-touch sets within a
wave**. This is the pre-dispatch collision pre-check from the skill's Step 2.5, formalized.

### 2.1 Per-task touch-set declaration

For every candidate task the **Opus orchestrator** (in the planning phase, before any worker spawns)
declares a `touch` set: the files the task is *predicted* to write. It is computed from:

- **Class:** `new-file` (touch = `[]`, the new rule + gold files don't count — they're unique by
  construction) vs `shared-edit` (touch = the explicit shared files).
- A source grep + the seam map §1.2: which `*RuleHelpers`, which AST primitive, `AbilityClassifier.cs`,
  `ConditionParser.cs`, or interaction file the task's spec names.

Touch-set is a *hypothesis*, exactly as the skill's "Anticipated updates" column states — it is used to
size the wave and find collisions, never handed to the worker as a spec.

### 2.2 Wave packing (graph-coloring the conflict graph)

Build a conflict graph: nodes = tasks, edge between two tasks iff their touch-sets intersect (new-file
tasks are isolated nodes — no edges). A **wave is an independent set**: tasks whose touch-sets are
pairwise disjoint. Greedy packing:

1. Put **all `new-file` tasks** in wave 1 (they collide with nothing) up to the width cap (10–20).
2. Add `shared-edit` tasks to wave 1 only while each new addition's touch-set stays disjoint from every
   task already in the wave. First shared-edit on a hot file claims it for the wave; any later task
   touching it spills to a subsequent wave.
3. Remaining shared-edit tasks pack into wave 2, 3, … by the same disjointness rule.
4. **Dependency edges** (PB-1 "must land before PB-3"; PB-3 "before Slice 6") are honored as ordering
   constraints across waves — a task waits for its predecessor's *merge*, not just its build.

Because new-file tasks dominate the corpus (654 rule files vs a handful of hot files), most waves are
wide (mostly new-file) with at most one occupant of each hot shared file.

### 2.3 The "wave of one" for atomic megaslices

PB-3, Slice 6, and each interaction flow arm are *intrinsically* whole-file-spanning atomic edits. They
get a **dedicated wave with a single worker** — there is no partition that makes them concurrent-safe.
This is not a failure of soft assignment; it is the correct output of it (the conflict graph clique
forces serialization).

### 2.4 Interaction cap

Because every interaction change is the three-layer shared edit (§1.3) touching `PortGraph.cs` +
`PortGraphEngine.cs`, **at most one interaction worker per wave**, full stop. Parser new-file workers
run alongside it freely (disjoint subtree).

### 2.5 Touch-set is a guard, not a contract

If a worker's actual diff touches a file outside its declared touch-set, that is signal for the *next*
wave's packing, not a worker error — and it is caught harmlessly at merge time anyway (§3), because the
serial merge rebuilds + gates between every merge. Soft assignment makes the common case conflict-free;
serial merge is the safety net for the prediction miss.

---

## 3. Worktree isolation + orchestrator serial-merge protocol (priority 3)

### 3.1 Worker isolation

Every worker is dispatched with `isolation: 'worktree'` (the `mast-worker` agent def already defaults
this; v3 sets it explicitly too as belt-and-braces). Config already in place:
`.claude/settings.json → worktree.baseRef: "head"` (each worktree branches from current local HEAD, the
integration branch `feat/mast-improvements`, **not** `main`), and `.worktreeinclude` copies the
gitignored `glossary.json` + `rules-structure.json` into each worktree so workers can `jq` the CR.

Each worker, first action, runs `bash tools/gate-isolation.sh <baseSha>` — nonzero means it is in the
main checkout or on a stale base → it STOPs and makes no changes. Then
`WORKTREE_ROOT="$(pwd)"; git -C "$WORKTREE_ROOT" checkout -b mast-tdd/<YYYY-MM-DD>-<slug>` (hyphen
separator, never slash, so `clean-worktrees.sh` can reap it). Workers never `cd`, use relative paths for
Read/Write/Edit, `git -C "$WORKTREE_ROOT"` for git, and `dotnet` directly (nx unavailable in
worktrees). A worker produces a **self-contained green + committed change on its branch and does NOT
merge.**

### 3.2 Per-worker definition of done

A worker returns `green:true` only when, in its worktree: clean build **and** its targeted/affected
tests pass **and** it committed on its branch. Delta-scope discipline (from v2): structure only its
target axis/residual; leaving another axis's residual + its whitelist entry is correct. Revert-on-fail
leaves a clean tree; defer-and-continue means a bad worker never blocks the wave.

**Parser workers — `Input` is pre-seeded & authoritative; author only `Output` + parser.** The orchestrator seeds each card's gold `Input` from the corpus via `tools/seed-gold-input.py` (Step 2) and hands it to the worker **verbatim**. The worker MUST copy that `Input` exactly into its gold — never paraphrase the OracleText, add a reminder the real card lacks, or alter P/T/cost — and then author the `Output` AST + parser to match. The reason this is orchestrator-seeded rather than worker-transcribed: `GoldOracleTextFidelityTests` (Input vs corpus) **cannot run in a worktree** (the gitignored corpus is absent → it skips), so a worker-transcribed Input drifts undetected through both the worker's own suite run *and* the judge, surfacing only at the orchestrator's post-merge CORE gate — after the parser was built against the wrong text (the Maddening-Cacophony reminder + Peregrin-Took P/T failures). Seeding from corpus on main moves the fidelity gate *before* dispatch.

**Interaction flow-arm workers — mirror the product reconstruction reach.** A worktree can't run the
corpus bench, so an interaction worker proves its arm with a scope test (Walk golds → `Materialize` →
`FindCycles` → assert tier). It MUST call `FindCycles(edges, LengthBound)` with the **same bound the
product/bench uses** (`MaterializeCyclesStep` / `ComboRecallRunner`, currently **6**), never the
unbounded `FindCycles(edges)` default — otherwise a cycle *longer* than the product reconstructs passes
the scope test and reports `green:true`, but the combo never flips in the orchestrator's bench (a false
"will-flip" the interaction-judge won't catch — it judges soundness, not reach). The fan-out trial hit
this exactly: a 6-hop Displacer cast-blink arm passed an unbounded scope test but only flipped after a
deliberate 5→6 bound bump. If a worker's loop legitimately needs a longer reach than the current bound,
that is an orchestrator-level product decision (raise `LengthBound` + re-verify the corpus), **not**
something a worker silently assumes via an unbounded scope test.

### 3.3 Delta-judge fan-out (Opus judges)

After a worker reports green, the orchestrator fans out **per-gold delta-judges** exactly as v2: one
`mast-judge` (parser) or `interaction-judge` (interaction) agent per regenerated gold / per reconstructed
edge, asking the **delta question** ("did this worker structure ITS target correctly and introduce no
new residual / regression / dropped-or-inverted ability?"), **not** whole-gold purity. A still-present
other-axis residual is a PASS. Judges are READ-ONLY (`mast-judge`/`interaction-judge` have no
Write/Edit) and run **non-isolated in the main checkout** so they can `git diff <base>..<branch>` the
unmerged branch. The halt decision is the deterministic gate
`tools/gate-judge-verdict.sh <verdict.json>` (nonzero = any non-PASS → that branch is held unmerged),
not a prose reading. Judges are **Opus** (owner decision).

A judge-FAIL holds the branch unmerged and defers the task (its branch is preserved for human review or
re-dispatch); it does not stop the wave.

### 3.4 Serial merge (the orchestrator's exclusive job)

Only **judge-PASSED** branches enter the merge queue. The Opus orchestrator merges them **one at a
time, in file-affinity order** (skill Steps 5–6):

1. New-file (unique-file) rule branches first — trivial `--ff-only`, no conflicts, `--ours` on any
   GLOSSARY conflict.
2. `AbilityClassifier.cs` branches sequentially — additive routing entries, keep both sides.
3. Parser-orchestration / interaction (three-layer) branches last — rare, one at a time.

For **each** merge in turn (this is the heart of priority 3):

```
a. bash tools/gate-fixture-immutability.sh <base> <branch>   # additions-only; HALT on edit-of-gold
b. git merge --no-verify <branch>                            # ff where possible
c. ON CONFLICT (only ever on a hot shared file): the ORCHESTRATOR resolves it HERE,
   at the merge boundary — keep-both for additive routing/registry lines; for a genuine
   semantic overlap, rebase the later branch onto the merge result and re-run its targeted
   test. This is the "rebase/resolve the unavoidable shared-file conflicts at the merge
   boundary, not concurrently in the workers" mandate.
d. REBUILD: dotnet build (clean)
e. GATE between merges (no-ratchet-tolerance — any red HALTS this merge):
     - dotnet test ...MagicAtlas.Ast.Tests.csproj --nologo   (CORE ring: gold fidelity,
       no-unparsed, round-trip, DestringSinkRatchet, DiscriminatorUniqueness,
       PortWalkExhaustiveness)
     - nx run magic-ast:lint-discriminators (per merge group; stateless — no baseline to advance)
     - on an interaction merge: bench recall must not DECREASE (auto-advances on a gain)
f. GREEN  → keep the merge; commit any advanced baselines/regenerated snapshots.
   RED    → git reset --hard HEAD@{1} (undo this merge only), leave <branch> intact,
            DEFER it with the redReason; CONTINUE with the next branch.
```

Two individually-green branches can be jointly-red; the rebuild+gate *between* merges is what catches
it, and it is undone in isolation without poisoning the merges that already landed. This is the v2
revert-on-failure + defer-and-continue, lifted to the merge layer.

### 3.5 Partial-commit via stateless whitelists

Carried verbatim from v2: the free-text/unparsed whitelists are stateless and keyed per `(card,sink)`
testing **set-membership**, so a worker removes a `(card,sink)` entry **iff** the card stops carrying
that sink entirely; a still-present other-instance/other-axis residual legitimately **keeps** the entry.
The merge commit records both removed and intentionally-kept entries for an auditable partial-commit
trail. This is what lets a wide wave land many *correct per-axis deltas* even on multi-residual golds
that no single worker fully cleans.

### 3.6 End-of-wave

After the merge queue drains: regenerate GLOSSARY once on the integration branch, re-triage, run the
`bench:recall` ratchet, reap worktrees + merged branches (`nx run mast:worktree-clean`), and report
committed/deferred/held counts. Then the next wave (the next independent set) dispatches against the new
HEAD. Dependency-ordered tasks (PB-3 after PB-1) wait here for their predecessor's merge.

---

## 4. Model assignment (owner decision, baked in)

| Role | Model | Why |
|---|---|---|
| **Orchestrator** | `opus` | plans the wave packing, owns the serial merge + conflict resolution, the highest-judgment work |
| **All judges** (`mast-judge`, `interaction-judge`) | `opus` | rules-accuracy is the correctness keystone; the delta verdict gates the merge |
| **Workers** (`mast-worker`) | `sonnet` | the AST + plan handed in is clear-cut enough for a Sonnet to execute against clear guidance |

The worker prompt is engineered for Sonnet: the orchestrator hands the **card DTO verbatim**, the **CR
rule number + verbatim text** (pulled by Opus from `rules-structure.json`), the **exact rule interface
to implement** (or, for a shared edit, the exact file + the surgical change), the branch name, the base
sha, and the fixture path. The worker never looks anything up or makes an AST-shape design decision —
that lives in the briefing the Opus orchestrator authored. Per-spawn model override
(`model: 'opus'|'sonnet'`) is set from the assignment matrix; a *novel-shape* family (new node /
trait-boundary / architectural) can be escalated to an Opus worker, but the default worker is Sonnet.

---

## 5. How the v2 gates plug in

| v2 mechanic | Where it lives in v3 |
|---|---|
| **Delta-judge** (per-axis/per-family, not whole-gold) | §3.3 — fanned out per gold/edge across the wave's PASSED workers, Opus judges |
| **Stateless named whitelists** (partial-commit) | §3.5 — unchanged semantics; removal iff sink fully cleared |
| **Revert-on-failure leaving a clean tree** | §3.2 (worker, in worktree) **and** §3.4f (orchestrator, `reset --hard HEAD@{1}` per merge) |
| **Defer-and-continue** | §3.3 (judge-FAIL defers, wave continues) **and** §3.4f (merge-red defers, queue continues) |
| **Deterministic gates** (`gate-isolation`, `gate-fixture-immutability`, `gate-judge-verdict`, `gate-preflight`) | §3.1 / §3.3 / §3.4 — nonzero = unconditional HALT, quoted in the report |
| **CORE ring** (`mast:test`) + recall ratchet | §3.4e — run between every merge, no ratchet tolerance |

---

## 6. Open risks (for the human to weigh in on)

1. **★ RATIFIED 2026-06-16 — HYBRID (option b): serial-merge throughput.** The owner chose the hybrid:
   targeted between-merge gate + batched merge for the provably-disjoint new-file branches, strict
   one-at-a-time full-gated serial merge for shared-edit/interaction branches, one full CORE-ring
   consolidation at wave end. Wired in `tdd-fanout-harness-v3.js` (`gateCmdFor` + the file-affinity merge
   order + the end-of-wave consolidation). Original write-up retained below for context.

   **serial-merge throughput is the bottleneck that can erase the fan-out win.** Workers run
   10–20 wide, but merges are strictly serial with a *full rebuild + full CORE-ring test suite between
   each one*. If the suite takes minutes and a wave lands ~15 branches, the merge phase is ~15×(build +
   suite) of wall-clock — potentially longer than the parallel build phase it follows, and it cannot be
   parallelized without abandoning priority 3. Mitigations to decide between: (a) `--filter` the
   between-merge gate to only the affected tests and run the full CORE ring once at end-of-wave (faster,
   but a joint regression surfaces later and rolls back more work); (b) merge new-file branches in a
   single batched commit with one gate (safe because they're provably disjoint) and only serialize the
   shared-edit branches (the ones that can actually conflict). **Recommendation: (b)** — it preserves
   the serial-merge guarantee exactly where collisions are possible while collapsing the
   provably-safe-disjoint majority. **This is the call I most want your ruling on**, because it trades a
   little of the "rebuild between every merge" purity for the fan-out actually paying off.

2. **Touch-set misprediction on the interaction layer.** A parser change that adds a new discriminator
   *silently demotes* interaction recall if no `CharacteristicKind` switch / projection learns it — the
   parser suite stays green while the end-product regresses. v3 relies on the `bench:recall` ratchet +
   `PortWalkExhaustivenessTests` to catch this at merge, but if a parser worker and an interaction
   worker land in the same wave (forbidden by §2.4, but a misprediction could let two parser workers
   both add discriminators), the demotion may not localize cleanly. Confirm §2.4's "one interaction
   worker per wave" should extend to "at most one *new-discriminator-introducing* worker per wave."

3. **Entangled slices still need HITL.** Slice 6 (the `ExcludeSelf` cross-card firability carve-out) and
   Slice 7/8E couple the parser change to an engine-firability decision the green suite won't catch.
   These are explicitly *not* fan-out work — they are single-worker waves that additionally require human
   design sign-off (the carve-out is already ratified in the plan). v3 should *refuse to auto-dispatch*
   any task flagged entangled and surface it instead. Confirm the orchestrator should hard-gate on an
   `entangled: true` flag.

4. **Worker quality at Sonnet for shared edits.** New-file work at Sonnet is proven. A *shared edit*
   (even a wave-of-one PB-3) demands byte-identical serialization of ~12 `Comparison` consumers and a
   four-helper extraction — that is the riskiest thing to hand a Sonnet. Option: keep all `shared-edit`
   waves-of-one at **Opus** workers, reserving Sonnet for the new-file majority. This costs more but de-
   risks exactly the tasks the judge is most likely to FAIL. Worth deciding per-class rather than per-
   task.
