# MAST judge — delta verdict (SLICE PB-3, RazortoothRats)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (structured-characteristic axis + comparative-power)
**Scope:** 1 fixture (delta judgment)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/9ED/RazortoothRats.json` — PASS.
  Slice structured the Fear evasion residual: the free-text
  `Characteristics: [{other:"artifact"}, {other:"black"}]` sink was replaced with
  structured axes `CardTypes: ["creature","artifact"]` + `Colors: ["B"]`, per the
  slice's `artifact→CardTypes` + `black→Colors` mapping. Faithful to the real card
  (oracle text = "Fear (This creature can't be blocked except by artifact creatures
  and/or black creatures.)", verified against oracle-cards.json) and to CR 702.36b.
  The single-filter encoding of the "and/or" disjunction is the established codebase
  simplification, identical in shape to the canonical Intimidate evasion sibling
  (`CardTypes:["creature","artifact"]` + colour axis); the producer FearKeyword.cs
  doc-comment explicitly acknowledges this. No regression: static ability, `evasion`
  EffectType, `KeywordSource: "Fear"`, and Reminder all preserved; remaining diffs
  (`IsVariable:false`, Value/Raw key reordering) are serialization normalization.
  Whitelist `OtherCharacteristic` debt entry for `9ED/RazortoothRats` removed (gold
  fully cleaned).

## FAIL verdicts

(none)

## Out-of-scope residuals remaining

None. This gold is fully cleaned by the slice — no other-axis residual remains.

## Process notes

Delta-scope check: searched the regenerated gold for any `Characteristics` /
`CharacteristicType:"other"` / `Description` / `unparsed` node — none remain. The
S6-shared carve-out (AdeptWatershaper, SarythTheVipersFang other/another residual)
does not apply to this gold.

ALL PASS
