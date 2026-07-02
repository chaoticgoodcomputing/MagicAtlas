# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** trusty-companion
**Branch:** mast-tdd/2026-07-02-trusty-companion (base 1526dd74)
**Scope:** 1 fixture (AKH/TrustyCompanion.json) + 1 parser rule (CantAttackAloneRule.cs); projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/AKH/TrustyCompanion.json` — PASS. "This creature can't attack alone." is modeled as a `Kind: static` ability whose `cantAttack` effect carries the structured boolean `Alone: true` — the canonical shape for the "alone" qualifier (dual of `OnlyAlone` = Master of Cruelties), not a free-text "alone" string. Describe-not-execute (records the declare-attackers restriction, does not model the runtime decision); no timing baked in. Oracle text verified against oracle-cards.json ("Vigilance\nThis creature can't attack alone.", {1}{W}, Creature — Hyena, 3/2 — exact). Vigilance sibling preserved as a separate `keywordAbility` static; manaCost/colors/colorIdentity/creatureStats attributes all intact; no `unparsed` residual anywhere. CR 508.1 exists and its literal text uses THIS restriction as its worked example; CR 508.1c (attack restrictions checked at declare-attackers) matches the modeling verbatim.
- `mast-tdd/2026-07-02-trusty-companion#projection` — PASS. Branch introduces NO new AST discriminator: the `cantAttack` effect type and its `Alone` field both pre-exist at the base commit; this branch adds only a parser rule (`CantAttackAloneRule`, priority 959, anchored `^...$`, non-colliding with the `can only`/`attack or block` siblings) that emits the existing effect. The initiative-03 ratchet is therefore not tripped here. `cantAttack` already carries a `known-coarse-projections.json` entry ("baseline coarse fallback … no flow rule consumes it yet"); that coarse choice is sensible — an attacker-side legality restriction is inert to the interaction/flow graph.

## Glossary gaps

_none surfaced._

## Process notes

The diff touches exactly two files (new parser + new fixture); `CantAttackEffect.cs` is unchanged from base, confirming the `Alone`/`OnlyAlone` qualifiers are pre-existing structure being newly *reached* by a parser, not newly *introduced*. No sibling fixtures altered; no cross-axis regression.
