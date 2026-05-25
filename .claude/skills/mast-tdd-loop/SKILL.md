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

The skill is read from two perspectives. Annotations on each step say which agent owns it.

- **`[main]` — main agent (orchestrator).** Reads the triage report, batch-picks gaps, dispatches one sub-agent per gap via the `Agent` tool (each in its own worktree), then merges, validates, regenerates the glossary, and re-runs the triage flow after sub-agents complete. The main agent owns every cross-cutting artifact: the merged tree, the glossary, the judge briefings + verdicts, the triage report.
- **`[sub]` — sub-agent (worker).** Receives one assigned `(pattern, candidate-line)` as input. Hand-parses, drives the red→red→green cycle on its own worktree, reports a minimal manifest back. Touches only the card's fixture file, the AST nodes it added, and the parser code it changed. Never regenerates the glossary, never re-runs triage, never sees other sub-agents' work.

If you are invoked directly by the user (no orchestrator above you), wear both hats: do every step yourself in sequence, single-threaded.

## Quick start

`[main]` orchestration (two judge gates, one batch):

```
Step 0  Rebase + refresh triage
Step 1  Pick N non-overlapping candidates from topGaps
Step 1.5  Enrich candidates inline (judge-pass-1) → docs/judgments/briefing-{date}.md
Step 2  Dispatch N sub-agents (worktrees), each pointed at its briefing section
Step 3-6 [sub] Hand-parse → Red #1 → Red #2 → green → manifest
Step 7  Judge-pass-2 (mast-judge sub-agent) → docs/judgments/verdict-{date}.md
        - PROCEED → continue to Step 8
        - HALT (BLOCKING verdict) → do not merge, surface to human
Step 8  Merge → NUnit (100% green required) → glossary → re-run triage → loop
```

`[sub]` per-session:

```bash
# 1) Read conventions + your judge briefing
cat libs/magic-ast/CONTRIBUTING.md
cat libs/magic-ast/GLOSSARY.md
cat docs/judgments/briefing-{date}.md   # your section establishes the rule facts
# 2) Hand-parse the assigned fixture, then iterate:
nx run mast:test
# 3) Close Red #1 (schema), then Red #2 (parser). Re-run between iterations.
# 4) When green, emit the manifest and stop.
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

Before dispatching sub-agents, the orchestrator briefs each candidate with authoritative MTG rules context. This is **judge-pass-1 (enrichment)** — but the orchestrator acts as judge here rather than dispatching a sub-agent, because the local state already has the candidate picks and direct file access to the rules data. (Judge-pass-2 in Step 7 IS a separate sub-agent — different cost-benefit.)

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

### Step 2 — `[main]` Dispatch sub-agents

Now dispatch. Spawn a sub-agent per candidate via the `Agent` tool with `isolation: "worktree"`. The sub-agent prompt **must** include:

- The assigned pattern name and `projectedCoverageGain`.
- The full chosen candidate-line record (oracle text, source card metadata, cleanliness score, `Input` DTO).
- A pointer to this skill (`use the mast-tdd-loop skill`) and the memory item names listed above.
- The branch name to commit on (e.g., `mast-tdd/{pattern-slug}`).
- A **pointer to the briefing**: `read your section in docs/judgments/briefing-{date}.md — section "## Candidate {n}: {card}"`. By reference, not embedded; the briefing is canonical and the sub-agent can re-read mid-session.

After dispatching, wait for every sub-agent to report back before continuing to Step 7.

### Step 3 — `[sub]` Hand-parse the candidate (gold AST, eventual-truth)

**The hand-parsed JSON is the gold AST — what a fully-implemented parser SHOULD eventually emit for this card.** It is not a snapshot of the current parser's output, and it is not partially populated with `UnparsedAbility` nodes where the parser currently falls short.

This is the single most important rule of the loop. Get it wrong and the TDD direction inverts: the test "passes" by matching the parser's current limitations rather than driving the parser forward.

**Forbidden in the gold output:**
- `"Kind": "unparsed"` anywhere in `Output.Oracle.Abilities` (or nested inside Saga chapters, Modal options, Level-up stanzas, etc.).
- Embedded `Diagnostics[]` arrays describing current parser failures.
- `Pattern` strings copied from `FallbackParser.InferFailurePattern`.

**Allowed and expected:**
- AST node shapes that don't yet exist — create them in `libs/magic-ast/AST/` as Red #1 (see Step 4).
- Abilities you can't fully spec — model them as descriptively as you can with existing nodes; use the `OtherHistoryPredicate`-style escape hatches sparingly; surface the design decision in your manifest.

**Card-scope choice.** If the assigned candidate-line lives on a card with multiple complex abilities you can't reasonably gold-model in this session, you have two options:
1. **Pick a simpler card** containing the same pattern in cleaner isolation. The triage report's `candidateLines[]` is sorted by `cleanlinessScore`; scan deeper for a card where the target line dominates.
2. **Gold-AST every ability on the card AND teach every parser surface needed to make all of them green.** The per-card `Parser_ProducesExpectedOutput` test must pass green for the batch to land — no ratchet tolerance. If the card has 5 abilities, the batch teaches the parser to produce all 5 correctly, or the fixture doesn't land.

Prefer (1) when a simpler exemplar exists. Use (2) when the candidate is genuinely the cleanest one — and accept the larger parser scope it implies.

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

### Step 4 — `[sub]` Surface Red #1 (schema gap)

```bash
nx run mast:test
```

**Red #1 doesn't always fire.** If your hand-parsed shape uses only AST primitives that already exist (`UntapEffect`, `ObjectFilter`, common discriminator strings, etc.), the deserialization succeeds on first attempt and you jump straight to Red #2. That's a sign the gap is purely a parser-rule gap, not a schema gap — keep going.

When Red #1 does fire, it's a `JsonException` thrown during `CardTestCase.GetOutput()` — i.e., **deserialization** of the hand-parsed JSON to `CardOutputAST`. Two flavors:

**3a. Unknown discriminator value**
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

**3b. Unmapped JSON property**
```
System.Text.Json.JsonException : The JSON property '{name}' could not be mapped to any
.NET member contained in type '{FullType}'.
```
You added a field that the target concrete record doesn't declare.

Action: either
- add the field to the record (with `[JsonPropertyName("{name}")]` and `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` for optional fields), or
- remove the field from the JSON (and reconsider whether it belonged there at all).

Iterate until Red #1 clears. When the test moves from **errored** to **failed with a diff**, you've reached Red #2.

### Step 5 — `[sub]` Surface Red #2 (parser gap)

Red #2 appears as `Parser_ProducesExpectedOutput` failing with a JSON diff between expected and actual ASTs. The parser is producing the wrong thing — typically an `UnparsedAbility` (with a diagnostic) instead of the structured node you authored.

Action: extend the appropriate `IAbilityParser` implementation in `libs/magic-ast/Parsing/Parsers/`. The dispatch table is reflection-discovered via `[OracleAbilityParser(AbilityKind.X)]` — **do not edit `OracleParser.cs` or `AbilityParserRegistry.cs`**.

- New ability-kind parser (e.g., `ModalAbilityParser`, `SpellAbilityParser`): create a new file with the attribute. The registry picks it up automatically.
- Existing parser missing a case (e.g., `TriggeredAbilityParser` for a new trigger event): extend the relevant private method.

Iterate until both `Output_RoundTrip_ProducesIdenticalJson` and `Parser_ProducesExpectedOutput` pass for your fixture.

### Step 6 — `[sub]` Report back

Confirm the local NUnit suite is 100% green:

```bash
nx run mast:test
```

Vanilla NUnit doctrine: **every test must pass** for the batch to be eligible to merge. If your fixture's `Parser_ProducesExpectedOutput` test isn't green, the parser work isn't done. There is no baseline file, no stable-failure tolerance — see if you missed an ability, a discriminator, or a field shape.

If the test passes, the diff dump at `/tmp/mast-diffs/{set}_{card}.expected.json` + `.actual.json` shouldn't exist (only failed tests dump diffs). If it does exist for your fixture, that's the diff you need to close.

Commit on the assigned branch. Then emit this manifest as your closing message and stop:

```markdown
## MAST sub-agent — manifest

