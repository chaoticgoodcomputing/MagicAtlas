# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 4 files (1 fixture, 1 AST Quantity node, 2 SpellRule parsers) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ONS/HarmonyOfNature.json` — PASS. Input.OracleText is byte-identical to Scryfall ("Tap any number of untapped creatures you control. You gain 4 life for each creature tapped this way."); ManaCost {2}{G}, TypeLine Sorcery, colors [G] all correct. Gold decomposes into two sibling effects with no unparsed/free-text/lossy nodes: (1) `tap` with `Target.Kind = Any` (indefinite controller choice, correctly NOT a target per CR 115.1 — no "target" keyword), an ObjectFilter of `creature` + `tapped:false` + `Controller:You` (structured "untapped creatures you control", mirroring the tapped-creature filter shape used elsewhere), and `Count = anyAmount` ("any number", controller-chosen — CR 107.3); (2) `gainLife` with `Amount = 4 x creaturesTappedThisWay` (CalculatedQuantity multiply/4 over the this-way back-reference), `Player = You`. Semantics faithful to CR 701.26a (tap) and CR 119.3 (life gain).
- `libs/magic-ast/AST/Quantities/CreaturesTappedThisWayQuantity.cs` — PASS. Field-less Quantity `[OracleQuantity("creaturesTappedThisWay")]`, the tap sibling of the existing `CardsRevealedThisWayQuantity`; ADR-0004 reference-not-resolution (records the textual "this way" link, engine resolves the count). Cited CR 701.26a matches rules-structure.json verbatim.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/TapAnyNumberOfUntappedCreaturesYouControlRule.cs` — PASS. Anchored parser producing the sound TapEffect above. Cited CR 701.26a (verbatim), CR 115.1 (correctly grounds Any-not-Target), and CR 107.3 (controller-chosen value analogy for anyAmount) all exist and are consistent with the modeling.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/GainLifeForEachCreatureTappedThisWayRule.cs` — PASS. Anchored parser producing GainLifeEffect(N x creaturesTappedThisWay), collapsing to the bare count at N=1 (mirrors GainLifeForEachPermanentSpellRule). Cited CR 119.3 matches verbatim.
- `mast/tap-any-number-untapped-creatures` projection decision — PASS. The branch introduces no new projected discriminator: the only new node is `creaturesTappedThisWay`, a Quantity, which lies outside the exhaustiveness ratchet's four dimensions (effectType / costType / triggerEvent / restriction — confirmed in PortWalkExhaustivenessTests). The two EffectType discriminators it participates in, `tap` and `gainLife`, are both pre-existing and already carry semantic projections (`tap` -> tap:self cost; `gainLife` -> emit:life:gain life-flow arm). No coarse parking, no ratchet obligation triggered — sensible.

## Glossary gaps

None. "tap" / "untap" / "life" are standard and present in glossary.json.

## Process notes

- Oracle text verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (exact match), not memory.
- All four cited CR rules cross-referenced against rules-structure.json: 701.26a, 119.3, 115.1, 107.3 all present; texts consistent with modeling. CR 107.3 is invoked as a controller-chooses-a-value analogy for `anyAmount` ("any number") rather than an exact keyword rule — loose but not contradictory, so not a FAIL.
- "Untapped" is modeled structurally as a `tapped` characteristic with `Tapped=false`, not free text — correct.
