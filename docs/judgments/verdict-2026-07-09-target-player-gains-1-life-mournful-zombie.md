# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** tdd/target-player-gains-1-life-mournful-zombie
**Family:** target-player-gains-1-life — "Target player gains 1 life." on Mournful Zombie
**Scope:** 2 files (1 fixture, 1 AST parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MournfulZombie.json` — PASS. OracleText `{W}, {T}: Target player gains 1 life.` is byte-identical to oracle-cards.json (mana `{2}{B}`, P/T 2/1, colors B, CI B/W all match). The single sentence is fully structured: an `activated` ability with `mana {W}` + `tap` costs and a `gainLife` effect carrying literal `Amount` 1 to a `Target` player. No `unparsed`, no `UnstructuredEffect`, no free text, no lossy drop/merge. Consistent with CR 119.3 (an effect causing a player to gain life).
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/TargetPlayerGainsLifeEffectRule.cs` — PASS. Cited CR 119.3 exists in rules-structure.json and its text ("If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly.") matches the modeling. The rule anchors `^Target player gains N life$`, maps to the existing `GainLifeEffect` with `Player = Target(ObjectFilter.Player())`, and handles literal/number-word/variable (X/Y/Z) amounts (ParseNumberWord resolves digits via `\b(\d+)\b`, so `1` → 1). No new node, no escape hatch, no timing baked into the effect.
- `tdd/target-player-gains-1-life-mournful-zombie` (projection) — PASS. No new discriminator introduced: the rule reuses the pre-existing `gainLife` effect type and the pre-existing `Target`/`Player` object reference. No new `PortGraph` case or `known-coarse-projections.json` entry is required; the exhaustiveness ratchet does not trip.

## Glossary gaps

None.

## Process notes

- The `Player` filter is serialized as `{ "CardTypes": ["player"] }`. "player" is not a CR card type (CR 300-series), but this is the codebase's established `ObjectFilter.Player()` representation, used consistently across ~20 existing fixtures (AbsorbVis, AncestralRecall, BrainFreeze, MindRot, ThoughtScour, VedalkenEntrancer, etc.). This is a pre-existing convention, not novel drift introduced by this branch; judging the convention itself is the engine-lens audit's scope, so it does not block PASS.
- Worker report corroborated: newAstNode=false, shared=[]. `git diff --stat` confirms exactly two added files (the new rule + the new fixture); no shared/generalization edits to review.

ALL PASS
