# MAST TDD briefing — batch 14 (autonomous run 10/10 — FINAL BATCH)

**Entering coverage:** 8,213 / 29,614 (27.73%). NUnit 446/0/446.

## Family A — Exile target land (cluster 2, +18 marginal)

**Shape:** `Exile target land.`

### Mechanical — no new AST

`SpellAbility { Effects: [ExileEffect { Target: ObjectReference(Target, Filter: { CardTypes: ["land"] }) }] }`. All types exist.

### Cards (3 fixtures, helper-mech)

Pre-curate:
```bash
jq -r '.[] | select(.oracle_text == "Exile target land.") | "\(.name) | \(.mana_cost) | \(.type_line)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -5
```

### Parser surface (mech)

Check existing Exile rules in `Spell/Rules/` — `ExileTypeDisjunctionRule`, `ExileColorDisjunctionPermanentRule`, etc. Likely an extension to handle bare `target <cardtype>` (no color, no subtype, no disjunction). Add as a new rule or extend an existing one.

---

## Family B — Persist keyword (cluster 4, +16 marginal)

**Shape:** `Persist (When this creature dies, if it had no -1/-1 counters on it, return it to the battlefield under its owner's control with a -1/-1 counter on it.)`

### AST type
- **`PersistEffect`** — `[OracleEffect("persist")]`. No params. Mirror InfectEffect. Source: `libs/magic-ast/AST/Effects/Keyword/PersistEffect.cs`. Cite Rule 702.78.

### Cards (3 fixtures, helper-novel)

Pre-curate single-line Persist creatures (Shadowmoor/Eventide).

### Parser surface (mech)

OracleParsers SimpleKeyword — mirror Infect/Wither entries.

---

## Family C — Bestow keyword (cluster 5, +16 marginal)

**Shape:** `Bestow {cost} (If you cast this card for its bestow cost, it's an Aura spell with enchant creature. It becomes a creature again if it's not attached.)`

### AST type
- **`BestowEffect`** — `[OracleEffect("bestow")]`. Required field: `Cost: Cost`. Mirror CyclingEffect/EquipEffect. Source: `libs/magic-ast/AST/Effects/Keyword/BestowEffect.cs`. Cite Rule 702.103.

### Cards (3 fixtures, helper-novel)

Pre-curate single-line Bestow creatures (Theros block — Boon Satyr, Heliod, etc.).

### Parser surface (mech)

OracleParsers ParameterizedKeyword — mirror Cycling/Equip pattern.

---

## Dispatch plan

**Wave 1 (2 parallel):**
- `[sub:helper-novel]` (Opus): Persist + Bestow AST + 6 fixtures.
- `[sub:helper-mech]` (Sonnet): 3 Exile-target-land fixtures.

**Wave 2 (2 parallel, after merge):**
- `[sub:mech]` (Sonnet) Family A: Spell rule extension for `Exile target land`.
- `[sub:mech]` (Sonnet) Families B+C: OracleParsers Persist (SimpleKeyword) + Bestow (ParameterizedKeyword).

**Yield ceiling:** ~50 cards. Wraps the 10-batch autonomous run.
