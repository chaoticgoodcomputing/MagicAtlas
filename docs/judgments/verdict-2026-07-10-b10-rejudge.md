# MAST judge — batch-10 FINAL re-judge

**Date:** 2026-07-10
**Branch:** feat/loop-trial @ 288e99e8 (remediation commit)
**Scope:** 6 items (1 fixture + parser, 1 operator-soundness source + test, 4 citation fixes)
**Result:** PASS

## Summary
- PASS: 6
- FAIL: 0

## Per-item verdicts
- `tests/magic-ast-tests/Fixtures/HandParsedCards/Vigor.json` (P14, REAL) — PASS. Count.DerivedFrom = `DamagePrevented`, zero residual `DamageDealt`; faithful to "for each 1 damage prevented this way" (CR 615.5) and consistent with P15's `DerivedKind.DamagePrevented` (Quantity.cs:101, GainLifeEqualToDamagePreventedSpellRule).
- `libs/magic-ast/AST/References/ObjectFilterRelations.cs` (P10, REAL soundness) — PASS. `SharesPermanentTypeWith` now floored in BOTH `UndecidedAxis` (l.521) and `SupUndecidedAxis` (l.861), and added to `OperatorPropertyTests.IsRelational` — all three registrations present, mirroring `SharesCardTypeWith`. Intersects/Subsumes now sound on that axis; core ring green at 7243.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/GainLifeAndGetEnergyTriggeredRule.cs` (P20) — PASS. Stray "CR 701.20" (Reveal) dropped; retained CR 119.3 = "If an effect causes a player to gain life or lose life…" — correct gain-life rule; no residual 701.20.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExileNonlandCardFromHandImprintTriggeredRule.cs` (P08) — PASS. "CR 702.38 (Imprint)" (702.38 = Amplify) replaced with "Imprint is an ability word … linked ability CR 406.6". 406.6 verbatim covers an object exiling cards + a second ability referring to "the exiled cards" (linked per rule 607) — apt. Imprint has no keyword-ability rule in this ruleset; correction accurate. No residual 702.38.
- `libs/magic-ast/AST/Effects/Core/LoseTheGameEffect.cs` (P15 spot-check) — PASS. 104.3a (concede) → 104.3e = "An effect may state that a player loses the game." — exact match to the modeled absolute lose-effect; no residual 104.3a.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/NonlandCreatureTypeGrantRule.cs` (P04 spot-check) — PASS. 205.1a (sets/replaces card type) → 205.1b (object retains a prior card/supertype/subtype) — matches the "in addition to its other types" retention modeling; no residual 205.1a.

## FAIL verdicts
None.

## Glossary gaps
None surfaced.

## Process notes
All four originally-FAILed items and both opportunistic spot-checks are remediated faithfully. Every new/retained CR number exists in rules-structure.json and its text matches the modeling; every dropped citation is confirmed to name the wrong mechanic (701.20 Reveal, 702.38 Amplify, 104.3a concede, 205.1a set-type). No residual bad citations, no residual `DamageDealt`. Cross-branch consistency of `DamagePrevented` (P14↔P15) confirmed.

ALL PASS
