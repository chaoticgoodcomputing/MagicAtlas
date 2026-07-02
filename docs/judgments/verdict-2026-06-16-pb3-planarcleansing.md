# MAST judge — delta verdict (slice PB-3)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice
**Scope:** 1 regenerated gold (delta judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Per-item verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/M14/PlanarCleansing.json
**Verdict:** PASS

**Real card (live Scryfall + Input.OracleText):** "Destroy all nonland permanents." — Sorcery {3}{W}{W}{W}.

**Slice's target residual on this gold:** the "nonland" exclusion, previously inlined as free text
`Characteristics: [{"CharacteristicType": "other", "Description": "nonland"}]`.

**(a) Target structured correctly — YES.** Regen replaced the free-text sink with the structured axis
`CardTypes: ["permanent"]` + `ExcludedCardTypes: ["land"]`. This is exactly the reuse PB-3's spec calls
for (it explicitly reuses the existing `ExcludedCardTypes` axis), and it is faithful to the card: CR 110.1
defines a permanent, and "nonland permanents" is the literal negation the excluded-type axis encodes.
`ExcludedCardTypes` is a real, pre-existing ObjectFilter axis (libs/magic-ast/AST/References/ObjectFilter.cs:34).

**(b) No new out-of-scope residual — YES (primary criterion).** No `Kind:"unparsed"`, no `EffectType:"unparsed"`,
no new free-text `Description`/`*Raw`/`other` carrier. The only remaining `Raw` strings are the exempt
verbatim-by-design fields (TypeLine.Raw, manaCost.Raw). `CantBeRegenerated:false` and `IsVariable:false`
are added structured scalars from the regen, not free text.

**(c) No regression — YES.** Single `destroy` effect retained; `Target.Kind:"Each"` retained; the sibling
`CardTypes:["permanent"]` filter preserved (not lost when the exclusion was lifted out of free text);
no ability dropped, added, or inverted. The remainder of the diff is whitespace reformatting and
schema-normalization scalars.

**Rule cross-reference:** CR 110.1 (permanent definition) — present in rules-structure.json and consistent
with the modeling. CR 110.5 (tapped/untapped, cited by the slice's TappedStateCharacteristic) also verified
present, though no tapped/counter characteristic appears on this particular gold.

## Out-of-scope residual on this gold

None. Planar Cleansing's lone residual was this slice's own target and it was fully cleaned.

## Process notes

- The SKILL's data-source paths (`tests/atlas-flow-test/Data/_03_Primary/Datasets/...`) are stale;
  rules-structure.json and glossary.json now live under `libs/mtg-rules/Data/_03_Primary/Datasets/`.
  Queried the live location. Worth refreshing the SKILL's file table.
