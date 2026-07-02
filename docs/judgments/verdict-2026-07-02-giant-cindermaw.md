# MAST judge — batch verdict (giant-cindermaw)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-giant-cindermaw
**Base:** 90209551
**Scope:** 1 fixture (Giant Cindermaw) + 1 projection decision (cantGainLife)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/FDN/GiantCindermaw.json` — PASS.
  Oracle text verified verbatim against oracle-cards.json ("Trample (...)\nPlayers can't gain
  life."). The target line "Players can't gain life." is modeled as a `Kind: static` ability
  carrying a single `cantGainLife` effect scoped to `Player: { Kind: EachPlayer }` — the
  symmetric all-players case. Discriminator matches CR 119.7 terminology ("a player can't gain
  life"); it names only the action (describe-not-execute) with no baked-in timing. It is a
  rules-of-the-game continuous effect (CR 611.1) written as a plain static statement (CR 604.1),
  overriding the baseline life-adjustment of CR 119.3. No free-text or unparsed residual. No
  regression: the Trample sibling is preserved (`keywordAbility` + verbatim reminder, exempt
  by design), and manaCost/colors/colorIdentity/creatureStats attributes (4/3) are intact.
- `libs/mast-interaction/known-coarse-projections.json#cantGainLife` — PASS.
  The branch adds a new discriminator `cantGainLife` and records a projection decision: a coarse
  entry justified as a CR 119.7 rules-of-the-game prohibition (sibling of cantBeCast /
  cantCastMoreThanNSpells / noMaxHandSize, all coarse restriction statics). A life-gain lock is
  a restriction, not an emit/consume port event, and no flow rule reads life-gain — it is
  genuinely inert for combo-cycle discovery, so the coarse choice is sensible, not something a
  flow rule would clearly want.

## Rule cross-reference

- CR 611.1 — present, text matches (continuous effect affecting players/rules of the game).
- CR 604.1 — present, text matches (static abilities are written as statements, simply true).
- CR 119.7 — present, text matches (authoritative can't-gain-life rule).
- CR 119.3 — present, text matches (baseline life-gain/loss adjustment).

## Glossary gaps

(none surfaced)

## Process notes

Judged read-only against the unmerged branch. The `cantGainLife` node's doc-comment and the
parser rule both cite the four rules above; all four exist in rules-structure.json and their
text is consistent with the modeling. Node keeps `Player` as an ObjectReference field rather
than baking "each player" into the discriminator, which correctly leaves room for asymmetric
"You/Your opponents can't gain life" variants.

**ALL PASS**
