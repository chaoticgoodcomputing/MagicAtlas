# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/gain-control-target-artifact
**Family:** gain-control-of-target-artifact ("Gain control of target artifact for as long as you control this creature." — Aladdin, ARN)
**Scope:** 3 files (1 fixture, 1 parser rule, 1 AST condition node) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ARN/Aladdin.json` — PASS. `Input.OracleText` is byte-identical to Scryfall oracle-cards.json ("{1}{R}{R}, {T}: Gain control of target artifact for as long as you control this creature."), as are ManaCost/TypeLine/P-T/Colors. Gold ability is a clean activated ability: `mana` + `tap` costs, single `gainControl` effect on a `Target` reference filtered `CardTypes:["artifact"]` (CR 115.1), with the duration properly decomposed into `asLongAs` + `controlsObject(Reference:Self, Controller:You)` (CR 611.2 / 109.5). Timing is composited onto the effect, not baked into a discriminator. No `unparsed`, no `UnstructuredEffect`/`other`, no rules-bearing free text.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/GainControlTargetForAsLongAsYouControlRule.cs` — PASS. Fully-anchored (`^…$`) single-sentence parser rule that reuses `GainControlEffect` (`gainControl`) + `AsLongAsDuration` (`asLongAs`) and emits the new `ControlsObjectCondition`; delegates filter parsing to `SpellRuleHelpers.ParseTargetFilter`. Cited CR 611.2 (verbatim Master Thief example — the exact "for as long as you control this creature" clause), CR 109.5 ("you" = controller), CR 115.1 ("target") all exist in rules-structure.json and match the modeling.
- `libs/magic-ast/AST/Abilities/ControlsObjectCondition.cs` — PASS. New `[ConditionKind("controlsObject")]` record with `Reference` + `Controller` (both `ObjectReference`). Present-tense live control gate, correctly distinguished in its doc-comment from the pre-existing past-tense `[ConditionKind("controlledObject")]` (CR 109.4) — no discriminator collision. Base `Condition` uses `[PolymorphicBase("ConditionType")]`, so the fixture's `"ConditionType":"controlsObject"` serialization is correct. Cites CR 611.2 and 109.5 verbatim; both exist and match.
- `mast/gain-control-target-artifact#projection` — PASS. The only new discriminator introduced is a `ConditionType` (`controlsObject`). The PortWalk exhaustiveness ratchet dispatches on four categories only — `costType`, `effectType`, `restriction`, `triggerEvent` — with no `conditionType` category; durations and their conditions are not projected into the port graph. The dispatched effect (`gainControl`) is pre-existing (already a coarse baseline entry in `known-coarse-projections.json`) and merely reused. This branch therefore adds no new PortWalk-dispatched discriminator, the ratchet is not implicated, and no projection entry is required. Sensible.

## Glossary gaps

None.

## Process notes

- Pre-existing observation (out of scope for this branch, not a FAIL): `gainControl` sits in `known-coarse-projections.json` as an inception-era "baseline coarse fallback." Gain-control (permanent theft) is a combo-relevant effect a future flow rule may want to read, but that projection decision predates this branch, which only reuses the existing effect. Flagged for the interaction-layer backlog, not this merge.
