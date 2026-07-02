# MAST TDD briefing — batch 6 (autonomous run 2/10)

**Entering coverage:** 7,235 / 29,614 (24.43%). NUnit 316/0/316.

**Run shape:** 2 families across 2 non-overlapping parser files.

## Family A — `This creature can't block.` (cluster 1, +34 marginal)

**Parser file:** `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (monolithic; only Static-touching family this batch).

### Cards (3 mechanical fixtures, helper-novel writes them alongside the new AST type)
1. **Hulking Cyclops** — single-line creature, oracle = `This creature can't block.`
2. **Craven Knight** — same
3. **Scavenging Scarab** — same

### Relevant rules
- **Rule 509.1c** — declares blockers; restrictions like "can't block" are blocking restrictions.
- **Glossary "can't block"** — no canonical entry; descriptive predicate.

### AST type to add
- **`CantBlockEffect`** — discriminator `cantBlock`. No parameters (descriptive marker). Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Source: `libs/magic-ast/AST/Effects/Combat/CantBlockEffect.cs`. Place under `Combat/` alongside existing `MustBlockEffect`, `MustAttackEffect`, `MustBeBlockedEffect`.

### Expected generalization
ONE small dispatch in `StaticAbilityParser.TryParse` — sentence match `^\s*This (creature|land|permanent) can't block\.\s*$` (mirror the existing `TryParseEntersTapped` pattern). Emit `StaticAbility { Effect: CantBlockEffect{} }` (no `KeywordSource` — full sentence, not a keyword word).

### Anti-patterns
- Don't add the rules-engine semantics (`can't be declared as a blocker`). Descriptive only per `feedback_mast_describes_not_executes`.
- Don't conflate with `defender` (which is the keyword; here it's the sentence form on cards that aren't formally Defenders).

---

## Family B — Spell bare PT mod with sign + duration (clusters 2+3, +62 marginal combined)

**Parser file:** `libs/magic-ast/Parsing/Parsers/Spell/Rules/` — new rule file `ModifyPTSpellRule.cs`. Parallel-safe (rule-per-file Spell).

### Cards (6 mechanical, helper-mech writes them)

Positive sign (+/+):
1. **Giant Growth** — `{G}` Instant — `Target creature gets +3/+3 until end of turn.`
2. **Mighty Leap** — `{1}{W}` Instant — `Target creature gets +2/+2 until end of turn.` (verify in corpus)
3. **Titanic Growth** — `{1}{G}` Instant — `Target creature gets +4/+4 until end of turn.`

Negative sign (−/−):
4. **Disfigure** — `{B}` Instant — `Target creature gets -2/-2 until end of turn.`
5. **Bone Splinters** — `{B}` Sorcery — `Target creature gets -1/-1 until end of turn.` (verify — Bone Splinters may have a sacrifice cost rider)
6. **Wither Away** or similar — search corpus for a bare `-N/-N` spell

(Helper-mech: pre-curate from corpus with `jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Target creature gets [+\\-]\\d+/[+\\-]\\d+ until end of turn\\.$"))'`.)

### Relevant rules
- **Rule 614 (Continuous effects)** — until-end-of-turn duration on PT modifications applies in layer 7C.
- **Rule 113.3a** — single-effect spell.

### AST types in scope
- `ModifyPTEffect` (existing). `PowerModifier`/`ToughnessModifier` accept signed `LiteralQuantity { Value: ±N }`. `Duration: UntilEndOfTurn`.
- Negative modifier convention: `Value: -2` etc. (same as existing NightOfSoulsBetrayal fixture from batch 1).

### Expected generalization
ONE new `[SpellRule]` file. Regex: `^Target creature gets ([+\-]\d+)/([+\-]\d+) until end of turn\.$`. Emit `SpellAbility { Effects: [ModifyPTEffect { Target: Target(creature), PowerModifier, ToughnessModifier, Duration: UntilEndOfTurn }] }`.

The existing `ModifyPTAndGainKeywordSpellRule` (from batch 5) handles the version with "and gains KW"; this is the bare version without the keyword conjunction. Two distinct rules.

### Anti-patterns
- Don't merge into the batch-5 rule — that one requires `and gains <keyword>`. Bare is its own rule file.
- Don't model the duration as a separate effect; it's an attribute on the ModifyPTEffect.

---

## Dispatch plan

**Wave 1 (2 parallel sub-agents):**
- `[sub:helper-novel]` (Opus) — adds `CantBlockEffect` + writes 3 Family A fixtures.
- `[sub:helper-mech]` (Sonnet) — writes 6 Family B fixtures (mix of +/+ and −/− signs).

**Wave 2 (2 parallel sub-agents, after wave 1 merges + GLOSSARY regen):**
- `[sub:mech]` (Sonnet) — Family A: small sentence dispatch in `StaticAbilityParser`.
- `[sub:mech]` (Sonnet) — Family B: new `ModifyPTSpellRule.cs` file.

**Yield ceiling:** ~96 cards combined.
