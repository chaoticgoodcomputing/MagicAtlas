# Batch 49 verdict — ETB deals N damage to target opponent

**Date:** 2026-05-27
**Briefing:** `docs/judgments/briefing-2026-05-27-batch49.md`
**Verdict:** PROCEED (0 FAIL, 0 CONCERN)

---

## Items reviewed

### 1. `DFT/LonelyArroyo.json`

**PASS**

- No `UnparsedAbility` nodes.
- Three abilities in oracle text order: static `entersTapped`, triggered `dealDamage` to `Opponent`, activated `addMana`.
- `entersTapped` is a static ability per Rule 614 (replacement effect at entry). Correct.
- Triggered ability: `TriggerTiming: When`, `TriggerEvent: Enters`, filter `CardTypes: ["land"]` — "When this land enters" is a When-timed ETB trigger. Correct.
- `DealDamageEffect`: `Source: { Kind: "It" }` (pronoun "it" refers to the land entering), `Amount: literal 1`, `Target: { Kind: "Opponent" }`. "Target opponent" invokes targeting rules (Rule 115.1); `Opponent` is the correct kind for a single opponent (Rules 102.2–102.3). Not `AnyTarget`, not `EachOpponent`. Correct.
- `IsOptional: false` — no "you may" prefix. Correct.
- `addMana` with `Mana: "{W} or {U}"` — standard disjunctive mana ability. Correct.
- ColorIdentity `["U", "W"]` — set-semantic, order irrelevant.

### 2. `DFT/JaggedBarrens.json`

**PASS**

- Structurally identical to LonelyArroyo. Mana colors `{B} or {R}`, ColorIdentity `["B", "R"]`. Same rules analysis applies.

### 3. `DFT/ErodedCanyon.json`

**PASS**

- Structurally identical. Mana colors `{U} or {R}`, ColorIdentity `["R", "U"]`. Same rules analysis applies.

### 4. `SelfDealsDamageToOpponentRule.cs`

**PASS**

- `[TriggeredRule]` attribute — picked up by reflection-based dispatch. Correct.
- Regex anchored with `^...$`. Matches `"it deals N damage to target opponent."` and variants. Does not overlap with `SelfDealsDamageToAnyTargetTriggeredRule` (which only matches "any target") or `SelfDealsDamageToYouRule` (which matches "to you").
- Emits `DealDamageEffect` with `Source: It`, `Amount: LiteralQuantity.Of(n)`, `Target: { Kind: Opponent }`.
- Word-number parsing matches the existing pattern in the sibling rule.
- No new AST types. No infrastructure files modified.

---

**Overall:** PROCEED. All 4 items PASS. Suite at 772 (766 + 6 newly-green parser tests).
