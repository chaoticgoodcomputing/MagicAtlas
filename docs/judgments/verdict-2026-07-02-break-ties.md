# MAST judge — batch verdict (break-ties)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-break-ties
**Base:** 176e495dda71494b915330f72bde000e5cd90f0f
**Scope:** 3 files (1 fixture, 2 AST nodes) + 1 projection check
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TSP/BreakTies.json` — PASS. Oracle text matches oracle-cards.json verbatim. Target line "Reinforce 1—{W} ({W}, Discard this card: Put a +1/+1 counter on target creature.)" is unpacked into an `activated` ability (`KeywordSource: "Reinforce"`) per CR 702.77a: Costs = mana {W} + `discard` (1 card), Effects = `putCounters` (CounterType "+1/+1", literal Count 1) on `Target` creature. Faithful, describe-not-execute, no baked-in timing. Reminder text kept verbatim (exempt field). The modal sibling ("Choose one —" destroy artifact / destroy enchantment / exile card from a graveyard) is fully preserved and correctly structured (ModeSelection 1–1, AllowDuplicates false; exile carries `Zone: Graveyard`). No unparsed/OtherX/free-text residual anywhere.
- `libs/magic-ast/Keywords/Definitions/ReinforceKeyword.cs` — PASS. Decomposition (mana cost + `DiscardCost` "this card" + `PutCountersEffect` with literal N) mirrors the established `CyclingKeyword`/`ScavengeKeyword` conventions. Doc-comment cites CR 702.77a/b verbatim; both subrules exist in `rules-structure.json` and their text matches the modeling. "Discard this card" modeled as `DiscardCost{Filter:card, Quantity:1}` is the maximal fidelity the node supports and matches the sibling keyword — not a free-text shortcut.
- `libs/magic-ast/AST/References/KeywordAbility.cs#Reinforce` — PASS. New `Reinforce` enum member cites CR 702.77 (exists in data); no existing member changed.
- `mast-tdd/2026-07-02-break-ties` (projection) — PASS. No new port-relevant discriminator introduced: Reinforce reuses the existing `putCounters` effect and `discard`/`mana` cost types (already projected). `KeywordSource` is provenance only. No PortWalk projection decision required.

## Glossary gaps

None. "Reinforce" is a CR-defined keyword (702.77); no undefined domain term surfaced.

## Process notes

- CR 702.77 cross-referenced against `rules-structure.json`: subrules a and b both present, verbatim-matching the doc-comment quotes.
- Fixture is a net-new file on the branch; both card abilities (modal + Reinforce) are modeled — nothing dropped, added, or inverted.

**ALL PASS**
