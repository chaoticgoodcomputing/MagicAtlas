# MAST judge — PB-3 delta verdict: Crowned Ceratok

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (counter / tapped-state axis + comparative-power merge)
**Scope:** 1 gold (delta judgment) — `tests/magic-ast-tests/Fixtures/HandParsedCards/CrownedCeratok.json`
**Result:** PASS

## Summary

- PASS: 2 (gold residual + projection decision)
- FAIL: 0

## Card

Oracle text (confirmed vs oracle-cards.json / Scryfall dump):
> Trample
> Each creature you control with a +1/+1 counter on it has trample.

## Target axis this slice owned

The free-text counter-presence characteristic. Pre-slice gold carried it as a typed
residual:
`{ "CharacteristicType": "other", "Description": "with a +1/+1 counter" }`.
Post-slice it is the structured `CounterCharacteristic`:
`{ "CharacteristicType": "counter", "CounterType": "+1/+1" }`.

## Per-criterion findings

- **(a) Target structured correctly** — PASS. The counter axis maps to the new
  `CounterCharacteristic{CounterType:"+1/+1"}` (CR 122.1 — counters are markers on an
  object). `Characteristic.FromLabel` maps both "with a +1/+1 counter" and
  "...on it" to this node. The node and its `[CharacteristicKind("counter")]`
  discriminator are registered in `ast-schema.json`. Faithful to the printed card.
- **(b) No new out-of-scope residual** — PASS. No remaining `"other"` or `"unparsed"`
  node anywhere in the gold. Crowned Ceratok correctly removed from
  `whitelist-freetext.json`.
- **(c) No regression** — PASS. Normalized abilities diff (old vs new) shows the
  counter conversion as the ONLY semantic change. Trample keyword ability, the
  `gainAbility` effect, the `Each` target with `CardTypes:["creature"]` +
  `Controller:"You"` co-occurring filters, and the `GainedAbility` Trample body are
  all preserved. Other reorderings (ManaValue/IsVariable, Power/Toughness key order)
  serialize equivalently.

## Projection decision (initiative 03)

The new `counter` (and sibling `tapped`) CharacteristicKind are ObjectFilter
sub-predicates, not new firable effect/trigger/cost discriminators — the slice spec
itself notes "no firability change." The ObjectFilter projects as a unit; these
predicates do not introduce a new top-level PortWalk node, so the ratchet requires no
`PortGraph`/`PortWalkProjection` entry and none is parked insensibly in
`known-coarse-projections.json`. Sensible.

## Out-of-scope residual remaining on this gold

None. This gold's sole filter axis was the counter predicate, which this slice owned.

## Verdict

`tests/magic-ast-tests/Fixtures/HandParsedCards/CrownedCeratok.json` — PASS.
Structured the +1/+1 counter characteristic (CR 122.1) into `CounterCharacteristic`;
no new residual, no regression, no out-of-scope axis left behind.

ALL PASS
