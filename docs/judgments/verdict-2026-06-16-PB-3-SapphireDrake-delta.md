# MAST judge — DELTA verdict: SapphireDrake (SLICE PB-3)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (consolidated structured-characteristic axis + comparative-power PB-2 merge)
**Scope:** 1 fixture (delta judgment, NOT whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## What the slice structured on this gold

Oracle text (confirmed against oracle-cards.json / Scryfall):
> Flying
> Each creature you control with a +1/+1 counter on it has flying.

The PB-3 target axis for SapphireDrake is the structured-characteristic residual inside the
second static ability's `gainAbility` Each-filter. The slice replaced:

- `{ "CharacteristicType": "other", "Description": "with a +1/+1 counter" }`

with the new structured node:

- `{ "CharacteristicType": "counter", "CounterType": "+1/+1" }`  (CounterCharacteristic, CR 122)

This is the correct axis and the correct new node (`CounterCharacteristic` in
AST/References/Characteristic.cs, cited to CR 122 — confirmed present in rules-structure.json,
subsection 122 "Counters"), faithful to the printed "with a +1/+1 counter on it".

## Delta criteria

- (a) Target residual structured CORRECTLY: YES — right node (CounterCharacteristic), right axis,
  right CounterType "+1/+1", faithful to the real card.
- (b) NO new free-text/unparsed residual beyond scope: CONFIRMED — zero remaining `other` /
  `Description` / `unparsed` nodes anywhere in the gold (scanned full ability tree;
  CharacteristicTypes are now only [keyword, keyword, counter, keyword, keyword]).
- (c) NO regression: CONFIRMED — key-normalized diff of the entire Abilities tree shows EXACTLY
  the counter restructure and nothing else. Both static abilities preserved (count = 2). The
  gainAbility Each-filter's co-occurring siblings (CardTypes:[creature], Controller:You) are intact;
  the granted Flying evasion (Flying/Reach blockers) is unchanged. Remaining churn is serializer
  field-ordering (KeywordSource after Effects, IsVariable added, Power/Toughness Value/Raw order) —
  no semantic change.

## Out-of-scope residual remaining

None. SapphireDrake carried only the one structured-characteristic (counter) residual, which this
slice owns and resolved. The gold was correctly removed from whitelist-freetext.json (fully cleaned;
not an S6-shared gold).

## Verdict

`tests/magic-ast-tests/Fixtures/HandParsedCards/SapphireDrake.json` — PASS. Structured the
"with a +1/+1 counter" residual into CounterCharacteristic (CR 122); no new residual, no regression,
no out-of-scope residual remains.

ALL PASS
