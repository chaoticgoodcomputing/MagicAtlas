# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** bountiful-harvest (branch `mast-tdd/2026-07-02-bountiful-harvest`)
**Scope:** 1 fixture + 1 parser rule (doc-citation) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/BountifulHarvest.json` — PASS. One-shot sorcery spell effect `gainLife`; `Amount` is a `CountQuantity` (`QuantityType: count`) over `ObjectFilter{CardTypes:[land], Zone:Battlefield, Controller:You}`, faithfully describing "for each land you control" as a reference-not-resolution count (CR 119.3). Rate is 1 so the amount is the bare count. `Player: You`. No timing baked into the effect, no free-text/unparsed residual, Attributes (manaCost {4}{G} → ManaValue 5, colors/identity G) correct.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/GainLifeForEachPermanentSpellRule.cs#doc-citation` — PASS. Cites CR 119.3, which exists verbatim in `rules-structure.json` (subsection 119 "Life") and matches the gainLife modeling.
- `mast-tdd/2026-07-02-bountiful-harvest#projection-decision` — PASS. No new discriminator: reuses the pre-existing `gainLife` OracleEffect and `count` OracleQuantity (both already in the AST and exercised by many fixtures, e.g. ElvishArchdruid). No PortWalk projection decision required.

## Verification notes

- Oracle text confirmed against `oracle-cards.json`: "You gain 1 life for each land you control." ({4}{G}, Sorcery, colors G) — Input matches exactly.
- Shape matches established convention: `ElvishArchdruid.json` uses the identical `CountQuantity`/`CountOf` shape (Subtypes vs CardTypes, both `Controller:You` + `Zone:Battlefield`). Card-type value `"land"` follows the lowercase convention (99 prior occurrences).
- New file (not a regeneration of existing gold), so no dropped/inverted/added ability regression possible; single ability, single effect.

## Glossary gaps

(none)

## Process notes

The `.cs` parser rule's amount fallback logic (X/Y/Z → bare count, n>1 → CalculatedQuantity multiply) is parser behavior, out of judge scope (NUnit gates parser correctness). The gold fixture — the judged artifact — only exercises the rate-1 bare-count path, which is correct.

ALL PASS
