# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** sphere-of-law
**Branch:** mast-tdd/2026-07-02-sphere-of-law
**Base:** cb048c63ea6ae85ef069e0d47244ec68945a5415
**Scope:** 1 fixture + 1 branch projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SphereofLaw.json` — PASS. Oracle text matches oracle-cards.json verbatim ("If a red source would deal damage to you, prevent 2 of that damage."). Modeled as `Kind: static` → `ReplacementEffect` watching a `DamageEvent` (`Source.Colors=[R]` = "a red source", `AffectedObjects.Controller=You` = "to you") whose `Replacement` is `preventDamage` with `Amount` literal 2 and `Target You`. Right node/discriminators, faithful to the card, describe-not-execute, no baked-in timing (static ability carries no timing wrapper), no free-text/unparsed residual. Sibling out-of-axis nodes correct (mana {3}{W} → ManaValue 4, Colors [W], ColorIdentity [W]). New card fixture, so no regression surface. CR 615.10's own example is this exact template with N=1; 615.1/615.1a/615.2 cited in the parser doc-comment all exist and match.
- `mast-tdd/2026-07-02-sphere-of-law#projection` — PASS. Branch adds only a parser rule + fixture and reuses existing AST nodes (`preventDamage`, `damage` replacement-event, `replacement`) confirmed present on the base; the color/controller constraints are `ObjectFilter` fields, not new discriminators. No new effect/cost type, trigger event, or restriction → PortWalk projection ratchet not triggered; no new projection decision required.

## Glossary gaps

_none_

## Process notes

- `OriginalEventOccurs: false` on the replacement wrapper for a *partial* prevention ("prevent 2 of that damage", residual still dealt) is a replacement-application detail — engine semantics, explicitly out of judge scope. The descriptive content (static, red source, to you, prevent 2) is faithful and structured, so it does not affect the verdict.
- The prevention concept is structured via the `preventDamage` discriminator (CR 615.1a "prevent"), not free text. Whether a bounded-amount prevention static should nest `preventDamage` under `ReplacementEffect` vs. sit as a bare `ContinuousEffect` is an AST-family structural question (engine-lens audit), out of scope here.

ALL PASS
