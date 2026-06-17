# MAST judge — PB-3 delta verdict: AdeptWatershaper

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (atomic: structured-characteristic axis + comparative-power)
**Scope:** 1 gold (delta judgment)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## PASS verdicts
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AdeptWatershaper.json` — PASS.
  Real oracle (Scryfall): "Other tapped creatures you control have indestructible."
  - (a) TARGET structured correctly: the `tapped` residual moved from free-text
    `{CharacteristicType:"other", Description:"tapped"}` to the new structured
    `TappedStateCharacteristic{Tapped:true}` (CR 110.5 — tapped/untapped is a permanent
    status). Faithful to the card.
  - (b) No new out-of-scope free-text/unparsed residual: the only remaining residual is the
    single in-spec `OtherCharacteristic{Description:"other"}`, which is the [S6-SHARED]
    other/another exclusion the slice was explicitly told to LEAVE (Slice 6 owns it). Its
    whitelist-freetext.json entry is correctly KEPT. No unparsed nodes.
  - (c) No regression: the `gainAbility` / `Target.Kind=Each` (creature, Controller:You) /
    `GainedAbility` Indestructible ability is intact and not inverted/dropped. Co-occurring
    sibling `other` filter preserved alongside the new `tapped` filter. Remaining diff lines
    (`KeywordSource` reorder, `IsVariable:false` on manaCost, `Value`/`Raw` reorder) are
    regen serialization canonicalization, byte-shape only — no semantic change.

## Projection (initiative 03)
New `TappedStateCharacteristic` / `CounterCharacteristic` discriminators are ObjectFilter
**filter predicates**, not PortGraph branch discriminators (effect/cost type, trigger event,
restriction). The PortWalk projection does not branch on object-filter characteristics; the slice
spec confirms "filter predicates, no firability change." No projection entry is required for these
kinds — sensible. (Schema discriminators "tapped"/"counter" are present in ast-schema.json.)

## Notes
CR 110.5 confirmed present in rules-structure.json and on-point (tapped/untapped permanent status).
