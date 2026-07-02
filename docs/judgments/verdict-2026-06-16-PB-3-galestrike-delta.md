# MAST judge — PB-3 delta verdict (Galestrike)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (structured-characteristic axis + comparative-power PB-2 merge)
**Scope:** 1 fixture (delta judgment, uncommitted working tree)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Target

Real oracle text (oracle-cards.json, confirmed): "Return target tapped creature to its owner's hand.\nDraw a card."

The slice's target axis on this gold is the structured-characteristic axis — specifically the
"tapped" state constraint on the bounce target.

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/Galestrike.json
**Verdict:** PASS

- (a) Target residual structured correctly: the prior free-text
  `{"CharacteristicType":"other","Description":"tapped"}` is now the new
  `TappedStateCharacteristic{"Tapped":true}` — the exact axis this slice introduced.
  CR 110.5 establishes tapped/untapped as a two-valued permanent status, which the
  `bool Tapped` field captures precisely; modeling is faithful to "target tapped creature".
- (b) No new free-text/unparsed residual: gold contains no `unparsed`, no `OtherCharacteristic`,
  no stray `Description` field anywhere.
- (c) No regression: both abilities preserved — `returnToHand` over a `Target` whose filter keeps
  the co-occurring `CardTypes:["creature"]` sibling, plus `drawCards{Count: literal 1, Player: You}`.
  Nothing dropped, added, or inverted. Other (literal/structured) nodes serialize unchanged; the
  only diff beyond the tapped axis is benign input-block cleanup (`Power`/`Toughness` null removal)
  and `IsVariable:false` on the mana-cost attribute.
- Out-of-scope residual: NONE. Galestrike carried a single residual (the tapped axis) and the slice
  reached it, so the card is fully cleaned and is correctly absent from whitelist-freetext.json and
  oracle-text-quarantine.json.

## Citation cross-reference

- CR 110.5 — present in rules-structure.json: "A permanent's status is its physical state. There are
  four status categories, each ... tapped/untapped ...". Matches the modeling. PASS.
- (Node doc also points at CR 701.21 for sacrifice via the glossary's tap/untap reference; the
  load-bearing 110.5 citation is correct and that secondary pointer does not affect this gold.)

## Process notes

Data files (rules-structure.json / glossary.json) live under libs/mtg-rules/Data/_03_Primary/Datasets/,
not the path named in the SKILL table; cross-referenced there. This is a delta judgment scoped only to
the structured-characteristic axis on Galestrike.

ALL PASS
