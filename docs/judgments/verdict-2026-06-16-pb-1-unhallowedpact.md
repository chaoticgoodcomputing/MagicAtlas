# MAST judge — DELTA verdict (SLICE PB-1)

**Date:** 2026-06-16
**Slice:** PB-1 — aura IsEnchanted + BearUmbra
**Scope:** 1 gold (delta judgment, not whole-gold purity) — tests/magic-ast-tests/Fixtures/HandParsedCards/M14/UnhallowedPact.json
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/M14/UnhallowedPact.json
**Verdict:** PASS
**Slice target structured:** The Dies-trigger object filter for "enchanted creature" previously carried the free-text residual `Characteristics: [{ "CharacteristicType": "other", "Description": "enchanted" }]` (the `Other("enchanted")` / OtherCharacteristic sink). It is now the structured flat axis `IsEnchanted: true` alongside the preserved `CardTypes: ["creature"]`.
**Rule citation:** CR 303.4 / CR 702.5
**Rule text:** > 303.4 "An Aura enters the battlefield attached to an object or player. What an Aura can be attached to is defined by its enchant keyword ability (see rule 702.5, 'Enchant')." Both rules exist in rules-structure.json; the citation matches what `IsEnchanted` models (the Aura's attached permanent).

**Delta criteria:**
- (a) Target residual structured CORRECTLY: yes — right axis (`bool? IsEnchanted` on ObjectFilter, mirroring IsSelf/IsToken), faithful to "enchanted creature dies".
- (b) No NEW free-text/unparsed residual introduced: confirmed — full-fixture scan finds zero `unparsed` nodes and zero `other`/`Description` free-text characteristics.
- (c) No regression: ability/effect structure byte-identical to prior gold (static `enchantRestriction` + triggered `Dies` -> `returnToBattlefield` preserved; sibling `CardTypes:["creature"]` retained). Other diff lines (`Tapped:false`, `IsVariable:false`, KeywordSource reorder, removed null Input Power/Toughness) are non-semantic regeneration normalization.

**Supporting facts:**
- Oracle text matches authoritative Scryfall (oracle-cards.json) verbatim: "Enchant creature\nWhen enchanted creature dies, return that card to the battlefield under your control."
- `whitelist-freetext.json` entry for `M14/UnhallowedPact` (sink OtherCharacteristic) correctly removed — the sink no longer occurs.
- Parser routing: `TriggeredRuleHelpers.ParseObjectFilter` now emits `IsEnchanted = true` instead of `Characteristic.Other("enchanted")` for the "enchanted creature" branch.

**Out-of-scope residual remaining:** none on this gold.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M14/UnhallowedPact.json` — PASS. "enchanted creature" residual structured to `IsEnchanted: true` (CR 303.4/702.5); no new residual; no regression.
