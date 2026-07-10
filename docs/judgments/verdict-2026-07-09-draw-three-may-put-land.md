# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** draw-three-may-put-land
**Branch:** mast/draw-three-may-put-land (baseSha aaec9d3b)
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/EmbracetheParadox.json` — PASS. Card "Embrace the Paradox" ({3}{G}{U} Instant). `Input.OracleText` is byte-identical to oracle-cards.json: "Draw three cards. You may put a land card from your hand onto the battlefield tapped." Gold spell ability decomposes both sentences into typed effects: sentence 1 → `drawCards` (Count literal 3, Player You); sentence 2 → `optional` wrapper ("you may", CR 117.7) over `putFromHandOntoBattlefield` with `Filter{CardTypes:[land], Zone:Hand, Controller:You}` and `Tapped:true` (CR 400.7 hand→battlefield zone change; CR 110.5b enter-tapped). "land card" correctly maps to `CardTypes:[land]` with no spurious subtypes. No `unparsed`, no `UnstructuredEffect`, no free-text, no lossy drop/merge. Attributes (manaCost {3}{G}{U} MV5, colors [G,U], colorIdentity [G,U]) all match the card.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/MayPutCardFromHandOntoBattlefieldRule.cs` — PASS. New anchored (`^…$`) `[SpellRule]` matching sentence 2 of the bundle; emits `OptionalEffect{Inner = PutFromHandOntoBattlefieldEffect}`, reusing the pre-existing Kaalia/Spelunking effect node (newAstNode=false). Doc-comment cites CR 400.7 and CR 110.5 — both exist verbatim in rules-structure.json and their text matches the modeling (zone change → new object; permanent status includes tapped/untapped, 110.5b: "Permanents enter the battlefield untapped … unless a spell or ability says otherwise"). Filter builder maps bare/qualified card types soundly; "land" is a valid card type. `DrawCardsSimpleRule` is unchanged (sentence 1 handled via existing sentence-bundle dispatch) — no shared edits (shared=[]).
- `mast/draw-three-may-put-land#projection` — PASS. Branch introduces no new discriminator: `drawCards`, `optional`, and `putFromHandOntoBattlefield` are all pre-existing effect types. Each already carries a projection decision in `libs/mast-interaction/known-coarse-projections.json` (drawCards → baseline coarse; putFromHandOntoBattlefield → justified coarse, "no interaction flow rule reads hand-to-battlefield cheat effects yet; consciously inert for recall"). The initiative-03 ratchet has nothing new to enforce, and no flow-relevant discriminator is parked as coarse insensibly.

## Glossary gaps

_None._ Fixture uses only standard terms (battlefield, tapped, hand) already covered.

## Process notes

- The only new AST-facing artifact is a parser rule under `Parsing/Parsers/` (parser correctness is NUnit's job, out of judge scope); its rule *citations* are in-scope and check out.
- `PutFromHandOntoBattlefieldEffect` pre-exists on the base commit (verified via `git cat-file -e <baseSha>:…`), so it was ratified in the earlier Kaalia/Spelunking batch and is not re-judged here.
