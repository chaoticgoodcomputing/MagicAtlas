# MAST judge — PB-3 delta verdict: Inundate

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (delta judgment)
**Scope:** 1 fixture (tests/magic-ast-tests/Fixtures/HandParsedCards/Inundate.json)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/Inundate.json
**Verdict:** PASS
**Oracle text (verified vs raw oracle-cards.json):** "Return all nonblue creatures to their owners' hands."

**(a) TARGET residual structured correctly.** The slice replaced the free-text
`Characteristics: [{"CharacteristicType":"other","Description":"nonblue"}]` residual with the new
structured `ExcludedColors: ["U"]` axis on the Each-target's ObjectFilter. CR 105.1 lists blue as
one of the five colors and CR 105.2 defines an object's color; "nonblue" is precisely the exclusion
of color U, so `ExcludedColors:["U"]` is faithful to the real card. ExcludedColors is the new
ObjectFilter axis this slice introduced (confirmed in ObjectFilter.cs).

**(b) No new out-of-scope residual.** No `unparsed`, no `CharacteristicType:"other"`, no
`Description`/`Raw` free-text remains in the ability body. The only `Raw`-like string is
`Oracle.RawText`, a verbatim-by-design field (exempt per doctrine).

**(c) No regression.** The single `returnToHand` effect, the `Each` target kind, and the
co-occurring `CardTypes:["creature"]` filter are all preserved. The Inundate entry was removed from
whitelist-freetext.json (correct: gold is now fully clean, not an S6-shared gold). The
`IsVariable:false` addition on the manaCost attribute is an out-of-scope serialization shape on a
different node, not a residual, and does not affect this slice's axis.

**Out-of-scope residual remaining:** none. This gold is fully cleaned by this slice.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Inundate.json` — PASS. "nonblue" structured as
  ExcludedColors:["U"] (CR 105.1/105.2); creature filter + Each target preserved; no new residual.

## FAIL verdicts

(none)
