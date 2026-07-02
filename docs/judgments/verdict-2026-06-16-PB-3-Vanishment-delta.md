# MAST judge — delta verdict (PB-3, Vanishment)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice
**Scope:** 1 fixture (delta judgment, working-tree uncommitted)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Target

`tests/magic-ast-tests/Fixtures/HandParsedCards/AVR/Vanishment.json`

Oracle text (confirmed against oracle-cards.json): "Put target **nonland** permanent on top of its owner's library. / Miracle {U}".

## What the slice structured

The target residual on the `putOnTopOfLibrary` effect's filter:

- BEFORE: `Characteristics: [{"CharacteristicType": "other", "Description": "nonland"}]` (free-text sink)
- AFTER: `ExcludedCardTypes: ["land"]`

This is the slice's structured-characteristic axis at work. "land" is a card type (CR 300.1); "nonland permanent" = a permanent (CR 110.1) that is not a land. The structured form matches the canonical pattern documented in `libs/magic-ast/AST/References/ObjectFilter.cs` ("nonland card -> CardTypes + ExcludedCardTypes:[land]"). Faithful to the real card.

## Criteria

- (a) Target residual structured correctly: YES — right axis (`ExcludedCardTypes`), faithful to "nonland".
- (b) No new free-text/unparsed residual: YES — none introduced; `Reminder.Text` is verbatim-by-design (exempt).
- (c) No regression: YES — `CardTypes:["permanent"]` sibling filter preserved; both abilities (spell putOnTopOfLibrary + static miracle) intact; miracle `Cost {U}`, `KeywordSource`, `Reminder` all retained (CR 702.94). Remaining diff is cosmetic (whitespace reflow, dropped null Power/Toughness on Input, added IsVariable:false canonicalization).

## Notes

- Vanishment had a single exclusion residual, fully reachable by this slice; no out-of-scope residual remains on this gold.
- Correctly removed from `whitelist-freetext.json` (verified absent) per the slice's "remove fully-cleaned golds" directive. Not an S6-shared gold, so no whitelist entry should persist.

ALL PASS
