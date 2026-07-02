# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** abandon-the-post (branch `mast-tdd/2026-07-02-abandon-the-post`)
**Scope:** 1 new fixture (MID/AbandonThePost.json) + supporting parser rule/classifier route
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MID/AbandonThePost.json` — PASS.
  Oracle line "Up to two target creatures can't block this turn." is modeled as a `spell`
  ability with a single `cantBlock` effect: `Target` (Kind=Target, Filter CardTypes:[creature])
  carrying an `upTo` Quantity (Maximum 2, Minimum 0) plus a separate `untilTime` duration
  (Turn/End). Right node + discriminator, faithful to the card ("up to two" → bounded target
  count on the reference, not an effect-level count), describe-not-execute, and no firing timing
  baked into the effect discriminator. CR 115.1 (Targets — "up to N target" is a bounded target
  count) and CR 509.1 (Declare Blockers Step — blocking restrictions resolve there) both exist in
  rules-structure.json and match the modeling. No new free-text/unparsed residual. Siblings and
  out-of-axis nodes preserved: the Flashback {3}{R} line (own axis) is intact as a `static`
  ability with an `alternativeCast` effect from Graveyard, and all Attributes (manaCost {1}{R}
  MV 2, colors R, colorIdentity R) are faithful.

- `...AbandonThePost.json#projection` — PASS.
  No new discriminator is introduced. `cantBlock` (CantBlockEffect, `[OracleEffect("cantBlock")]`),
  `upTo` (UpToQuantity, `[OracleQuantity("upTo")]`), the `Target` reference kind, the `creature`
  filter, and the `untilTime` duration all pre-exist on the base sha. The branch reuses existing
  vocabulary (mirroring TapUpToNTargetsRule's up-to-N target plumbing and
  CreaturesCantBlockThisTurnRule's cant-block shape), so the initiative-03 exhaustiveness ratchet
  is not triggered and no PortWalk projection decision is required.

## Glossary gaps

None.

## Process notes

Fixture is net-new on this branch (absent at base 539b20a8), so there is no prior fixture to
regress against; verified instead that the fixture faithfully covers BOTH oracle lines and that
the target line's discriminators are all pre-existing (no silently-introduced escape hatch).
The new parser rule's doc-comment cites Rule 115.1 and Rule 509.1; the classifier route cites
Rule 509.1 — all present in the rules data. Subrule-letter imprecision (509.1 vs 509.1c) is
non-blocking per doctrine.

ALL PASS
