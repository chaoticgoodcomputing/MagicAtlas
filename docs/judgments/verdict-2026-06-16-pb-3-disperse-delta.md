# MAST judge — PB-3 delta verdict (Disperse)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (delta judgment)
**Scope:** 1 fixture (Disperse.json, uncommitted working-tree regen)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/Disperse.json
**Verdict:** PASS

Real oracle text (confirmed against Scryfall raw dump `oracle-cards.json`):
"Return target nonland permanent to its owner's hand." ({1}{U} Instant).

**(a) Target residual structured correctly.** The slice converted the free-text
"nonland" qualifier from `Characteristics: [{"CharacteristicType":"other","Description":"nonland"}]`
to the structured negation axis `ExcludedCardTypes: ["land"]`, sitting alongside
`CardTypes: ["permanent"]`. This is exactly the axis the slice owns and the pattern
the `ObjectFilter.ExcludedCardTypes` doc-comment documents ("a nonland card →
CardTypes + ExcludedCardTypes=['land']"). Faithful to the card: a nonland permanent
is a permanent (CR 110.1) that is not a land (CR 110.4 lists land among the six
permanent types).

**(b) No new residual introduced.** The ability tree is fully structured — no
`Characteristics`, `Description`, `Raw` semantic free-text, or `unparsed` node
remains in `Output.Oracle.Abilities`. The free-text sink was eliminated, not moved.

**(c) No regression.** The single `returnToHand` spell effect and its `Target`
are preserved; mana cost / colors / colorIdentity attributes are unchanged (the
remaining diff lines are pure whitespace re-formatting plus the regen dropping
null `Power`/`Toughness` inputs and adding the canonical `IsVariable:false`). No
ability dropped, added, or inverted.

**Whitelist:** Disperse is fully cleaned and correctly absent from
`whitelist-freetext.json` (not an S6-shared gold).

**Projection:** No new discriminator is introduced on this gold — it reuses the
pre-existing `ExcludedCardTypes` axis, so no PortWalk projection decision is in
scope for this fixture.

## Out-of-scope residuals remaining

None. This gold carries no residual on any other axis.

## Process notes

DELTA judgment per spec: judged only the PB-3 change on this gold. rules-structure.json
read from `libs/mtg-rules/Data/_03_Primary/Datasets/` (the SKILL's documented path under
`tests/atlas-flow-test/` is stale; same dataset).

**ALL PASS**
