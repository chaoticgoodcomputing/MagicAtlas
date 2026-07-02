# MAST judge — DELTA verdict (SLICE PB-3, structured-characteristic megaslice)

**Date:** 2026-06-16
**Scope:** 1 fixture (delta judgment — `ALA/AkrasanSquire`, uncommitted working-tree regen)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## Delta verdict

### tests/magic-ast-tests/Fixtures/HandParsedCards/ALA/AkrasanSquire.json
**Verdict:** PASS

- **Slice target structured (criterion a):** the free-text combat-state residual
  `{CharacteristicType:"other", Description:"attacking alone"}` is now the structured
  `{CharacteristicType:"combatState", State:"AttackingAlone"}` — correct node/axis
  (`CombatStateCharacteristic{AttackingAlone}`, existing variant), faithful to the real card.
  Real oracle text (oracle-cards.json + Scryfall): "Exalted (Whenever a creature you control attacks
  alone, that creature gets +1/+1 until end of turn.)". CR 702.83a defines Exalted verbatim; CR 506.5
  / 702.83b define "attacks alone" as the combat-state predicate the node models.
- **No new residual (criterion b — primary):** precise scan for `other`/`unparsed`/`Description`
  nodes returns empty. The added `Reminder.Text` is exempt verbatim reminder text matching the real
  card; the `Raw` strings present are verbatim-by-design display fields (TypeLine/manaCost/PT).
- **No regression (criterion c):** Exalted triggered ability preserved — Whenever/Attacks,
  filter `creature`+`Controller:You`+combat-state, ModifyPT +1/+1 until end-of-turn on `ThatCreature`,
  `KeywordSource:Exalted`. Remaining diff hunks are non-semantic (field-order swaps, additive
  `IsVariable:false`, dropped empty `Supertypes:[]`, repositioned `KeywordSource`).
- **Whitelist:** gold fully cleaned; not S6-shared → its `whitelist-freetext.json` entry correctly
  removed (`-      "card": "ALA/AkrasanSquire",`).

**Out-of-scope residual remaining:** none. This gold is fully clean after the slice.

## Process notes

Citations cross-referenced against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`:
CR 702.83a (Exalted) and CR 506.5 / 702.83b (attacks alone) both present and matching the modeling.

ALL PASS
