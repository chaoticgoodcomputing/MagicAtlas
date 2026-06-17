# MAST judge — DELTA verdict (PB-4, INR/YoungWolf)

**Date:** 2026-06-16
**Slice:** PB-4 — Bucket A counter-gate
**Scope:** 1 regenerated gold (delta judgment, not whole-gold purity)
**Result:** PASS

## Slice target on this gold
Route the hardcoded Undying intervening-if producer through the existing `ConditionParser.Parse`.
- Producer for this gold: `Keywords/Definitions/UndyingKeyword.cs` (text "it had no +1/+1 counters on it").
- Target AST: `TriggeringObjectCounterCondition{CounterType:"+1/+1", Present:false}` on the spell ability (Young Wolf has Undying intrinsically; this is NOT the GainedAbility case).

## Verdict
**`tests/magic-ast-tests/Fixtures/HandParsedCards/INR/YoungWolf.json`** — PASS.

- **(a) Target residual structured correctly.** `InterveningIf` went from `OtherCondition{Text:"it had no +1/+1 counters on it"}` → `TriggeringObjectCounterCondition{ConditionType:"triggeringObjectCounter", CounterType:"+1/+1", Present:false}`. Faithful to CR 702.93a ("if it had no +1/+1 counters on it"). Real oracle text confirmed against `oracle-cards.json` (matches fixture byte-for-byte). No new AST — `triggeringObjectCounter` already exists in `libs/magic-ast/AST/Abilities/Condition.cs` with `CounterType`/`Present`.
- **(b) No new residual.** No `ConditionType:"other"`, no `Kind:"unparsed"`, no `EffectType:"unparsed"` remain. Removed from `whitelist-freetext.json` (all 7 slice cards dropped).
- **(c) No regression.** Trigger{Dies}, Filter{IsSelf, creature}, returnToBattlefield, UnderControl{Owner}, WithCounters{+1/+1, 1}, KeywordSource:Undying all semantically identical to HEAD (verified via key-normalized jq diff). The only other diffs are inert: em-dash unicode normalization, additive default fields (`Tapped:false`, `IsVariable:false`), and key reordering — no node dropped/added/inverted, no sibling effect lost.

## Projection decision (initiative 03)
Not applicable. This slice adds no new discriminator (effect/cost type, trigger event, restriction) — it reuses the pre-existing `triggeringObjectCounter` condition. No PortWalk projection item required.

## Out-of-scope residual remaining
None on this gold. (Young Wolf carries only the single Undying intervening-if axis this slice owned.)

ALL PASS
