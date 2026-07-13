# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/reveal-any-number-green-cards
**Family:** reveal-any-number-of-green-cards ("Scent of Ivy")
**Scope:** 3 files (1 fixture, 2 spell-rule parsers) + 1 projection check
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TOR/ScentOfIvy.json` — PASS. Input.OracleText byte-identical to Scryfall (`{G}` Instant, "Reveal any number of green cards in your hand. Target creature gets +X/+X until end of turn, where X is the number of cards revealed this way."). Gold cleanly decomposes into two structured effects: `revealCards` (Player=You, Count=anyAmount, Zone=Hand, Filter=`{card, G}`) and `modifyPT` (Target creature, PowerMod/ToughnessMod = `cardsRevealedThisWay`, Duration untilTime end of turn). No `unparsed`/IUnparsed/UnstructuredEffect, no lossy drop or merge. It is the exact positive mirror of the committed Scent of Nightshade `-X/-X` sibling (which negates via `calculated × -1`).
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/RevealAnyNumberGreenCardsFromHandRule.cs` — PASS. Anchored `^Reveal any number of green cards in your hand$` → `RevealCardsEffect`. Doc-comment cites **CR 701.20a** ("To reveal a card, show that card to all players for a brief time…"), which is present verbatim in `rules-structure.json` and matches the modeling. Structurally identical to `RevealAnyNumberBlackCardsFromHandRule` save `Colors=["G"]`.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/TargetCreatureGetsPlusXRevealedThisWayRule.cs` — PASS. Anchored payoff clause → `ModifyPTEffect` with `CardsRevealedThisWayQuantity` used directly for both P and T (correct: the "+X" needs no negation, unlike the sibling's `CalculatedQuantity` × -1). Timing is a composed `Duration` node, not baked into the effect. No CR citation, which is fine — the count-reference link is textual (ADR 0004 reference-not-resolution). `Priority = 60` matches the sibling and is documented as defensive precedence over the generic `ModifyPTSpellRule`.
- `mast/reveal-any-number-green-cards#projection` — PASS. Worker reports `newAstNode=false`, `shared=[]`; all emitted effect/quantity types (`RevealCardsEffect`, `ModifyPTEffect`, `CardsRevealedThisWayQuantity`, `AnyAmountQuantity`) already exist. No new discriminator ⇒ the PortWalk exhaustiveness ratchet is not triggered and no new `PortGraph` case / `known-coarse-projections.json` entry is required.

## Glossary gaps

(none — "reveal" is glossary-indexed at CR 701.20; count references are structured quantity nodes.)

## Process notes

Cross-checked the gold against the real card via `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (byte-identical). Confirmed the four referenced AST types pre-exist under `libs/magic-ast/AST/` and that the two sibling rules (`RevealAnyNumberBlackCardsFromHandRule`, `TargetCreatureGetsMinusXRevealedThisWayRule`) are the claimed mirrors. No shared-file edits in the diff (three new files only), so no generalization to review.

ALL PASS
