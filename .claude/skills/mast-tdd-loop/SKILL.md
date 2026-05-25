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

The skill is read from four perspectives. Annotations on each step say which agent owns it.

- **`[main]` — main agent (orchestrator).** Coordinates the batch. Picks candidates from triage, writes the judge briefing (rule facts), dispatches the hand-parsing helper, merges its output, dispatches mechanical sub-agents in parallel, dispatches the judge-verify sub-agent, merges everything, validates NUnit at 100%, regenerates the glossary, re-runs the triage flow. Owns every cross-cutting artifact.
- **`[sub:helper]` — hand-parsing helper sub-agent (one per batch).** Receives the judge briefing + the N candidate picks. Writes all N gold-AST fixtures and creates any new AST types needed (Red #1 — schema gap). Each fixture must RoundTrip cleanly before they finish. Independent MTG-context check on the orchestrator's candidate picks.
- **`[sub:mech]` — mechanical parser sub-agents (N per batch, parallel).** Each receives ONE fixture path. Job is exclusively: teach the parser to produce the gold AST in that fixture. No fixture work, no AST-type creation. NUnit's `Parser_ProducesExpectedOutput` for the assigned card must pass before they commit.
- **`[sub:judge]` — judge-verify sub-agent (one per batch).** Reads the batch's changed files post-merge, renders strict PASS/FAIL verdicts per the `mast-judge` skill. Any FAIL halts the merge into main.

If you are invoked directly by the user (no orchestrator above you), wear all four hats: do every step yourself in sequence, single-threaded. In that mode, the value of the separate hand-parsing pass shrinks — but the discipline (rule lookup before gold writing, gold before parser work, judge gate before declaring done) still stands.

## Quick start

`[main]` orchestration (two judge gates flanking a parallel mechanical phase):

```
Step 0    Rebase + refresh triage
Step 1    Pick N non-overlapping candidates from topGaps
Step 1.5  Enrich candidates inline (judge-pass-1) → docs/judgments/briefing-{date}.md
Step 2    Dispatch [sub:helper] — writes N fixtures + any new AST types
Step 3    Merge helper's branch → confirm all N RoundTrip tests pass (Red #1 closed)
Step 4    Dispatch N [sub:mech] in parallel — each closes Red #2 for its fixture
Step 5-6  [sub:mech] Close Red #2 → confirm Parser_Produces green → manifest
Step 9    Dispatch [sub:judge] — judge-pass-2 → docs/judgments/verdict-{date}.md
          - PROCEED (0 FAIL) → continue to Step 10
          - HALT (any FAIL)  → don't merge mech branches, remediate inline or via follow-up sub-agent
Step 10   Merge mechanical branches → NUnit (100% green required) → glossary → re-run triage → loop
```

`[sub:helper]` per-session (handles all N candidates sequentially):

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

`[sub:mech]` per-session (handles exactly ONE assigned fixture):

```bash
# 1) Read conventions
cat libs/magic-ast/CONTRIBUTING.md
cat libs/magic-ast/GLOSSARY.md
cat {your assigned fixture path}        # the gold AST you must teach the parser to produce
# 2) Run your card's test to see the current diff:
dotnet test --filter "FullyQualifiedName~{ShortCardName}"
cat /tmp/mast-diffs/{Set}_{Card}.actual.json   # diff diff diff
# 3) Extend the parser until your test passes. NO fixture edits, NO AST changes.
# 4) When green, commit and report.
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

### Step 0 — `[sub]` Rebase your worktree onto main

Before doing anything else, sanity-check that your worktree is current:

```bash
git log --oneline main..HEAD            # what's only on my branch
git log --oneline HEAD..main            # what's on main but not in my worktree
```

If `main` is ahead of your worktree (commits exist on `main` that you don't have), **rebase before working**:

```bash
git fetch origin    # in case main has remote-only commits
git rebase main
```

The dispatch mechanism may branch your worktree from a stale ref. If you start writing code against pre-consolidation file paths (`tools/test/magic-ast/...`), or you can't find files the skill or your assignment references, that's the smell — stop and rebase.

### Step 1 — `[main]` Pick gaps

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. Walk `topGaps[]` in rank order. For each gap you want to address this batch:

- Select the cleanest exemplar from `candidateLines[]`: lowest `cleanlinessScore` (P-purity ratio — lower means the assigned pattern dominates this line's failures), shortest `lineLength` as tiebreaker. Skip lines with `alreadyHandParsed: true`.

Choose batch size based on the number of **non-overlapping** patterns. Two gaps that touch the same `relatedPatterns[]` should be serialized across batches, not paralleled, to avoid merge conflicts on the same parser file or trait interface.

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
## Candidate {n}: {Card Name} ({pattern})

**Oracle:** "{oracle text of the candidate line}"

### Relevant rules
- **{Rule number} {Rule title}** — {subrule text or glossary def, ~1-2 sentences quoted from the rules data}
- {additional rules as needed}

### Anti-patterns
- {1-3 bullets of specific things the sub-agent should NOT do, grounded in the rules}

### Glossary gaps (if any)
- {term} — referenced in oracle but missing from glossary.json. {what the sub-agent should do — usually surface in their manifest}
```

If a candidate's mechanic isn't in `glossary.json` at all (and the term is genuinely MTG-domain, not vernacular), do not dispatch the sub-agent — swap the candidate for a different one or escalate to the human.

The briefing is **informative, not prescriptive**. The judge reports rule facts; the sub-agent owns the AST shape.

### Step 2 — `[main]` Dispatch the hand-parsing helper

Spawn ONE sub-agent via the `Agent` tool with `isolation: "worktree"`. Its prompt **must** include:

- The judge briefing path (`docs/judgments/briefing-{date}.md`).
- All N candidate picks (oracle text, source card metadata, `Input` DTO).
- A pointer to this skill (the `[sub:helper]` sections specifically — Steps 3 and 4).
- The memory items listed in "Before you touch anything."
- The branch name to commit on (e.g., `mast-tdd/helper-{date}`).
- Explicit instructions: "Write all N gold-AST fixtures at the canonical paths. Create any new AST types needed to make RoundTrip green. Don't touch any parser code. When all N RoundTrip tests pass, commit and report."

Wait for the helper to finish before continuing.

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

### Step 6 — `[main]` Dispatch mechanical sub-agents in parallel

Spawn N sub-agents, ONE per fixture, in parallel via the `Agent` tool with `isolation: "worktree"`. Each prompt **must** include:

- The exact fixture path the sub-agent is responsible for.
- A pointer to this skill (the `[sub:mech]` sections — Steps 7 and 8).
- The branch name (e.g., `mast-tdd/{pattern-slug}-mech`).
- Explicit narrow scope: "Teach the parser to make `Parser_ProducesExpectedOutput` for `{ShortCardName}` pass. Do NOT modify the fixture. Do NOT add or modify AST types. The helper did that work; if you find yourself wanting to, that's a signal to stop and report — the helper's gold may be wrong."

After dispatching, wait for all N to report back before continuing to Step 9.

### Step 7 — `[sub:mech]` Close Red #2 (parser gap)

Red #2: `Parser_ProducesExpectedOutput` failing with a JSON diff. The parser is producing the wrong thing — typically an `UnparsedAbility` (with a diagnostic) instead of the structured node the gold contains.

Use `/tmp/mast-diffs/{Set}_{Card}.expected.json` + `.actual.json` to see the precise diff (auto-dumped on test failure). Read both, find the field-level differences, work backward to which parser rule emits or misses the gold shape.

Action: extend the appropriate `IAbilityParser` implementation in `libs/magic-ast/Parsing/Parsers/`. The dispatch table is reflection-discovered via `[OracleAbilityParser(AbilityKind.X)]` — **do not edit `OracleParser.cs` or `AbilityParserRegistry.cs`**.

- New ability-kind parser (e.g., `ModalAbilityParser`, `SpellAbilityParser`): create a new file with the attribute. The registry picks it up automatically.
- Existing parser missing a case (e.g., `TriggeredAbilityParser` for a new trigger event): extend the relevant private method.

Iterate until `Parser_ProducesExpectedOutput` for your card passes.

**Mechanical scope. Don't:**
- Modify the fixture. If you think the gold is wrong, STOP and report — the helper's gold-writing is upstream; if it's wrong, judge-pass-2 should catch it.
- Add or modify AST types. Same — that's the helper's territory.
- Modify other fixtures, even to "improve consistency." Stay in your card's lane.

### Step 8 — `[sub:mech]` Report back

Confirm the test passes:

```bash
dotnet test --filter "Parser_ProducesExpectedOutput&FullyQualifiedName~{ShortCardName}"
```

Commit on the assigned branch. Then emit this manifest as your closing message and stop:

```markdown
## MAST mechanical sub-agent — manifest

**Branch:** {branch-name}
**Card:** {set}/{card-name}

### Parser-files touched
- {file:lines}, ...

### New parser surfaces
- {one-line description per added regex / dispatch case}

### Stop / handoff
- {anything I bailed on, per Stop conditions; otherwise "none"}
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
