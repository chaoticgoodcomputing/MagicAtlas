# MAST judge — DELTA verdict (SLICE PB-4 — Bucket A counter-gate)

**Date:** 2026-06-16
**Slice:** PB-4 — Bucket A counter-gate
**Target gold (this judgment):** tests/magic-ast-tests/Fixtures/HandParsedCards/DKA/UndyingEvil.json (uncommitted)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## Delta judgment

This is a DELTA judgment of the one axis the slice was tasked to structure on this gold:
the intervening-if counter-gate on Undying Evil's GRANTED ability.

Real oracle text (oracle-cards.json / Scryfall) matches the fixture byte-for-byte:
"Target creature gains undying until end of turn. (When it dies, if it had no +1/+1
counters on it, return it to the battlefield under its owner's control with a +1/+1
counter on it.)"

### (a) Target residual structured correctly — YES
InterveningIf on the GainedAbility (NOT the spell ability, per spec) moved from the
free-text sink `{ConditionType:"other", Text:"it had no +1/+1 counters on it"}` to the
structured `{ConditionType:"triggeringObjectCounter", CounterType:"+1/+1", Present:false}`.
`Present:false` correctly encodes "had NO +1/+1 counters." Faithful to CR 702.93a (Undying
reminder text) with the counter look-back grounded in CR 603.10. The
TriggeringObjectCounterCondition node (libs/magic-ast/AST/Abilities/Condition.cs) carries
the required CounterType + Present fields. No new AST/schema introduced.

### (b) No new free-text/unparsed residual — YES (primary criterion)
grep for "other"/"unparsed"/"Text:" condition sinks in the post-change gold returns none.
Undying Evil removed from whitelist-freetext.json (count 0).

### (c) No regression — YES
Key-normalized diff (jq -S) confirms only three changes vs HEAD:
  1. InterveningIf -> structured (the slice's target).
  2. Power:null / Toughness:null dropped from Input — null-omission on an Instant, no
     semantic loss.
  3. Tapped:false added to returnToBattlefield — Tapped is a non-nullable bool that always
     serializes; false is the faithful default (Undying does not return the creature tapped,
     CR 702.93a).
All sibling nodes — Trigger{Event:Dies}, returnToBattlefield, UnderControl{Owner},
WithCounters{+1/+1 x1}, gainAbility, KeywordSource:"Undying", Duration{untilTime/Turn/End},
It target — are value-identical. No dropped/added/inverted ability or effect.

## Out-of-scope residuals remaining
None on this gold. (Per delta doctrine, any such residual on a different axis would NOT be
a FAIL; there are none here.)

## PASS verdicts
- `tests/magic-ast-tests/Fixtures/HandParsedCards/DKA/UndyingEvil.json` — PASS. Structures the
  Undying counter-gate intervening-if as triggeringObjectCounter{+1/+1, Present:false} on the
  granted ability (CR 702.93a / CR 603.10); no new residual; no regression.

ALL PASS
