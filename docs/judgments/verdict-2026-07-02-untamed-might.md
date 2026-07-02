# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (Untamed Might), 1 new parser rule; branch `mast-tdd/2026-07-02-untamed-might`
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/HOU/UntamedMight.json` — PASS. Oracle text
  ("Target creature gets +X/+X until end of turn.", {X}{G}, Instant, G) exact-matched against
  oracle-cards.json. Modeled as `modifyPT` (a P/T-*modifying* effect, not a set-P/T effect) per
  CR 613.4c (Layer 7c). Target = target creature filter. Both `PowerModifier` and `ToughnessModifier`
  are `variable` X — the controller announces X on cast (CR 107.3a) and all instances of X share one
  value (CR 107.3i), so a single shared variable name is correct. `Duration` is a separate composable
  `untilTime` Turn/End node — timing is NOT baked into the effect discriminator (describe-not-execute
  honored). No `unparsed`/free-text residual; no dropped/added/inverted ability; manaCost ({X}{G} with
  variable + colored symbols), colors, and colorIdentity siblings preserved.
- `mast-tdd/2026-07-02-untamed-might#projection` — PASS. The branch adds only a parser recognizer
  (`ModifyPTBothVariableSpellRule`) + fixture, emitting the pre-existing `ModifyPTEffect` (`modifyPT`)
  and `VariableQuantity` (`variable`) nodes. No new discriminator (effect/cost/trigger/restriction) is
  introduced, so no PortWalk projection decision is required; none is parked as coarse.

## Glossary gaps

(none)

## Process notes

Cited CR rules cross-referenced against rules-structure.json and confirmed present with matching text:
107.3a (controller announces X on cast), 107.3i (all X share a value), 613.4c (Layer 7c P/T
modification, distinct from set-P/T). Diff touches exactly two new files; ModifyPTEffect/modifyPT and
VariableQuantity/variable both pre-exist at the base sha, confirming no new discriminator surface.

**Result: ALL PASS — PROCEED.**
