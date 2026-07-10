# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/untap-up-to-two-target-creatures
**Scope:** 3 files (1 fixture, 2 parser rules) + 1 projection verdict
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JoinForces.json` — PASS. Input.Name/ManaCost/TypeLine/OracleText/Colors/ColorIdentity all byte-identical to Join Forces in oracle-cards.json ("Untap up to two target creatures. They each get +2/+2 until end of turn."). Gold is a single `spell` ability with two fully-structured effects: `untap` on a `Target` reference filtered to `creature` with `UpToQuantity{Max:2,Min:0}` (CR 701.26b + CR 115.1), and `modifyPT` +2/+2 with `untilTime` Turn/End duration on the `It` back-reference (CR 611.1 continuous effect, CR 514.2 cleanup). No `unparsed`, no `UnstructuredEffect`, no free-text, no lossy drop/merge.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/UntapUpToNTargetsRule.cs` — PASS. Anchored spell rule emitting a pre-existing `UntapEffect` with the "up to N" cardinality on `ObjectReference.Quantity` (matching the oracle phrasing "up to N target", not "untap N targets"). CR 701.26b quoted verbatim and correct.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/UntapUpToNTargetsAndPumpRule.cs` — PASS. `ISpellRule` (pump fragment) + `IMultiSpellRule` (full two-sentence) as briefed. "They each" plural back-reference modeled as `ObjectReference.It()`, consistent with the established Frost Breath ("Those creatures") convention in `TapAndFreezeRule`. Emits pre-existing `ModifyPTEffect` (+2/+2, EndOfTurn). Cited CR (701.26b, 611, 514.2, 115.1, 107.3) present and non-contradictory.
- `mast/untap-up-to-two-target-creatures#projection` — PASS. No new discriminator introduced. `newAstNode=false`; both effect nodes (`untap`=`[OracleEffect("untap")]` UntapEffect, `modifyPT`=`[OracleEffect("modifyPT")]` ModifyPTEffect) are pre-existing with existing projections. The exhaustiveness ratchet has nothing new to enforce; no PortWalk projection decision required.

## Glossary gaps

None.

## Process notes

- Byte-identity of `Input.OracleText` (and all Input fields) verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` — Join Forces is a real card (Instant, {2}{W}, W); exact match.
- Rule cross-reference: CR 701.26b, 115.1, 611.1, 514.2 all present in rules-structure.json with matching text. `shared=[]` — the diff touches only the two new parser rules + the new fixture; no shared-file generalizations to review.
- Minor, non-blocking: the doc-comments gloss CR 107.3 as "up to N". CR 107.3 (section 107, Numbers and Symbols) actually concerns the {X}/X placeholder rather than "up to". It is imprecise but in the correct rules neighborhood and does not contradict the up-to-N cardinality modeling, so per judge doctrine (FAIL only on absent-from-data or contradiction) it is not a FAIL. The load-bearing untap citation (701.26b) is exact.

ALL PASS
