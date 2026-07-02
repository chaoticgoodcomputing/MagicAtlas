# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** tangle-angler
**Branch:** mast-tdd/2026-07-02-tangle-angler
**Scope:** 1 fixture + 1 projection decision (delta-judge)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NPH/TangleAngler.json` — PASS. Activated line
  "{G}: Target creature blocks this creature this turn if able." is modeled as an `activated`
  ability with a `{G}` mana cost and a single `mustBlock` effect: `Target` = target creature
  (the forced blocker), the new optional `Blocks` = `Self` (the named attacker "this creature"),
  and `Duration` = untilTime/end-of-turn for "this turn". Discriminator `mustBlock` matches
  CR 509.1c ("a creature must block"); the effect describes the blocker-side requirement without
  executing "if able" resolution, and timing (Duration) is a separate composable field, not baked
  into the discriminator. The Infect sibling is the established `static` / `KeywordSource: Infect`
  / `keywordAbility` convention (matches GlistenerElf/CoreProwler/ContagiousNim), with verbatim
  reminder text (exempt). No unparsed nodes, no free-text characteristics. Oracle text is verbatim
  from oracle-cards.json. Out-of-axis nodes (typeline, attributes, Infect) unchanged; the `Blocks`
  field is additive/optional so the Culling Mark gold (no `Blocks`) is unaffected.

- `mast-tdd/2026-07-02-tangle-angler#projection` — PASS. Initiative-03 projection: no new
  effect/cost/trigger discriminator is introduced. `mustBlock` is pre-existing (Culling Mark gold,
  `MustBlockTargetRule`); the branch only adds an additive optional `Blocks` field, so no new
  `PortGraph` case / `PortWalkProjection` entry is required and the branch touches no projection
  files. Sensible.

## CR cross-reference

- **CR 602.1** — present verbatim in rules-structure.json; correctly grounds the "[Cost]: [Effect]"
  activated-ability shape.
- **CR 509.1c** — present verbatim; "requirements (effects that say a creature must block, or that
  it must block if some condition is met)" exactly grounds the blocker-side `mustBlock` requirement,
  including the "blocks if able during a certain turn / multiple combat phases" clause.

## Glossary gaps

(none)

## Process notes

`mustBlock` is the blocker-side dual of the "must be blocked" lure; the task brief's "must-be-blocked
lure" phrasing refers to the mechanic family, but the card places a *must-block* requirement on the
target and names this creature as the attacker — the `MustBlockEffect` + `Blocks: Self` shape is the
faithful encoding, and the doc-comment states this dual relationship correctly.

ALL PASS
