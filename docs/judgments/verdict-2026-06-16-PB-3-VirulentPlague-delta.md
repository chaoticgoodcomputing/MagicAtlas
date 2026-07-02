# MAST judge — PB-3 delta verdict: DTK/VirulentPlague

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (consolidated ATOMIC: structured-characteristic axis + comparative-power)
**Scope:** 1 fixture (delta judgment — working-tree, uncommitted)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## What the slice changed (delta)

Real oracle text (Scryfall / oracle-cards.json): "Creature tokens get -2/-2." (Enchantment, {2}{B}).

The slice structured the token-characteristic residual on the `modifyPT` target filter:

- Before: `"Characteristics": [{"CharacteristicType": "other", "Description": "token"}]` (free-text residual).
- After: `"IsToken": true` — the established ObjectFilter token/nontoken axis (CR 111). Faithful to "Creature tokens".

Co-occurring sibling axis `CardTypes: ["creature"]` preserved; the `modifyPT` -2/-2 PowerModifier/ToughnessModifier preserved unchanged. A serialization-only delta adds `"IsVariable": false` to the manaCost attribute (the established `IsVariable` field on the AST) — descriptive, not a residual.

## Delta criteria

- (a) TARGET residual structured correctly: free-text `{other, token}` -> `IsToken: true`, the right axis, faithful to the card. PASS.
- (b) No new free-text/unparsed residual introduced: the gold now carries zero free-text Characteristics/Description nodes and no unparsed Kind/EffectType. PASS.
- (c) No regression: single static modifyPT ability intact; -2/-2 modifiers and creature filter preserved; no dropped/added/inverted effect. PASS.

## Out-of-scope residual remaining

None. VirulentPlague is fully cleaned by this slice (it is not an [S6-SHARED] gold). No comparative-power or combat-state axis applies to this card.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DTK/VirulentPlague.json` — PASS. Free-text token characteristic restructured to `IsToken: true` (CR 111); sibling creature filter + modifyPT -2/-2 preserved; no new residual; gold fully cleaned.

ALL PASS
