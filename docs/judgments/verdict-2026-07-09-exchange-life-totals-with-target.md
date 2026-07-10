# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/exchange-life-totals-with-target
**Base:** 6b4b1d17083f7b580b0223119b7a0fdea73d7d30
**Family:** exchange-life-totals-with-target — "Exchange life totals with target opponent. Activate only during your upkeep." (Magus of the Mirror)
**Scope:** 5 targets (1 fixture, 1 new AST discriminator, 1 new effect rule, 1 shared parser edit, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/FUT/MagusoftheMirror.json` — PASS. Input.OracleText byte-identical to Scryfall oracle-cards.json; mana `{4}{B}{B}`, P/T 4/2, colors B all match. Gold ability decomposes the whole line with no residue: costs `{T}` (tap) + `Sacrifice this creature` (sacrifice, self creature, Quantity 1); effect `exchangeCharacteristic`/`LifeTotals` with `First: You` / `Second: Opponent` per CR 701.12a ("exchange something … for example, life totals") and 701.12c ("each player gains or loses the amount of life necessary to equal the other player's previous life total"); restriction `OnlyDuringYourUpkeep`. No IUnparsed, no UnstructuredEffect, no free text, no lossy drop/merge. `IsManaAbility: false` correct.
- `libs/magic-ast/AST/Abilities/ActivatedAbility.cs#OnlyDuringYourUpkeep` — PASS. New `ActivationRestriction` enum value with doc-comment citing CR 602.5 ("A player can't begin to activate an ability that's prohibited from being activated") — the rule exists in rules-structure.json and matches the modeling. Correctly documented as strictly narrower than `OnlyDuringYourTurn`.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/ExchangeLifeTotalsEffectRule.cs` — PASS. Reuses the pre-existing `ExchangeCharacteristicEffect` and its `ExchangeableCharacteristic.LifeTotals` facet (both present on base) — no new effect discriminator introduced. Doc-comment cites CR 701.12a/701.12c, both present verbatim in the rules data and matching the modeling. Anchored (`^…$`) regex means it cannot claim a substring of a larger "exchange …" sentence. "target opponent" carried by `ObjectReferenceKind.Opponent`, whose own enum doc-comment lists "an opponent"/"target opponent" — consistent with established codebase convention (Blood Tribute precedent).
- `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs` — PASS. Sound, conservative generalization: matches only the two canonical whole-string phrasings ("activate only during your upkeep" / "activate this ability only during your upkeep") after `Trim().TrimEnd('.').ToLowerInvariant()`. Anchored equality (not `Contains`) so a compound restriction like "…and only once each turn" will not be swallowed with its trailing sibling silently dropped.
- `libs/mast-interaction/known-coarse-projections.json#OnlyDuringYourUpkeep` — PASS (projection decision, initiative 03). The new discriminator's projection is present and parked as a justified coarse entry. Rationale is sensible: a step-scoped timing restriction does not gate an intra-window loop's firability (a loop confined to the upkeep step can still repeat), so it is genuinely inert for recall — mirroring `OnlyDuringYourTurn` / `OnlyAsSorcery`, which are already coarse. Not something a flow rule would clearly want; the coarse choice is correct.

## Glossary gaps

None. "Exchange" is present in glossary.json (cites rule 701.12); "upkeep" timing is covered by CR 602.5 / 500-series steps.

## Process notes

- CR cross-reference: 701.12a, 701.12c, and 602.5 all confirmed present in `rules-structure.json` with text matching the modeling. Glossary "Exchange" confirmed.
- The `ExchangeCharacteristicEffect` base type is `ContinuousEffect` (pre-existing), whereas an exchange of life totals resolves as a one-shot per CR 701.12c. That base-class categorization is a pre-existing AST-family concern (engine-lens audit territory), not introduced by this branch, and does not affect the descriptive fidelity of the reused node — out of scope for this verdict.
- "newAstNode=true" in the dispatch refers to the `OnlyDuringYourUpkeep` restriction discriminator; the effect side reuses existing structure.

ALL PASS
