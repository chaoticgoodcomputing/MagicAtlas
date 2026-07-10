# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** you-may-have-it-fight-target-cre
**Branch:** mast/you-may-have-it-fight-target-cre
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DKA/SomberwaldStag.json` — PASS. Input.OracleText ("When this creature enters, you may have it fight target creature you don't control.") is byte-identical to the corpus (oracle-cards.json). Fully structured: `triggered` (Timing When / Event Enters / Filter IsSelf+creature) wrapping `optional` (the "you may", CR 117.7) over `fight` (CR 701.14) with Controlled=`It` (the entering creature back-reference) and Opposed=`Target` creature Controller `Opponent` ("target creature you don't control"). No IUnparsed, no UnstructuredEffect, no `unparsed` EffectType, no lossy drop/merge.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/HaveItFightTargetTriggeredRule.cs` — PASS. Emits only pre-existing sound nodes (FightEffect, OptionalEffect, ObjectReference.It/Target, ControllerFilter.Opponent); no new AST node. Cited CR 701.14 (Fight) exists in rules-structure.json and 701.14a matches the modeling. Mirrors the sibling `ItFightsColorControlledSpellRule` exactly (both map "it" -> `ObjectReferenceKind.It`, both collapse "you don't control"/"an opponent controls" -> Controller: Opponent).
- `mast/you-may-have-it-fight-target-cre#projection` — PASS. No new discriminator (newAstNode=false, shared=[]); reuses the existing `fight`/`optional` effect discriminators and `Enters` trigger event. The initiative-03 ratchet fires only on new discriminators, so no PortWalk projection decision is required for this branch.

## Glossary gaps

(none — "fight" is covered by CR 701.14; the trigger/optional shapes are established)

## Process notes

- Cross-referenced CR 701.14 (Fight) and CR 109.5 against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`. 701.14 exists with subrules a–d and matches the two-participant fight modeling.
- The doc-comment's secondary aside cites CR 109.5 for the "it" pronoun back-reference. CR 109.5 is literally about "you"/"your" resolving to an object's controller, so it is an imperfect analogy for the "it" antecedent — but it is present in the data and does not contradict the modeling. Per judge doctrine (FAIL a citation only if absent-from-data or contradictory; do not nitpick), this is non-blocking. The load-bearing citation (CR 701.14) is correct.
- FightEffect's `Controlled` doc-comment says "typically target creature you control"; here it correctly carries `It` (the entering creature the controller directs), which is the right semantics for "have it fight".

ALL PASS
