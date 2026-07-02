# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** halam-djinn
**Branch:** mast-tdd/2026-07-02-halam-djinn
**Scope:** delta-judge of regenerated gold `libs/magic-ast/schema/ast-schema.json` (+ projection decision)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `libs/magic-ast/schema/ast-schema.json#MostCommonColorCondition` — PASS. Regenerated gold adds
  exactly one condition entry (`Type: MostCommonColorCondition`, `Discriminator: mostCommonColor`,
  `IsUnparsed: false`, `Fields: [Among, Color, IncludeTies]`) plus the SchemaHash update; nothing
  removed. Faithful to Halam Djinn's real oracle text (verified against oracle-cards.json:
  "This creature gets -2/-2 as long as red is the most common color among all permanents or is tied
  for most common"): `Color: "R"`, `IncludeTies: true` (the "or is tied for most common" tail),
  `Among: {CardTypes:["permanent"]}` ("among all permanents"). Right node/discriminator — a max-by-color
  tally, correctly distinguished from a numeric `CountCondition`. Timing is carried by a separate
  `AsLongAsDuration` wrapper over a plain `modifyPT` effect, so no timing is baked into the effect
  discriminator. Describe-not-execute: reference-not-resolution (ADR 0004) — the phrase is recorded
  structurally, not pre-evaluated to a boolean. No free-text/unparsed residual introduced. No
  regression: the Haste static sibling and every out-of-axis schema entry are preserved. Cited CR
  rules cross-checked against rules-structure.json — CR 105.1 (five colors WUBRG, for the color-code
  axis), CR 604.2 (static abilities create continuous effects active while on the battlefield),
  CR 613.4c (layer-7c P/T modification) all exist and match the modeling; the node's doc-comment
  correctly states there is NO CR rule defining "most common color" (a card-defined, engine-evaluated
  tally), which is accurate.

- `mast-tdd/2026-07-02-halam-djinn#projection-decision` — PASS. The only new discriminator is a
  `Condition` (`ConditionType = mostCommonColor`). The PortWalk exhaustiveness ratchet
  (`PortWalkExhaustivenessTests`) enumerates exactly four axes — `effectType`, `costType`,
  `triggerEvent`, `restriction` — and `ConditionType` is none of them. Conditions reach the port graph
  only through the already-projected `Conditional` gating restriction (and PortGraph's explicit
  intervening-if inspection), not via per-condition-type dispatch, so no PortWalk projection entry is
  required for a new condition kind. The branch correctly touched no `PortWalkProjection.cs`,
  `PortGraph.cs`, or `known-coarse-projections.json` — sensible and complete.

## Glossary gaps

(none — "most common color" is a card-defined tally, not a keyword/named quality; no glossary term expected.)

## Process notes

- Oracle text verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`
  (Halam Djinn, {5}{R}, Creature — Djinn, 6/5) — the fixture Input.OracleText round-trips it exactly.
- The `Among: {CardTypes:["permanent"]}` filter uses the established `NounToFilter` "permanents"
  convention (ConditionParser whitelist); it is a pre-existing filter axis this task does not own and
  is not a residual introduced here.
