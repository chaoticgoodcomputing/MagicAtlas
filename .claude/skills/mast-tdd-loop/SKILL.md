---
name: mast-tdd-loop
description: Drives a TDD cycle for extending MagicAST (the Magic-the-Gathering oracle-text parser at libs/magic-ast/). Pick an unparseable card from triage, hand-parse the gold AST, run the ratchet to surface schema and parser gaps, then close each gap with a new AST node or parser rule. Use when extending MagicAST coverage, working on the MAST TDD loop, hand-parsing a card, adding a new AST node or ability/effect/cost type, adding an ability-kind parser, or when the user references issue #7, "mast-tdd-loop", "MAST round-trip", or "the MAST cycle".
---

# MAST TDD loop

This skill drives one round of extending MagicAST. Each round:
- starts at parser gaps surfaced by the triage report,
- ends with new AST nodes and/or parser rules that close them,
- preserves the ratchet (no regressions) and rolls the corpus-wide triage forward.

## Roles

The skill is read from two perspectives. Annotations on each step say which agent owns it.

- **`[main]` — main agent (orchestrator).** Reads the triage report, batch-picks gaps, dispatches one sub-agent per gap via the `Agent` tool (each in its own worktree), then merges, validates, regenerates the glossary, and re-runs the triage flow after sub-agents complete. The main agent owns every cross-cutting artifact: the merged tree, the glossary, the baseline, the triage report.
- **`[sub]` — sub-agent (worker).** Receives one assigned `(pattern, candidate-line)` as input. Hand-parses, drives the red→red→green cycle on its own worktree, reports a minimal manifest back. Touches only the card's fixture file, the AST nodes it added, and the parser code it changed. Never regenerates the glossary, never re-runs triage, never sees other sub-agents' work.

If you are invoked directly by the user (no orchestrator above you), wear both hats: do every step yourself in sequence, single-threaded.

## Quick start

`[main]` orchestration:

```bash
# Read the situation
cat tests/magic-ast-tests/Data/_08_Reporting/triage-report.json | jq '.topGaps[0:5]'
cat libs/magic-ast/GLOSSARY.md | less
# Then spawn N sub-agents via the Agent tool, one per gap.
```

`[sub]` per-session:

