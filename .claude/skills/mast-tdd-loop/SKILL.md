---
name: mast-tdd-loop
description: Drives a TDD cycle for extending MagicAST (the Magic-the-Gathering oracle-text parser at libs/magic-ast/). Pick an unparseable card from triage, hand-parse the gold AST, run the NUnit suite to surface schema and parser gaps, then close each gap with a new AST node or parser rule. Every test must be green to land a batch — no ratchet tolerance. Use when extending MagicAST coverage, working on the MAST TDD loop, hand-parsing a card, adding a new AST node or ability/effect/cost type, adding an ability-kind parser, or when the user references issue #7, "mast-tdd-loop", "MAST round-trip", or "the MAST cycle".
---

# MAST TDD loop

This skill drives one round of extending MagicAST. Each round:
- starts at parser gaps surfaced by the triage report,
- ends with new AST nodes and/or parser rules that close them,
- lands `nx run mast:test` at 100% green (vanilla NUnit; no ratchet tolerance) and rolls the corpus-wide triage forward.

## Roles

The skill is read from five perspectives. Annotations on each step say which agent owns it.

- **`[main]` — main agent (orchestrator).** Coordinates the batch. Picks **families** from triage (clusters of failures sharing `(pattern, lastAttemptedRule)`), writes the judge briefing (rule facts), dispatches helpers, merges output, dispatches mechanical sub-agents, dispatches judge-verify, merges everything, validates NUnit at 100%, regenerates the glossary, re-runs triage. Owns every cross-cutting artifact.
- **`[sub:helper-novel]` — Opus novel-shape helper (one per batch).** Receives the briefing + only those candidates that need new AST types, new discriminators, or sit on a doctrinal edge (multi-effect-per-clause, colorless, color-ordering, trait-boundary calls). Creates any new AST types (Red #1) and writes the gold fixtures for them. RoundTrip must be green for all its fixtures before it finishes.
- **`[sub:helper-mech]` — Sonnet mechanical-fixture helpers (M per batch, parallel).** Each receives a group of fixtures whose AST shapes already exist in `GLOSSARY.md`. Their contract is strictly mechanical: look up existing AST types, write the gold AST, RoundTrip green. **If they'd need a new AST type, they bail** — that's `[sub:helper-novel]`'s territory, not theirs.
- **`[sub:mech]` — mechanical parser sub-agents (N per batch, parallel) — FAMILY CONTRACT.** Each receives a **family**: a `(pattern, lastAttemptedRule)` cluster plus N fixtures (5-10) spanning that family's surface. The contract is: **make ALL N fixtures green via ONE consolidated parser surface** (one new method, or one extended existing method). If the mech finds itself writing N separate `TryParseX` methods, it's misread the family — bail and report the sub-patterns. NUnit's `Parser_ProducesExpectedOutput` for every assigned card must pass before commit.
- **`[sub:judge]` — judge-verify sub-agent (one per batch).** Reads the batch's changed files post-merge, renders strict PASS/FAIL verdicts per the `mast-judge` skill. Any FAIL halts the merge into main.

If you are invoked directly by the user (no orchestrator above you), wear all five hats: do every step yourself in sequence, single-threaded. In that mode, the value of the helper/mech split shrinks — but the discipline (rule lookup → gold → parser → judge gate) still stands.

## Quick start

`[main]` orchestration (two judge gates flanking parallel helper + mech phases):

```
Step 0    Rebase + refresh triage. Capture $WORKTREE_ROOT (sub-agents use git -C "$WORKTREE_ROOT").
Step 1    Pick N families from topGaps[]. A family = (pattern, lastAttemptedRule) cluster
          with 5-10 candidate fixtures sharing the same parser failure point.
Step 1.5  Enrich each family inline (judge-pass-1) → docs/judgments/briefing-{date}.md
Step 2a   Triage each family's candidate cards into:
          - novel-shape (need new AST type / discriminator / doctrinal edge)
          - mechanical (gold can be written from existing GLOSSARY.md types)
Step 2b   Dispatch [sub:helper-novel] (1 Opus, all novel-shape cards)
          Dispatch M [sub:helper-mech] (Sonnet, parallel, mechanical cards grouped by family)
Step 3-4  Helpers hand-parse their assigned fixtures → RoundTrip green
Step 5    Merge all helper branches → confirm Red #1 closed across all fixtures
Step 6    Dispatch N [sub:mech] in parallel — one per FAMILY (not per fixture)
Step 7-8  [sub:mech] Close Red #2 for ALL fixtures in the family via one parser surface
Step 9    Dispatch [sub:judge] — judge-pass-2 → docs/judgments/verdict-{date}.md
          - PROCEED (0 FAIL) → continue to Step 10
          - HALT (any FAIL)  → don't merge mech branches, remediate inline or via follow-up sub-agent
Step 10   Merge mechanical branches → NUnit (100% green required) → glossary → re-run triage → loop
```

`[sub:helper-novel]` / `[sub:helper-mech]` per-session (handles assigned candidates sequentially):

```bash
# 1) Read conventions + the judge briefing
cat libs/magic-ast/CONTRIBUTING.md
cat libs/magic-ast/GLOSSARY.md
cat docs/judgments/briefing-{date}.md   # the rule facts you must respect
# 2) For each candidate: hand-parse, write fixture, run RoundTrip:
nx run mast:test  # all Output_RoundTrip tests must be green for your fixtures
# 3) If RoundTrip fails: Red #1 — create the missing AST type. Re-run.
# 4) When all N RoundTrips are green, commit and report.
```

`[sub:mech]` per-session (handles a FAMILY — N fixtures sharing one parser failure point):

```bash
# 1) Read conventions + your family briefing
cat libs/magic-ast/CONTRIBUTING.md
cat libs/magic-ast/GLOSSARY.md
cat {your family briefing path}                # pattern + lastAttemptedRule + N fixture paths
# 2) Run ALL your family's tests to see the diffs:
for card in {ShortCardName1} {ShortCardName2} ...; do
  dotnet test --filter "FullyQualifiedName~${card}"
  cat /tmp/mast-diffs/{Set}_${card}.actual.json
done
# 3) Find the GENERALIZATION: what one parser surface (one new method, or one extension
#    of an existing method) covers all N fixtures? Look at lastAttemptedRule — that's
#    where the existing parser gave up. The gap is usually right at that bail point.
# 4) Extend the parser until ALL N tests pass. NO fixture edits, NO AST changes.
# 5) If you find yourself writing N separate methods → STOP, bail with sub-pattern report.
# 6) When all green, commit (git -C "$WORKTREE_ROOT" ...) and report.
```

## Before you touch anything

Read these in order. They take five minutes and they save hours.

1. **Agent memory.** Three items are load-bearing for this skill:
   - `feedback_mast_describes_not_executes` — MAST is descriptive, not a rules engine. Model what oracle text *says*, not what the rules *do* at runtime.
   - `reference_mtg_glossary_location` — where the parsed MTG Comprehensive Rules glossary lives, and when to consult it.
   - `feedback_contributing_replaces_context` — in this workspace, library glossaries/conventions live in `CONTRIBUTING.md`, not `CONTEXT.md`.

2. **`libs/magic-ast/GLOSSARY.md`** — auto-generated index of every current AST node, with discriminator strings and source links. **Look here before inventing a new node.** Many things you might want already exist (`Quantity`, `ObjectFilter`, `TriggerCondition`, `UnlessClause`, the three trait interfaces under `AST/Effects/Traits/`).

3. **`libs/magic-ast/CONTRIBUTING.md`** — terminology, AST styling, attribute conventions for the magic-ast library.

4. **`tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json`** — parsed MTG Comprehensive Rules glossary. **Consult this whenever oracle text uses an MTG-domain term** (e.g., "ward", "embalm", "scry", "step", "ability"). Use the rules-accurate definition, not vernacular. **Note for sub-agents in worktrees:** this file lives in the main repo's source tree and may not be present inside an agent worktree. If you can't read it locally, surface the gap rather than guessing — or `git show main:tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json` from your worktree to read it.

## The cycle

### Step 0 — `[sub]` Lock down your worktree and rebase onto main

Two mandatory pre-flight steps. **Every git command in this session must use `git -C "$WORKTREE_ROOT"`** so CWD slips can't land commits on the wrong branch.

**Step 0a — Capture the worktree root.**

```bash
export WORKTREE_ROOT="$(pwd)"
echo "$WORKTREE_ROOT"   # sanity-check: should be /home/.../MagicAtlas/.claude/worktrees/agent-XXX
```

If `pwd` does NOT contain `.claude/worktrees/`, you are NOT in a worktree — STOP and report. Working from the main repo will land commits on `main`.

**Step 0b — MANDATORY pre-flight rebase gate.** Run this script verbatim before any other action. It HALTS your session if the worktree was branched from a stale base — the recurring failure mode that has destroyed batches in past runs (worktree branched from a commit deep in history, never rebased, modifications to shared files would revert the rule-split / fixture work / parser refinements on merge).

```bash
# Pre-flight: refuse to proceed if worktree is too far behind main.
git -C "$WORKTREE_ROOT" fetch origin main 2>/dev/null || git -C "$WORKTREE_ROOT" fetch origin
MERGE_BASE=$(git -C "$WORKTREE_ROOT" merge-base HEAD main)
BEHIND=$(git -C "$WORKTREE_ROOT" rev-list --count "${MERGE_BASE}..main")
echo "Worktree merge-base: $(git -C "$WORKTREE_ROOT" log --oneline -1 $MERGE_BASE)"
echo "Behind main by: $BEHIND commits"

if [ "$BEHIND" -gt 20 ]; then
  echo "STOP — worktree is $BEHIND commits behind main." >&2
  echo "Either rebase: git -C \"$WORKTREE_ROOT\" rebase main" >&2
  echo "Or report base-staleness to the orchestrator and exit without committing." >&2
  exit 1
fi

# If behind by 1-20 commits, attempt automatic rebase. Abort the session on conflict.
if [ "$BEHIND" -gt 0 ]; then
  git -C "$WORKTREE_ROOT" rebase main || {
    echo "STOP — rebase conflict. Report to orchestrator." >&2
    git -C "$WORKTREE_ROOT" rebase --abort
    exit 1
  }
fi
```

Tell-tale smells that the gate missed something (rare but worth knowing):
- Code references pre-consolidation paths (`tools/test/magic-ast/...` instead of `tests/magic-ast-tests/...`).
- AST property accesses that don't compile (e.g., `Effect.Duration` instead of `(effect as IDurativeEffect)?.Duration`).
- Test counts that disagree wildly with what the orchestrator briefed you on.

If you see those AFTER the gate passed, STOP and report — the gate threshold may need tightening, or you're working against an unmerged in-flight branch.

**For the rest of this session: all `git` commands MUST use the `-C "$WORKTREE_ROOT"` flag.** This includes `add`, `commit`, `push`, `merge`, `status`, `log`, `diff`. CWD-based git is forbidden. If you find yourself `cd`-ing to inspect a main-repo file, use `git -C "$WORKTREE_ROOT" show main:path/to/file` instead of changing directory.

### Step 1 — `[main]` Pick families

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. Each `topGaps[]` entry now carries `pattern` AND `lastAttemptedRule` (post-telemetry). A **family** is a `(pattern, lastAttemptedRule)` cluster — failures that share both the high-level pattern AND the specific parser rule that bailed. This is the unit of work for a family-contract mech.

For each family you want to address this batch:

- Select **5-10 candidate fixtures** from `candidateLines[]`, sorted by `cleanlinessScore` ascending. The mech will be expected to make all of them green via one parser surface — so they need to genuinely share a structural shape. Skip lines with `alreadyHandParsed: true`.
- **Diversity check:** the 5-10 should not all be near-duplicates. If three lines are "[CardName] deals 3 damage to target creature" with only the name changing, that's effectively one fixture's worth of pressure. Pick lines that vary the dimensions you suspect the parser surface needs to handle (target shapes, count phrasings, modifier suffixes).
- **Bail-out signal:** if the family's `lastAttemptedRule` is `null` or `"FallbackParser.*"`, the failure isn't sharp enough yet — this family is in the `AmbiguousStructure` zone. Either pick a different family or surface it for pattern-bucket refinement.

Choose batch size based on the number of **non-overlapping** families. Two families that target the same parser file (e.g., both want `SpellAbilityParser`) should be serialized across batches, not paralleled — unless the parser-rule split has landed and each rule lives in its own file.

### Step 1.5 — `[main]` Enrich candidates with rules context (judge mode = inline)

Before dispatching the helper, the orchestrator briefs each candidate with authoritative MTG rules context. This is **judge-pass-1 (enrichment)** — orchestrator-internal, because the local state already has the candidate picks and direct file access to the rules data. (Judge-pass-2 in Step 9 IS a separate sub-agent — independent verification value justifies the dispatch.)

Write a single batch briefing to `docs/judgments/briefing-{YYYY-MM-DD}.md` (suffix `-N` if it already exists for today). One section per candidate. **Keep it light** — establish facts, don't prescribe AST shapes. ~200 words per candidate.

For each candidate:

1. Identify the MTG mechanic(s) the oracle line invokes (keyword ability, triggered event, keyword action, effect type, cost type).
2. Look each mechanic up in `glossary.json` and `rules-structure.json`:
   ```bash
   jq '.terms.{Term}' tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json
   jq '.sections[].subsections[] | select(.number == {N}) | .rules[] | select(.number == "{N.M}")' \
     tests/atlas-flow-test/Data/_03_Primary/Datasets/rules-structure.json
   ```
3. Write a candidate briefing in this shape:

```markdown
## Family {n}: ({pattern}, {lastAttemptedRule})

**Failure signal:** parser bails at `{lastAttemptedRule}` for {N} cards in this cluster.

### Cards in this family
1. **{Card Name 1}** — `{oracle text of the candidate line}` (cleanliness={score})
2. **{Card Name 2}** — `{oracle text}` (cleanliness={score})
... (5-10 cards)

### Relevant rules
- **{Rule number} {Rule title}** — {subrule text or glossary def, ~1-2 sentences quoted from the rules data}
- {additional rules as needed}

### Expected generalization
- {a sentence or two on what ONE parser surface should look like to cover all N — informative, not prescriptive. The mech may discover the family is too coarse, in which case they bail.}

### Anti-patterns
- {1-3 bullets of specific things the sub-agent should NOT do, grounded in the rules}

### Glossary gaps (if any)
- {term} — referenced in oracle but missing from glossary.json
```

If a candidate's mechanic isn't in `glossary.json` at all (and the term is genuinely MTG-domain, not vernacular), do not dispatch the sub-agent — swap the candidate for a different one or escalate to the human.

The briefing is **informative, not prescriptive**. The judge reports rule facts; the sub-agent owns the AST shape.

### Step 2 — `[main]` Triage candidates and dispatch helpers

**Step 2a — Triage each family's candidates into novel-shape vs. mechanical.**

For each candidate fixture across all families, decide:

- **Novel-shape:** the gold AST would need a new discriminator (`kind`, `effectType`, etc.), a new field on an existing record, a doctrinal edge (multi-effect-per-clause, colorless-as-empty, color-ordering), or a trait-boundary decision. These go to the Opus helper.
- **Mechanical:** the gold AST can be assembled from existing types listed in `GLOSSARY.md`. No new types, no new fields. These go to Sonnet helpers.

A coarse heuristic: if you can name the discriminator strings the gold will use (`"effectType": "destroy"`, `"kind": "spell"`, etc.) by looking at `GLOSSARY.md` without doubt, it's mechanical. If there's any "I think we'd need a new X" instinct, it's novel.

**Step 2b — Dispatch helpers in parallel.**

Spawn ONE `[sub:helper-novel]` via `Agent` (model: Opus, `isolation: "worktree"`). Its prompt **must** include:

- The judge briefing path.
- The novel-shape candidates only (oracle text, source card metadata, `Input` DTO).
- A pointer to this skill — the `[sub:helper-novel]` sections (Steps 3 and 4).
- The memory items listed in "Before you touch anything."
- The branch name (e.g., `mast-tdd/helper-novel-{date}`).
- Explicit instructions: "You own AST-type creation and the fixtures that need new types. Write the fixtures, create any new AST types needed for RoundTrip to pass. Don't touch parser code. **Use git -C \"$WORKTREE_ROOT\" for every git command** (Step 0 mandate)."

Spawn M `[sub:helper-mech]` sub-agents via `Agent` (model: Sonnet, `isolation: "worktree"`, **in parallel**). Group fixtures by family so each helper-mech sees coherent work. Each prompt **must** include:

- The judge briefing path.
- The assigned mechanical fixtures (1-3 per helper-mech).
- A pointer to this skill — the `[sub:helper-mech]` sections.
- The branch name (e.g., `mast-tdd/helper-mech-{family-slug}-{date}`).
- Explicit instructions: "Look up existing AST types in GLOSSARY.md, write the gold AST, RoundTrip green. **Two bail criteria — fire on EITHER:** (a) you would need a new AST type, or (b) the card's sibling abilities (non-family ones on the same card) would require parser work beyond the scope of any in-flight family. Criterion (b) is the Bloodcurdler lesson — fixture's RoundTrip can be green while `Parser_ProducesExpectedOutput` will still red because a sibling needs parser work no mech in the batch is scoped to provide. Run `dotnet test --filter Parser_ProducesExpectedOutput&FullyQualifiedName~{Card}` after writing each fixture; if it fails for reasons OTHER than your assigned family's parser gap, BAIL. **ColorIdentity arrays are set-semantic — order is no longer enforced by the comparator, but emit WUBRG for readability.** **Use git -C \"$WORKTREE_ROOT\" for every git command.**"

Wait for ALL helpers to finish before continuing. If a helper-mech bails on a fixture, the orchestrator reassigns it to the novel-shape helper (which may need to be re-dispatched).

### Step 3 — `[sub:helper]` Hand-parse all candidates (gold AST, eventual-truth)

**The hand-parsed JSON is the gold AST — what a fully-implemented parser SHOULD eventually emit for this card.** It is not a snapshot of the current parser's output, and it is not partially populated with `UnparsedAbility` nodes where the parser currently falls short.

This is the single most important rule of the loop. Get it wrong and the TDD direction inverts: the test "passes" by matching the parser's current limitations rather than driving the parser forward.

**Forbidden in the gold output:**
- `"Kind": "unparsed"` anywhere in `Output.Oracle.Abilities` (or nested inside Saga chapters, Modal options, Level-up stanzas, etc.).
- Embedded `Diagnostics[]` arrays describing current parser failures.
- `Pattern` strings copied from `FallbackParser.InferFailurePattern`.

**Allowed and expected:**
- AST node shapes that don't yet exist — create them in `libs/magic-ast/AST/` as Red #1 (see Step 4 below).
- Abilities the gold needs to model fully, even if no current parser produces them. Mechanical sub-agents will close that parser gap in Step 7. Your job is the gold; theirs is the parser.

**Card-scope:** every ability on the chosen card must be gold-modeled. The mechanical sub-agents need to drive the per-card `Parser_ProducesExpectedOutput` test green — a fixture with one untaught sibling ability is unmergable. If a candidate is too complex to fully gold-model in this batch, swap it: the triage report's `candidateLines[]` is sorted by `cleanlinessScore`; scan deeper for a card where the target pattern dominates with simpler siblings.

**File location and casing:**

Create `tests/magic-ast-tests/Data/HandParsedCards/{Set}/{CardName}.json` containing:

```json
{
  "Input": {
    "Name": "...",
    "ManaCost": "{...}",
    "TypeLine": "...",
    "OracleText": "...",
    "Power": "...",
    "Toughness": "...",
    "Colors": ["..."],
    "ColorIdentity": ["..."]
  },
  "Output": {
    "Name": "...",
    "TypeLine": { "Raw": "...", "Types": [...], "Subtypes": [...] },
    "Oracle": { "RawText": "...", "Abilities": [ /* AST */ ] },
    "Attributes": [ /* CardAttribute polymorphic list */ ]
  }
}
```

**JSON casing convention:**
- Property names: **PascalCase** (`Effects`, `Trigger`, `EffectType`, `Target`).
- Discriminator string values: **camelCase** (`"EffectType": "dealDamage"`, `"Kind": "triggered"`, `"DurationType": "untilEndOfTurn"`).

**Schema discipline:**
- Reuse existing discriminator strings. Consult `GLOSSARY.md` first. Don't invent `"dealDamage"` if it's already there.
- Convention for new discriminators is **camelCase** (matches every existing discriminator: `dealDamage`, `addMana`, `untilEndOfTurn`).
- Model what the oracle text **says**, not what the rules **do**. No turn-state, priority, stack ordering, or layering fields.
- For optional effect dimensions (duration, "you may" / "if you do", "unless [player] pays [cost]"): use the existing trait interfaces (`IDurativeEffect`, `IOptionalEffect`, `IPreventableEffect`). JSON property names: `Duration`, `IsOptional` / `IfYouDo`, `UnlessClause`.

### Step 4 — `[sub:helper]` Surface and close Red #1 (schema gap)

```bash
nx run mast:test
```

The helper's job ends when every fixture's `Output_RoundTrip_ProducesIdenticalJson` is green. `Parser_ProducesExpectedOutput` can be red — that's the mechanical sub-agents' job to close in Step 7.

**Red #1 doesn't always fire.** If your gold uses only AST primitives that already exist, RoundTrip succeeds on first attempt. That's a sign the schema is sufficient.

When Red #1 does fire, it's a `JsonException` thrown during `CardTestCase.GetOutput()` — i.e., **deserialization** of the hand-parsed JSON to `CardOutputAST`. Two flavors:

**4a. Unknown discriminator value**
```
System.Text.Json.JsonException : Unknown {Base} discriminator '{value}'. Known: {list}.
```
You wrote a `kind`/`effectType`/`durationType`/etc. value that no AST type registers.

Action: create a new sealed record under the appropriate `AST/` subdirectory. Decorate with the matching attribute:

| Base type | Attribute | Discriminator JSON property |
|---|---|---|
| `Ability` | `[OracleAbility("foo")]` | `"kind"` |
| `Effect` | `[OracleEffect("foo")]` | `"effectType"` |
| `Duration` | `[OracleDuration("foo")]` | `"durationType"` |
| `Cost` | `[OracleCost("foo")]` | `"costType"` |
| `Quantity` | `[OracleQuantity("foo")]` | `"quantityType"` |
| `ReplacementEvent` | `[OracleReplacementEvent("foo")]` | `"eventType"` |
| `CardAttribute` | `[CardAttributeKind("foo")]` | `"kind"` |
| `PowerToughnessValue` | `[PowerToughnessKind("foo")]` | `"valueType"` |

The record must `: <Base>` and (for Effect subtypes) typically `, IOptionalEffect, IDurativeEffect, IPreventableEffect` — copy the pattern from any existing sibling under `AST/Effects/`.

**4b. Unmapped JSON property**
```
System.Text.Json.JsonException : The JSON property '{name}' could not be mapped to any
.NET member contained in type '{FullType}'.
```
You added a field that the target concrete record doesn't declare.

Action: either
- add the field to the record (with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` for optional fields), or
- remove the field from the JSON (and reconsider whether it belonged there at all).

Iterate until every fixture's RoundTrip is green. Commit on your branch.

Closing helper manifest:

```markdown
## MAST helper — manifest

**Branch:** {branch-name}
**Fixtures written:** {list of N paths}
**New AST types:** {list with discriminators}, or "none"
**RoundTrip status:** all N green / X red (with reasons)
```

Do not write any parser code. Do not regenerate `GLOSSARY.md`. Do not re-run triage. Orchestrator owns those.

### Step 5 — `[main]` Merge helper's branch + confirm Red #1 closed

```bash
git merge --no-ff {helper-branch}
nx run mast:test
```

After merge, the helper's N new `Parser_ProducesExpectedOutput` tests are red (no parser yet); their `Output_RoundTrip_ProducesIdenticalJson` tests must be green (schema represents the gold). That's the expected state going into Step 6.

If any RoundTrip is red on main post-merge, the helper didn't finish. Dispatch a focused follow-up sub-agent on the failing fixture(s), or roll back and re-dispatch the helper with more explicit context.

### Step 6 — `[main]` Dispatch mechanical sub-agents in parallel — ONE PER FAMILY

Spawn N sub-agents, ONE per **family** (not per fixture), in parallel via the `Agent` tool with `isolation: "worktree"`. Each prompt **must** include:

- The family identity: `pattern`, `lastAttemptedRule` from triage.
- **All 5-10 fixture paths in the family** (not just one).
- A pointer to this skill (the `[sub:mech]` sections — Steps 7 and 8).
- The branch name (e.g., `mast-tdd/{family-slug}-mech`).
- Explicit family contract: "Teach the parser to make `Parser_ProducesExpectedOutput` pass for ALL of these cards via ONE consolidated parser surface (one new method, or one extension of `lastAttemptedRule`). If you find yourself writing N separate methods, you've misread the family — STOP and bail with a sub-pattern breakdown. Do NOT modify fixtures. Do NOT add or modify AST types. **Use git -C \"$WORKTREE_ROOT\" for every git command.**"

After dispatching, wait for all N to report back before continuing to Step 9.

**Why family contracts.** Per-fixture mechs accumulate adjacent `TryParseX` methods in the same parser file — the merge-conflict hotspot that closed out the prior 10-batch run. Family contracts force generalization at write time: the mech must find a parser surface that handles the family's variation, not bolt on a new method for each line. Bonus: when a family is genuinely too coarse (mech bails with sub-patterns), the bail itself refines the triage taxonomy.

### Step 7 — `[sub:mech]` Close Red #2 (parser gap) — FAMILY CONTRACT

Red #2: `Parser_ProducesExpectedOutput` failing across your assigned family. The parser produces an `UnparsedAbility` (or the wrong structured node) for each card in the family. Your job is to make **all of them** pass via **one** parser surface.

**Read the family briefing first.** `lastAttemptedRule` tells you exactly where the existing parser gave up. That's where the gap lives — usually within the rule, not in a wholly new method. Inspect the rule and extend it; don't reach for a sibling rule unless the failure point is genuinely outside the existing rule's scope.

**Sub-pattern bail.** If two cards in the family fail for genuinely different reasons (different token shapes, different AST nodes needed), the family is too coarse. STOP — don't force it. Write a sub-pattern breakdown:

```markdown
## Family bail — sub-patterns discovered

**Family:** ({pattern}, {lastAttemptedRule})
**Cards processed before bail:** {n}
**Sub-pattern A:** {description}
- {card 1}, {card 2}, ...
**Sub-pattern B:** {description}
- {card 3}, {card 4}, ...
```

The orchestrator routes the bail to the human and `FallbackParser.InferFailurePattern` gets a refinement ticket. **Bailing is not failure** — it's the explicit value of the family contract.


Use `/tmp/mast-diffs/{Set}_{Card}.expected.json` + `.actual.json` to see the precise diff (auto-dumped on test failure). Read both, find the field-level differences, work backward to which parser rule emits or misses the gold shape.

Action: extend the appropriate `IAbilityParser` implementation in `libs/magic-ast/Parsing/Parsers/`. The dispatch table is reflection-discovered via `[OracleAbilityParser(AbilityKind.X)]` — **do not edit `OracleParser.cs` or `AbilityParserRegistry.cs`**.

- New ability-kind parser (e.g., `ModalAbilityParser`, `SpellAbilityParser`): create a new file with the attribute. The registry picks it up automatically.
- Existing parser missing a case (e.g., `TriggeredAbilityParser` for a new trigger event): extend the relevant private method.

Iterate until `Parser_ProducesExpectedOutput` for **every card in your family** passes.

**Mechanical scope. Don't:**
- Modify any fixture. If you think a gold is wrong (including colorIdentity ordering — see "Sibling-shape allowance" below), STOP and report — orchestrator-side fix.
- Add or modify AST types. That's the helper's territory.
- Modify fixtures outside your family, even to "improve consistency." Stay in your family's lane.
- Add N separate methods when the family contract calls for one. If you can't find the generalization, bail with sub-patterns — that's the explicit escape valve.

**Sibling-shape allowance.** Real fixture cards are multi-ability. Your family addresses one ability shape; the SAME fixture may carry a sibling ability that needs a separate parser surface for `Parser_ProducesExpectedOutput` to pass on that card. You MAY add a tight, narrowly-scoped sibling parser surface when ALL of these hold:

1. The sibling shape is **single-shape** (one new method or one new `[SpellRule]` / `[TriggeredRule]` file — NOT a whole family's worth of work).
2. The sibling shape does NOT belong to another family being addressed in the current batch (check the briefing for what other families are in-flight). If a conflict exists, BAIL on the multi-ability card instead.
3. The sibling shape is fully covered by **existing AST types** (consult `GLOSSARY.md`). If it would need a new AST type, BAIL — the helper-mech should have caught this.
4. The sibling work is genuinely smaller than the family work — a paragraph, not a section. A new parser file or one small method is acceptable; a new ability-kind parser is not.
5. You record the sibling work explicitly in the closing manifest under a `### Sibling additions` section, so judge and orchestrator can review.

If the sibling shape doesn't meet ALL five criteria, BAIL on the multi-ability card. The orchestrator will swap it for a single-family card or schedule the sibling work as a follow-up family. Don't stretch the family contract beyond recognition — the discipline is what makes parallel mech dispatch tractable.

**Examples of acceptable sibling-shape work** (from prior batches):
- AsLongAs mech adding an "attack with X and another" trigger to make Merry's full test pass.
- CreateToken mech adding `InvestigateSpellRule` for HardEvidence's `Investigate.` sibling line.
- BeginningOfTurn mech adding activated-parser extensions for Broodheart's `Activate only as a sorcery` restriction.

**Examples that would NOT qualify (would require BAIL):**
- Adding a Modal ability parser when only one family fixture happens to be modal.
- Implementing a new TriggerEvent enum value plus its parser surface.
- Solving the Threshold ability-word + composite-AsLongAs shape because Bloodcurdler has it — that's a separate family's worth of work.

### Step 8 — `[sub:mech]` Report back

Confirm all family tests pass:

```bash
for card in {ShortCardName1} {ShortCardName2} ...; do
  dotnet test --filter "Parser_ProducesExpectedOutput&FullyQualifiedName~${card}" || break
done
```

Commit on the assigned branch (`git -C "$WORKTREE_ROOT" commit ...`). Then emit this manifest:

```markdown
## MAST mechanical sub-agent — manifest

**Branch:** {branch-name}
**Family:** ({pattern}, {lastAttemptedRule})
**Cards in scope:** {N}     **Cards green:** {n_green}     **Cards bailed:** {n_bailed}

### Parser surface
- {one paragraph describing the SINGLE parser change that closed the family}
- File: {file:lines}

### Generalization notes
- {what variation across the N cards the new surface handles}

### Stop / handoff
- {sub-pattern bail block (Step 7), or "none"}
```

Do not regenerate `GLOSSARY.md`. Do not re-run triage. Do not touch fixtures or AST types. Orchestrator owns those.

### Step 9 — `[main]` Judge-pass-2 (rules-accuracy verify)

Before merging mechanical branches, dispatch a sub-agent that uses the `mast-judge` skill to verify the rules-accuracy of the batch's output. This is the second judge pass — Step 1.5 established what the rules SAY; this one checks whether the work descriptively MATCHES.

Gather the scope: helper's merged fixtures + AST changes (already on main) plus every parser file touched across the mechanical branches:

```bash
# Helper's work (already on main)
git diff --name-only {pre-helper-main}..main | grep -E '(tests/magic-ast-tests/Data/HandParsedCards/.*\.json|libs/magic-ast/AST/.*\.cs)$'
# Mechanical branches' parser changes
for branch in {mechanical-branches}; do
  git diff --name-only main..$branch | grep -E 'libs/magic-ast/Parsing/.*\.cs$'
done
```

Dispatch the judge:

```
Agent({
  description: "MAST judge — verify batch",
  subagent_type: "claude",
  # No worktree — judge reads files, no writes to AST source
  prompt: "Use the mast-judge skill. Mode: verify. Scope: {list of file paths}.
           Output path: docs/judgments/verdict-{date}.md.
           After writing the verdict file, return PROCEED or HALT."
})
```

Read the judge's closing message:

- **PROCEED**: judge rendered 0 FAILs. Continue to Step 10.
- **HALT**: one or more FAIL verdicts. Do not merge mechanical branches. Address every FAIL — either inline (orchestrator fixes the items) or via a follow-up sub-agent dispatched specifically to remediate. Then re-run Step 9. The loop does not continue until the verdict is PASS.

The judge is strict binary PASS/FAIL; there is no "concern" tier. See `.claude/skills/mast-judge/SKILL.md` for the anti-pattern enumeration (free text, escape hatches, unparsed nodes, imprecise citations all FAIL).

### Step 10 — `[main]` Merge mechanical branches, validate, regenerate, re-triage

After Step 9 returns PROCEED:

```bash
# 1) Merge all reporting sub-agent branches in order.
for branch in {sub-agent branches}; do
  git merge --no-ff "$branch"
done

# 2) Confirm post-merge NUnit is 100% green. Two sub-agents' edits can be
# individually-green but jointly-red — this catches that. No ratchet
# tolerance: any test red after merge halts the batch.
nx run mast:test

# 3) Regenerate the glossary once for the merged tree.
nx run magic-ast:glossary

# 4) Refresh the corpus-wide triage report.
nx run mast:run
```

If substep 2 fails, do not continue. Roll the merges back and investigate per the `[main]`-only stop conditions below.

Produce a batch-level report and decide whether to loop:

```markdown
## MAST TDD batch — aggregate

**Sub-agents dispatched:** {n}     **Bailed:** {n}     **Landed:** {n}
**Branches merged:** {list}
**Judge briefing:** `docs/judgments/briefing-{date}.md`
**Judge verdict:** `docs/judgments/verdict-{date}.md` ({n CORRECT} / {n CONCERN} / {n BLOCKING})

### Cumulative landed
- AST types: {flat union}
- Parsers: {flat union}

### Judge CONCERNs to track (if any)
- {one-line summary per CONCERN, with the verdict file as the cite}

### Corpus-wide delta (post-triage rerun)
- Total cards flipped green: {count}
- Pattern frequencies (top 5 changes): {pattern: before → after}
- New patterns surfaced: {list}

### Next batch
- Suggested gaps: {next topGaps[0..N], or "stop — diminishing returns"}
- Patterns to watch (didn't shrink as expected): {list, route to human if persistent}
```

Loop back to Step 1 with the refreshed triage report, or stop if returns are diminishing.

## Stop conditions

Bail out and escalate if any of these hold.

`[sub]` — write the reason into the "Stop / handoff" line of your manifest and exit. Do not retry.

`[main]` — when a sub-agent reports a stop, do not silently re-dispatch. Route to human with the sub-agent's manifest attached, then proceed with the remaining sub-agents.

Conditions:

- A trait boundary decision is needed (e.g., adding a new `Effect` trait interface beyond the existing three). This is a HITL decision per `feedback_mast_describes_not_executes`.
- The card's oracle text drags in mechanics that fundamentally challenge the descriptive/engine boundary — layering, replacement-effect ordering, priority, etc. Surface the tension; don't paper over it.
- More than 3 consecutive reds without forward progress. Likely a misclassification, a deeper architectural gap, or you're working at the wrong level of abstraction.
- You need to edit infrastructure files:
  - `libs/magic-ast/Parsing/OracleParser.cs` (orchestrator)
  - `libs/magic-ast/Parsing/AbilityParserRegistry.cs`
  - `libs/magic-ast/Serialization/PolymorphicReflectionConverter.cs`
  - Any base class carrying `[PolymorphicBase]` (Ability, Effect, Duration, Cost, Quantity, ReplacementEvent, CardAttribute, PowerToughnessValue)

  The whole point of the restructure was that one-card sessions shouldn't touch these. If you genuinely need to, that's a separate architectural ticket.
- An MTG term in oracle text isn't in `glossary.json` (the parsed Comprehensive Rules glossary). Surface the gap. Don't guess at the meaning.
- The NUnit suite isn't 100% green at the end of your session. Either you didn't teach the parser to produce the gold (the normal case — keep working), or one of your changes regressed another fixture (read the diff dumps in `/tmp/mast-diffs/` for both).

`[main]`-only conditions:

- Post-merge NUnit (Step 10 substep 2) isn't 100% green. Two mechanical sub-agents' parser edits conflict semantically even though each was green in isolation. Roll back the merges and either re-dispatch them serially or route to human.
- Judge-pass-2 returns HALT (one or more BLOCKING verdicts). Do not merge. Surface the verdict report at `docs/judgments/verdict-{date}.md` to the human along with the offending sub-agent branches.
- Two sub-agents in the same batch claim the same `AbilityKind` for a new parser, or the same discriminator string for a new AST node. Serialize: pick one to land first, then re-dispatch the other against the post-merge tree.
- Two `[sub:mech]` family contracts target the SAME parser file (e.g., both want to extend `SpellAbilityParser`). Either (a) wait for the parser-rule split to land (rule-per-file means no conflict), or (b) serialize the two families across batches.
- A `[sub:mech]` bails with a sub-pattern report. Don't force the bailed sub-pattern through — file a follow-up family briefing for the next batch, and let the post-batch triage refresh decide whether the sub-patterns warrant `FallbackParser.InferFailurePattern` refinement.
- A `[sub:helper-mech]` bails on a fixture because it would need a new AST type. Reassign the fixture to the novel-shape helper (which may need to be re-dispatched if it has already completed).
- Post-batch triage rerun shows fewer total parsing successes than the pre-batch state. Roll back the merges and investigate.
- A pattern bucket consistently fails to shrink across multiple batches (suggests the bucket is too coarse and needs `FallbackParser.InferFailurePattern` to be refined). File a follow-up issue; that work is out of scope for this skill.

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
| Test runner | `nx run mast:test` |
| Triage runner | `nx run mast:run` |
| Glossary regenerator | `nx run magic-ast:glossary` |
