# MAST judge — PB-3 delta verdict (10E/Vengeance)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (delta judgment, not whole-gold purity)
**Scope:** 1 fixture (10E/Vengeance, uncommitted working-tree regen)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/10E/Vengeance.json` — PASS.
  Oracle: "Destroy target tapped creature." The slice's TARGET residual — the
  `tapped` characteristic, previously the free-text sink
  `{CharacteristicType:"other", Description:"tapped"}` — is now structured as
  `TappedStateCharacteristic{Tapped:true}` on the destroy effect's target
  `creature` filter. Faithful to the real card (CR 110.5 / 110.5a govern the
  tapped/untapped status category; both confirmed present in rules-structure.json).
  No regression: the `destroy` EffectType, the `Target` reference, and the
  `creature` CardType are all preserved; the new node is a filter predicate with
  no firability change. Whitelist-freetext entry for `10E/Vengeance`
  (sink OtherCharacteristic) correctly removed — the gold is fully cleaned.
  Schema export carries `TappedStateCharacteristic` (discriminator `"tapped"`,
  IsUnparsed:false). Zero free-text / unparsed residual remains on this gold.

## Delta-scope notes

- Vengeance is NOT an S6-shared gold, so no other/another-exclusion residual is
  expected here; correctly none remains.
- The new `TappedStateCharacteristic` is an ObjectFilter predicate, not a new
  effect/cost/trigger/restriction discriminator — it does not affect PortWalk
  firability (the interaction layer does not switch on CharacteristicKind), so
  no projection decision is owed for THIS gold's delta. (Projection-presence for
  the slice's broader work is judged elsewhere.)

## Process notes

- rules-structure.json / glossary.json live under `libs/mtg-rules/Data/_03_Primary/Datasets/`
  in this checkout (not `tests/atlas-flow-test/...` as the SKILL table lists).
- The TappedStateCharacteristic doc-comment carries a secondary parenthetical
  "CR 701.21" alongside the load-bearing "CR 110.5"; 701.21 is "Sacrifice" in the
  rules data, not Tap/Untap — a minor doc-comment slip, NOT load-bearing on the
  Vengeance node (which is grounded in 110.5) and not a FAIL under the SKILL's
  secondary-citation tolerance. Worth a one-line doc fix later.

ALL PASS
