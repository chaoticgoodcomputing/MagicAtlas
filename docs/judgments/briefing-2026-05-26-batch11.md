# MAST TDD briefing — batch 11 (autonomous run 7/10)

**Entering coverage:** 7,774 / 29,614 (26.25%). NUnit 400/0/400.

Both families novel-AST.

## Family A — CanBlockOnly restriction (cluster 3, +22 marginal)

**Shape:** `This creature can block only creatures with <characteristic>.` / `This creature can block only <filter>.`

### AST type
- **`CanBlockOnlyEffect`** — `[OracleEffect("canBlockOnly")]`. Required field: `Filter: ObjectFilter`. Inherits 4 traits. Source: `libs/magic-ast/AST/Effects/Combat/CanBlockOnlyEffect.cs` (next to `CantBlockEffect`).

### Cards (3 fixtures, helper-novel)
Pre-curate:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("can block only")) | "\(.name) | \(.type_line) | \(.oracle_text | gsub("\n"; " | "))"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

Pick 3 with simple filters: `creatures with flying`, `creatures with reach`, `Soldiers`, etc.

### Gold AST per fixture
```json
{
  "Kind": "static",
  "Effect": {
    "EffectType": "canBlockOnly",
    "Filter": { "CardTypes": ["creature"], "Characteristics": ["with flying"] },
    "IsOptional": false
  }
}
```

### Parser surface (mech)
StaticAbilityParser sentence dispatch. Regex captures the filter phrase, builds an `ObjectFilter`.

---

## Family B — Typecycling (cluster 5, +20 marginal)

**Shape:** `<LandType>cycling {cost} ({cost}, Discard this card: Search your library for a <LandType> card, reveal it, put it into your hand, then shuffle.)`

Examples: Swampcycling, Mountaincycling, Plainscycling, Islandcycling, Forestcycling.

### AST type
- **`TypecyclingEffect`** — `[OracleEffect("typecycling")]`. Required fields: `Type: string` (the basic land type) + `Cost: Cost`. Inherits 4 traits. Source: `libs/magic-ast/AST/Effects/Keyword/TypecyclingEffect.cs`.

Distinct from regular CyclingEffect — Typecycling has a type-search effect, not the bare draw-a-card effect. Two separate effect types.

### Cards (3 fixtures, helper-novel)

Pre-curate:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^(Swamp|Mountain|Plains|Island|Forest)cycling \\{")) | "\(.name) | \(.mana_cost) | \(.oracle_text)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

Pick 3 single-line cards varying the land type.

### Gold AST per fixture
```json
{
  "Kind": "static",
  "KeywordSource": "Swampcycling",
  "Effect": {
    "EffectType": "typecycling",
    "Type": "Swamp",
    "Cost": { "CostType": "mana", "Raw": "{2}", "Symbols": [...] },
    "IsOptional": false
  }
}
```

### Parser surface (mech)
OracleParsers — extend ParameterizedKeyword chain. Each typecycling variant is a separate combinator OR a single combinator that captures the land-type prefix dynamically. Latter is cleaner.

Combinator sketch:
```csharp
Keyword(<one of "Swampcycling" | "Mountaincycling" | "Plainscycling" | "Islandcycling" | "Forestcycling">)
+ mana cost tokens
+ optional reminder
→ StaticAbility { KeywordSource: <kw>, Effect: TypecyclingEffect { Type: <prefix>, Cost } }
```

---

## Dispatch plan

**Wave 1:**
- `[sub:helper-novel]` (Opus): both AST types + 6 fixtures (3+3).

**Wave 2 (2 parallel):**
- `[sub:mech]` Family A: StaticAbilityParser sentence dispatch for CanBlockOnly.
- `[sub:mech]` Family B: OracleParsers ParameterizedKeyword Typecycling variant.

**Yield ceiling:** ~42 cards.
