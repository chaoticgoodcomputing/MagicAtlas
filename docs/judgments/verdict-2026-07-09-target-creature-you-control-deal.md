# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/target-creature-you-control-deal
**Base:** 6b4b1d17083f7b580b0223119b7a0fdea73d7d30
**Scope:** 2 files (1 fixture, 1 new SpellRule) + 1 projection verdict
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/BLB/RockyRebuke.json` — PASS. `Input.OracleText`
  is byte-identical to the real card (verified against `oracle-cards.json`: "Target creature you
  control deals damage equal to its power to target creature an opponent controls."); mana cost
  {1}{G}, colors [G], color identity [G], type Instant all match. The single-sentence effect is
  fully structured as one `dealDamage` effect: `Source` = target creature you control
  (`Controller: You`), `Amount` = `derived` `Power` from `"it"` ("equal to its power"), `Target` =
  target creature an opponent controls (`Controller: Opponent`). No `unparsed`, no
  `UnstructuredEffect`, no lossy drop/merge. `IsCombat` left absent (null) correctly marks
  non-combat damage — bite damage is not combat, mirroring Fight (CR 701.14d). Grounded in CR 120.1
  ("An object that deals damage is the source of that damage").

- `libs/magic-ast/Parsing/Parsers/Spell/Rules/BiteOpponentControlsSpellRule.cs` — PASS. New
  `[SpellRule]` that recognises the "an opponent controls" controller phrasing (vs the sibling
  `BiteRule`'s "you don't control"). Regex is anchored `^...$` on the full sentence, so it cannot
  over-swallow a broader/narrower sibling. Emits the identical, pre-existing `DealDamageEffect`
  shape as `BiteRule` (Source you-controlled creature, derived Power from "it", Target
  opponent-controlled creature, optional "or planeswalker" broadening CardTypes). Reuses the
  existing `DealDamageEffect` node (`[OracleEffect("dealDamage")]`, `bool? IsCombat`) — no new AST
  node. Cited CR 120.1 (damage source) and CR 701.14 (Fight, contrasted as the symmetric case;
  701.14d confirms fight/bite damage is non-combat) both exist in `rules-structure.json` and match
  the modeling. The doc-comment's claim that "an opponent controls" and "you don't control" denote
  the same object set holds (a permanent is always controlled by exactly one player; any non-you
  controller is an opponent) — both map to `ControllerFilter.Opponent`.

- `mast/target-creature-you-control-deal#projection` — PASS. Worker reports `newAstNode=false`,
  `shared=[]`; the diff adds only the new SpellRule + fixture. No new discriminator (effect/cost
  type, trigger event, or restriction) is introduced — the rule reuses the pre-existing
  `DealDamageEffect` and `ControllerFilter.Opponent`, both already projected. No PortWalk projection
  decision is required for this branch; the exhaustiveness ratchet would not trip.

## Glossary gaps

None. (Damage / Fight are standard CR terms already covered.)

## Process notes

Trustworthy to merge. The change is a narrow, anchored parser addition parallel to the existing
`BiteRule`, with a fully-structured gold fixture and correct CR grounding. No shared-code edits to
generalize.

ALL PASS
