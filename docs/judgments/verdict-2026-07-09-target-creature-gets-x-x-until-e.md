# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** target-creature-gets-x-x-until-e
**Branch:** mast/target-creature-gets-x-x-until-e
**Scope:** 2 files (1 fixture, 1 spell rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LGN/WirewoodPride.json` — PASS. Wirewood Pride ({G} Instant, LGN). `Input.OracleText` is byte-identical to oracle-cards.json: "Target creature gets +X/+X until end of turn, where X is the number of Elves on the battlefield." Gold models one `spell` ability with a single `modifyPT` effect: `Target` = creature filter; `PowerModifier` and `ToughnessModifier` are both `count` quantities over `ObjectFilter{Subtypes:[Elf], Zone:Battlefield}`; `Duration` = `untilTime` {Part:Turn, Edge:End}. No IUnparsed, no UnstructuredEffect, no lossy drop/merge. The self-defined X (CR 107.3 / 107.3c) is a game-state count, correctly a `CountQuantity` not a caster-announced `VariableQuantity`; both P/T instances share the same count (CR 107.3i). "on the battlefield" carries no controller restriction, so the filter omits `Controller` — every Elf regardless of controller — and does not over-restrict to `CardTypes:[creature]`, faithful to "Elves on the battlefield". Serialization matches the node definitions exactly (`modifyPT`, `count`/`CountOf`, `untilTime`).

- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ModifyPTXCountSubtypeSpellRule.cs` — PASS. Anchored (`^…$`) SpellRule producing the ModifyPTEffect above. Doc-comment cites CR 107.3 (X defined by the ability's text → not caster-announced), CR 613.4 (Layer 7 "+X/+X" is a modify, not a set, effect → `ModifyPTEffect`), and CR 205.3m (creature subtype names, e.g. Elf, are proper-noun-capitalised, justifying the singularize+title-case). All three rules exist in rules-structure.json and their text matches the modeling; none contradicts. Reuses existing AST nodes only.

## Projection decision (initiative 03)

- `mast/target-creature-gets-x-x-until-e#projection` — PASS. Worker reports `newAstNode=false`, `shared=[]`; the branch introduces no new effect/cost type, trigger event, or restriction — it adds a parser recognizer over pre-existing discriminators (`ModifyPTEffect`, `CountQuantity`, `UntilTimeDuration`, `ObjectFilter`). No new PortGraph case / PortWalkProjection entry (nor known-coarse-projections.json entry) is required. N/A, sensible.

## Glossary gaps

(none)

## Process notes

Oracle text, mana cost ({G}), and type line (Instant) verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`. Elf confirmed as a creature subtype in CR 205.3m. Node serialization discriminators (`modifyPT`, `count`, `untilTime`) confirmed against the on-branch AST definitions.

ALL PASS
