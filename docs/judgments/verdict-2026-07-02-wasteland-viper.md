# MAST judge — batch verdict (wasteland-viper)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-wasteland-viper
**Base:** c9b1439a35f44d0207b28446529176c13106f531
**Scope:** 1 fixture (regenerated gold) + projection check; 1 new parser rule (not a new discriminator)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WastelandViper.json` — PASS. Oracle text matches oracle-cards.json verbatim ("Deathtouch\nBloodrush — {G}, Discard this card: Target attacking creature gets +1/+2 and gains deathtouch until end of turn."). The target line — the from-hand bloodrush pump+grant — is structured as a `composite` of two composable effects:
  - `modifyPT` targeting `Kind: Target` filtered to `creature` + `combatState: Attacking`, `PowerModifier` literal 1, `ToughnessModifier` literal 2, `Duration untilTime Turn/End` — faithful to "Target attacking creature gets +1/+2 until end of turn" (CR 613.4c layer-7c modifier, described not executed).
  - `gainAbility` targeting `Kind: It` (anaphora back to the same targeted attacking creature — correctly not `Self`), `GainedAbility` a nested static `deathtouch` keyword (CR 702.2a), `Duration untilTime Turn/End` — faithful to "and gains deathtouch until end of turn".
  Timing is a separate `Duration` field, not baked into the effect discriminators. The "Bloodrush" ability word is captured as the activated ability's `AbilityWord` label with no rules meaning (CR 207.2c lists bloodrush explicitly). No `unparsed` Kind, no `unparsed` EffectType, no rules-load-bearing free text. Deathtouch static sibling and all Attributes (manaCost/colors/colorIdentity/creatureStats) preserved; new file, so no prior state to regress.

- `mast-tdd/2026-07-02-wasteland-viper#projection` — PASS. No new discriminator. The new parser rule `TargetAttackingCreatureGetsPTAndGainsKeywordEffectRule` splices two pre-existing rules' halves and emits only pre-existing AST node types (`ModifyPTEffect`, `GainAbilityEffect`, `CompositeEffect`) over the pre-existing `combatState: Attacking` characteristic (`enum CombatState` in Characteristic.cs). No new effect/cost/trigger/restriction discriminator is introduced, so the exhaustiveness ratchet does not require a PortWalk projection decision, and nothing is parked as insensibly coarse.

## Rule cross-reference

- **CR 613.4c** — verbatim match in rules-structure.json: "Layer 7c: Effects and counters that modify power and/or toughness (but don't set power and/or toughness to a specific number or value) are applied." Matches the `modifyPT` clause.
- **CR 207.2c** — verbatim match: ability words "have no special rules meaning… The ability words are adamant, addendum, alliance, battalion, bloodrush, …". Grounds capturing "Bloodrush" as a label rather than an effect.
- **CR 702.2a** — verbatim match: "Deathtouch is a static ability." Grounds the granted `deathtouch` as a nested static keyword ability.

## Glossary gaps

None.

## Process notes

- The discard cost is modeled structurally as `CostType: discard`, `Filter: { CardTypes: ["card"] }`, `Quantity: literal 1`. The oracle wording is "Discard **this card**", and this generic form does not encode the self-card constraint. This is fully structured (not free text, not unparsed) and lives on the cost axis, not the target pump+grant effect line this task owns — consistent with sibling bloodrush/discard-cost fixtures. Not a FAIL for this delta; noted only for the cost-axis owner.

**Result: ALL PASS**
