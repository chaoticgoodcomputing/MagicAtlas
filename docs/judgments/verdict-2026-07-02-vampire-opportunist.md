# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** vampire-opportunist
**Branch:** mast-tdd/2026-07-02-vampire-opportunist
**Scope:** 1 fixture + projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/VampireOpportunist.json` — PASS. Oracle text verified against oracle-cards.json verbatim (`{6}{B}: Each opponent loses 2 life and you gain 2 life.`, {1}{B} 2/1 Vampire, colors/CI B). Activated ability: mana cost {6}{B} correctly decomposed (generic 6 + colored B); the "… and …" sentence expands to two flat sibling effects — `loseLife` (EachOpponent, literal 2) and `gainLife` (You, literal 2). Describe-not-execute, no baked-in timing (plain activated ability, no trigger wrapper). Both conjuncts preserved, neither dropped nor inverted. No free-text/unparsed residual (RawText is verbatim-by-design). CR 119.3 exists and matches ("If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly.").
- `mast-tdd/2026-07-02-vampire-opportunist#projection` — PASS. No new discriminator: `loseLife`/`gainLife` (LoseLifeEffect/GainLifeEffect) and `EachOpponent`/`You` (ObjectReferenceKind, base line 75) all pre-exist on base. The branch adds only a composite parser rule (EachOpponentLosesLifeYouGainLifeEffectRule) that reuses existing nodes, so the PortWalk projection surface is unchanged — no projection decision required.

## Glossary gaps

(none)

## Process notes

Branch adds a parser rule (`libs/magic-ast/Parsing/Parsers/Activated/Rules/EachOpponentLosesLifeYouGainLifeEffectRule.cs`) whose doc-comment cites CR 119.3 — verified present and matching. Parser correctness is out of judge scope (NUnit gate owns it); the fixture is the judged gold artifact.
