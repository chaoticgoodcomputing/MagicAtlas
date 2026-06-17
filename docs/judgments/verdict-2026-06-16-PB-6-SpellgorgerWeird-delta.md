# MAST judge — DELTA verdict (PB-6 / SpellgorgerWeird regression-verify)

**Date:** 2026-06-16
**Slice:** PB-6 — DisplacerKitten (shared "noncreature" producer change)
**Scope:** 1 gold (regression-verification target named by the slice spec), DELTA judgment
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## What the slice structured (on this gold)

PB-6 folded the "noncreature" producer into the structured non-type-negation path. On
WAR/SpellgorgerWeird the spell-cast trigger filter changed from a free-text sink:

`"Characteristics": [{"CharacteristicType": "other", "Description": "noncreature"}]`

to the structured shape the slice spec mandates:

`{"CardTypes": ["spell"], "ExcludedCardTypes": ["creature"], "Controller": "You"}`

`ExcludedCardTypes` is the established AST negation axis (ObjectFilter.cs; "nonland card",
"nonland permanent" precedents). "Whenever you cast a noncreature spell" is a spell-cast
triggered ability (CR 603.2); the `Event: SpellCast` + controller/card-type `Filter` shape is
the correct descriptive model.

## Delta checks

- (a) Target residual structured CORRECTLY: free-text `OtherCharacteristic{noncreature}` →
  `CardTypes:[spell] + ExcludedCardTypes:[creature] + Controller:You`. Faithful to the real card.
- (b) NO new free-text/unparsed residual: scan for OtherCharacteristic / Description / unparsed
  nodes returns empty across the whole gold. SpellgorgerWeird removed from
  whitelist-freetext.json (it was whitelisted at HEAD; the sink is now gone, so the entry MUST
  be removed to satisfy the stateless invariant — done correctly).
- (c) NO regression: `putCounters` effect (Target Self, CounterType +1/+1, Count literal 1) and
  the `Controller:You` sibling filter are preserved. Oracle text, Power/Toughness (now 2/2), and
  the self-reference ("this creature") were corrected to match real Scryfall data — a net
  improvement, not a dropped/inverted ability. `IsVariable:false` added to the manaCost attribute
  is a serialization-completeness field, not a residual.

## Out-of-scope residual remaining

None. This gold is fully structured after the change; no other-axis debt remains on it.

## Verdict

`tests/magic-ast-tests/Fixtures/HandParsedCards/WAR/SpellgorgerWeird.json` — PASS. Noncreature
spell-cast filter correctly restructured to the structured ExcludedCardTypes negation path
(CR 603.2); no new residual; no regression; whitelist entry correctly retired.

ALL PASS
