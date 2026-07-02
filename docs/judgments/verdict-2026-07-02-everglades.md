# MAST judge — batch verdict (everglades)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-everglades
**Base:** 1526dd74fd92af29b86588b620ae5405cf8de511
**Scope:** 1 fixture (Everglades) + 1 parser rule (SacrificeUnlessReturnUntappedLandTriggeredRule.cs); target axis = Karoo ETB "sacrifice it unless you return an untapped <basic> to its owner's hand"
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Everglades.json` — PASS. Oracle text matches oracle-cards.json verbatim ("This land enters tapped. / When this land enters, sacrifice it unless you return an untapped Swamp you control to its owner's hand. / {T}: Add {C}{B}."). The target line decomposes cleanly into a `Trigger{Timing:When, Event:Enters, Filter:{land, IsSelf}}` (timing/event separated from action — no baked-in timing) plus a `preventable` effect whose `Inner` is `sacrifice(Target:It)` and whose `Unless` is `{Player:You, Cost: returnToHand of an object filtered CardTypes:[land], Subtypes:[Swamp], Controller:You, Characteristics:[tapped=false]}`. "untapped" is structured as a `tapped:false` characteristic (CR 110.5), "Swamp" as a basic land subtype (CR 305.6), the bounce as `returnToHand` to owner's hand (CR 108.3), the self-destruction as `sacrifice` (CR 701.21a). Correctly uses `It` for the pronoun "sacrifice it" (referring to the trigger subject) rather than `Self` — consistent with the sibling `SacrificeUnlessPay` convention (BreedingPit uses `Self` for the explicit "this enchantment"). No free-text residual, no `unparsed` node/effect. Siblings preserved: enters-tapped static ability (`When:asThisEnters` + plain `tap` effect, decomposed not swallowed) and the `{T}: Add {C}{B}` mana ability (`IsManaAbility:true`); colors/colorIdentity attributes intact. Describe-not-execute throughout.
- `mast-tdd/2026-07-02-everglades#projection` — PASS. The branch adds a parser rule + fixture that compose only pre-existing AST nodes (`PreventableEffect`/`UnlessClause`, `SacrificeEffect`, `ReturnToHandCost`, `TappedStateCharacteristic`); the `preventable`, `sacrifice`, `returnToHand`, and `tapped`/`untapped` discriminators already appear across existing gold fixtures. No new effect/cost type, trigger event, or restriction is introduced, so no PortWalk projection decision (semantic `PortGraph` case or `known-coarse-projections.json` entry) is required.

## Glossary gaps

_none_

## Process notes

- Citation cross-reference: `CR 701.21a` (Sacrifice — "its controller moves it from the battlefield directly to its owner's graveyard"), `CR 305.6` (basic land types Plains/Island/Swamp/Mountain/Forest), `CR 110.5` (tapped/untapped status), and `CR 108.3` (ownership → "its owner's hand") all exist in rules-structure.json and match the modeling.
- The `.cs` doc-comment additionally hedges the "unless" clause as "CR 117.7-adjacent (paying a cost to avoid an effect)." CR 117.7 exists but its actual text is about casting "in response to" spells/abilities on the stack — not about alternative/preventive costs. This is an imprecise, self-flagged ("-adjacent") secondary note in a code comment, not the load-bearing structural citation and not present in the fixture; it neither contradicts the modeling nor blocks the fixture verdict. Recommend dropping or replacing the 117.7 reference in a future touch, but it is not a FAIL.
