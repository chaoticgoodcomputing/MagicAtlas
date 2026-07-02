# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** void-grafter
**Branch:** mast-tdd/2026-07-02-void-grafter (base 90209551)
**Scope:** 2 files (1 fixture, 1 AST/parser node) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/OGW/VoidGrafter.json` — PASS. Oracle text matches oracle-cards.json verbatim. The regenerated target line "another target creature you control gains hexproof until end of turn" is modeled as a triggered `gainAbility` effect: `Target` (Kind=Target — CR 115.1) with `Filter{CardTypes:[creature], Controller:You, ExcludeSelf:true}` ("another … you control" — CR 109.5/109.4), `GainedAbility` a static Hexproof keyword ability (CR 702.11a "Hexproof is a static ability" / 702.11b), and `Duration{untilTime, Turn/End}` ("until end of turn" — CR 514.2). Describe-not-execute: the "when this creature enters" timing lives in the `Trigger` node (When/Enters/IsSelf), not baked into the effect discriminator. Devoid (static keywordAbility) and Flash (static timingModification Grant/Instant) siblings preserved; out-of-axis nodes (mana {1}{G}{U} MV 3, empty colors per Devoid, GU identity, 2/4 stats) intact. No `unparsed`, no rules-meaningful free text (only verbatim-by-design Raw/RawText fields).

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AnotherTargetCreatureYouControlGainsKeywordUntilEndOfTurnRule.cs` — PASS. New triggered-effect parser rule emitting the pre-existing `GainAbilityEffect`. Doc-comment CR citations (603.1, 115.1, 611.1, 514.2, 702.11a/b, 109.4, 109.5) all verified present and consistent in rules-structure.json. On an unrecognized keyword the local builder returns null and the rule bails (`return false`) — no free-text fallback, so no escape hatch enters gold.

- `mast-tdd/2026-07-02-void-grafter#projection` — PASS (N/A). Diff touches only the fixture + parser rule; introduces no new effect/cost type, trigger event, or restriction. `gainAbility`, `ObjectFilter.ExcludeSelf`/`Controller`, `UntilTimeDuration.EndOfTurn`, and `KeywordAbility.Hexproof` all pre-exist on base. No PortGraph case or known-coarse-projections entry required; the projection ratchet does not apply.

## Glossary gaps

None new. Hexproof (CR 702.11), Devoid (702.114), Flash (702.8) all covered.

## Process notes

- Minor, non-blocking: the .cs doc-comment cites CR 109.4 for `Controller=You` and CR 109.5 for the "another"/`ExcludeSelf` concept; the "you control → controller" mapping is more squarely CR 109.5. Both rules exist in the data and sit in the correct conceptual neighborhood (control / "you"), so per judge doctrine this is subrule-precision imprecision, not an absent-or-contradictory citation — it does not block PASS.

**Result: ALL PASS.**
