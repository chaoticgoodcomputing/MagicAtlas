# MAST judge — PB-3 delta verdict (GRN/BargingSergeant)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (consolidated ATOMIC: structured-characteristic axis + comparative-power, PB-2 merged)
**Scope:** 1 gold (delta judgment, uncommitted working tree)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Card

Barging Sergeant — "Haste\nMentor (Whenever this creature attacks, put a +1/+1 counter on target attacking creature with lesser power.)" — oracle text confirmed against oracle-cards.json (Scryfall).

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/GRN/BargingSergeant.json` — PASS.
  - (a) Target residuals structured correctly. The Mentor target carried two `OtherCharacteristic`/`Description` free-text residuals: `"attacking"` and `"with power less than this creature's power"`. PB-3 structured BOTH — `"attacking"` → `CombatStateCharacteristic{State: Attacking}` (reused existing axis, CR 508); `"with power less than this creature's power"` → `PowerComparison{Operator: LessThan, RelativeTo: Self, RelativeCharacteristic: Power}` (merged PB-2 comparative). Faithful to CR 702.134a: "Whenever this creature attacks, put a +1/+1 counter on target attacking creature with power less than this creature's power."
  - (b) No new free-text/unparsed residual introduced. Grep confirms no `other`/`unparsed`/`Description`/`OtherCharacteristic` remains. Whitelist-freetext entry `GRN/BargingSergeant` (sink `OtherCharacteristic`) correctly removed.
  - (c) No regression. Haste keyword ability preserved; `Attacks`/`Whenever` trigger, `putCounters` +1/+1, `Count` and sibling `CardTypes:["creature"]` target filter all preserved. Remaining diff is byte-level key reordering plus the schema-wide additive `IsVariable:false` normalization; literal-int Comparison consumers serialize byte-identically via `WhenWritingNull`.

## Out-of-scope residual

None on this gold. BargingSergeant is fully cleaned by PB-3. The S6-shared other/another exclusion debt lives on AdeptWatershaper / SarythTheVipersFang (Slice 6 owns it), not here.

## Process notes

CR 702.134 (Mentor) and CR 508 (combat / attacking) both confirmed present in rules-structure.json (relocated to libs/mtg-rules/Data/_03_Primary/Datasets/). MentorKeyword.cs producer emits the identical structured shape, so producer and hand-parsed gold agree.

PROCEED.
