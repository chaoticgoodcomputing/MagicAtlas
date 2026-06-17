# MAST judge — PB-3 delta verdict (GRN/BladeInstructor)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (+ merged comparative-power, PB-2)
**Scope:** 1 fixture (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/GRN/BladeInstructor.json
**Verdict:** PASS
**Rule citation:** CR 702.134a (Mentor)
**Rule text:** > "Whenever this creature attacks, put a +1/+1 counter on target attacking creature with power less than this creature's power."

This slice owns BOTH the structured-characteristic axis (attacking) AND comparative-power (PB-2 merged) for the Mentor golds. Real oracle text confirmed verbatim against oracle-cards.json: "Mentor (Whenever this creature attacks, put a +1/+1 counter on target attacking creature with lesser power.)"

- **(a) Target residuals structured correctly.**
  - `{CharacteristicType:"other", Description:"attacking"}` -> `{CharacteristicType:"combatState", State:"Attacking"}` — correct axis, faithful to "attacking creature."
  - `{CharacteristicType:"other", Description:"with power less than this creature's power"}` -> `PowerComparison{Operator:"LessThan", RelativeTo:{Kind:"Self"}, RelativeCharacteristic:"Power"}` — exactly the relative-power form CR 702.134a spells out ("power less than this creature's power"); the printed "lesser power" is the oracle shorthand for the same comparison. Matches the per-gold checklist target.
- **(b) No new residual introduced.** No OtherCharacteristic/Description/unparsed/IUnparsed anywhere in the gold; it is fully residual-free. PRIMARY criterion satisfied.
- **(c) No regression.** Trigger (Whenever/Attacks/creature filter), putCounters effect, CounterType "+1/+1", Count literal 1, KeywordSource "Mentor", Reminder, and the co-occurring `CardTypes:["creature"]` filter all preserved. Remaining diffs are benign regen field-ordering (manaCost IsVariable:false; creatureStats Value/Raw order) — no structural change. The Comparison-record extension (Value made nullable; RelativeTo/RelativeCharacteristic added, all WhenWritingNull) leaves literal-int consumers serializing byte-identically — the `Value:8` reorder seen in AggressiveMammoth is creatureStats, not a Comparison, so no literal-Comparison regression.

This gold is NOT S6-shared (Mentor carries no other/another exclusion residual), so it is correctly removed from whitelist-freetext.json. No out-of-scope residual remains on it.

## Process notes

Delta-scoped: judged only the change this slice made on this one gold. The Comparison.RelativeTo substrate and MentorKeyword/CantBeBlockedRule producers are the merged PB-2 mechanism; their cross-fixture byte-identity for literal consumers was spot-checked and holds.