**Branch:** {branch-name}
**Assigned pattern:** {pattern-name}
**Card:** {set}/{card-name}

### Added
- AST types: {list, with discriminators}, or "none"
- Parsers: {list, with AbilityKind}, or "none"
- Fixture: {path-to-HandParsedCards-json}

### Stop / handoff
- {anything I bailed on, per Stop conditions; otherwise "none"}
```

Do not regenerate `GLOSSARY.md`. Do not re-run triage. Do not touch any file outside your fixture + the AST/parser code you added or modified. The orchestrator owns those.

### Step 7 — `[main]` Judge-pass-2 (rules-accuracy verify)

Before merging, dispatch a sub-agent that uses the `mast-judge` skill to verify the rules-accuracy of the batch's output. This is the second judge pass — Step 1.5 established what the rules SAY; this one checks whether the sub-agents' work descriptively MATCHES what they said.

Gather the scope: every fixture file and every AST node file touched across all reporting sub-agent branches:

```bash
# List files touched by any sub-agent branch in this batch
git diff --name-only main..{branch} | grep -E '(tests/magic-ast-tests/Data/HandParsedCards/.*\.json|libs/magic-ast/AST/.*\.cs)$'
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

- **PROCEED**: judge rendered 0 FAILs. Continue to Step 8.
- **HALT**: one or more FAIL verdicts. Do not merge. Address every FAIL — either inline (orchestrator fixes the items) or via a follow-up sub-agent dispatched specifically to remediate. Then re-run Step 7. The loop does not continue until the verdict is PASS.

The judge is strict binary PASS/FAIL; there is no "concern" tier. See `.claude/skills/mast-judge/SKILL.md` for the anti-pattern enumeration (free text, escape hatches, unparsed nodes, imprecise citations all FAIL).

### Step 8 — `[main]` Merge, validate, regenerate, re-triage

After Step 7 returns PROCEED:

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

- Post-merge NUnit (Step 8 substep 2) isn't 100% green. Two sub-agents' edits conflict semantically even though each was green in isolation. Roll back the merges and either re-dispatch them serially or route to human.
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
