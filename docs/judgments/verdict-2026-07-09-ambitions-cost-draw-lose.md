# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 AST/parse-rule) + 1 projection decision
**Branch:** mast/ambitions-cost-draw-lose
**Family:** you-draw-three-cards-and-you-los — "You draw three cards and you lose 3 life." (Ambition's Cost)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MMQ/AmbitionsCost.json` — PASS. Input.OracleText "You draw three cards and you lose 3 life." is byte-identical to oracle-cards.json (mana {3}{B}, Sorcery, colors/identity [B] all match). The spell ability decomposes into two sibling effects — `drawCards{literal 3, You}` (CR 121.1) and `loseLife{literal 3, You}` (CR 119.3). Eventual-truth: no `unparsed` Kind, no `UnstructuredEffect`, no lossy drop/merge of the lose-life conjunct; the self-drain is correctly `You` (not a target). Attributes correct.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/YouDrawCardsAndYouLoseLifeRule.cs` — PASS. Anchored `^...$` regex requires the mandatory second "you" ("and you lose") to disambiguate this controller-drain spell shape from the targeted (`TargetPlayerDrawsLosesLifeRule`) and activated variants; emits a flat `[DrawCardsEffect, LoseLifeEffect]` via `TryMatchMulti` with both effects carrying `ObjectReference.You()`. Cited CR 121.1 (draw) and CR 119.3 (lose life) both exist in rules-structure.json and their text matches the modeling.
- `mast/ambitions-cost-draw-lose#projection` — PASS. No new discriminator introduced (newAstNode=false, shared=[]); the rule reuses existing `drawCards` (known-coarse baseline) and `loseLife` (semantic `PortWalkProjection` → emit:life:loss). Both already carry sensible projection decisions; the initiative-03 ratchet has nothing new to enforce.

## Glossary gaps

None. "draw" and "lose life" are both covered by CR 121 / CR 119.

## Process notes

Diff touches exactly two files (new parse rule + new fixture) — shared=[] confirmed, no shared-file generalizations to audit. The `TryMatch` single-effect path is intentionally disabled (returns false) so the shape always yields the two-sibling list, preventing a sibling rule from consuming the sentence and silently dropping the lose-life conjunct.

**ALL PASS**
