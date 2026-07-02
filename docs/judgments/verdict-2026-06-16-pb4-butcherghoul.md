# MAST judge — delta verdict (Slice PB-4, Bucket A counter-gate)

**Date:** 2026-06-16
**Scope:** 1 gold (delta judgment) — INR/ButcherGhoul.json
**Result:** PASS

## Slice target

Route the hardcoded Undying intervening-if producer through the EXISTING
`ConditionParser.Parse` (regex already matches both +1/+1 and -1/-1 counter
texts; no new arm added). Target AST:
`TriggeringObjectCounterCondition{CounterType:"+1/+1", Present:false}`.

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/INR/ButcherGhoul.json
**Verdict:** PASS

- (a) TARGET structured correctly. The `InterveningIf` flipped from
  `OtherCondition{Text:"it had no +1/+1 counters on it"}` to
  `TriggeringObjectCounterCondition{ConditionType:"triggeringObjectCounter",
  CounterType:"+1/+1", Present:false}`. Real oracle text (Scryfall oracle-cards.json):
  "Undying (When this creature dies, if it had no +1/+1 counters on it, return it
  to the battlefield under its owner's control with a +1/+1 counter on it.)" —
  "no" → Present:false, "+1/+1" → CounterType. Faithful. Source change is in
  `UndyingKeyword.cs` (`InterveningIf = ConditionParser.Parse(...)`), and the
  parser's existing `TriggeringObjectCounter` regex (ConditionParser.cs L70-72)
  matches via the shared `[+\-]?\d+/[+\-]?\d+` group — no new arm.
- (b) NO new free-text/unparsed residual. The OtherCondition sink was removed;
  no replacement residual introduced. INR/ButcherGhoul also removed from
  whitelist-freetext.json (OtherCondition sink).
- (c) NO regression. Trigger{Dies}/Filter{IsSelf:true}, ReturnToBattlefieldEffect
  with Target{It}, UnderControl{Owner}, WithCounters{+1/+1, 1}, KeywordSource
  "Undying", and Reminder all preserved byte-faithfully. Remaining diff lines are
  serializer canonicalization shared across the regenerated batch (em-dash
  unescape, key reordering, defaulted `Tapped:false`/`IsVariable:false`) — not
  semantic.

## Out-of-scope residuals remaining

None on this gold. ButcherGhoul's only intervening-if was the slice's target;
no other axis/residual is left on this fixture.

## Summary

- PASS: 1
- FAIL: 0

ALL PASS
