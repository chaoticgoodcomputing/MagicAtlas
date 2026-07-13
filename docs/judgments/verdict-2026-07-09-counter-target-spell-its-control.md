# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** counter-target-spell-its-control
**Branch:** family/counter-target-spell-its-control (base aaec9d3b)
**Scope:** 2 files (1 fixture, 1 AST rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DidntSayPlease.json` — PASS. `Input.OracleText` = "Counter target spell. Its controller mills three cards." is byte-identical to oracle-cards.json (name/manaCost/typeLine/colors/colorIdentity all match). The spell decomposes into two fully-structured effects: `counterSpell` (Target.Filter.CardTypes:["spell"]) and `mill` (Count literal 3 + Player Kind:Controller). No IUnparsed, UnstructuredEffect, OtherX, or free-text escape hatch. counterSpell shape matches the established family convention (PunishIgnorance — the direct "Counter target spell. Its controller loses N…" analog — plus SpellSnare, Nullify). Models CR 701.6 (counter) + CR 701.17a (mill).
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ItsControllerMillsRule.cs` — PASS. New rule reuses the existing `MillEffect` node (no new AST node; newAstNode=false as briefed) and mirrors the established `ItsControllerLosesLifeRule` anaphoric-controller pattern exactly. "Its controller" → `ObjectReference{Kind: Controller}` (valid ObjectReferenceKind). Amount via `LiteralQuantity.Of(ParseSmallWord(...))`, the standard helper. Doc-comment cites CR 701.17a verbatim; the quote matches rules-structure.json 701.17a word-for-word.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ItsControllerMillsRule.cs#projection` — PASS. The branch introduces no new discriminator: `mill`, `counterSpell`, and the `Controller` reference kind all pre-exist. `mill` already carries a projection decision in `libs/mast-interaction/known-coarse-projections.json` ("baseline coarse fallback … no flow rule consumes it yet"), which is sensible — milling library→graveyard is inert to the current interaction port-walk (no consuming flow rule). No ratchet obligation added by this branch.

## Glossary gaps

(none) — "Mill" is present in glossary.json with the CR 701.17 citation.

## Process notes

- Diff is exactly 2 new files (rule + fixture); `--name-only` confirms zero shared-file edits (shared=[] as briefed). No shared generalization to audit.
- The "Counter target spell." first sentence is handled by the pre-existing `CounterSpellRule` (reachedVia) via `SpellAbilityParser.TryParseSentenceBundleEffects` auto-split; the new work is only the trailing "Its controller mills N cards." sentence. Both halves land as structured gold — nothing lossy, dropped, or merged.

**Result: ALL PASS**
