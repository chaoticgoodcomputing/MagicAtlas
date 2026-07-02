# MAST judge — PB-3 delta verdict (UDS/SquirmingMass)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (type/color axis on this gold)
**Scope:** 1 fixture (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/UDS/SquirmingMass.json` — PASS.
  PB-3 structured the type/color axis of the Fear evasion filter. The two free-text
  residuals `{"CharacteristicType":"other","Description":"artifact"}` and
  `{...,"Description":"black"}` on `CanBeBlockedBy` were replaced with the structured
  axes `CardTypes:["creature","artifact"]` + `Colors:["B"]` — faithful to CR 702.36b
  ("A creature with fear can't be blocked except by artifact creatures and/or black
  creatures") and byte-identical to the canonical sibling Fear golds
  `9ED/RazortoothRats` and `10E/SeveredLegion`.

## Delta criteria

- **(a) Target structured correctly:** YES. Both `OtherCharacteristic` free-text entries
  collapsed to the type axis (artifact added to creature) + color axis (black → `Colors:["B"]`).
  Right node, right axis, faithful to the real card (oracle text confirmed against
  oracle-cards.json: "Fear (This creature can't be blocked except by artifact creatures
  and/or black creatures.)").
- **(b) No new residual:** YES. Post-regen the gold carries no `unparsed`, no
  `OtherCharacteristic`, no free-text `Description`. The only `Raw` field is the exempt
  `TypeLine.Raw`.
- **(c) No regression:** YES. Ability count (1), kind (static), effect type (evasion),
  `KeywordSource: "Fear"`, and reminder text all preserved. Remaining diff hunks
  (`IsVariable:false` add; key reordering of Value/Raw and KeywordSource/Reminder) are
  serialization canonicalization only — no semantic change.

## Out-of-scope residual

None. Squirming Mass is a pure type/color-axis gold; it never carried a comparative-power
or combat-state residual, so nothing is left for another slice. Correctly removed from
`whitelist-freetext.json` (fully cleaned, not S6-shared).

## Process notes

`rules-structure.json` / `glossary.json` are at `libs/mtg-rules/Data/_03_Primary/Datasets/`
in this checkout, not the SKILL's documented `tests/atlas-flow-test/...` path. CR 702.36
("Fear") with subrule 702.36b verified there.

ALL PASS
