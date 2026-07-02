# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (Plaxmanta) + 1 projection decision on branch `mast-tdd/2026-07-02-plaxmanta`
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/RAV/Plaxmanta.json` — PASS. Oracle text matches oracle-cards.json verbatim. Target line "When this creature enters, sacrifice it unless {G} was spent to cast it" is a `triggered` ability: `Trigger{When, Enters, Filter{creature, IsSelf}}` + `Effects:[ConditionalEffect{Condition: manaSpentToCast(Color:"G", WasSpent:false), Then: sacrifice(Target: It)}]`. Timing lives in the trigger, not the effect (no baked-in timing). The `WasSpent:false` polarity is the "unless" negation — sacrifice fires when {G} was NOT spent — a lookup on a fact fixed at casting (CR 601.2f–h; CR 707.10 Dawnglow Infusion precedent), correctly NOT modeled as a payable `UnlessClause`/`PreventableEffect` (Player+Cost). Sacrifice = CR 701.21a. No unparsed/free-text residual (the ability-2 `Reminder` is verbatim reminder text, exempt). Siblings faithful: Flash → `timingModification Grant Instant`; shroud ETB → `gainAbility` to Each creature you control, untilTime end-of-turn. New file, so no prior gold to regress from; all three abilities present, none dropped/inverted.
- `mast-tdd/2026-07-02-plaxmanta#projection` — PASS. The only new discriminator is a `Condition` kind (`ConditionKind("manaSpentToCast")`). Conditions are not among the four PortWalk dispatch dimensions the exhaustiveness ratchet enumerates (`effectType`, `costType`, `triggerEvent`, `restriction` — see PortWalkExhaustivenessTests + known-coarse-projections.json keys), so no projection/coarse entry is required. The enclosing `conditional` effectType is already semantically projected (recurses Then/Else as gated inner ports), so the `sacrifice` port in the Then branch keeps its flow reachability. No insensible coarse parking.

## Rule cross-reference

- **CR 601.2f** — present; "The player determines the total cost of the spell..." matches the doc-comment.
- **CR 601.2h** — present; "The player pays the total cost..." matches ("mana is spent to cast at this step").
- **CR 707.10** — present; contains the exact cited example: "Dawnglow Infusion ... 'You gain X life if {G} was spent to cast this spell...' Because mana isn't an object, a copy ... won't cause you to gain any life ...". Confirms "{color} was spent to cast" is a fixed historical fact, not a payable cost — grounding the ConditionalEffect (vs. UnlessClause) choice.
- **CR 701.21a** — present; Sacrifice definition. Matches `SacrificeEffect`.

## Glossary gaps

_None surfaced._

## Process notes

- Fixture is a brand-new hand-parsed file (did not exist at base `4618d17`); "no regression" is judged as faithful presence of all three oracle abilities with none dropped or inverted.
- The `ManaSpentToCastCondition` doc-comment explicitly and correctly distinguishes this fact-check shape from the payment-based "sacrifice unless you pay {COST}" (`UnlessClause`: Player + Cost) — a meaningful MTG-rules distinction, well grounded.
