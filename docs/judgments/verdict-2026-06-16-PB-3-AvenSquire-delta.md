# MAST judge — PB-3 delta verdict (SHM/AvenSquire)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (consolidated ATOMIC slice; structured-characteristic axis + comparative-power)
**Scope:** 1 regenerated gold (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Per-item verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/SHM/AvenSquire.json
**Verdict:** PASS
**What the slice structured:** the Exalted trigger's "attacks alone" filter characteristic, converted from the free-text residual `{"CharacteristicType":"other","Description":"attacking alone"}` to the structured `{"CharacteristicType":"combatState","State":"AttackingAlone"}` (the existing CombatStateCharacteristic axis the spec says to reuse).
**Faithful to card:** Oracle (Scryfall + oracle-cards.json) = "Flying\nExalted (Whenever a creature you control attacks alone, that creature gets +1/+1 until end of turn.)". CR 702.83a defines Exalted exactly as that trigger; CR 506.5 defines "attacks alone." The structured node matches both.
**(a) Target residual structured correctly:** YES — right node/axis, faithful to the real card.
**(b) No new out-of-scope residual:** YES — gold contains zero `other`/`unparsed`/`Description` residuals; remaining `"Raw"` strings are verbatim-by-design (type line, mana cost, P/T) and exempt.
**(c) No regression:** YES — both abilities preserved (Flying evasion ability; Exalted triggered ability with modifyPT +1/+1, untilTime Turn/End, Target ThatCreature). Sibling filters (CardTypes: creature, Controller: You) intact. Other diffs are serialization-order normalization (KeywordSource field moved, Reminder text added, IsVariable/empty-Supertypes elision) — no semantic loss, no dropped/added/inverted ability.
**Whitelist hygiene:** SHM/AvenSquire removed from whitelist-freetext.json (fully cleaned). S6-shared golds AdeptWatershaper + SarythTheVipersFang kept whitelisted, as the slice spec requires.

## Out-of-scope residuals remaining
None on this gold.

## Process notes
- The CR data files live under `libs/mtg-rules/Data/_03_Primary/Datasets/` (the SKILL/dispatch `tests/atlas-flow-test/...` path is stale); citations cross-referenced against the live location.

ALL PASS
