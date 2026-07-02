# MAST judge — batch verdict (delta: stasis-cocoon)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-stasis-cocoon
**Scope:** 1 fixture (tests/magic-ast-tests/Fixtures/HandParsedCards/5DN/StasisCocoon.json) + regex generalization in EnchantedCantAttackOrBlockRule.cs
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/5DN/StasisCocoon.json` — PASS. Oracle text confirmed byte-for-byte against oracle-cards.json ("Enchant artifact\nEnchanted artifact can't attack or block, and its activated abilities can't be activated."). Two oracle lines → two static abilities. Line 1 = Enchant static (KeywordSource "Enchant", enchantRestriction, LegalTargets.CardTypes=["artifact"]) — faithful to "Enchant artifact." Line 2 = the compound restriction, correctly decomposed into three separate effect nodes (cantAttack, cantBlock, cantActivateAbilities), each Target.Kind=EnchantedOrEquipped, under Kind:static. Describe-not-execute, no baked-in timing, no free-text/unparsed residual. Cited CR rules 604.1/604.2 (static abilities), 602.5 (can't begin to activate a prohibited ability), 508.1 (declare attackers restriction), 509.1 (declare blockers restriction) all exist in rules-structure.json and match the modeling.

- `mast-tdd/2026-07-02-stasis-cocoon#projection` — PASS. No new discriminator introduced: cantAttack, cantBlock, cantActivateAbilities, and enchantRestriction all pre-exist on base sha 539b20a8. The branch only widens the rule's subject-noun regex token from `creature` to a closed permanent-type alternation. No PortWalk projection decision is required.

## Regression check

Diff touches exactly two files: the rule's regex (subject-noun generalization, Target=EnchantedOrEquipped is noun-independent) and the new gold fixture. No existing fixtures modified; siblings preserved; three restriction sub-effects all present, none dropped/added/inverted; out-of-axis nodes (manaCost/colors/colorIdentity attributes, TypeLine) unchanged.

## Glossary gaps

(none)

## Process notes

CR citations were verified live against libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json; all five present with matching text.

ALL PASS
