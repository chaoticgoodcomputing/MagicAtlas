# MAST judge — batch verdict (balduvian-berserker)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-balduvian-berserker
**Scope:** 1 fixture (DMU/BalduvianBerserker.json), delta vs 539b20a8
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DMU/BalduvianBerserker.json#death-trigger` — PASS.
  Target line "When this creature dies, it deals damage equal to its power to any target"
  is `Kind: triggered` with `Trigger{Timing:When, Event:Dies, Filter{CardTypes:[creature],
  IsSelf:true}}` + a plain `dealDamage` effect (`Source:It`, `Amount:{derived, DerivedFrom:Power,
  Source:it}`, `Target:AnyTarget`). Timing is a separate composite node (not baked into the
  effect); the amount is a derived Power quantity (describe-not-execute — it does not bake the
  current printed power of 1); "any target" → `AnyTarget` per CR 115.4. Cited CR 603.1 (triggered
  ability = condition + effect), CR 120.1 (dealer is the source), CR 115.4 (any target = creature/
  player/planeswalker/battle) all exist in rules-structure.json and match the modeling.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/DMU/BalduvianBerserker.json#projection` — PASS.
  Branch adds only a new parser rule (ItDealsDamageEqualToItsPowerToAnyTargetTriggeredRule) plus
  the fixture; it composes pre-existing discriminators (`dealDamage`, `derived`/`DerivedKind.Power`,
  `Dies`, `AnyTarget`) and introduces no new effect/cost type, trigger event, or restriction, so
  the PortWalk projection ratchet is not triggered.

## Cross-checks

- Oracle text verified verbatim against oracle-cards.json (Enlist reminder + death-trigger line
  match exactly; mana {2}{R}, P/T 1/3, Kor Berserker all faithful).
- No free-text / unparsed residual on the target axis. The Enlist sibling is modeled as
  `keywordAbility{Keyword:Enlist}` + verbatim reminder text (exempt) — a different axis, preserved,
  not a fail.
- No regression: new fixture; both printed abilities present and faithful; manaCost / colors /
  colorIdentity / creatureStats attributes all correct.

## Glossary gaps

None.

## Process notes

New file (added, not modified), so the regression check is fidelity of the whole card rather than
a diff against a prior gold. Both abilities and all attributes are faithful.

ALL PASS
