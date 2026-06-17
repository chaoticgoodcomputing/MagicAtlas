# MAST judge — DELTA verdict (PB-3, Saltblast)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (structured-characteristic axis + comparative-power)
**Scope:** 1 fixture (DELTA judgment — only the change this slice made)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Card

- Name: Saltblast — Sorcery, {3}{W}{W}
- Real oracle (Scryfall dump): "Destroy target nonwhite permanent." (confirmed)

## Delta judged

The slice's target residual on this gold was the free-text characteristic encoding of the
"nonwhite" qualifier:

- BEFORE: `Filter.Characteristics: [{ "CharacteristicType": "other", "Description": "nonwhite" }]`
- AFTER:  `Filter.ExcludedColors: ["W"]`

### (a) Target residual structured correctly — PASS
"nonwhite" is a color-exclusion predicate. The slice routes it to the structured
`ExcludedColors` axis on the destroy target's ObjectFilter — the documented pattern
(`ObjectFilter.cs:120`, doc example Doom Blade `ExcludedColors=["B"]`, Frazzle "nonblue").
White is a color (CR 105.1), so "nonwhite" is a color-negation, not a colorless/IsColorless
case — the axis choice is faithful to the real card.

### (b) No new out-of-scope residual introduced — PASS (primary criterion)
The free-text sink was eliminated, not relocated. No `Kind: unparsed`, no
`EffectType: unparsed`, no new `*Description`/`*Raw`/`Characteristics` free-text anywhere in
`Output.Oracle.Abilities`.

### (c) No regression — PASS
Single `destroy` effect preserved; co-occurring sibling filter `CardTypes: ["permanent"]`
retained; `CantBeRegenerated: false` is the normal serialized default (no behavior change);
mana-cost / colors / colorIdentity attributes unchanged in meaning. No ability dropped,
added, or inverted.

## Scope note

Saltblast is NOT one of the [S6-SHARED] golds (AdeptWatershaper / SarythTheVipersFang), so
full cleanup is correct here and the gold carries no remaining residual on any axis —
appropriate for removal from whitelist-freetext.json.

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Saltblast.json` — PASS. "nonwhite" free-text
  characteristic → structured `ExcludedColors: ["W"]` (CR 105.1 color-exclusion axis); no new
  residual, no regression.

## Closing

PROCEED. 1 PASS / 0 FAIL. Clean delta: the structured-characteristic axis was applied
correctly with no out-of-scope debt introduced.
