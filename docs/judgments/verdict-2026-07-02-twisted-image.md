# MAST judge — batch verdict (twisted-image)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-twisted-image
**Scope:** 1 fixture + 1 projection decision (new `switchPT` discriminator)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WWK/TwistedImage.json` — PASS. Oracle text verified
  verbatim against oracle-cards.json ("Switch target creature's power and toughness until end of turn.\nDraw
  a card.", Instant, {U}). The target line is a first-class `switchPT` effect whose discriminator matches
  CR 613.4d's own verb ("switch a creature's power and toughness"). `Target` = `Kind: Target` + `CardTypes:
  ["creature"]` is faithful to "target creature". "Until end of turn" is carried by a **separate** composite
  `Duration { untilTime, Turn/End }` node (borne on the `ContinuousEffect`/CR 611 base), not baked into the
  effect discriminator — describe-not-execute honored. Sibling ability "Draw a card." is preserved as a
  `drawCards` effect (literal 1, Player You). No free-text `Characteristics`, no `unparsed` node anywhere.
  New file, so no prior AST to regress; both abilities and all attributes present. The node doc-comment
  correctly distinguishes `switchPT` (a single creature's own P/T, layer 7d) from `ExchangeCharacteristicEffect`
  (between two objects) and additive `modifyPT` (layer 7c).

- `mast-tdd/2026-07-02-twisted-image#projection:switchPT` — PASS. The new `switchPT` discriminator carries a
  **semantic** PortWalk projection: `PortGraph.cs` adds `"switchPT" => Port(card, "switch:pt", Emit)` and
  `PortWalkProjection.cs` registers `switchPT`. A P/T switch is an inert stat modification (no mana/untap/
  life/damage/trigger flow), so an edge-sparse emit port mirroring `modifyPT` is the sensible choice; notably
  it is NOT parked in `known-coarse-projections.json` — it gets the stronger semantic label. Nothing a flow
  rule would clearly want is being lost to a coarse bucket.

## Glossary gaps

(none)

## Process notes

CR 613.4d cross-referenced in `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`:
"Layer 7d: Effects that switch a creature's power and toughness are applied. Such effects take the value of
power and apply it to the creature's toughness, and take the value of toughness and apply it to the creature's
power." — exists and matches the modeling exactly. The schema's `SwitchPTEffect.Fields: ["Target"]` lists only
the record's own declared field; `Duration` is inherited from `ContinuousEffect` and its emission in the
fixture is valid (parser/schema mechanics are NUnit's domain, out of judge scope).

ALL PASS
