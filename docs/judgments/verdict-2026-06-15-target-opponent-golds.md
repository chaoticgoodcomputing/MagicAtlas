# MAST judge — batch verdict

**Date:** 2026-06-15
**Scope:** 3 files (3 fixtures, 0 AST nodes) — initiative-01 gold back-prop re-judge
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

Re-judging an orchestrator gold back-prop: three "target opponent loses … life" golds had their
loseLife **recipient** sharpened from the generic `Player:{Kind:Target, Filter:{CardTypes:["player"]}}`
to the dedicated `Player:{Kind:Opponent}`. Inputs unchanged (Scryfall-verified). Only the sharpened
loseLife recipient was judged — the gain side (`Player:You`) and all other nodes are explicitly out of scope.

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M21/VitoThornOfTheDuskRose.json` — PASS. "Whenever you gain life, target opponent loses that much life" → loseLife recipient `Player:{Kind:Opponent}`. Per CR 102.2 ("a player's opponent is the other player") and glossary "Opponent" ("Someone a player is playing against"), an opponent is **not** any player; the old generic `player` filter wrongly admitted the controller. Dropping it for `Kind:Opponent` is the rules-correct descriptive narrowing.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/DakmorGhoul.json` — PASS. "…target opponent loses 2 life…" → `Player:{Kind:Opponent}`, literal Amount 2. Matches CR 102.2 and the corpus convention (Eroded Canyon / Jagged Barrens / Lonely Arroyo all model "target opponent" as `Kind:Opponent`).
- `tests/magic-ast-tests/Fixtures/HandParsedCards/HighwayRobber.json` — PASS. Identical clause to Dakmor Ghoul; same correct modeling.

## Glossary gaps

(none) — "Opponent" is present in `glossary.json`: "Someone a player is playing against. See rules 102.2 and 102.3."

## Process notes

**(a) Dropping the generic `player` filter is correct.** The pre-change shape
`{Kind:Target, Filter:{CardTypes:["player"]}}` describes "target **player**" — which includes the
controller. The oracle says "target **opponent**." CR 102.2 ("In a two-player game, a player's
opponent is the other player") and the glossary entry establish that an opponent is strictly a player
one is playing against, never oneself. So the narrowing from `Target/player` → `Opponent` removes a
rules-wrong over-broad recipient. Correct.

**(b) Folding "targeted" into `Kind:Opponent` is acceptable.** The `ObjectReferenceKind` enum has no
targeted-opponent variant; its `Opponent` member doc-comment explicitly reads `"an opponent", "target
opponent"`, i.e. `Kind:Opponent` is the deliberate, established singular-opponent convention covering
both the chosen-at-resolution "an opponent" and the targeted "target opponent." This is consistent
with the cited corpus (Eroded Canyon's "deals 1 damage to target opponent" → `Target:{Kind:Opponent}`).
MAST is descriptive, not executive (SKILL "Engine semantics out of scope"); the load-bearing
descriptive constraint — opponent, not any player — is captured. No better-fitting existing node:
`AnyTarget` (CR 115.4: creature/player/planeswalker/battle) is broader and wrong; re-introducing a
generic `Target` + `player` filter is exactly the rules-wrong shape that was removed.

Diff verified: all three golds changed **only** the loseLife `Player` recipient; the gain side
(`Player:You`) and every other node are untouched, matching the stated scope.
