# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (delta-judge of the tribal-anthem-keyword-grant line on Mist Dancer)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/CMR/MistDancer.json#tribal-anthem-keyword-grant` — PASS. Target line "Other Merfolk you control get +1/+0 and have flying." is one `static` ability carrying two effects sharing an identical subject: `modifyPT` (PowerModifier literal 1, ToughnessModifier literal 0 — +1/+0 with the 0 side present, not dropped) and `gainAbility` granting Flying. Subject filter is `Each` creature, `Subtypes:[Merfolk]`, `Controller:You`, `ExcludeSelf:true` ("Other" → excludes source; CR 109.2 subtype-on-battlefield, CR 109.5 you=controller). Granted Flying is fully structured as a recursive `evasion` static (CanBeBlockedBy creatures with Flying/Reach), matching CR 702.9b — no free-text keyword residual. Continuous static effect, no baked-in timing (CR 611.3). All four cited CR rules exist in rules-structure.json and match the modeling.

## Delta checks

- (a) Structure/discriminator: `modifyPT` + `gainAbility` are the right nodes for an anthem P/T buff + keyword grant; faithful to "+1/+0" and "flying"; describe-not-execute; no timing baked in. PASS.
- (b) No new free-text/unparsed residual on the target axis: granted "flying" is a structured evasion static, not a string or KeywordReferenceEffect. PASS.
- (c) No regression: new fixture; siblings intact — self-Flying (ability 1, evasion static) and Encore (ability 3, `encore` effect + verbatim reminder) preserved, nothing dropped/inverted. Encore is a different-axis concern owned elsewhere. PASS.
- (d) Citation cross-reference: CR 611.3 (continuous effect from static ability), 109.2 (subtype = permanent on battlefield), 109.5 (you = controller), 702.9 (Flying) all present and consistent. PASS.

## Glossary gaps

None.

## Process notes

Fixture Input.OracleText matches oracle-cards.json verbatim. This is a new-file fixture, so "no regression" is judged against faithful modeling of the untouched siblings rather than a prior gold.
