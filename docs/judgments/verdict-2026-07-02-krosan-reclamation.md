# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (Krosan Reclamation) + 1 projection decision, on branch `mast-tdd/2026-07-02-krosan-reclamation`
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JUD/KrosanReclamation.json` — PASS. New `shuffleCardsFromGraveyardIntoLibrary` effect faithfully models the target line "Target player shuffles up to two target cards from their graveyard into their library." Two independent targets per CR 601.2c ("target" appears twice): `Player` (Target, filter player) and `Cards` (Target, filter `card` + `Zone: Graveyard` + `Controller: Target` back-referencing the targeted player's own graveyard), quantity `upTo` Max 2 / Min 0 ("up to two", can target zero — matches the CR 701.24 Loaming Shaman worked example). Spell ability (CR 113.3a one-shot imperative), describe-not-execute, no baked-in timing. Correctly distinct from the whole-zone `ShuffleGraveyardIntoLibraryEffect` (Player-only, CR 400.12). No `"Kind": "unparsed"` / `"EffectType": "unparsed"` and no free-text residual. Flashback sibling preserved and structured (`alternativeCast`, FromZone Graveyard, cost {1}{G}); attributes/type-line intact. Oracle text matches oracle-cards.json exactly. `CardTypes: ["player"]` is the established codebase convention for player targets.
- `libs/mast-interaction/known-coarse-projections.json#shuffleCardsFromGraveyardIntoLibrary` — PASS. The new discriminator has an explicit projection decision: a justified `known-coarse-projections.json` entry. It sits alongside the already-coarse sibling shuffle effects; no flow rule consumes graveyard-recycle shuffle events, so parking it coarse is genuinely inert for interaction recall — a sensible choice, not something a flow rule would clearly want.

## Citations cross-referenced

- CR 701.24 — exists; the Shuffle keyword action, whose own worked example is Loaming Shaman "target player shuffles any number of target cards from their graveyard into their library" (the exact bounded-variant template). Matches the modeling.
- CR 701.24a — verbatim match of the doc-comment quote ("To shuffle a library or a face-down pile of cards, randomize the cards within it...").
- CR 601.2c — confirms two independent targets when "target" appears in multiple places.
- CR 113.3a — confirms the spell-ability classification.
- CR 400.12 — confirms the whole-zone sibling distinction cited in the node doc-comment.

## Glossary gaps

(none)

## Process notes

Regression check: the fixture is a new file; the Flashback sibling is present and correctly modeled, no ability dropped/added/inverted, out-of-axis nodes (attributes, type-line) intact. The `AbilityClassifier` regex change is purely additive ("shuffles?" appended to the target-player resource-instruction alternation) and does not touch the rules-accuracy axis.
