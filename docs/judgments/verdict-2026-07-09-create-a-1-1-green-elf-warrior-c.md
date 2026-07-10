# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** create-a-1-1-green-elf-warrior-c
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ElvenAmbush.json` — PASS. Input.OracleText, ManaCost ({3}{G}), TypeLine (Instant), Colors/ColorIdentity ([G]) all byte-identical to oracle-cards.json. Body is a fully-structured `createToken` (CR 111.1) with `Count` = `count` quantity over `ObjectFilter{Subtypes:["Elf"],Controller:You,Zone:Battlefield}` (= "for each Elf you control") and a `TokenDefinition` of a 1/1 green (G) creature Elf Warrior, `IsCopy:false`. No IUnparsed, no UnstructuredEffect, no free-text, no lossy drop/merge. Count shape is byte-identical to the Elvish Archdruid gold's "add {G} for each Elf you control".
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/CreateCreatureTokenForEachControlledSubtypeRule.cs` — PASS. New `[SpellRule(Priority=70)]` that reuses the existing `CreateTokenEffect` (`createToken`) + `CountQuantity` (`count`) — no new AST node/discriminator (newAstNode=false). Color map (green→G), card-type-vs-subtype split for the "for each" clause, and You/Battlefield controller/zone are all sound. Doc-comment cites ADR 0004 (reference-not-resolution) and the Elvish Archdruid precedent; it names no CR rule number, so there is no citation to contradict (a missing citation does not block PASS).
- `create-a-1-1-green-elf-warrior-c#projection` — PASS. The branch introduces no new discriminator (effect/cost type, trigger event, or restriction) — it reuses `createToken` and `count`, both of which already carry projection decisions. Initiative-03's ratchet only enforces presence for *new* discriminators; nothing sensible is parked as coarse here. No projection decision required.

## Glossary gaps

- "Elf" — subtype referenced in the fixture. Not present as a term in `glossary.json` (subtypes are carried in `type-ontology.json`, not the CR glossary), so this is expected, not a corpus gap.

## Process notes

Diff touches only two new files (the parser rule + the fixture); shared=[] — no shared-file generalizations to audit. Token creation grounded in CR 111.1; the "for each … you control" count concept is consistent with CR 107.3 (count-defining quantity). Trustworthy to merge.
