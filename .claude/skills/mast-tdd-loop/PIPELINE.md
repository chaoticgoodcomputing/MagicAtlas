# MAST TDD loop — pipeline reference

Detailed reference for [SKILL.md](SKILL.md). Two parts:

1. **Authoring reference** — the gold-AST and schema-gap rules every agent needs when writing fixtures (combined or split model alike).
2. **Two-phase helper/mech fallback** — the older split dispatch model, for the rare batch dominated by genuinely novel doctrinal shapes.

The Invariants in SKILL.md ("Gold AST = eventual truth", fixtures immutable, GLOSSARY orchestrator-only, `git -C "$WORKTREE_ROOT"`, MAST describes-not-executes, no ratchet tolerance) apply throughout. They are not repeated here.

---

## Authoring reference

### Fixture file shape

Create `tests/magic-ast-tests/Fixtures/HandParsedCards/{Set}/{CardName}.json`:

```json
{
  "Input": {
    "Name": "...", "ManaCost": "{...}", "TypeLine": "...", "OracleText": "...",
    "Power": "...", "Toughness": "...", "Colors": ["..."], "ColorIdentity": ["..."]
  },
  "Output": {
    "Name": "...",
    "TypeLine": { "Raw": "...", "Types": [], "Subtypes": [] },
    "Oracle": { "RawText": "...", "Abilities": [ /* AST */ ] },
    "Attributes": [ /* CardAttribute polymorphic list */ ]
  }
}
```

**The `Input` block is handed to you in the dispatch prompt** — the orchestrator copies the exemplar's `Input` DTO verbatim from `triage-report.json` (`Name`, `ManaCost`, `TypeLine`, `OracleText`, `Power`, `Toughness`, `Colors`, `ColorIdentity`; DFCs carry a `CardFaces` block). Copy it straight into the fixture. **Do not fetch card data from the network / Scryfall** — the handed DTO and the local `oracle-cards.json` are the only sources. (If you must find a cleaner alternate exemplar, `jq` `oracle-cards.json` locally.)

**Card-scope rule:** every ability on the card must be gold-modeled, even abilities no current parser produces — otherwise the per-card `Parser_ProducesExpectedOutput` test can't go green. If a card is too complex to fully gold-model this batch, swap it for a cleaner exemplar (scan `candidateLines[]` deeper by `cleanlinessScore`).

### Rule citations in doc-comments — cite from the briefing, never from memory

The briefing's "Relevant rules" section gives you the **exact CR rule number(s) + quoted text** for this family — the orchestrator pulled them from `rules-structure.json` so you don't have to, and so you can't hallucinate one. When you add a new AST node, cite **that** number/text in its XML doc-comment. Do NOT write a rule number from memory or beyond what the briefing provided; guessed numbers were a recurring defect (e.g. Multikicker cited 702.32 vs the real 702.33; a node citing a nonexistent "701.12"). If no number was provided and you think one applies, note it in your report rather than guessing — the judge cross-references every citation against `rules-structure.json`.

### JSON casing

- Property names: **PascalCase** (`Effects`, `Trigger`, `EffectType`, `Target`).
- Discriminator string values: **camelCase** (`"EffectType": "dealDamage"`, `"Kind": "triggered"`, `"DurationType": "untilEndOfTurn"`).
- Reuse existing discriminator strings — consult `GLOSSARY.md` first; don't invent `"dealDamage"` if it exists. New discriminators are camelCase to match every existing one.
- `ColorIdentity` arrays are **set-semantic** — order is not enforced by the comparator, but emit WUBRG for readability.
- For optional effect dimensions use the existing trait interfaces: `IDurativeEffect` (`Duration`), `IOptionalEffect` (`IsOptional` / `IfYouDo`), `IPreventableEffect` (`UnlessClause`). Don't invent parallel fields.

### Red #1 — schema gap (RoundTrip fails)

`Output_RoundTrip_ProducesIdenticalJson` failing is a `JsonException` during deserialization of the hand-parsed JSON to `CardOutputAST`. Two flavors:

**(a) Unknown discriminator value** — `Unknown {Base} discriminator '{value}'. Known: {list}.` You wrote a `kind`/`effectType`/etc. value no AST type registers. Create a new sealed record under the right `AST/` subdirectory with the matching attribute:

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

The record must `: <Base>` and (for Effect subtypes) typically `, IOptionalEffect, IDurativeEffect, IPreventableEffect` — copy from an existing sibling under `AST/Effects/`.

