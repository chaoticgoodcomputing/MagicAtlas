# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** mnemonic-nexus
**Branch:** mast-tdd/2026-07-02-mnemonic-nexus
**Scope:** 1 fixture + 1 projection decision (supporting: 1 new effect node, 1 spell rule, schema entry)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/RAV/MnemonicNexus.json` — PASS. Oracle text verified against oracle-cards.json verbatim ("Each player shuffles their graveyard into their library."). The single spell ability emits one structured `shuffleGraveyardIntoLibrary` effect with `Player: {Kind: EachPlayer}` — a faithful whole-zone recycle. `EachPlayer` is a valid `ObjectReferenceKind` (ObjectReference.cs:78). Describe-not-execute, no timing baked into the effect (Kind: spell), no free-text/unparsed residual, and no regression (new single-ability instant; mana/color/colorIdentity attributes intact). Cited CR 400.12 (whole-zone action performed on all cards in the zone) and CR 701.24a (shuffle = randomize) both exist in rules-structure.json and match the modeling.
- `libs/mast-interaction/known-coarse-projections.json#shuffleGraveyardIntoLibrary` — PASS. The new discriminator introduces a projection decision; the branch parks it coarse with a justified reason, consistent with its already-coarse `shuffle` and `shuffleIntoLibrary` siblings. No flow rule currently reads graveyard-recycle shuffle events, so the inert carve-out is sensible rather than a flow-relevant signal parked coarse.

## Glossary gaps

(none)

## Process notes

New effect node `ShuffleGraveyardIntoLibraryEffect` is well-distinguished from `ShuffleIntoLibraryEffect` (specific target object) and `ShuffleEffect` (library-only, no zone move) in its doc-comment, with correct CR 400.12 / 701.24a citations. Schema registers the discriminator with `IsUnparsed: false`. Nothing routed to design.
