# MAST judge — PB-3 delta verdict: M13/KrenkosEnforcer

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (atomic; structured-characteristic axis + comparative-power merged in)
**Scope:** 1 fixture (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/M13/KrenkosEnforcer.json
**Verdict:** PASS

**Real oracle text (oracle-cards.json, confirmed):**
> Intimidate (This creature can't be blocked except by artifact creatures and/or creatures that share a color with it.)

Matches the fixture `Input.OracleText` and `RawText` verbatim.

**(a) Target residual structured correctly.** The slice's target on this gold was the structured-characteristic axis: the two free-text `OtherCharacteristic` entries `{CharacteristicType:"other", Description:"artifact"}` and `{... "shares a color"}` inside the Intimidate evasion filter. Both are now structured:
- `artifact` → `CardTypes: ["creature", "artifact"]` (artifact-creature = both types, the AND-intersection on the CardTypes axis — faithful).
- `shares a color` → `SharesColorWith: {Kind: "Self"}` — the relational color axis referencing the source object (Self), exactly the documented use of `ObjectFilter.SharesColorWith` (CR 702.78a relational-color parallel). Pre-existing axis, correctly reused, not invented here.

This exactly parallels the slice's sibling Fear transform (artifact→CardTypes, black→Colors), per the spec's per-axis mapping.

**(b) No new residual.** Zero free-text/unparsed nodes remain in the AST body. The only "artifact"/"shares a color" string occurrences left are in `OracleText`, `RawText`, and `Reminder.Text` — verbatim-by-design fields that are exempt. The gold was removed from `whitelist-freetext.json` (fully cleaned), consistent with the ratchet.

**(c) No regression.** The single `static` evasion ability is preserved; `EffectType: "evasion"`, `KeywordSource: "Intimidate"`, and `Reminder` are intact. No ability dropped/added/inverted. The other diff hunks (`IsVariable: false` added to manaCost; Power/Toughness and key reordering) are regen serialization normalization, not semantic changes.

**Citation note.** Intimidate is a deprecated keyword (M12 era) absent from the current `rules-structure.json` and `glossary.json`; the fixture claims no CR citation, which is fine (a missing citation does not block PASS). No contradictory citation present.

**Out-of-scope residual remaining:** None on this gold. (The and/or disjunction is modeled as a single conjunctive filter carrying both axes — the same simplification the prior free-text list used; this is a pre-existing evasion-disjunction modeling concern, not introduced or worsened by this slice, and outside its scope.)

## FAIL verdicts

None.

## Glossary gaps

- "Intimidate" — deprecated keyword, not in glossary.json. Expected for a discontinued mechanic; the gold carries it only via verbatim reminder/raw text. No action required for this slice.

ALL PASS
