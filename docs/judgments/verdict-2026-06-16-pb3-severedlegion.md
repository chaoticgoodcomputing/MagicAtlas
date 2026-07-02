# MAST judge — DELTA verdict (slice PB-3)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (atomic; structured-characteristic axis + comparative-power)
**Scope:** 1 gold (regenerated, uncommitted in working tree)
**Mode:** DELTA (judge only the change this slice made; sibling/other-axis residuals are other slices' debt)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Item

### tests/magic-ast-tests/Fixtures/HandParsedCards/10E/SeveredLegion.json
**Verdict:** PASS
**Card:** Severed Legion — "Fear (This creature can't be blocked except by artifact creatures and/or black creatures.)"
**Rule citation:** CR 702.36b
**Rule text:** > "A creature with fear can't be blocked except by artifact creatures and/or black creatures."

**(a) TARGET structured correctly.** The slice's target residual was the free-text
`CanBeBlockedBy.Characteristics: [{CharacteristicType:"other", Description:"artifact"},
{CharacteristicType:"other", Description:"black"}]`. The regen replaces it with structured axes
`CardTypes: ["creature","artifact"]` + `Colors: ["B"]` on the `evasion` effect's `CanBeBlockedBy`
ObjectFilter. This faithfully encodes "artifact creatures and/or black creatures" per CR 702.36b
and is byte-identical to the established sibling Fear golds (9ED/RazortoothRats, UDS/SquirmingMass).

**(b) No new free-text/unparsed residual (primary criterion).** No `Characteristics`/`Description`/
`"other"`/`unparsed` keys remain. The only surviving `Raw` keys are verbatim-by-design fields
(TypeLine, manaCost, creatureStats) — exempt.

**(c) No regression.** Effect count unchanged (1); `KeywordSource:"Fear"` and the `Reminder` block
preserved; no sibling filter/effect dropped, added, or inverted. The remaining diff noise
(`IsVariable:false` on manaCost, Value/Raw and KeywordSource/Reminder field-order reshuffles) is
benign serialization normalization from regen — outside this slice's axis and semantically inert.

**Out-of-scope residual remaining:** none. Fear carries no comparative-power or combat-state axis,
so this gold is fully clean. Correctly removed from whitelist-freetext.json (no longer present).

## Process notes

Rules data now lives at `libs/mtg-rules/Data/_03_Primary/Datasets/` (rules-structure.json,
glossary.json) rather than the path in SKILL.md's table; CR 702.36 (Fear) and its subrules a/b/c
verified present, glossary "Fear" entry present and consistent.

ALL PASS
