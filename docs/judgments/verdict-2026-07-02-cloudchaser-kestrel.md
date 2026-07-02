# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** cloudchaser-kestrel
**Branch:** mast-tdd/2026-07-02-cloudchaser-kestrel
**Scope:** 1 fixture (color-change axis) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/PLC/CloudchaserKestrel.json#changeColor` — PASS. Oracle `{W}: Target permanent becomes white until end of turn.` verified verbatim against oracle-cards.json. Modeled as an `activated` ability: cost `{W}` (mana, colored W), `IsManaAbility: false`; effect `changeColor` with `Target` filter `CardTypes: ["permanent"]` (faithful to "Target permanent"), `Colors: ["W"]`, and `Duration untilTime → Turn/End`. Layer-5 color-changing continuous effect, CR 105.3 — rule exists in rules-structure.json and its text matches the modeling and the node's verbatim doc-comment. Describe-not-execute; the "until end of turn" is carried as a proper Duration field, not baked into the effect discriminator. No free text, no unparsed residual. Siblings on other axes preserved and correct (Flying static evasion; ETB `destroy` target enchantment triggered ability). Attributes intact.
- `mast-tdd/2026-07-02-cloudchaser-kestrel#projection` — PASS. Branch introduces no new discriminator: `changeColor` (EffectType) pre-exists (MetathranTransport, discriminator-baseline.json). The change generalizes the recognizer's target noun and threads it into `CardTypes` — "permanent" is a filter value, not a new effect/cost/trigger/restriction discriminator. `changeColor` already carries a coarse projection entry in `known-coarse-projections.json` ("baseline coarse fallback — no flow rule consumes it yet"), which is sensible: a layer-5 color-change is inert for interaction/combo flow.

## Glossary gaps

(none)

## Process notes

- Parser change (`ChangeColorEffectRule.cs`) broadens the regex from `Target creature becomes ...` to `Target (creature|permanent) becomes ...` and threads the captured noun into the filter. Backward-compatible with the creature-noun path (MetathranTransport); parser/test greenness is NUnit's job, out of judge scope.
- Base sha judged: 176e495dda71494b915330f72bde000e5cd90f0f.

## Result

ALL PASS
