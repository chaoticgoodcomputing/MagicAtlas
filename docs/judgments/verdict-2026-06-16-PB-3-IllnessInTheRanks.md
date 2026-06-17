# MAST judge — DELTA verdict (Slice PB-3)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice
**Scope:** 1 gold (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Target

`tests/magic-ast-tests/Fixtures/HandParsedCards/GTC/IllnessInTheRanks.json`

Real oracle (oracle-cards.json / Scryfall): "Creature tokens get -1/-1." — Type: Enchantment. Matches the fixture Input exactly.

## Per-item verdict

- `tests/magic-ast-tests/Fixtures/HandParsedCards/GTC/IllnessInTheRanks.json` — PASS.

### (a) Target residual structured correctly
The slice replaced the free-text characteristic
`{"CharacteristicType": "other", "Description": "token"}`
with the structured axis `IsToken: true` on the `Each`-kind filter. `IsToken` is the real `bool?` ObjectFilter axis the slice spec lists for reuse (libs/magic-ast/AST/References/ObjectFilter.cs:44). Faithful to CR 111.1 ("A token is a marker used to represent any permanent that isn't represented by a card.") and to the real card text "Creature tokens".

### (b) No new free-text introduced (PRIMARY criterion)
No new unparsed / free-text node. The only remaining string fields are the exempt verbatim `Raw` / `RawText` (type line, oracle raw text, mana cost) — verbatim-by-design, not rules-meaningful structure.

### (c) No regression
- `EffectType: modifyPT` preserved; `PowerModifier: -1`, `ToughnessModifier: -1` preserved.
- Co-occurring `CardTypes: ["creature"]` filter preserved alongside the new `IsToken` axis.
- No dropped / added / inverted ability or effect (single static ability, intact).
- Mana-cost attribute gained `IsVariable: false` — benign serialization-completeness addition consistent with the schema regen, not a semantic change.

## Out-of-scope residual

None. This is a single-ability card whose sole residual was the token characteristic, now fully cleaned. Card correctly carries no whitelist-freetext entry (not an S6-shared gold).

## Process notes

- Rules data lives at `libs/mtg-rules/Data/_03_Primary/Datasets/` (not the `tests/atlas-flow-test/...` path the SKILL table names — mtg-rules is the live location).
- Judged only the GTC/IllnessInTheRanks delta; the broader PB-3 batch (other golds + the 192-line whitelist purge) is out of this single-target dispatch.

ALL PASS