**(b) Unmapped JSON property** — `The JSON property '{name}' could not be mapped to any .NET member contained in type '{FullType}'.` You added a field the target record doesn't declare. Either add the field (with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` for optional fields) or remove it from the JSON (and reconsider whether it belonged).

Red #1 doesn't always fire — if your gold uses only existing AST primitives, RoundTrip passes first try. That's a sign the schema is sufficient. Iterate until every fixture's RoundTrip is green.

### Red #2 — parser gap (`Parser_ProducesExpectedOutput` fails)

The parser produces an `UnparsedAbility` or wrong node. Read `lastAttemptedRule` — that's where the parser gave up, and usually where the gap lives (extend the rule, don't reach for a sibling). Use `/tmp/mast-diffs/{Set}_{Card}.expected.json` + `.actual.json` (auto-dumped on failure) for the field-level diff.

Extend the appropriate `IAbilityParser` in `libs/magic-ast/Parsing/Parsers/`. The dispatch table is reflection-discovered via `[OracleAbilityParser(AbilityKind.X)]` — **do not edit `OracleParser.cs` or `AbilityParserRegistry.cs`.** New ability-kind parser → new file with the attribute (registry auto-discovers it).

For the families that are already one-file-per-rule registries — Spell, Static, Triggered (conditions *and* effects), Activated (costs *and* effects), and keywords — a missing case is a **new rule file**, not an edit to the parser. Drop a `[SpellRule]` / `[StaticRule]` / `[TriggerConditionRule]` / `[TriggeredRule]` / `[ActivatedCostRule]` / `[ActivatedEffectRule]` / `[StructuralKeyword]` class in the parser's `Rules/` directory (or `Tokens/Keywords/`); the registry discovers it by reflection and dispatches by descending `Priority`. Migrate the priority order-preserving when extracting (`Priority = 1000 - legacy chain index`) so relative dispatch order is unchanged. Edit a parser's own body only for genuinely cross-cutting orchestration (trigger timing/split, the multi-sentence effect pre-pass); `AbilityClassifier.cs` is the only parser surface that still routinely takes in-place edits.

<a id="sibling-shape-allowance"></a>
### Sibling-shape allowance

A family addresses one ability shape, but the same fixture card may carry a sibling ability needing a separate parser surface for that card's test to pass. You MAY add a tight sibling surface only when ALL of these hold:

1. Single-shape — one new rule file (`[SpellRule]` / `[StaticRule]` / `[TriggerConditionRule]` / `[TriggeredRule]` / `[ActivatedCostRule]` / `[ActivatedEffectRule]`) or, for the unconverted classifier, one new method — not a family's worth of work.
2. The sibling does NOT belong to another family in-flight this batch (check the briefing). Conflict → BAIL on the multi-ability card.
3. Fully covered by **existing** AST types (consult `GLOSSARY.md`). Needs a new type → BAIL.
4. Genuinely smaller than the family work — a paragraph, not a section. A new ability-kind parser is not acceptable.
5. Recorded explicitly in the manifest under `### Sibling additions`.

If any criterion fails, BAIL on the multi-ability card; the orchestrator swaps it or schedules the sibling as a follow-up family.

**Acceptable** (prior batches): AsLongAs mech adding an "attack with X and another" trigger for Merry; CreateToken mech adding `InvestigateSpellRule` for HardEvidence's `Investigate.` line; BeginningOfTurn mech adding an activated-parser `Activate only as a sorcery` restriction for Broodheart.

**Would NOT qualify** (BAIL): adding a Modal ability parser because one fixture happens to be modal; implementing a new `TriggerEvent` enum value plus its parser surface; solving a Threshold ability-word + composite-AsLongAs shape that is a separate family's work.

<a id="briefing-template"></a>
### Briefing template

```markdown
## Family {n}: ({pattern}, {lastAttemptedRule})

**Failure signal:** parser bails at `{lastAttemptedRule}` for {N} cards in this cluster.

### Cards in this family
1. **{Card Name}** — `{oracle line}` (cleanliness={score})
... (1-3 cards)

### Relevant rules  (REQUIRED — pulled verbatim from rules-structure.json; the agent cites these, the judge cross-references them)
- **{exact CR rule number, e.g. 702.33c} {Rule title}** — "{verbatim text quoted from rules-structure.json}"

### AST types in scope (convenience pointer — sub-agents can read GLOSSARY.md directly)
- **`{TypeName}`** — `[OracleEffect("{discriminator}")]`. Inherits `Effect, IOptionalEffect, ...`.
  Key fields: `{Field: Type, ...}`. Source: `libs/magic-ast/AST/.../{TypeName}.cs`.

### Expected generalization
- {one or two sentences on what ONE parser surface should cover — informative, not prescriptive}

### Anti-patterns
- {1-3 specific things NOT to do, grounded in the rules}

### Glossary gaps (if any)
- {term} — referenced in oracle but missing from glossary.json
```

