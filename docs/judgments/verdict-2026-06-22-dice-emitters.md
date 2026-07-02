# MAST judge — batch verdict

**Date:** 2026-06-22
**Scope:** dice-emitter round — 7 fixtures, 2 schema additions (DiceRollEvent + DieResultValues), 1 projection decision
**Result:** PASS

## Summary

- PASS: 11
- FAIL: 0

Base for diffs: `d92f0d7d`. Oracle text verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (never from memory). All CR citations cross-referenced against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`.

## FAIL verdicts

None.

## PASS verdicts

### Family 1 — Activated roll outlet
- `tests/magic-ast-tests/Fixtures/HandParsedCards/WillingTestSubject.json` — PASS. Activated `{6}:` ability effect is `rollDie {Sides:6}` (reuses RollDieEffect, CR 706.1). Reach static and the "whenever you roll a 4 or higher" trigger (`DieResultThreshold:4`, the minimum form) firing `putCounters +1/+1 Count:1` ("a +1/+1 counter" = literal 1, correct — no fabricated die-result count) all parse faithfully.

### Family 2 — whenever-you-roll payoffs (exact value)
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ComplaintsClerk.json` — PASS. "Whenever you roll a 1" → `DieResultValues:[1]` (exact-value set), correctly distinct from the minimum `DieResultThreshold`. Effect `createToken` 1/1 white Clown Robot artifact creature is faithful; ETB `openAttraction` + reminder preserved. CR 706.2 / 706.7.
- `libs/magic-ast/AST/Triggers/TriggerCondition.cs#DieResultValues` — PASS. New `IReadOnlyList<int>?` is a sound enumerated-set qualifier; the doc-comment correctly frames it as mutually exclusive with `DieResultThreshold` (a *minimum*, "a 4 or higher") vs an *exact match against a set* ("a 1", "a 1 or 2"). Not a new PortWalk discriminator (it refines the already-projected `DiceRolled` trigger event), so no separate projection decision is required. CR 706.2 / 706.7 resolve and match.

### Family 3 — Dice advantage replacement
- `tests/magic-ast-tests/Fixtures/HandParsedCards/PixieGuide.json` — PASS. "If you would roll one or more dice, instead roll that many dice plus one and ignore the lowest roll" modeled as `Kind:static` replacement: `Event=diceRoll` (MinimumQuantity:1, Controller You), `OriginalEventOccurs:false`, `Modifier.Type="advantage"`. Sound per CR 614.1a ("instead" → replacement effect). Flying + "Grant an Advantage" ability word (CR 207.2c) faithful.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/WyllBladeOfFrontiers.json` — PASS. Same advantage replacement (no ability-word header — correct, Wyll prints the bare line). The "whenever you roll one or more dice" trigger correctly carries NO result qualifier (fires on any roll). Choose-a-Background partner faithful.
- `libs/magic-ast/AST/Effects/Replacement/DiceRollEvent.cs` — PASS. New `diceRoll` replacement-event discriminator is faithful and non-duplicative (mirrors `MillEvent`/`CounterPlacementEvent` MinimumQuantity parity). Cites CR 706.1 / 614.1 — both resolve and match the modeling.
- `libs/magic-ast/AST/Effects/Replacement/ReplacementModifier.cs#advantage` — PASS. Keeping "advantage" as an atomic `Modifier.Type` (roll N+1, ignore lowest) is acceptable — CR 706.6 ("an ignored roll is treated as never having happened") is the load-bearing rule and it resolves. This is an atomic template (a value of the existing `Type` string), not rules-meaningful free text and not a new discriminated-union case, so no decomposition is required.

### Projection decision (initiative 03)
- `libs/mast-interaction#OracleReplacementEvent:diceRoll-projection` — PASS. The new `diceRoll` event is *semantically projected* through the generic replacement path (`PortGraph.cs:548` → `intercept:replacement:diceRoll`), and the engine's Modifier edge (`PortGraphEngine.cs:209-212`) draws an edge from any `emit:rolldice` to it — exactly the right semantics (an advantage shield modifies roll emissions). The advantage modifier carries no inner roll-emitting effect (verified: no `Replacement` key in either gold), which is correct: it is a shield, not a roll outlet, so it sensibly emits nothing. Present and sensible — not parked in `known-coarse-projections.json`.

### Family 4 — ETB roll-result-spend
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AdorableKitten.json` — PASS. ETB → `[rollDie Sides:6, gainLife {Amount: dieRollResult}]`. "the result" is `dieRollResult`, not a literal (CR 706.2). CR 706.4 / 119.3 resolve and match.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/DissatisfiedCustomer.json` — PASS. ETB → `[rollDie Sides:6, conditional{ quantityComparison(dieRollResult LessThanOrEqual 3), Then: loseLife {dieRollResult} }]`. "If the result is 3 or less, you lose that much life" — the gate and the spent amount both bind to `dieRollResult`. Flying + haste faithful.

### Family 5 — Attack-roll P/T pump
- `tests/magic-ast-tests/Fixtures/HandParsedCards/VelukanDragon.json` — PASS. "Whenever this creature attacks or blocks" → `AttacksOrBlocks` compound trigger PRESERVED (not split/dropped). Effect `[rollDie, modifyPT PowerModifier=calculated(dieRollResult add -1), ToughnessModifier=0, Duration untilTime Turn/End]`. "X is the result minus 1" → additive offset of -1 over the die result; "+X/+0" and the until-end-of-turn duration are faithful. CR 706.2 / 706.4.

## Glossary gaps

None. `DiceRollEvent`, the `advantage` modifier, and `DieResultValues` are documented in `libs/magic-ast/GLOSSARY.md` with matching CR citations. The MTG Comprehensive Rules glossary (`glossary.json`) does not index "advantage"/"roll a die" as terms, but CR 706 + 614.1 cover the mechanics directly and were cross-referenced.

## Process notes

- The earlier dice round already landed the interaction-flow machinery (`emit:rolldice` / `trigger:rolldice`, the `rollDie` and `DiceRolled` PortWalk projections); `libs/mast-interaction/` has NO diff in this batch. The only new PortWalk-facing discriminator here is `OracleReplacementEvent:diceRoll`, judged above.
- The commit message surfaces three out-of-scope follow-ups (NOT failed here): (1) no "put N counters equal to the die result" effect rule — none of the 7 golds depend on it (Willing Test Subject's counter is a literal `Count:1`); (2) no d20 results-table AST — no gold here uses one; (3) Strength-Testing Hammer's pre-existing fabricated `drawCards{6}` — out of batch per dispatch. Mother Kangaroo / Strength-Testing Hammer are explicitly out of scope and were not used to fail this batch.

**Result: ALL PASS.**
