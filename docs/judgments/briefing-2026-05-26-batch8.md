# MAST TDD briefing — batch 8 (autonomous run 4/10)

**Entering coverage:** 7,455 / 29,614 (25.17%). NUnit 350/0/350.

## Family A — `This X can't be blocked.` (cluster 2, +25 marginal)

**Parser file:** StaticAbilityParser (monolithic; only Static-touching family).

### AST type to add
- **`CantBeBlockedEffect`** — `[OracleEffect("cantBeBlocked")]`. No params. Template: mirror `CantBlockEffect` from batch 6.

### Cards (3 mechanical, helper-novel writes them):
1. **Tidal Kraken** — Creature, oracle `This creature can't be blocked.`
2. **Talas Warrior** — Creature, single-line shape
3. **Secret Tunnel** — Land, `This land can't be blocked.`

### Parser surface
StaticAbilityParser sentence dispatch — mirror `TryParseCantBlock` from batch 6. Regex: `^\s*This (creature|land|permanent) can't be blocked\.\s*$`.

---

## Family B — Morph keyword (cluster 3, +26 marginal)

**Parser file:** OracleParsers (ParameterizedKeyword combinator).

### AST type to add
- **`MorphEffect`** — `[OracleEffect("morph")]`. Required field: `Cost: Cost`. Template: mirror `CyclingEffect` / `EquipEffect`.

### Cards (3 fixtures from helper-novel):
1. **Willbender** — `{1}{U}` Creature, Morph cost `{1}{U}`
2. **Daring Apprentice** — wait, not Morph. Pick from corpus.
3. Pre-curate 3 single-line Morph creatures from corpus.

Sample query: `jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Morph \\{[^}]+\\}"))' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -5`.

### Parser surface
Extend `OracleParsers.ParameterizedKeyword` chain with `Morph` entry. Mirror Cycling pattern (keyword + mana-symbol consumption + optional reminder).

---

## Family C — Spell mass-anthem (cluster 4, +25 marginal)

**Parser file:** SpellAbilityParser (rule-per-file, parallel-safe).

### Cards (4-5 mechanical, helper-mech)

Shape: `Creatures you control get +N/+M until end of turn.` (mass version of bare PT mod from batch 6).

Pre-curate:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Creatures you control get [+\\-]\\d+/[+\\-]\\d+ until end of turn\\.$")) | "\(.name) | \(.mana_cost) | \(.type_line)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -8
```

### Gold AST shape
```json
{
  "Kind": "spell",
  "Effects": [
    {
      "EffectType": "modifyPT",
      "Target": { "Kind": "Each", "Filter": { "CardTypes": ["creature"], "Controller": "You" } },
      "PowerModifier": { "QuantityType": "literal", "Value": N },
      "ToughnessModifier": { "QuantityType": "literal", "Value": M },
      "Duration": { "DurationType": "untilEndOfTurn" },
      "IsOptional": false
    }
  ]
}
```

### Parser surface
New rule file `Spell/Rules/MassAnthemSpellRule.cs`. Same pattern as `ModifyPTSpellRule` from batch 6 but with `Each + Controller:You` target instead of `Target` single-creature.

---

## Dispatch plan

**Wave 1 (2 parallel):**
- `[sub:helper-novel]` (Opus): CantBeBlockedEffect + MorphEffect + 6 fixtures (3 + 3).
- `[sub:helper-mech]` (Sonnet): Family C fixtures (4-5 mass-anthem spells).

**Wave 2 (3 parallel):**
- `[sub:mech]` (Sonnet) Family A: StaticAbilityParser sentence dispatch for can't-be-blocked.
- `[sub:mech]` (Sonnet) Family B: OracleParsers ParameterizedKeyword Morph entry.
- `[sub:mech]` (Sonnet) Family C: New `MassAnthemSpellRule.cs`.

**Yield ceiling:** ~76 cards combined.
