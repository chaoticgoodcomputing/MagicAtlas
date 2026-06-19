# MAST judge — Captain Rex Nebula re-render

**Date:** 2026-06-18
**Scope:** 2 re-rendered items (1 citation, 1 projection decision) on HEAD `1849e157` (`feat/mast-improvements`)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

These re-render the two prior FAILs from `verdict-2026-06-18-captainrexnebula.{json,md}`; both fixes verified on the current committed state.

## FAIL verdicts

(none)

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AnimateTargetIntoVehicleWithCrewAndCrashLandRule.cs` — PASS. The doc-comment (lines 116-118) near `SacrificeEffect`/`ConditionalEffect` now reads "CR 706 result; CR 701.21 sacrifice; CR 120 damage." CR 701.21 (Sacrifice) and CR 120 (Damage) both exist in `rules-structure.json` and match the modeled `SacrificeEffect` + `DealDamageEffect`. The stale CR 701.16 (which is Investigate, not Sacrifice) is no longer cited.
- `libs/mast-interaction/PortWalkProjection.cs#becomesPermanent` — PASS. The `becomesPermanent` discriminator now carries an explicit projection decision in `PortWalkProjection.EffectTypes` (line 46): PortWalk recurses the effect's `GainedAbilities` as own port units (the granted Crash Land triggered ability), treating only the grant emit itself as the coarse inert `emit:becomespermanent`. It is NOT parked in `known-coarse-projections.json`. Recursing the granted abilities is exactly what a flow rule would want, so the choice is sensible. `PortWalkExhaustivenessTests` passes 5/5, including `Every_discriminator_is_projected_or_justified`.

## Glossary gaps

(none for the re-rendered items)

## Process notes

The earlier `becomesPermanent` projection FAIL was a stale read of the pre-fix merge commit; on HEAD `1849e157` the semantic projection is present and the ratchet test is green.
