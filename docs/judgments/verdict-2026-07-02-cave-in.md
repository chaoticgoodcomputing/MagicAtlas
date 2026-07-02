# MAST judge — batch verdict (cave-in)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-cave-in
**Scope:** 1 fixture (CaveIn.json) — delta-judge of the pitch alternative-cost line
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `CaveIn.json#alternativeCosts` — PASS. "You may exile a red card from your hand rather than pay this spell's mana cost." (oracle text verified against oracle-cards.json) is hosted as an `AlternativeCostsAttribute` carrying `AlternativeCost{ Cost: ExileCost, Filter{ CardTypes:[card], Colors:[R] }, Quantity:1, FromZone:Hand }`. Right node (`exile` cost / `alternativeCosts` attribute), faithful to the card, describe-not-execute, no baked-in timing. CR 118.9 ("You may [action] rather than pay [this object's] mana cost" is an alternative cost) + CR 118.9a (only one alternative cost per spell) + CR 604.5 (functions while on the stack) all exist verbatim in rules-structure.json and match the modeling. Color encoded as genuine "R" (not the CR 105.1 colorless anti-pattern).
- `CaveIn.json#residual` — PASS. No `unparsed`/free-text residual anywhere in Abilities. Cost line is consumed by AttributeExtractor and skipped in ClauseSplitter (prevents a spurious UnparsedAbility). The damage line "deals 2 damage to each creature and each player" is fully structured as a `composite` of `dealDamage{Each creature}` + `dealDamage{EachPlayer}`. Out-of-axis siblings (manaCost, colors, colorIdentity) preserved; new fixture so no prior-fixture regression.
- `mast-tdd/2026-07-02-cave-in#projection` — PASS (N/A). No new discriminator is introduced: the branch adds only a parser path (regex extractor + clause-skip) emitting the pre-existing `exile` cost and `alternativeCosts` attribute (same shape as Bestow / the Borderpost cycle). No new `PortGraph` case or `known-coarse-projections.json` entry is required.

## Glossary gaps

None.

## Process notes

Oracle text confirmed exact against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`. Diff touches only AttributeExtractor.cs, ClauseSplitter.cs, and the new CaveIn.json fixture — no other fixtures or AST node files affected. The `CardTypes:["card"]` token in the exile filter denotes "any card" (the restriction that carries rules weight, "red", lives in `Colors:[R]`); this is an established convention, not a free-text shortcut.

ALL PASS
