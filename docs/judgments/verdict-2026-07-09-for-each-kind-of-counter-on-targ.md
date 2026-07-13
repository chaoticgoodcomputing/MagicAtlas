# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/for-each-kind-of-counter-on-targ
**Family:** for-each-kind-of-counter-on-targ (Dramatist's Puppet)
**Scope:** 5 targets (1 fixture, 1 effect node, 1 parser rule, 1 schema edit, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DSK/DramatistsPuppet.json` — PASS. `Input.OracleText` is byte-identical to the Scryfall oracle text ("When this creature enters, for each kind of counter on target permanent, put another counter of that kind on it or remove one from it."); mana cost `{4}`, type line, P/T 2/4, empty colors/identity all match. Single ability modeled as a self-ETB `Trigger` (Timing `When`, Event `Enters`, Filter `CardTypes:[creature] + IsSelf:true`) composed with a plain structured `adjustEachCounterKind` effect — timing and action correctly kept as separate composable nodes. No `unparsed`/`IsUnparsed:true`/`UnstructuredEffect`; the only `Raw` fields are verbatim-by-design inputs (type line, mana cost, P/T). No dropped or merged ability.
- `libs/magic-ast/AST/Effects/Counter/AdjustEachCounterKindEffect.cs` — PASS. Descriptively captures "adjust each present counter kind by +/-1, controller's choice" with a `Target` `ObjectReference`; both the iterated kind set and the per-kind add/remove are correctly treated as runtime decisions rather than a fixed `PutCounters`/`RemoveCounters` type. Cites CR 122.1 verbatim; text confirmed present in `rules-structure.json` and matches the modeling. Doc-comment cleanly distinguishes its cluster axis from `proliferate` and `putAdditionalCounterOfChosenKind`.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ForEachCounterKindAdjustRule.cs` — PASS. Fully anchored `^...$` regex over the entire single-sentence fragment (priority 999), preserving the "another counter of that kind"/"remove one from it" anaphor to the iterated kind; emits `AdjustEachCounterKindEffect` with a bare `Kind=Target` ref. CR 122.1 citation correct.
- `libs/magic-ast/schema/ast-schema.json` — PASS. Additive registration of `AdjustEachCounterKindEffect` (discriminator `adjustEachCounterKind`, `IsUnparsed:false`, single `Target` field) plus schema-hash bump; consistent with the new node, a sound generalization.
- `libs/mast-interaction/known-coarse-projections.json#adjustEachCounterKind` — PASS (projection decision, initiative 03). New discriminator is present as a justified coarse projection with a plausible reason. Sensible: the entire counter-adjustment family (`proliferate`, `moveCounters`, `removeCounters`, `putCounters`, sibling `putAdditionalCounterOfChosenKind`) is uniformly coarse because no flow rule reads counter-adjustment events; per-kind +/-1 on a target permanent is genuinely inert for interaction combo-recall today. Not a case a flow rule would clearly want parked.

## Glossary gaps

(none) — "counter" is in `glossary.json` (points to rule 122); the "kind of counter" concept is covered by CR 122.1's "counters with the same name or description are interchangeable."

## Process notes

- The gold models "target permanent" as a bare `ObjectReference { Kind: Target }` with no permanent-type filter. This exactly mirrors the already-landed sibling `PutAdditionalCounterOfChosenKindEffect` / `ChooseAndPutAdditionalCounterOfChosenKindRule` (Ichormoon Gauntlet gold), which uses the identical bare-Target convention for "target permanent". Consistent with precedent and not a free-text/lossy hole, so not a FAIL; noted only for convention awareness (any future decision to add a `permanent` filter should be applied family-wide).

ALL PASS
