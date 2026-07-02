# MAST judge — batch verdict (pouncing-kavu)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-pouncing-kavu
**Base:** cb048c63ea6ae85ef069e0d47244ec68945a5415
**Scope:** 1 fixture (+ 1 new parser rule, projection cross-check)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/PLS/PouncingKavu.json` — PASS. Oracle text verified verbatim against oracle-cards.json ("If this creature was kicked, it enters with two +1/+1 counters on it and with haste."). The kicked-conditional ETB is modeled as a `static` ability with `When: asThisEnters` — correct, since "[This permanent] enters with ..." is a replacement effect (CR 614.1c), not a triggered ability. The "was kicked" gate is a structured `Condition { keywordCostPaid, Kicker }` (CR 702.33d/e linked condition), not free text. The body decomposes into two conjoined plain effects on one ability — `putCounters` (Self, literal 2, +1/+1; counters granted as it enters per CR 122.6) and `keywordAbility` Haste (CR 702.10a). Timing and condition are separate composable nodes; nothing bakes firing context into an effect discriminator. Kicker (`additionalCastCost`, CR 702.33a) and First strike siblings preserved.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/KickerConditionalEntersWithCountersAndKeywordRule.cs#projection` — PASS. New parser rule composes only pre-existing AST nodes (`PutCountersEffect`, `KeywordAbilityEffect`, `keywordCostPaid` condition, `AsThisEnters` timing). No new effect/cost/trigger/restriction discriminator is introduced, so no PortWalk projection decision is required; the exhaustiveness ratchet is not triggered.

## Rules cross-reference

- CR 702.33a — "Kicker is a static ability... 'You may pay an additional [cost] as you cast this spell.'" → matches Kicker sibling `additionalCastCost` (IsOptional).  Present in rules-structure.json.
- CR 702.33d/e — "kicked" is the declared-intent condition; the conditional ability is linked to kicker (CR 607). → matches `Condition { keywordCostPaid, Kicker }`. Present.
- CR 614.1c — "'[This permanent] enters with ...' ... are replacement effects." → matches `Kind: static`, `When: asThisEnters`. Present.
- CR 122.6 — counters given "as it enters the battlefield." → matches `putCounters` on Self at entry. Present.
- CR 702.10a — "Haste is a static ability." → matches `keywordAbility` Haste grant. Present.

## Glossary gaps

(none)

## Process notes

- Brand-new fixture (absent at base sha) — no prior gold to regress against; siblings judged for presence/faithfulness instead.
- Out-of-axis: First strike is modeled as `combatDamageTiming { Timing: First }` rather than a keyword-grant effect. That is a separate, pre-existing convention on a different axis and is not part of this task's target line; not a fail here.

ALL PASS
