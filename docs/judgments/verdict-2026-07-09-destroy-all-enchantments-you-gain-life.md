# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 3 files (1 fixture, 2 AST/parser nodes) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/USG/MultanisDecree.json` — PASS. Input.OracleText byte-identical to Scryfall ("Destroy all enchantments. You gain 2 life for each enchantment destroyed this way."); ManaCost {3}{G}, TypeLine Sorcery, Colors/ColorIdentity [G] all match. Sentence 1 -> DestroyEffect Target{Kind:Each, Filter CardTypes:[enchantment]} (CR 701.8a). Sentence 2 -> GainLifeEffect Player:You, Amount = CalculatedQuantity(multiply, Operand 2, BaseQuantity objectsDestroyedThisWay{Filter CardTypes:[enchantment]}) = 2 x (enchantments destroyed this way) (CR 119.3). No `unparsed`, no `UnstructuredEffect`, no free text, no lossy drop/merge; both sentences captured with correct semantics.
- `libs/magic-ast/AST/Quantities/ObjectsDestroyedThisWayQuantity.cs` — PASS. Cited CR 701.8a ("To destroy a permanent, move it from the battlefield to its owner's graveyard.") exists in rules-structure.json and matches the modeled destroy count. Carries `Filter: ObjectFilter` to name the counted noun (varies across family), a structured reference not free text; reference-not-resolution per ADR 0004, consistent with the CardsRevealedThisWayQuantity / CountersRemovedThisWayQuantity siblings.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/GainLifeForEachDestroyedThisWaySpellRule.cs` — PASS. Cited CR 119.3 ("If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly.") exists and matches. Anchored (^...$) so it cannot poach the sibling "for each ... you control" / bare "you gain N life" sentences; emits GainLifeEffect and wraps rate>1 in CalculatedQuantity multiply exactly as GainLifeForEachPermanentSpellRule does.
- `mast/destroy-all-enchantments-you-gain-life#projection` — PASS. The only new discriminator is a Quantity type (`objectsDestroyedThisWay`), not an effect/cost/trigger/restriction, so it creates no new port label; PortGraph.Qty() routes any non-literal/fixed QuantityType through the default arm to symbolic (null) multiplicity, degrading gracefully with no ratchet entry needed. The port-producing effects (gainLife, destroy) are already projection-accounted — gainLife has a semantic PortWalkProjection (emit:life:gain) and destroy has a justified known-coarse entry — neither introduced by this branch. Projection decision present and sensible.

## Glossary gaps

(none)

## Process notes

- Worker reported shared=[]; the diff confirms 3 net-new files and zero edits to shared/existing nodes, so there are no shared generalizations to review.
- ObjectsDestroyedThisWayQuantity intentionally carries a `Filter` while its field-less "this way" siblings (cardsRevealedThisWay) do not; the doc-comment justifies this (the counted noun varies across the destroy family). This is a design choice, not a rules-accuracy issue, and introduces no free text.
