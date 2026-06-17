# MAST judge — PB-6 delta verdict (OTJ/SlickshotShowOff)

**Date:** 2026-06-16
**Slice:** PB-6 — DisplacerKitten (regression target: OTJ/SlickshotShowOff)
**Mode:** delta (judging only the noncreature-producer change on this gold)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## Verdict
- `tests/magic-ast-tests/Fixtures/HandParsedCards/OTJ/SlickshotShowOff.json` — PASS.

### What the slice structured
The "Whenever you cast a noncreature spell" trigger filter was migrated from the free-text
sink `Characteristics:[{CharacteristicType:"other", Description:"noncreature"}]` to the
structured negation `CardTypes:["spell"] + ExcludedCardTypes:["creature"] + Controller:"You"`.
`ExcludedCardTypes` is a first-class `ObjectFilter` field (libs/magic-ast/AST/References/ObjectFilter.cs:34,
doc-comment pattern `CardTypes=["card"] + ExcludedCardTypes=["land"]`), so this reuses the
existing non-type negation path exactly as the slice spec required.

### Criteria
- (a) TARGET residual structured correctly: yes — faithful to the real Scryfall oracle
  ("Whenever you cast a noncreature spell, this creature gets +2/+0 until end of turn").
- (b) No NEW free-text/unparsed residual: confirmed — 0 `CharacteristicType:"other"` nodes and
  0 unparsed Kind/EffectType nodes remain in the gold.
- (c) No regression: all four abilities preserved (Flying evasion, Haste, +2/+0 cast trigger, Plot).
  Remaining diff hunks are benign reserialization: key-order churn (`Value`/`Raw`,
  `KeywordSource` placement), additive `IsVariable:false` on the X-free mana cost, and an additive
  faithful `Reminder` on Plot. No dropped/added/inverted ability or effect.

### Bonus / cross-checks
- DisplacerKitten, OTJ/SlickshotShowOff, and WAR/SpellgorgerWeird whitelist-freetext.json
  `OtherCharacteristic` entries were removed — the shared producer cleaned them; this matches the
  spec's "bonus: remove their whitelist entries."
- The AbilityWord("Avoidance") concern is a DisplacerKitten-only issue; SlickshotShowOff has no
  AbilityWord field, so it is out of scope for this target and not a factor.

## Out-of-scope residual remaining on this gold
None. The SlickshotShowOff gold is fully structured after this slice.

ALL PASS
