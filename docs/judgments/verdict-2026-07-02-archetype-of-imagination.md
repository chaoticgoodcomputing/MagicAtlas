# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** archetype-of-imagination
**Branch:** mast-tdd/2026-07-02-archetype-of-imagination
**Scope:** 1 fixture + 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JOU/ArchetypeofImagination.json` — PASS.
  Oracle text matches oracle-cards.json verbatim. Target line "Creatures your opponents
  control lose flying and can't have or gain flying." is modeled as a single
  `cantHaveOrGainKeyword` continuous effect (Keyword: Flying, Target: Each creature /
  Controller Opponent). CR 113.11 ("If the object has that ability, it loses it")
  authorizes collapsing the removal into the can't-have lock — one effect, not a
  double-counted loseAbility + cantGain. CR 611.3 is the correct static-continuous
  authority; CR 702.9 defines Flying. Discriminator names the state, not a timing —
  describe-not-execute, no baked-in trigger. Keyword carried by the structured
  KeywordAbility enum (no free text). Anthem sibling "Creatures you control have flying."
  preserved as gainAbility→evasion(Flying). No unparsed nodes, no free-text residual,
  no dropped/inverted ability.

- `libs/mast-interaction/known-coarse-projections.json#cantHaveOrGainKeyword` — PASS.
  The one new discriminator introduced by the branch. Its PortWalk projection decision
  is present (a justified coarse entry) and sensible: its ability-modification siblings
  loseAbility / gainAbility / keywordAbility are all baseline-coarse, no flow rule reads
  ability-denial, and there is no semantic PortGraph case a flow rule would clearly want.
  Consciously inert for interaction recall — a defensible coarse park, not an insensible one.

## Glossary gaps

(none)

## Process notes

Both cited rules cross-checked against rules-structure.json and match the modeling verbatim.
The single-node collapse of "lose X and can't have or gain X" is the load-bearing modeling
call; CR 113.11's literal "if the object has that ability, it loses it" makes it faithful
rather than lossy. This is a new fixture (no prior version), so the regression check reduces
to: both card lines present, correctly axized, siblings intact — confirmed.

**PROCEED.**
