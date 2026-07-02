# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (DwarvenSong) on branch `mast-tdd/2026-07-02-dwarven-song` + projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DwarvenSong.json` — PASS. Oracle text matches oracle-cards.json verbatim ("One or more target creatures become red until end of turn."). The single `Kind: spell` ability (instant spell ability, CR 113.3a) carries one `changeColor` effect: `Target` reference with `Filter.CardTypes:["creature"]` and `Quantity:{atLeast, Minimum:1}` faithfully models "one or more target creatures" (CR 115.1 targeting; CR 601.2c variable target count); `Colors:["R"]` is the correct color code for "red" (CR 105.1); the layer-5 color change is grounded in CR 105.3 / CR 613.1e. Timing is a separate composable `Duration:{untilTime, Until:{Turn/End}}` field, NOT baked into the effect discriminator — correct describe-not-execute composite. No free-text, no `unparsed`, no `Characteristics` residual. New file, so no siblings/prior abilities to regress; the card's lone ability is complete.
- `mast-tdd/2026-07-02-dwarven-song#projection` — PASS. Branch introduces no new effect/cost/trigger/restriction discriminator: `changeColor` is the pre-existing `ChangeColorEffect` (already emitted for e.g. Metathran Transport), reused unchanged. The only new node is `AtLeastQuantity` ("atLeast"/Minimum), a target-count refinement of an existing target reference — not a projection-relevant discriminator — so no PortWalk projection decision is required and the exhaustiveness ratchet has nothing to enforce here.

## Glossary gaps

None.

## Process notes

- CR cross-reference (rules-structure.json): 105.3 (color-change effects), 613.1e (Layer 5 color-changing), 115.1 (one-or-more targeting), 601.2c (variable number of targets), 113.3a (spell abilities), 105.1 (five colors) — all present and their text matches the modeling and doc-comments in `TargetsBecomeColorRule.cs` / `AtLeastQuantity.cs`.
- `AtLeastQuantity` correctly distinguishes "one or more" (min 1, no ceiling) from `anyAmount` (min 0) and `upTo` (bounded above) — the doc-comment calls this out and the fixture uses the right one.
- The `Raw` / `RawText` fields in the fixture (type line, oracle container, mana cost) are verbatim-by-design source strings, not rules-load-bearing free text — exempt.
