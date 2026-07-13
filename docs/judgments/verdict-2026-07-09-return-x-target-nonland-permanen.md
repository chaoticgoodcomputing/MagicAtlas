# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** return-x-target-nonland-permanen
**Branch:** tdd/return-x-target-nonland-permanen
**Scope:** 2 files (1 fixture, 1 AST/parser node) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ARB/DistortingWake.json` — PASS. "Return X target nonland permanents to their owners' hands" → `Kind:spell` + `returnToHand` with `Target{Quantity:VariableQuantity X, Filter:{CardTypes:[permanent], ExcludedCardTypes:[land]}}`. Descriptively faithful: X target (CR 107.3), nonland permanents (excluded card type = land), return to owners' hands (CR 400.3 / 402.1), targets declared (CR 115.1). No `IUnparsed`, no `UnstructuredEffect`, no `OtherX`, no free-text — fully structured. `Input.OracleText` byte-identical to `Output.Oracle.RawText` (straight ASCII apostrophe 0x27, matching Scryfall "owners'"). ManaCost `{X}{U}{U}{U}`, colors/identity U all correct. Matches the established sibling convention (Alexi, Zephyr Mage: plural type → singular `CardTypes`, variable X → `VariableQuantity`).
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ReturnCountTargetPermanentsToOwnersHandsSpellRule.cs` — PASS. Spell-side sibling of `ReturnCountTargetPermanentsToOwnersHandsEffectRule`; anchored `^Return <count> target [mod] <plural-type> to their owners' hands$`, disjoint from the singular-target `ReturnTargetToHandRule`. Reuses `ReturnToHandEffect` (`returnToHand`), `ObjectReference.Quantity` (`VariableQuantity`), and pre-existing `ObjectFilter.ExcludedCardTypes`/`Colors` via `QualifierAxisMapper` — no bespoke count field, no new node. All four doc-comment citations cross-check verbatim against `rules-structure.json`: CR 107.3 (X placeholder), CR 400.3 (owner's zone), CR 402.1 (hand is a zone), CR 115.1 (targets declared on the stack). No citation is absent or contradictory.
- `tdd/return-x-target-nonland-permanen#projection` — PASS. No new discriminator (effect/cost type, trigger event, or restriction) is introduced: the rule reuses the existing `returnToHand` effect, `ObjectReference.Quantity`, and the pre-existing `ObjectFilter.ExcludedCardTypes` (present in base `ObjectFilter.cs`). No PortWalk projection decision is required, and none is expected by the ratchet.

## Glossary gaps

None. "permanent" (CR 110), "owner" (CR 108), "hand" (CR 402), and "target" (CR 115) are all standard rules terms; no novel MTG-domain vocabulary introduced.

## Process notes

- Branch touches exactly two files (new parser rule + new gold fixture); `shared=[]` confirmed — no shared AST generalization to audit.
- `CardTypes:["permanent"]` treats "permanent" as a filter token rather than a strict CR 300.1 card type. This is a pre-established codebase convention shared by the activated sibling (which whitelists "permanents" in its plural-type alternation) and is descriptively adequate here ("nonland permanents" = `CardTypes:[permanent]` minus `ExcludedCardTypes:[land]`). Whether "permanent" warrants a dedicated filter axis is a structural question for the engine-lens audit, not a rules-accuracy FAIL.

ALL PASS
