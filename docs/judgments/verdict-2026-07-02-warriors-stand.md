# MAST judge — batch verdict (warriors-stand)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-warriors-stand
**Scope:** 1 fixture (Warrior's Stand) + projection decision for new `beenAttackedThisStep` condition
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WarriorsStand.json` — PASS. The conditional casting-time restriction "Cast this spell only during the declare attackers step and only if you've been attacked this step" is modeled as `TimingModificationEffect{Modification: Restrict, Timing: Phase, Phase: "declare attackers step", Condition: beenAttackedThisStep}`. Correct decomposition: the "when" lives in composable data fields (Timing/Phase) and the intervening gate in a distinct typed `Condition` node — timing is NOT baked into the effect discriminator. Faithful to the printed card, describe-not-execute (WhoseTurn deliberately null; being-attacked implies opponent's turn, so no false "yours" restriction). No free-text/unparsed residual — the gate is a structured `beenAttackedThisStep` marker, keeping the phrase out of `OtherCondition`. The `+2/+2 until end of turn` sibling ability is fully modeled (`modifyPT` Each/creature/You, +2/+2, untilTime Turn/End) — no dropped/added/inverted ability. New file, so no out-of-axis regression; schema diff only ADDS the new condition discriminator (no removals). All cited rules exist verbatim in rules-structure.json: CR 601.3a (prohibition-effect casting restriction — matches the printed restriction semantics), CR 508.1 / 508.1a / 508.1b (Declare Attackers Step). Glossary "Attacking Creature" and "Declare Attackers Step" both verbatim.

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WarriorsStand.json#projection` — PASS. The only genuinely new discriminator is the `beenAttackedThisStep` **Condition**, an axis the PortWalk exhaustiveness ratchet does not track (it covers effectType/costType/triggerEvent/restriction). Its host `timingModification` effectType is already an explicit justified coarse projection (`known-coarse-projections.json` line 161), and the codebase treats casting/timing restrictions as consistently non-gating for intra-turn loops (`GatingRestrictions` note; `OnlyAsInstant`/`OnlyAsSorcery`/`OnlyDuringYourTurn` all coarse). A casting-time gate emits/consumes no interaction resource, so coarse is the sensible choice — nothing a flow rule would clearly want is being parked.

## Glossary gaps

(none — both referenced glossary terms "Attacking Creature" and "Declare Attackers Step" are present and verbatim)

## Process notes

Oracle text cross-checked against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` — the fixture's `Input.OracleText` matches the real Warrior's Stand oracle text exactly. All four CR citations and both glossary citations in the two new AST files were confirmed present and verbatim against `rules-structure.json` / `glossary.json`.

ALL PASS
