# MAST judge — batch verdict (raven-wings)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-raven-wings
**Base:** 1526dd74fd92af29b86588b620ae5405cf8de511
**Scope:** 1 fixture (RavenWings.json) — DELTA-JUDGE of the compound buff+keyword+type static line
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/RavenWings.json` — PASS.
  Oracle text confirmed verbatim against `oracle-cards.json` ("Equipped creature gets +1/+0,
  has flying, and is a Bird in addition to its other types. / Equip {2} ...").
  The target compound static line is modeled as a single `Kind: static` ability whose three
  clauses decompose into three composable effects, all targeting `EnchantedOrEquipped`:
  - `+1/+0` → `modifyPT` (`ModifyPTEffect`) with literal `PowerModifier:1`, `ToughnessModifier:0`
    — a *modify*, not a set (CR 613.4c, layer 7c). Correct.
  - `has flying` → `gainAbility` granting a static Flying keyword whose body is the standard
    structured evasion (`CanBeBlockedBy` creatures with keyword Flying or Reach) — no free text
    (CR 613.1f, layer 6). Correct.
  - `is a Bird in addition to its other types` → `addType` (`AddTypeEffect`) with
    `AddedSubtypes:["Bird"]` — *additive*, correctly NOT a replacing `ChangeSubtypeEffect`,
    per CR 205.1b ("in addition to its other types" retains all prior types) (CR 613.1d,
    layer 4). Correct.

  (a) Structure correct: right nodes/discriminators, faithful to the card, describe-not-execute,
  no baked-in timing (static ability, no `Duration`).
  (b) No residual: no `Kind:"unparsed"` / `EffectType:"unparsed"`; the only `Raw` fields are
  verbatim-by-design (type line, mana-cost attribute, `Oracle.RawText`); flying's block filter
  uses structured `CharacteristicType:"keyword"` objects, not free text.
  (c) No regression: new fixture; both abilities present — the compound static ability plus the
  `Equip {2}` activated ability (mana {2}, `attach` to target creature you control,
  `OnlyAsSorcery` restriction). No dropped/added/inverted ability; siblings preserved.
  (d) Citations cross-referenced: CR 205.1b, 613.1d, 613.1f, 613.4c all exist verbatim in
  `rules-structure.json` and match the modeling; CR 702.6 (Equip) exists.

## Projection decision (initiative 03)

Not applicable / no item required. The diff touches only a parser rule
(`EquippedPTKeywordAddTypeRule.cs`) and the fixture; it adds no new AST node, effect/cost type,
trigger event, or restriction. `AddTypeEffect`, `modifyPT`, `gainAbility`, `evasion`,
`EnchantedOrEquipped`, and the `OnlyAsSorcery` restriction all pre-exist on the base sha, so no
new discriminator is introduced and no PortWalk projection decision is owed.

## Glossary gaps

(none)

## Process notes

CR layering rules (613.x) are cited in the parser doc-comment for descriptive grounding only;
MAST remains descriptive (no layer-ordering execution encoded in the fixture). Citation
cross-reference passed; layering semantics themselves are out of judge scope.

ALL PASS
