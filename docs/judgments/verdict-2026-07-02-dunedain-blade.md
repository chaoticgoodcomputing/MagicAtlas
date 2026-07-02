# MAST judge — batch verdict (dunedain-blade)

**Date:** 2026-07-02
**Scope:** 1 branch (`mast-tdd/2026-07-02-dunedain-blade`) — 1 fixture, 1 AST node, 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## Task

Typed Equip variant — `Equip [creature type] {cost}` — equip that may target only a creature of a given type. Card: Dúnedain Blade (LTR), `Equipped creature gets +2/+1. / Equip Human {1} / Equip {3}`.

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LTR/DunedainBlade.json` — PASS. Target line `Equip Human {1}` is an `activated` ability, `KeywordSource: Equip`, cost `{1}` generic, `AttachEffect` targeting `CardTypes:[creature] + Subtypes:[Human] + Controller:You` — the chosen-quality restriction of CR 702.6c carried structurally (typed subtype filter, not a free-text characteristics string) — with `OnlyAsSorcery` in `Restrictions[]` (separate from the effect; no baked-in timing). Faithful to card; describes-not-executes.
- `libs/magic-ast/Keywords/Definitions/EquipQualityKeyword.cs` — PASS. Doc-comment cites CR 702.6a/702.6c/702.6d, all verbatim-present in `rules-structure.json` and consistent with the emitted shape; 702.6d justifies the typed variant coexisting with plain `EquipKeyword` as independent equip abilities.
- `mast-tdd/2026-07-02-dunedain-blade` (projection) — PASS. No new discriminator: reuses existing `AttachEffect`, `KeywordAbility.Equip`, `ActivationRestriction.OnlyAsSorcery`, and the `Subtypes` `ObjectFilter`. The quality is filter *data*, not a new effect/cost/trigger/restriction type, so no PortWalk projection entry is required; none touched.

## Regression check

New fixture (140 additions, 0 deletions). All three abilities on the card present and correct: static `+2/+1` (`modifyPT`, `EnchantedOrEquipped`), typed `Equip Human {1}`, plain `Equip {3}`. Plain-equip sibling matches existing `EquipKeyword` output exactly (creature + Controller:You + OnlyAsSorcery). No dropped/added/inverted ability; out-of-axis nodes (Attributes, TypeLine) unchanged. No `unparsed` nodes, no `EffectType: unparsed`, no free-text residual on the equip axis.

## Glossary gaps

None.

## Process notes

Oracle text cross-checked against `oracle-cards.json` — identical to the fixture `Input.OracleText`. Reminder text for `Equip {3}` is present in oracle text but not carried on the gold ability's `Reminder` field; reminder text is verbatim-by-design and exempt, so this is not a rules-accuracy concern (parser/test-matching, out of judge scope).
