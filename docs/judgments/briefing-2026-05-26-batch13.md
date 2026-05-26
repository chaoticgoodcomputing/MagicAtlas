# MAST TDD briefing — batch 13 (autonomous run 9/10)

**Entering coverage:** 8,160 / 29,614 (27.55%). NUnit 430/0/430.

## Family A — Soulshift + Wither keyword effects (clusters 4 + 5, +34 marginal combined)

### AST types to add (helper-novel)
- **`SoulshiftEffect`** — `[OracleEffect("soulshift")]`. Required field: `Value: int`. Mirror BushidoEffect from batch 9. Source: `libs/magic-ast/AST/Effects/Keyword/SoulshiftEffect.cs`. Cite Rule 702.46.
- **`WitherEffect`** — `[OracleEffect("wither")]`. No params. Mirror InfectEffect from batch 7. Source: `libs/magic-ast/AST/Effects/Keyword/WitherEffect.cs`. Cite Rule 702.80.

### Fixtures (helper-novel, 4-5)
- 2-3 single-line Soulshift creatures (Kamigawa)
- 2 single-line Wither creatures

Pre-curate:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Soulshift \\d+ \\(")) | "\(.name) | \(.mana_cost) | \(.oracle_text)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Wither \\(")) | "\(.name) | \(.mana_cost) | \(.oracle_text)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### Parser surface (mech)
OracleParsers: add Soulshift to ParameterizedKeyword (integer-param, mirror Bushido pattern from batch 9), add Wither to SimpleKeyword (mirror Infect from batch 7).

---

## Family B — ETB-with-counters (cluster 3, +18 marginal)

### Shape
`This creature enters with N +1/+1 counters on it.`

### AST inspection (helper-novel decides)
Check if existing effect types cover this. Could be:
- A trigger `OnEntersBattlefieldTrigger` with a `PutCountersEffect` (but the "enters with" semantic is a replacement, not a trigger).
- An `EntersWithCountersEffect` static-on-the-card.
- A field on the creature's `CreatureStats` attribute.

Per the MAST descriptive principle, this is a static-ability declarative statement. Best home is likely a new `EntersWithCountersEffect` (no params or `Count: Quantity` + `CounterType: string`). Helper-novel decides.

### Fixtures (helper-novel, 3)
Pre-curate single-line creatures with `This creature enters with N +1/+1 counters on it.`:
```bash
jq -r '.[] | select(.oracle_text != null and .type_line != null) | select(.type_line | test("Creature")) | select(.oracle_text | test("^This creature enters with \\d+ \\+1/\\+1 counters on it\\.$")) | "\(.name) | \(.mana_cost) | \(.power)/\(.toughness)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### Parser surface (mech)
StaticAbilityParser sentence dispatch. Regex: `^\s*This creature enters with (\d+|\w+) \+1/\+1 counters? on it\.\s*$`.

---

## Dispatch plan

**Wave 1:**
- `[sub:helper-novel]` (Opus): Both families. 2 AST types (Soulshift + Wither) + 1 (EntersWithCounters or similar) + ~8 fixtures total. Bail-friendly if EntersWithCounters needs base-class changes.

**Wave 2:**
- `[sub:mech]` Family A: OracleParsers Soulshift + Wither entries.
- `[sub:mech]` Family B: StaticAbilityParser EntersWithCounters dispatch.

**Yield ceiling:** ~52 cards.
