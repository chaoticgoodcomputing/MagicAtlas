# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** target-player-loses-2-life
**Branch:** tdd/target-player-loses-2-life
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MID/InfectiousHost.json` — PASS. `Input.OracleText` is byte-identical to oracle-cards.json ("When this creature dies, target player loses 2 life."). Gold models a triggered ability (Dies event, `IsSelf: true`, creature filter — CR 700.4 "dies") whose sole effect is `loseLife` 2 (literal) to a `Target` player reference. No `IUnparsed`, no `UnstructuredEffect`, no lossy drop/merge; timing (When/Dies) is a separate `Trigger` node, not baked into the effect (CR 603.2).
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TargetPlayerLosesLifeRule.cs` — PASS. Emits a structured `LoseLifeEffect` (`Amount = LiteralQuantity`, `Player = ObjectReference{Target, CardTypes:["player"]}`) matching the gold exactly. The anchored `^target player loses N life$` pattern terminates at "life", so it never overlaps the `TargetPlayerLoseAndYouGainLifeRule` (Blood-Artist drain) or `ThatPlayerLosesLifeRule` (anaphoric pronoun) siblings — no lossy merge. Cited CR 119.3 (life-total adjustment) and CR 603.2 (triggered-ability event matching) both exist in rules-structure.json and match the modeling.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TargetPlayerLosesLifeRule.cs#projection` — PASS. `reachedVia=new-rule-file`, `newAstNode=false`: the rule reuses the pre-existing `LoseLifeEffect` (`[OracleEffect("loseLife")]`, present at baseSha). No new effect/cost/trigger/restriction discriminator is introduced, so no fresh projection decision is owed; `loseLife` already has a live semantic PortWalk projection (`PortGraph.cs:774` / `PortWalkProjection.cs:38` → `emit:life:loss:<scope>`, the life flow arm) — sensible and unchanged.

## Glossary gaps

None.

## Process notes

- `shared=[]` — no shared/generalization edits to audit.
- Fixture is filed under `MID/`; `set` is null in the raw oracle-cards.json export (oracle-level bulk), which is not load-bearing for the fixture's correctness.
- Discriminators cross-checked live: fixture `EffectType:"loseLife"` == node `[OracleEffect("loseLife")]`; `Trigger.Event:"Dies"`; `Player.Kind:"Target"` with `CardTypes:["player"]` — all consistent between gold and parser output.

**Result: ALL PASS**