Keep it ~200 words per family. Informative, not prescriptive about parser shape — agents own the AST shape and parser design. The **"Relevant rules" block is mandatory and must carry the exact rule number + verbatim text pulled from `rules-structure.json`** — it is the canonical reference the agent cites in doc-comments and the judge cross-references, so pull it accurately (don't paraphrase a number from memory). **The exemplar `Input` DTO is not part of the briefing — it goes into the dispatch prompt (Step 3) verbatim**, so the agent writes the fixture without re-fetching card data.

<a id="batch-report"></a>
### Batch report template

```markdown
## MAST TDD batch — aggregate

**Sub-agents dispatched:** {n}  **Bailed:** {n}  **Landed:** {n}
**Branches merged:** {list}
**Briefing:** `docs/judgments/briefing-{date}.md`  **Verdict:** `docs/judgments/verdict-{date}.md`

### Cumulative landed
- AST types: {flat union}
- Parsers: {flat union}

### Corpus-wide delta (post-triage rerun)
- Cards flipped green: {count}   Lines: {count}   Abilities: {count}
- Pattern frequencies (top 5 changes): {pattern: before → after}
- New patterns surfaced: {list}

### Product delta (green recall — the objective, not the L2 proxy)
<!-- from `nx run mast:recall-report` then `bash tools/recall-log.sh {batch}`; deltas vs the prior recall-log.jsonl line -->
- GREEN: {n} ({+Δ})   AMBER: {n} ({±Δ})   missed: {n} ({±Δ})
- recallAtGreen: {pct} ({+Δpp})   popularity-weighted recall: {pct} ({+Δpp})
- One sentence: did this batch's parse work convert to trustworthy edges, or land on dark labels? {note}

### Projected vs observed yield
| Agent | Cluster/Scope | Projected MarginalYield | Observed Card Δ | Observed Line Δ | Yield Ratio |
|---|---|---|---|---|---|
| ... | ... | ... | ... | ... | ... |
| **Total** | | **Σ** | **Σ** | **Σ** | **Σ obs / Σ proj** |

**Yield ratio analysis:** {one sentence — why observed differed from projected}
**Coverage-per-fixture:** {total card delta / total fixtures written — the optimization target}

### Next batch
- Suggested gaps: {next topGaps[0..N], or "stop — diminishing returns"}
- Patterns to watch (didn't shrink as expected): {list, route to human if persistent}
```

For combo-depth/investigation agents not driven by a yield cluster, use "n/a (depth)" for Projected MarginalYield. The yield-ratio column shows which triage surfaces best predict real coverage — iterate the triage process to maximize coverage-per-fixture over successive batches.

---

## Two-phase helper/mech fallback

Use this only when a batch is dominated by genuinely novel doctrinal shapes, where isolating AST-authoring risk from parser work is worth the two-phase barrier. For everything else, use combined agents (SKILL.md). The split adds a hard serialization point — ALL AST authors must finish before ANY parser agent dispatches — so it costs wall-clock time; spend it only when the novel-AST risk justifies the isolation.

### Roles

- **`[sub:helper-novel]` (Opus, one per batch)** — receives only candidates needing new AST types, new discriminators, or a doctrinal edge (multi-effect-per-clause, colorless, color-ordering, trait-boundary). Creates the new AST types (Red #1) and writes their gold fixtures. RoundTrip green for all its fixtures before finishing. No parser code.
- **`[sub:helper-mech]` (Sonnet, M per batch, parallel)** — receives fixtures whose AST shapes already exist in `GLOSSARY.md`. Strictly mechanical: look up existing types, write the gold, RoundTrip green. Bails if it would need a new AST type, or if a sibling ability needs parser work beyond any in-flight family's scope.
- **`[sub:mech]` (N per batch, parallel — FAMILY CONTRACT)** — receives a `(pattern, lastAttemptedRule)` family + its 1-3 fixtures. Makes ALL fixtures' `Parser_ProducesExpectedOutput` pass via ONE consolidated parser surface. N separate `TryParseX` methods means it misread the family → bail with sub-patterns.
- **`[sub:judge]` (one per batch)** — reads changed files post-merge, renders strict PASS/FAIL per the `mast-judge` skill. Any FAIL halts the merge.

### Phase flow

```
Step 0    Worktree pre-flight (SKILL.md Step 0).
Step 1    Pick families (SKILL.md Step 1).
Step 1.5  Brief inline → briefing-{date}.md (judge-pass-1, enrichment).
Step 2a   Triage each candidate: novel-shape (→ helper-novel) vs mechanical (→ helper-mech).
Step 2b   Dispatch 1 helper-novel (Opus) + M helper-mech (Sonnet, parallel).
Step 3-4  Helpers hand-parse → RoundTrip green. Commit. Report manifests.
Step 5    Merge helper branches → confirm Red #1 closed (RoundTrip green, Parser tests red).
          Regenerate + commit GLOSSARY.md NOW (before mech wave) so mech briefings cite
          accurate new-type signatures.
Step 6    Dispatch N mech agents in parallel — ONE PER FAMILY.
Step 7-8  Mechs close Red #2 for ALL family fixtures via one parser surface. Commit. Report.
Step 9    Judge-pass-2 (mast-judge sub-agent) → verdict-{date}.md. PROCEED / HALT.
Step 10   Merge mech branches → NUnit 100% → regen GLOSSARY → re-triage → loop.
```

**Step 2a triage heuristic:** if you can name the discriminator strings the gold will use by reading `GLOSSARY.md` without doubt, it's mechanical. Any "I think we'd need a new X" instinct → novel.

**Why family contracts:** per-fixture mechs accumulate adjacent `TryParseX` methods in the same file — the merge-conflict hotspot. Family contracts force generalization at write time. When a family is genuinely too coarse, the mech's bail refines the triage taxonomy.

**Two judge passes:** Step 1.5 (enrichment) is orchestrator-internal — the orchestrator already has the picks and direct rules-file access. Step 9 (verification) IS a separate sub-agent — independent verification justifies the dispatch. Judge policy and the HALT-on-FAIL gate are the same as SKILL.md Step 4.

### Per-session recipes

**`[sub:helper-novel]` / `[sub:helper-mech]`:**
```bash
cat libs/magic-ast/CONTRIBUTING.md libs/magic-ast/GLOSSARY.md
cat docs/judgments/briefing-{date}.md
# For each candidate: hand-parse, write fixture, run:
nx run mast:test           # all Output_RoundTrip tests green for your fixtures
# RoundTrip fails → Red #1: create the missing AST type (see Authoring reference). Re-run.
# All green → commit on your branch and report the manifest.
```

**`[sub:mech]` (a family):**
```bash
cat libs/magic-ast/CONTRIBUTING.md libs/magic-ast/GLOSSARY.md {your family briefing}
for card in {Card1} {Card2} ...; do
  dotnet test --filter "FullyQualifiedName~${card}"
  cat /tmp/mast-diffs/{Set}_${card}.actual.json
done
# Find the GENERALIZATION: one parser surface covering all N. Start at lastAttemptedRule.
# Extend the parser until ALL N pass. NO fixture edits, NO AST changes.
# N separate methods → STOP, bail with sub-pattern report. Commit + report.
```

### Manifests

```markdown
## MAST helper — manifest
**Branch:** {name}
**Fixtures written:** {paths}
**New AST types:** {list with discriminators}, or "none"
**RoundTrip status:** all N green / X red (reasons)
```

```markdown
## MAST mechanical sub-agent — manifest
**Branch:** {name}   **Family:** ({pattern}, {lastAttemptedRule})
**Cards in scope:** {N}   **Green:** {n}   **Bailed:** {n}

### Parser surface
- {one paragraph: the SINGLE parser change that closed the family}  File: {file:lines}
### Generalization notes
- {what variation across the N cards the surface handles}
### Sibling additions (if any)
- {per sibling-shape allowance criteria}
### Stop / handoff
- {sub-pattern bail block, or "none"}
```

### Sub-pattern bail block

```markdown
## Family bail — sub-patterns discovered
**Family:** ({pattern}, {lastAttemptedRule})   **Cards processed before bail:** {n}
**Sub-pattern A:** {description} — {cards}
**Sub-pattern B:** {description} — {cards}
```

The orchestrator routes the bail to the human; the post-batch triage refresh decides whether the sub-patterns warrant a `FallbackParser.InferFailurePattern` refinement. Bailing is not failure — it's the explicit value of the family contract.
