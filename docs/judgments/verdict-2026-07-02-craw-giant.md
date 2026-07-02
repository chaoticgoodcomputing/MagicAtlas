# MAST judge — batch verdict (craw-giant)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-craw-giant (base 176e495d)
**Scope:** 1 fixture (LEG/CrawGiant.json), 1 keyword node (RampageKeyword.cs) — task: Rampage N
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LEG/CrawGiant.json#Rampage` — PASS. Models CR 702.23a: `Kind: triggered`, `Trigger{Whenever, BecomesBlocked, Filter creature}`, `modifyPT{Target:It, Power/Toughness +2 multiply, Duration untilEndOfTurn}`. The Rampage parameter N=2 is captured **structurally** via `CalculatedQuantity{Operand:2, Operation:"multiply"}` — precisely the type's documented worked example ("+2 for each …"). Timing is carried by the Trigger node, effect is a plain modifyPT — no baked-in timing, describe-not-execute. Oracle text matches oracle-cards.json verbatim.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/LEG/CrawGiant.json#Trample-sibling` — PASS. Sibling Trample preserved as `static / keywordAbility` (corpus-standard evergreen-keyword shape; matches ChargingBinox/CuboidColony/DreadSlag). No ability dropped/added/inverted; attributes (MV 7, mono-G, 6/4) correct; no `unparsed` anywhere in gold.
- `libs/magic-ast/Keywords/Definitions/RampageKeyword.cs#projection` — PASS. No new discriminator: `TriggerEvent.BecomesBlocked` and `EffectType modifyPT` both pre-exist on base; the branch adds only the `KeywordAbility.Rampage` enum value + a combinator. No new effect/cost/trigger discriminator ⇒ no PortWalk projection decision required.

## Process notes

- **Free-text residual (criterion b).** The `Expression: "for each creature blocking it beyond the first"` is the established `CalculatedQuantity.Expression` residual doctrine — a combat-state population query outside MAST's ObjectFilter scope, identical to the already-landed Melee keyword's per-opponent Expression. It is NOT a new free-text axis introduced by this task; the Rampage parameter axis that this task owns (N) is captured structurally via `Operand`/`Operation`. This is a residual on a different, already-owned axis, which the task brief explicitly does not fail.
- **CR cross-reference.** `702.23` exists in rules-structure.json with subrule `a` text matching the node's doc-comment verbatim ("Rampage is a triggered ability. \"Rampage N\" means \"Whenever this creature becomes blocked, it gets +N/+N until end of turn for each creature blocking it beyond the first.\"").

ALL PASS
