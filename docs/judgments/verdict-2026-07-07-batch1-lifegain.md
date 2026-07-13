# MAST judge — batch verdict

**Date:** 2026-07-07
**Batch:** batch1-lifegain
**Branch:** `mast-tdd/2026-07-07-lifegain-replacement` (base `b77e3912`)
**Scope:** 4 changed files — 2 parser rules, 1 gold fixture (7 judged surfaces incl. projection), 1 whitelist entry
**Result:** PASS

## Summary

- PASS: 7
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `MH3/PestRescuer.json#static-replacement` — PASS. "If you would gain life, you gain that much life plus 1 instead." modeled as `Kind: static` `ReplacementEffect` (CR 614.1 — a continuous replacement ability, NOT a triggered "whenever you gain life"). `Event = LifeChangeEvent{ChangeType: gain, Controller: You}` (CR 119.3), `OriginalEventOccurs: true` with `Modifier = ReplacementModifier{Type: plus, Amount: literal 1}`. Augmentation semantics are correct ("that much life" preserves the original gain; "plus 1" augments), and the +1 is a typed quantity, not free text.
- `LifeGainAugmentationReplacementRule.cs` — PASS. CR 614.1 and CR 119.3 both exist in `rules-structure.json` and match the modeling verbatim. Anchored regex; mirrors `SpellCopyAugmentationReplacementRule`'s `OriginalEventOccurs = true` augmentation pattern; carries "plus N" as a typed modifier so a "plus 2" variant reuses the shape.
- `MH3/PestRescuer.json#token-triggered-ability` — PASS. The token's granted `"When this token dies, you gain 1 life."` is decomposed into `Trigger{Timing: When, Event: Dies, Filter{CardTypes:[creature], IsSelf:true}}` PLUS a plain `GainLifeEffect{Amount: literal 1, Player: You}` — trigger and effect are separate nodes (CR 603.1), no timing baked into the effect. "Dies" matches the glossary / CR 700.4.
- `CreateTokenWithDiesGainLifeAbilityRule.cs` — PASS. Reuses the shared base-token helpers so the token's P/T, colors, subtypes, types match `CreateTokenRule` exactly, then attaches the ability the generic rule had dropped. Cited CR 119.3 exists and matches.
- `whitelist-freetext.json#MH3/PestRescuer` — PASS (justified carve-out). The parallel `ISD/VillagersOfEstwaldHowlpackOfEstwald` `OtherCondition` PB-7 debt entry genuinely exists; that fixture's gold uses the identical `"At the beginning of each upkeep, if <cond>, …"` trigger shape with `InterveningIf{ConditionType: "other", Text: …}` free-text condition. Pest Rescuer's `"you don't control a Pest creature token"` is the same structural class of intervening-if gate with no structured-condition arm yet — parked, not hidden.
- `MH3/PestRescuer.json` (whole gold) — PASS. Both card lines present (triggered upkeep ability + static replacement); no `unparsed` / `UnparsedEffect` / `Diagnostics` nodes anywhere; the only `Raw` strings are verbatim-by-design characteristic fields (TypeLine, ManaCost, P/T). No dropped sibling effect; no describe-vs-execute conflation.
- `mast-tdd/2026-07-07-lifegain-replacement#projection-decision` — PASS (no decision required). No new PortWalk-tracked discriminator (effect/cost type, trigger event, restriction) is introduced — the work reuses `replacement` / `lifeChange` / `gainLife` / `createToken` / `Dies`. The one genuinely-new value, `ReplacementModifier.Type = "plus"`, is a leaf-level typed amount-scaling descriptor living inside an already-projected `ReplacementEffect`; it is inert to any flow rule, so a coarse/no-op treatment is sensible and no semantic `PortGraph` case is warranted. The diff touches no PortGraph / PortWalk / known-coarse files.

## Glossary gaps

None. "Replacement Effect" (→ CR 614) and "Dies" (→ CR 700.4) are present in `glossary.json`; life-adjustment is covered by CR 119.3.

## Process notes

- Citation cross-reference: CR 614.1, CR 119.3, CR 603.1 all present in `rules-structure.json` with text matching the modeling.
- Minor internal-consistency observation (not a defect): `ReplacementModifier.Type = "plus"` is a new enum value (existing siblings are `plusOne`/`plusX`/`double`/`triple`/`advantage`). It is a cleaner typed generalization ("plus" + `Amount`) rather than a hardcoded `plusOne`, contradicts no rule, and is not free text — so it is descriptively fine and not PortWalk-relevant.

## Result

ALL PASS — PROCEED.
