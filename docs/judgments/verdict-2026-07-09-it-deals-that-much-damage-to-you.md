# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/it-deals-that-much-damage-to-you
**Family:** it-deals-that-much-damage-to-you (fragment "it deals that much damage to you." on Jackal Pup)
**Scope:** 3 files (1 fixture, 2 parser rule nodes) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TMP/JackalPup.json` — PASS. Input.OracleText ("Whenever this creature is dealt damage, it deals that much damage to you.") is byte-identical to oracle-cards.json; type line, mana cost, colors, 2/1 stats all match. Trigger models "whenever this creature is dealt damage" as `Event: CreatureDealtDamage` (CR 120.1) with `Filter: {CardTypes: ["creature"], IsSelf: true}` restricting to the source (CR 109). Effect models "it deals that much damage to you" as `dealDamage` with `Amount: derived(DamageDealt)` for "that much", `Target: You` (CR 109.5 controller), `Source: Self` for "it". Fully structured — no `unparsed` Kind, no `UnstructuredEffect`, no free text, no lossy drop/merge.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ItDealsThatMuchDamageToYouRule.cs` — PASS. Emits `DealDamageEffect{Amount = DerivedQuantity(DamageDealt), Target = You, Source = Self}`. All fields are real on the pre-existing `DealDamageEffect` (Amount/Target required, Source optional). Anchored `^…$` regex avoids collision with the "to that creature's controller" / "to each other opponent" siblings. Cited CR 120.1 (source of damage), 603.2 (trigger), 109.5 ("you"/"your" = controller) all exist in rules-structure.json and match the modeling.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SelfCreatureDealtDamageConditionRule.cs` — PASS. Emits `TriggerCondition{Event = CreatureDealtDamage, Filter = {CardTypes: ["creature"], IsSelf: true}}`. Priority 600 correctly outranks the generic `CreatureDealtDamageConditionRule` (Priority 500, "a creature is dealt damage") so "this creature" gets the self-restricted reading while the generic rule still catches "a creature". Cited CR 120.1/603.2 exist and match.
- `mast/it-deals-that-much-damage-to-you#projection` — PASS. No new discriminator is introduced: the `CreatureDealtDamage` trigger event, the `DamageDealt` derived-kind, the `IsSelf` restriction, and the `You`/`Self` references all pre-exist at baseSha (`b1c7f83`) with existing PortWalk projection coverage (PortGraph.cs / PortWalkProjection.cs / known-coarse-projections.json). No projection/coarse files were touched on the branch and none needed to be — the ratchet enforces projection presence only for new discriminators.

## Glossary gaps

(none — "damage", "controller", "source" are standard CR terms)

## Process notes

- Timing and effect are correctly decomposed: the "when" lives in the trigger-condition node (`Whenever` + `CreatureDealtDamage`) and the effect node names only the action (`dealDamage`); no timing is baked into the effect discriminator.
- The self/any axis is correctly modeled via `Filter.IsSelf: true` (source-restricted) rather than an over-broad "any creature" reading, matching the paired generic sibling that carries no IsSelf. This is the reflexive Jackal Pup semantics: the creature deals the damage it took back to its own controller.
- Branch touches exactly 3 files (no shared edits; `shared=[]` confirmed), so no generalization surface to audit.

ALL PASS
