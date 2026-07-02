# MAST judge — batch verdict (goblin-tunneler)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-goblin-tunneler
**Base:** 176e495dda71494b915330f72bde000e5cd90f0f
**Scope:** 1 fixture (Goblin Tunneler, M13) + 1 new parser rule (out of judge scope for structure, cross-checked for citations)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M13/GoblinTunneler.json` — PASS.
  - Oracle text verbatim vs oracle-cards.json: "{T}: Target creature with power 2 or less can't be blocked this turn." (exact match).
  - Activated ability with `{T}` cost → `Kind: activated`, `Costs: [{CostType: tap}]`, `IsManaAbility: false`. Faithful.
  - "Target creature" → `ObjectReference{Kind: Target}` with `Filter.CardTypes: ["creature"]`. Correct target/filter shape.
  - "with power 2 or less" → structured `PowerComparison{Operator: LessThanOrEqual, Value: 2}`. Correct operator for "or less"; no free-text filter.
  - "can't be blocked" → `EffectType: cantBeBlocked` (the `CantBeBlockedEffect` discriminator, `[OracleEffect("cantBeBlocked")]`). Full unconditional unblockability correctly modeled by omitting `BlockedByFilter`/`MaxBlockers`.
  - "this turn" → separate composite `Duration{DurationType: untilTime, Until:{Part: Turn, Edge: End}}` (== `UntilTimeDuration.EndOfTurn`), NOT baked into the effect discriminator. Describe-not-execute; timing/effect kept composite.
  - Attributes (manaCost {1}{R} MV2, colors R, colorIdentity R, creatureStats 1/1) present and correct.
  - Regression: new fixture (absent at base) — no siblings to drop/invert, no out-of-axis nodes to disturb.
  - Citations: doc-comment cites **CR 509.1b** and **CR 602.5**; both exist in rules-structure.json. 509.1b ("...restrictions (effects that say a creature can't block...)") is the declare-blockers restriction step governing evasion, and 509.1 defines evasion abilities; 602.5 governs the `{T}` activation. Neither contradicts the modeling.

- `mast-tdd/2026-07-02-goblin-tunneler#projection` — PASS.
  - No new effect/cost/trigger/restriction discriminator introduced. `cantBeBlocked` already exists as an `OracleEffect`, and `ObjectFilter.PowerComparison` (`Comparison?`) is a pre-existing filter field (shared with `DestroyTargetWithFilterRule`). The branch adds only a new parser rule (`TargetCantBeBlockedWithPowerFilterThisTurnEffectRule`, Priority 81), which composes existing AST nodes. Therefore no PortWalk projection decision (semantic `PortGraph` case or `known-coarse-projections.json` entry) is required.

## Glossary gaps

(none)

## Process notes

The new parser rule's XML doc-comment is careful and accurate: it notes full unblockability (no `BlockedByFilter`), cites the correct comparison operators, and correctly positions itself above the generic `CantBeBlockedThisTurnEffectRule`. Parser correctness is out of judge scope (vanilla NUnit owns it); the cited rules were the judge-relevant surface and both cross-check clean.

**PROCEED** — 0 FAIL.
