# MAST judge — DELTA verdict (PB-3)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (atomic; structured-characteristic axis + comparative-power)
**Mode:** DELTA (judge only the change this slice made; out-of-scope residuals on other axes are expected and not a FAIL)
**Scope:** 1 gold (NEO/VilespawnSpider.json, uncommitted in working tree)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NEO/VilespawnSpider.json` — PASS.
  - **Target residual structured (criterion a):** The trigger `Filter` previously carried a free-text
    `Characteristics: [{ CharacteristicType: "other", Description: "artifact" }]` alongside `CardTypes: ["spell"]`.
    The slice removed that free-text `other`/`Description` sink and folded the concept into the structured
    axis: `CardTypes: ["spell", "artifact"]`. "Artifact spell" = a spell (CR 112.1, a card on the stack)
    bearing the artifact card type — a card-type conjunction is the right structured node/axis. This matches
    the established canonical shape used by `KLD/FoundryInspector.json` (artifact spell), `SOM/HaltOrder.json`
    (target artifact spell), and `CHK/Nullify.json` (target creature spell).
  - **No new residual (criterion b — primary):** Full-gold scan shows `other:0`, `Description:0`,
    `Characteristics:0`, `unparsed:0`. The only `RawText` is the verbatim-by-design `Oracle.RawText`
    (exempt). No new free-text or unparsed node introduced.
  - **No regression (criterion c):** Both abilities preserved — (1) `static` keywordAbility "Reach";
    (2) `triggered` `Whenever`/`SpellCast` with `Controller: "You"` + `surveil 1`. Trigger event, timing,
    surveil effect, and Reach are byte-identical except the in-scope filter change. Remaining diff hunks
    (`KeywordSource` key reorder, `IsVariable: false` added to manaCost attr, `Value`/`Raw` reorder in
    creatureStats) are serialization-order / round-trip normalization with no semantic change.
  - **Whitelist:** Card is NOT present in `whitelist-freetext.json` — correct, per DELTA-SCOPE
    ("remove fully-cleaned golds from whitelist-freetext.json"). This gold is fully clean.

## Out-of-scope residual remaining
- None on this gold. The structured-characteristic axis was this slice's only relevant target here, and it
  is fully clean. (This is not an [S6-SHARED] gold; no other/another-exclusion residual to leave behind.)

## Process notes
- The fixture's `Input.OracleText` ("Reach\nWhenever you cast an artifact spell, surveil 1.") does NOT match
  the real Vilespawn Spider (NEO) per Scryfall `oracle-cards.json`, which is upkeep-mill + a sacrifice
  token-maker. This Input text was authored before PB-3 and is UNTOUCHED by this slice's diff (the diff only
  edits `Output`). Per the DELTA mandate I judge only the slice's change; the slice faithfully structured the
  Output to the fixture's own Input.OracleText. The Input vs real-card mismatch is a pre-existing fixture-data
  issue outside PB-3 scope and is not a regression introduced here — surfacing it for separate human triage.

ALL PASS
