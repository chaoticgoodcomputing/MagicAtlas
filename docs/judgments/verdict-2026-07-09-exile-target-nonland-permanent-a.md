# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 changed files (1 fixture, 1 parser rule) + 1 projection decision
**Batch:** exile-target-nonland-permanent-a (branch `mast/exile-target-nonland-permanent-a`)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/AFR/PortableHole.json` — PASS. `Input.OracleText` is byte-identical to the live Portable Hole oracle text ("When this artifact enters, exile target nonland permanent an opponent controls with mana value 2 or less until this artifact leaves the battlefield."); TypeLine `Artifact` and ManaCost `{W}` also match oracle-cards.json. The single triggered ability decomposes cleanly: `Trigger{Timing:When, Event:Enters, Filter:{CardTypes:[artifact], IsSelf:true}}` (CR 603.6 zone-change ETB trigger) + a plain `exile` effect (CR 701.13) whose `Target.Filter` captures every clause structurally — `CardTypes:[permanent]` + `ExcludedCardTypes:[land]` for "nonland permanent", `Controller:Opponent` for "an opponent controls", and `ManaValueComparison:{LessThanOrEqual, 2}` for "with mana value 2 or less" (CR 202.3) — with `UntilLeavesBattlefieldDuration{Object:"this artifact"}` for the temporary-exile duration (CR 611.1 / CR 406.1). No `unparsed`/`UnstructuredEffect` node, no free-text characteristic, no lossy drop or merge. Timing and effect are correctly a composite (Trigger node carries the "when"; the effect names only the action).

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExileTargetByManaValueUntilLeavesTriggeredRule.cs` — PASS. A faithful sibling of `ExileUntilLeavesTriggeredRule`: same anchored `^…$` shape with the mandatory "with mana value N or less/greater" qualifier inserted between the target filter and the "until this … leaves the battlefield" tail, so it cannot substring-collide with the plain sibling (which owns the no-qualifier form). Direction maps correctly (`less|fewer` → LessThanOrEqual, `greater|more` → GreaterThanOrEqual) onto the pre-existing `ObjectFilter.ManaValueComparison`. `ParseTargetFilter` returns null for unhandled shapes (declines rather than over-approximates). All six cited CR rules exist in `rules-structure.json` and their text matches the modeling: 611.1 (continuous effect with duration), 406.1 (exile zone — temporary exile), 202.3 (mana value), 701.13 (Exile), 603.6 (zone-change ETB trigger), 205.2 (card types).

- `mast/exile-target-nonland-permanent-a#projection` — PASS. No new discriminator (effect type, cost type, trigger event, or restriction) is introduced. `ManaValueComparison`, `ExcludedCardTypes`, and `Controller` on `ObjectFilter`, plus `ExileEffect` and `UntilLeavesBattlefieldDuration`, all pre-exist at the base sha; the branch merely newly *populates* the existing mana-value restriction. The exhaustiveness ratchet does not fire, so no new `PortGraph`/`PortWalkProjection` entry or `known-coarse-projections.json` justification is required.

## Glossary gaps

None. "Mana value" (CR 202.3), "permanent" (CR 110), "exile" (CR 701.13), and "opponent" are all standard, rules-grounded terms already carried structurally.

## Process notes

Only two files changed (verified via `git diff --name-only`); `newAstNode=false`, `shared=[]` from the worker report is consistent with the diff. The "nonland permanent" → `CardTypes:[permanent]` + `ExcludedCardTypes:[land]` encoding is the established codebase convention (documented on `ObjectFilter.cs` and used by the sibling rule), a structured representation rather than free text, so it is not flagged.
