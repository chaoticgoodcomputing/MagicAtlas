# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** karplusan-yeti-deals-damage-equal
**Branch:** mast/karplusan-yeti-deals-damage-equal
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ICE/KarplusanYeti.json` — PASS. `Input.OracleText` is byte-identical to oracle-cards.json (`{T}: This creature deals damage equal to its power to target creature. That creature deals damage equal to its power to this creature.`). The reciprocal power-fight longhand decomposes into two ordered `dealDamage` effects: (1) `Source: Self`, `Amount` derived Power of "it", `Target` target creature; (2) `Source: ThatCreature`, `Amount` derived Power of "it", `Target: Self`. Semantics faithful to CR 120.1 (the damage source); no `IUnparsed`, no `UnstructuredEffect`, no lossy drop/merge into a single fight, no free-text. `Amount.Source: "it"` matches the established `{DerivedFrom: Power, Source: "it"}` convention (6 prior fixtures).
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/DealsDamageEqualToPowerReciprocalEffectRule.cs` — PASS. Doc-comment cites CR 120.1 (verified: "An object that deals damage is the source of that damage.") and CR 701.14 Fight (verified: 701.14a "Each of those creatures deals damage equal to its power to the other creature."), the latter correctly cited as the contrast this longhand is NOT — Karplusan Yeti is not templated with "fights" and only one creature is a target. Both regexes are anchored `^...$` on the full sentence surface; emitted `DealDamageEffect` output matches the gold fixture field-for-field. `ThatCreature` is a sound linked back-reference (ADR 0004) to the first sentence's target.
- `mast/karplusan-yeti-deals-damage-equal#projection` — PASS. No new discriminator is introduced: the branch reuses the existing `dealDamage` EffectType, `ThatCreature` ObjectReferenceKind (ObjectReference.cs:123), `DerivedQuantity`/`DerivedKind.Power`, and `tap` cost. Only a parser rule + fixture were added (diff touches no enum/AST-node files). No PortWalk `PortGraph`/`PortWalkProjection` entry or `known-coarse-projections.json` entry is required, and none was omitted.

## Glossary gaps

None. "Fight" / damage are covered by CR 701.14 / CR 120.

## Process notes

Full branch diff is exactly two added files (parser rule + gold fixture); no projection, enum, or shared-node edits, so there is no shared-edit generalization to audit and the projection ratchet does not apply. The `Source: "it"` token on `DerivedQuantity` is a pre-existing loose-string convention on an existing typed field, not new drift introduced by this branch.

**Result: ALL PASS — PROCEED.**
