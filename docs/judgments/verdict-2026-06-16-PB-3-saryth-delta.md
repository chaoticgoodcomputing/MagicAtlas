# MAST judge — PB-3 delta verdict (SarythTheVipersFang)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (tapped/untapped axis on this gold)
**Mode:** DELTA judgment (structure ONE axis; other-axis residuals belong to other slices)
**Scope:** 1 gold fixture (uncommitted working-tree change)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Delta under judgment

Working-tree diff (only change) converts the two tapped/untapped placeholders to the structured node:

- `{CharacteristicType:"other", Description:"tapped"}`   -> `{CharacteristicType:"tapped", Tapped:true}`
- `{CharacteristicType:"other", Description:"untapped"}` -> `{CharacteristicType:"tapped", Tapped:false}`

Real oracle (Scryfall oracle-cards.json, byte-identical to fixture Input/RawText):
> Other tapped creatures you control have deathtouch.
> Other untapped creatures you control have hexproof.
> {1}, {T}: Untap another target creature or land you control.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SarythTheVipersFang.json` — PASS.
  - **(a) Target structured correctly:** tapped/untapped axis -> `TappedStateCharacteristic{Tapped:bool}`; clause 1 (tapped -> deathtouch) = `Tapped:true`, clause 2 (untapped -> hexproof) = `Tapped:false`. Distinguishes the two anthem clauses exactly as the plan's per-gold checklist requires (gold-burndown-plan.md L335). AST variant + `FromLabel` mapping + `[CharacteristicKind("tapped")]` discriminator all present; converter is attribute-driven so serialization is consistent. Cited rule CR 110.5 ("tapped/untapped" permanent status) exists in rules-structure.json and matches the modeling.
  - **(b) No new free-text:** the change REMOVES free-text `Description` placeholders in favor of a typed node; nothing free-text added.
  - **(c) No regression:** both anthem clauses preserved and not inverted (deathtouch stays on tapped, hexproof on untapped); the activated `untap` arm with structured `ExcludeSelf` and all attributes untouched; literal/structured siblings serialize unchanged.

## Out-of-scope residual remaining (NOT a FAIL)

Each anthem clause still carries `{CharacteristicType:"other", Description:"other"}` (the "Other ... creatures" exclusion). This is Slice 6's axis — explicitly owned elsewhere and required to remain until Slice 6 rebases (plan L335, L339, L347-348). The whitelist-freetext.json entry for `SarythTheVipersFang` is correctly KEPT (S6-shared gold).

## Process notes

rules-structure.json / glossary.json now live under `libs/mtg-rules/Data/_03_Primary/Datasets/` (the SKILL's tabled path `tests/atlas-flow-test/...` is stale); CR 110.5 confirmed present there.

ALL PASS
