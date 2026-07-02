# MAST judge — batch verdict (soaring-seacliff)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-soaring-seacliff
**Base:** 176e495dda71494b915330f72bde000e5cd90f0f
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ZEN/SoaringSeacliff.json` — PASS. Oracle text matches oracle-cards.json verbatim. The task-axis ability is a `triggered` node: `Trigger{Timing:When, Event:Enters, Filter:{CardTypes:[land], IsSelf:true}}` (self ETB, CR 603.1) plus a plain `gainAbility` effect whose `Target` is `Kind:Target` with a `creature` filter — a faithful "target creature" (CR 115.1, whose own example is "target creature gets -1/-1 until end of turn"). Flying is modeled structurally as a static `evasion` effect with `CanBeBlockedBy` = creature having keyword Flying or Reach (CR 702.9b), not free text. `Duration` is a separate composable `untilTime` Turn/End node (CR 611.1) — timing is NOT baked into the effect discriminator. No `unparsed` Kind/EffectType anywhere. Out-of-axis siblings ("enters tapped" static, `{T}: Add {U}` mana ability) are structured and preserved. Fixture is new at this base, so no regression surface.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TargetCreatureGainsKeywordUntilEndOfTurnRule.cs` — PASS. Emits pre-existing `GainAbilityEffect`/`ObjectReference.Target`/`UntilTimeDuration.EndOfTurn` nodes. Doc-comment CR citations (603.1, 115.1, 611.1) all exist in rules-structure.json and match the modeling. Correctly bails on an unrecognised keyword (returns false — no free-text fallback), and is explicitly distinguished from the anaphoric `ItGainsKeywordUntilEndOfTurnRule`.
- `mast-tdd/2026-07-02-soaring-seacliff#projection` — PASS. No new discriminator is introduced: `gainAbility`, `evasion`, and the `Enters` trigger event all pre-exist (e.g. AcrobaticLeap, BloodswornSteward). The branch adds only a parser rule; no `PortGraph`/`PortWalkProjection`/`known-coarse-projections.json` change is required or made. Projection decision N/A.

## Glossary gaps

None.

## Process notes

- Flying modeling matches the established convention (identical `evasion`/`CanBeBlockedBy`/`KeywordSource:"Flying"` shape as AcrobaticLeap); the only difference — `Target.Kind:"Target"` with a creature filter vs AcrobaticLeap's anaphoric `"It"` — is exactly the correct distinction for "target creature" vs "it", and the parser rule's doc-comment calls it out.

ALL PASS
