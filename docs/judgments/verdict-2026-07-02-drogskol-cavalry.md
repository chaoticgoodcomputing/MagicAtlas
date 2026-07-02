# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** drogskol-cavalry
**Branch:** mast-tdd/2026-07-02-drogskol-cavalry
**Scope:** 1 fixture (delta-judge of the activated token-creation line)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SOI/DrogskolCavalry.json` — PASS. Oracle text verified verbatim against oracle-cards.json ("{3}{W}: Create a 1/1 white Spirit creature token with flying."). Target activated ability modeled as mana Costs ({3}+{W}) + a plain `createToken` effect (describe-not-execute; timing carried by the activated-ability wrapper, not baked into the effect). Token: Power/Toughness 1/1, Colors `["W"]` (white is a color per CR 105.1 — correctly NOT `["C"]`), Types `["creature"]`, Subtypes `["Spirit"]`, IsCopy false. The "with flying" clause is a granted static evasion ability (`CanBeBlockedBy` creature with Flying/Reach), structurally identical to the card's own top-level Flying and to the GhostlyDancers white-Spirit-flying-token precedent (CR 111.3: token abilities are defined by the creating effect). No free-text `Characteristics` strings, no `unparsed` nodes anywhere. Siblings preserved and sensible: Flying static evasion, and the "Whenever another Spirit you control enters, you gain 2 life" gainLife trigger.

## Citations cross-referenced

- CR 105.1 — "There are five colors in the Magic game: white, blue, black, red, and green." Present; grounds Colors `["W"]`.
- CR 111.1 — tokens. Present.
- CR 111.3 — token characteristics/abilities defined by the creating effect. Present; grounds the granted flying ability on the token.
- CR 701.7a — "put the specified number of tokens with the specified characteristics onto the battlefield." Present; grounds createToken.
- CR 702.9a/b — "Flying is an evasion ability." / "can't be blocked except by creatures with flying and/or reach." Present; exactly grounds the evasion `CanBeBlockedBy` shape.

All cited rules exist in rules-structure.json and match the modeling.

## Projection decision (initiative 03)

Not triggered. The new parser rule `CreateColoredCreatureTokenWithKeywordEffectRule` emits the pre-existing `createToken`/`TokenDefinition`/`evasion` AST nodes — it introduces no new effect/cost-type discriminator, trigger event, or restriction — so no new PortWalk projection decision is required.

## Glossary gaps

(none)

## Process notes

- Out-of-axis observation (NOT a fail for this task): the "Whenever another Spirit…" trigger filter (creature + Spirit + Controller You) does not encode the self-exclusion implied by "another". This is a trigger-filter-restriction axis owned by another task, not a free-text/unparsed residual, and not the target activated line — outside this delta-judge's scope.

**PROCEED** — FAIL count 0.