```bash
# 1) Read conventions
cat libs/magic-ast/CONTRIBUTING.md
cat libs/magic-ast/GLOSSARY.md
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

4. **`tests/atlas-flow-test/Data/_03_Primary/Datasets/glossary.json`** — parsed MTG Comprehensive Rules glossary. **Consult this whenever oracle text uses an MTG-domain term** (e.g., "ward", "embalm", "scry", "step", "ability"). Use the rules-accurate definition, not vernacular.

## The cycle

### Step 1 — `[main]` Pick gaps and dispatch

Read `tests/magic-ast-tests/Data/_08_Reporting/triage-report.json`. Walk `topGaps[]` in rank order. For each gap you want to address this batch:

- Select the cleanest exemplar from `candidateLines[]`: lowest `cleanlinessScore` (P-purity ratio — lower means the assigned pattern dominates this line's failures), shortest `lineLength` as tiebreaker. Skip lines with `alreadyHandParsed: true`.
- Spawn a sub-agent via the `Agent` tool with `isolation: "worktree"`. The sub-agent prompt **must** include:
  - The assigned pattern name and `projectedCoverageGain`.
  - The full chosen candidate-line record (oracle text, source card metadata, cleanliness score, `input` DTO).
  - A pointer to this skill (`use the mast-tdd-loop skill`) and the memory item names listed above.
  - The branch name to commit on (e.g., `mast-tdd/{pattern-slug}`).

Choose batch size based on the number of **non-overlapping** patterns. Two gaps that touch the same `relatedPatterns[]` should be serialized across batches, not paralleled, to avoid merge conflicts on the same parser file or trait interface.

After dispatching, wait for every sub-agent to report back before continuing to Step 6.

### Step 2 — `[sub]` Hand-parse the candidate

Create `tests/magic-ast-tests/Data/HandParsedCards/{set}/{card-name}.json` containing:

```json
{
  "input": {
    "name": "...",
    "manaCost": "{...}",
    "typeLine": "...",
    "oracleText": "...",
    "power": "...",
    "toughness": "...",
    "colors": ["..."],
    "colorIdentity": ["..."]
  },
  "output": {
    "name": "...",
    "typeLine": { "raw": "...", "types": [...], "subtypes": [...] },
    "oracle": { "rawText": "...", "abilities": [ /* AST */ ] },
    "attributes": [ /* CardAttribute polymorphic list */ ]
  }
}
```

**Schema discipline:**
- Reuse existing discriminator strings. Consult `GLOSSARY.md` first. Don't invent `"dealDamage"` if it's already there.
- Convention for new discriminators is **camelCase** (matches every existing discriminator: `dealDamage`, `addMana`, `untilEndOfTurn`).
- Model what the oracle text **says**, not what the rules **do**. No turn-state, priority, stack ordering, or layering fields.
- For optional effect dimensions (duration, "you may" / "if you do", "unless [player] pays [cost]"): use the existing trait interfaces (`IDurativeEffect`, `IOptionalEffect`, `IPreventableEffect`). JSON property names: `duration`, `isOptional` / `ifYouDo`, `unlessClause`.

### Step 3 — `[sub]` Surface Red #1 (schema gap)

```bash
nx run mast:test
```

Red #1 appears as a `JsonException` thrown during `CardTestCase.GetOutput()` — i.e., **deserialization** of the hand-parsed JSON to `CardOutputAST`. Two flavors:

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

### Step 4 — `[sub]` Surface Red #2 (parser gap)

Red #2 appears as `Parser_ProducesExpectedOutput` failing with a JSON diff between expected and actual ASTs. The parser is producing the wrong thing — typically an `UnparsedAbility` (with a diagnostic) instead of the structured node you authored.

Action: extend the appropriate `IAbilityParser` implementation in `libs/magic-ast/Parsing/Parsers/`. The dispatch table is reflection-discovered via `[OracleAbilityParser(AbilityKind.X)]` — **do not edit `OracleParser.cs` or `AbilityParserRegistry.cs`**.

- New ability-kind parser (e.g., `ModalAbilityParser`, `SpellAbilityParser`): create a new file with the attribute. The registry picks it up automatically.
- Existing parser missing a case (e.g., `TriggeredAbilityParser` for a new trigger event): extend the relevant private method.

Iterate until both `Output_RoundTrip_ProducesIdenticalJson` and `Parser_ProducesExpectedOutput` pass for your fixture.

### Step 5 — `[sub]` Report back

Confirm the local ratchet is green:

```bash
nx run mast:test
```

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

### Step 6 — `[main]` Merge, validate, regenerate, re-triage

After every sub-agent in the batch has reported (or bailed):

```bash
# 1) Merge all reporting sub-agent branches in order.
for branch in {sub-agent branches}; do
  git merge --no-ff "$branch"
done

# 2) Confirm post-merge ratchet is green. Two sub-agents' edits can be
# individually-green but jointly-red — this catches that.
nx run mast:test

# 3) Regenerate the glossary once for the merged tree.
nx run magic-ast:glossary

# 4) Refresh the corpus-wide triage report.
nx run mast:run
```

If step 2 fails, do not continue. Roll the merges back and investigate per the `[main]`-only stop conditions below.

Produce a batch-level report and decide whether to loop:

```markdown
## MAST TDD batch — aggregate

**Sub-agents dispatched:** {n}     **Bailed:** {n}     **Landed:** {n}
**Branches merged:** {list}

### Cumulative landed
- AST types: {flat union}
- Parsers: {flat union}

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
- The ratchet baseline shows a **regression** (a previously-passing test now fails). Fix it before continuing the session.

`[main]`-only conditions:

- Post-merge ratchet (Step 6 substep 2) fails. Two sub-agents' edits conflict semantically even though each was green in isolation. Roll back the merges and either re-dispatch them serially or route to human.
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
| Ratchet baseline | `tests/magic-ast-tests/test-baseline.json` |
| Test runner | `nx run mast:test` |
| Triage runner | `nx run mast:run` |
| Glossary regenerator | `nx run magic-ast:glossary` |
