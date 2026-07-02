# MAST judge — batch verdict (surging-might)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-surging-might
**Base:** 4618d17338a822bf84df3140d62dfe77a15a617c
**Scope:** 1 fixture (SurgingMight.json — target line: Ripple 4) + 1 projection decision (ripple discriminator)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/CSP/SurgingMight.json#ripple` — PASS. Oracle text matches oracle-cards.json verbatim. "Ripple 4 (...)" is modeled as `Kind: static`, `KeywordSource: "Ripple"`, `Effects: [{ EffectType: "ripple", Value: 4 }]` plus a verbatim `Reminder`. This is the codebase's established parameterized-keyword-presence convention — identical shape to Fabricate (`KLD/MaulfistSquad.json`: `Kind: static` + `KeywordSource: "Fabricate"` + `Value: 1`), and Fabricate is likewise a CR-triggered keyword recorded via the `static` presence container. The effect discriminator is the bare keyword name `ripple` (no `rippleOnCast`-style baked-in timing); the reveal/free-cast/bottom machinery is consciously deferred to the engine, consistent with describe-not-execute. CR 702.60a exists in rules-structure.json and its text matches the doc-comment and reminder verbatim. The two sibling abilities are faithful: "Enchant creature" → `enchantRestriction` LegalTargets CardTypes creature; "Enchanted creature gets +2/+2" → `modifyPT` EnchantedOrEquipped +2/+2. No `unparsed` nodes, no rules-meaningful free text (reminder is verbatim-by-design and exempt).

- `libs/mast-interaction/known-coarse-projections.json#ripple` — PASS. The branch adds a new `ripple` effect discriminator and registers a coarse projection with a plausible reason. Sensible: the RippleEffect node records only keyword presence + integer N — it emits no structured free-cast/cast sub-events for a flow rule to read — so there is no fine-grained projection to make, exactly as with the cited sibling keywords (devour/fabricate/amplify/graft). Genuinely inert for interaction recall; coarse is the correct call, not a parked flow-relevant discriminator.

## Glossary gaps

(none — "Ripple" is a CR 702.60 keyword; no undefined domain term introduced.)

## Process notes

- Cross-referenced: CR 702.60 ("Ripple") and 702.60a present in `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`; reminder text is a verbatim match of 702.60a's quoted expansion. `KeywordAbility.Ripple` enum and `RippleEffect` (Value field) added; `ast-schema.json` registers discriminator `ripple` with `IsUnparsed: false`, Fields `["Value"]`. All consistent.

ALL PASS
