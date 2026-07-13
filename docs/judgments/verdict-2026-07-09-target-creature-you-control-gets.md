# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** target-creature-you-control-gets
**Branch:** mast-target-creature-you-control-gets
**Scope:** 2 files (1 fixture, 1 AST/parser rule) + 1 projection item
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/FranticConfrontation.json` — PASS. Input.OracleText byte-identical to oracle-cards.json ("Target creature you control gets +X/+0 and gains first strike and trample until end of turn.", {X}{R}, Instant). "+X/+0" → `modifyPT` with Target creature/Controller=You, `PowerModifier` VariableQuantity X (CR 107.3a — controller chooses X; CR 613.4c layer 7c), `ToughnessModifier` literal 0, until-end-of-turn; "gains first strike" → `gainAbility` on It, static `combatDamageTiming: First` (CR 702.7a); "gains trample" → `gainAbility` on It, static `keywordAbility: Trample` (CR 702.19a). No `unparsed`/`UnstructuredEffect`/free-text; KeywordSource casing ("First strike"/"Trample") matches existing gold fixtures.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/TargetControlledVariablePTAndGainKeywordsSpellRule.cs` — PASS. New `IMultiSpellRule` (single-effect `TryMatch` disabled) emitting a flat structured effect list; reuses pre-existing effect nodes only. Doc-comment cites CR 107.3a, 613.4c, 702.7a, 702.19a — all present in `rules-structure.json` with text matching the modeling. Keyword→StaticAbility factory mirrors sibling `ModifyPTAndGainKeywordControlledSpellRule`; unrecognised keyword bails to fallback rather than emitting an escape hatch.
- `mast-target-creature-you-control-gets#projection` — PASS. `newAstNode=false`, `shared=[]`; the branch adds a parser rule only and introduces no new effect/cost discriminator, trigger event, or restriction (it composes existing `modifyPT`/`gainAbility`/`combatDamageTiming`/`keywordAbility`). No PortWalk projection decision is required or expected.

## Glossary gaps

(none — "First Strike" and "Trample" are both in glossary.json)

## Process notes

- All four cited CR rules verified present in `rules-structure.json`: 107.3a (X announced by controller), 613.4c (layer 7c P/T modification), 702.7a (first strike), 702.19a (trample).
- The +X/+0 variable is captured structurally as `VariableQuantity { Name: "X" }` bound to the {X} mana cost (IsVariable=true), not as free text.
- Two separate `gainAbility` effects (one per keyword) rather than one combined effect — consistent with the established sibling-rule convention and descriptively faithful to "gains first strike and trample".

ALL PASS
