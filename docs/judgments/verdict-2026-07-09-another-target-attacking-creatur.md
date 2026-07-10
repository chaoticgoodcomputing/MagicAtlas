# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 changed files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DSK/ClammyProwler.json` — PASS. `Input.OracleText` is byte-identical to the real Duskmourn card (verified against `oracle-cards.json`); mana cost `{3}{U}` (MV 4), colors/identity `U`, P/T 2/5, type line all match. Gold AST is fully structured — no `unparsed`, no `UnstructuredEffect`, no free text, no lossy drop/merge. Trigger `{Whenever, Attacks, Filter creature+IsSelf}` correctly models "Whenever this creature attacks" (CR 508.1 declare-attackers, CR 603.1 triggered structure). Effect `cantBeBlocked` on a `Target` creature with `ExcludeSelf` ("another", CR 109.5 per codebase convention) + `combatState: Attacking` ("attacking creature") + `untilTime Turn/End` ("this turn") correctly models the resolution clause (CR 509.1b evasion). Semantics: the chosen other attacking creature becomes unblockable for the turn — correct.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AnotherTargetAttackingCreatureCantBeBlockedThisTurnRule.cs` — PASS. Emits the pre-existing `CantBeBlockedEffect` (`[OracleEffect("cantBeBlocked")]`) with `ExcludeSelf`, `Characteristic.InCombat(Attacking)`, `UntilTimeDuration.EndOfTurn`, `ObjectReferenceKind.Target` — shape matches sibling `TargetCantBeBlockedThisTurnRule` plus the two "another"+"attacking" axes. Regex fully anchored `^another\s+target\s+attacking\s+creature\s+can'?t\s+be\s+blocked\s+this\s+turn$`, so no substring collision with sibling `^target …` surfaces. Primary evasion citation CR 509.1b is correct; CR 508.1 / CR 603.1 correct for the trigger/structure. `newAstNode=false`, `shared=[]` confirmed by the diff (only the fixture + this rule file changed).
- `libs/mast-interaction/known-coarse-projections.json#cantBeBlocked` (projection, initiative 03) — PASS. No new discriminator is introduced: the effect type `cantBeBlocked`, the `CombatState.Attacking` characteristic, and `ObjectFilter.ExcludeSelf` all pre-exist. The ratchet does not fire. The existing `cantBeBlocked` coarse decision ("baseline coarse fallback … no flow rule consumes it yet") remains sensible — attacker-side blocking-declaration evasion is a runtime combat restriction that creates no resource/cost/trigger a flow rule would consume; coarse is the correct choice, not an insensible parking.

## Glossary gaps

None. "Attacking creature", "can't be blocked", and "another" are all rules-grounded (CR 508/509, CR 109.5 convention).

## Process notes

- Citation convention (not a defect of this branch): the branch's doc-comment cites **CR 109.5 for "another" → `ExcludeSelf`**. CR 109.5 literally governs the words "you"/"your" (= controller), not "another". However this is an **established, corpus-wide convention** — the core `libs/magic-ast/AST/References/ObjectFilterRelations.cs:505` documents `ExcludeSelf (CR 109.5 "another")` verbatim, and `ObjectFilter.cs` references CR 109 for the same axis. The branch faithfully follows it; the primary mechanic (evasion) is correctly cited at 509.1b, and the imprecise secondary aside is in-data and non-contradictory to the modeling, so per the judge's citation rule (FAIL only on absent-from-data or contradictory) it does not FAIL. Surfacing it as a **corpus-wide citation-cleanup candidate**: MTG has no dedicated numbered rule for "another" (self-exclusion); the codebase-wide 109.5 reference is a mild misattribution that should be corrected globally, not patched on this one branch.
