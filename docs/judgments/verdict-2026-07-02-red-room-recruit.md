# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** red-room-recruit
**Branch:** mast-tdd/2026-07-02-red-room-recruit
**Scope:** 1 fixture + 1 projection decision (2 AST/parser nodes added)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SPM/RedRoomRecruit.json` — PASS. "When this creature enters, it connives." modeled as a `triggered` ability: `Trigger{Timing:When, Event:Enters, Filter:{CardTypes:[creature], IsSelf:true}}` (timing is a separate composite node — NOT baked into the effect) + a plain `connive` effect with `Target:{Kind:It}`. Discriminator `connive` matches CR 701.50a terminology word-for-word. Describe-not-execute: the draw-then-discard-then-conditional-+1/+1 machinery is left to the engine (mirrors SurveilEffect/ExploreEffect). Reminder text carried verbatim in the exempt `Reminder.Text` field — not a free-text encoding of a rules concept. No `unparsed` Kind/EffectType. New single-ability card, so no sibling regression; manaCost/colors/colorIdentity/creatureStats attributes present and faithful (1/2, {1}{B}, mono-B).
- `libs/mast-interaction/known-coarse-projections.json#connive` — PASS. The new `connive` discriminator's PortWalk projection decision is present as a justified coarse entry. Sensible: connive is net-zero card flow (draw 1 / discard 1) with a conditional self +1/+1 counter — it produces no repeatable resource a flow rule chases, and it sits squarely with its coarse card-flow peers `explore`, `surveil`, `drawCards`, `discardCards`. Reason string documents CR 701.50a and "no flow rule consumes it yet."

## Rule citation cross-reference

- **CR 701.50a** — present in `rules-structure.json` verbatim: "Certain spells and abilities instruct a permanent to connive. To do so, that permanent's controller draws a card, then discards a card. If a nonland card is discarded this way, that player puts a +1/+1 counter on the conniving permanent." The `ConniveEffect` doc-comment quotes it exactly; the modeling (record keyword invocation + conniving subject) matches.

## Oracle-text fidelity

- `Input.OracleText` matches `oracle-cards.json` for Red Room Recruit exactly (including the parenthetical reminder). `{1}{B}`, Creature — Human Spy Villain, 1/2 all confirmed.

## Glossary gaps

(none — Connive is covered by CR 701.50.)

## Process notes

Regeneration diff touches only 4 files (new `ConniveEffect.cs`, new `ConniveTriggeredRule.cs`, the coarse-projection entry, and this new fixture). No pre-existing fixture altered, so no cross-card regression surface.

ALL PASS
