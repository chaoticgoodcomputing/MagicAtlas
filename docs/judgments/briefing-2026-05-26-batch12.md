# MAST TDD briefing — batch 12 (autonomous run 8/10)

**Entering coverage:** 7,824 / 29,614 (26.42%). NUnit 412/0/412.

## Family A — ETB surveil (cluster 4, +18 marginal)

**Shape:** `When this land enters, surveil N.` (mirror of ETB lifegain from batch 3).

### Mechanical — no new AST
All types exist: TriggeredAbility + TriggerEvent.Enters + Self filter + SurveilEffect (added in batch 3).

### Cards (3 fixtures, helper-mech)
Pre-curate single-line "When this land enters, surveil N" lands. Combined with mana ability is fine.

### Parser surface
TriggeredAbilityParser — add `SurveilTriggeredRule` follow-on or extend an existing rule. Pattern very similar to `YouGainLifeRule` (batch 3) but emitting `SurveilEffect` instead.

---

## Family B — Additional cost: sacrifice (cluster 5, +17 marginal)

**Shape:** `As an additional cost to cast this spell, sacrifice X.`

### Novel AST (helper-novel)

This is a **spell cost modifier** — not an effect but an alternative-cost addition to the spell's mana cost. Existing AST shapes for costs: `Cost` polymorphic with `mana`/`tap`/`sacrifice` discriminators.

Two design choices:
1. **Add a `AdditionalCosts: IReadOnlyList<Cost>?` field on `SpellAbility`** — describes "this spell has these costs beyond mana". Cleanest if SpellAbility is the right home.
2. **New `AdditionalCostEffect`** wrapping the cost — bad fit; this isn't an effect.

Recommendation: design call 1. Helper-novel decides. If it requires modifying `SpellAbility` (`[PolymorphicBase]`-adjacent), that's a stop-condition; BAIL with the architectural note.

### Cards (3 fixtures, helper-novel)
Pre-curate single-line spells with `As an additional cost to cast this spell, sacrifice X.\n<main effect>`:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^As an additional cost to cast this spell, sacrifice")) | "\(.name) | \(.mana_cost) | \(.oracle_text | gsub("\n"; " | "))"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

---

## Family C — Landfall ability-word (cluster 3, +21 marginal) [ARCHITECTURAL ATTEMPT]

**Shape:** `Landfall — Whenever a land you control enters, this creature gets +2/+2 until end of turn.`

### Architectural problem

Ability-words are oracle-text PREFIXES on triggered/static abilities (`Landfall —`, `Threshold —`, `Delirium —`, etc.). They're flavor markers in MTG but DO matter descriptively — MAST records oracle text.

Two design directions:
1. **Add `AbilityWord: string?` field on `TriggeredAbility` (or all `Ability` subtypes).** Modifies base records — risky per stop conditions.
2. **Wrap the inner ability in an `AbilityWordedAbility` polymorphic kind.** New Ability subtype: `{ Kind: "abilityWorded", Word: "Landfall", Inner: TriggeredAbility }`. Cleaner — doesn't touch base.

Recommendation: option 2. Helper-novel decides; if it requires modifying base classes, BAIL with the architectural note and surface the design question to orchestrator.

### Cards (3 fixtures, helper-novel)
Pre-curate Landfall creatures with simple inner triggers:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Landfall — Whenever a land you control enters")) | "\(.name) | \(.mana_cost) | \(.oracle_text)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

---

## Dispatch plan

**Wave 1:**
- `[sub:helper-novel]` (Opus): Families B + C — both novel/architectural. Bail-friendly per stop conditions.
- `[sub:helper-mech]` (Sonnet): Family A — 3 ETB surveil fixtures.

**Wave 2 (parallel based on what landed):**
- `[sub:mech]` Family A: TriggeredAbilityParser surveil rule.
- `[sub:mech]` Family B: SpellAbilityParser additional-cost recognition (if Family B AST landed).
- `[sub:mech]` Family C: TriggeredAbilityParser ability-word prefix (if Family C AST landed).

**Yield ceiling:** ~56 cards. If B or C bails, that's fine — we ship A's +18 + whatever else landed.
