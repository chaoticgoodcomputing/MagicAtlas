# MAST judge — PB-3 delta verdict (M14/YoungPyromancer)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (type-axis arm: instant/sorcery → CardTypes)
**Mode:** DELTA judgment (judge only the change this slice made; co-occurring other-axis residuals are other slices' debt)
**Scope:** 1 fixture (working-tree, uncommitted)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## Card
Young Pyromancer — "Whenever you cast an instant or sorcery spell, create a 1/1 red Elemental creature token."
Real oracle text confirmed against oracle-cards.json (byte-identical to fixture Input.OracleText).

## Delta judged
PB-3 plan maps this gold to the **type axis**: `M14/YoungPyromancer (instant/sorcery)` → `CardTypes` (gold-burndown-plan.md line 319).

Working-tree diff on the SpellCast trigger filter:
- BEFORE: `CardTypes:["spell"]` + free-text `Characteristics:[{CharacteristicType:"other",Description:"instant"},{...,"sorcery"}]`
- AFTER:  `CardTypes:["spell","instant","sorcery"]` (the two OtherCharacteristic free-text nodes removed)

Producer routed through the new `QualifierAxisMapper.Apply` (SpellCastConditionRule.cs); the mapper recognizes `instant`/`sorcery` as card types and appends them to the `["spell"]` base.

## Per-criterion
- (a) TARGET structured correctly — PASS. The instant/sorcery residual became the `CardTypes` axis; `"spell"` base type retained. Faithful to the card. instant/sorcery are card types per CR 300.1; spell-cast triggered ability per CR 603.2.
- (b) No new free-text/unparsed residual — PASS (primary criterion). Zero `OtherCharacteristic`/`"unparsed"`/`OtherCondition`/`Description` remaining in the gold. Whitelist entry `M14/YoungPyromancer` correctly removed (present at HEAD, gone in working tree) — gold is fully clean.
- (c) No regression — PASS. Single triggered ability preserved; `createToken` effect, token (1/1 red Elemental creature, IsCopy:false), `Player:You`, and the co-occurring `Controller:You` sibling filter all intact. Remaining diff lines are non-semantic serialization shape only (`IsVariable:false` schema default on manaCost; key reordering of Power/Toughness/Player).

## Out-of-scope residual remaining
None. This is not an [S6-SHARED] gold — it carries no other/another exclusion and no other-axis residual. Fully clean after this slice.

## PASS verdicts
- `tests/magic-ast-tests/Fixtures/HandParsedCards/M14/YoungPyromancer.json` — PASS. Type-axis delta: instant/sorcery free-text → CardTypes:['spell','instant','sorcery']; CR 300.1 / CR 603.2; no new residual; siblings/effects preserved; gold fully clean.

## Glossary gaps
None.

ALL PASS
