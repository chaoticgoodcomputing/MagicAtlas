# MAST judge — batch verdict (delta: haunted-cloak)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-haunted-cloak
**Base:** 539b20a877ad4a1736eb1085230c2b2c1be79609
**Scope:** 1 fixture (KLD/HauntedCloak.json) + 1 supporting parser rule (BareKeywordListGrantRule.cs, not independently judged)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/KLD/HauntedCloak.json` — PASS.
  Target line "Equipped creature has vigilance, trample, and haste." is modeled as a single
  `static` ability whose flat `Effects` list carries three `gainAbility` effects, each with
  `Target.Kind = EnchantedOrEquipped` and a `GainedAbility` static keyword grant
  (Vigilance → CR 702.21, Trample → CR 702.19, Haste → CR 702.10). This is a correct static
  continuous effect (CR 611.1 / 611.3) with no `Duration` node — a persistent as-attached grant,
  correctly distinct from the "gains [kw] until end of turn" temporary shape, so no timing is baked
  into the effect. The multi-keyword clause is faithfully split one-effect-per-keyword.

## Verification detail

- **Oracle text:** verified verbatim against `oracle-cards.json` —
  "Equipped creature has vigilance, trample, and haste.\nEquip {1} (...)". Exact match.
- **(a) Structure/discriminator:** correct. `static` node, `gainAbility` + `EnchantedOrEquipped`,
  keyword decomposition matches the existing Equipment-grant convention (cf. CliffhavenKitesail).
  Describe-not-execute; no baked-in timing; no `Duration`.
- **(b) No free-text/unparsed residual:** none. Only `Raw`/`RawText` fields present are
  verbatim-by-design (type line, oracle raw, mana-cost raw). No `unparsed`, no `*Text`/`*Description`.
- **(c) No regression:** new fixture; both abilities present — the static three-keyword grant and the
  `Equip {1}` activated ability (mana {1}, `attach` → target creature you control, `OnlyAsSorcery`).
  Out-of-axis nodes (mana cost {3}, colors, colorIdentity) correct. Nothing dropped/added/inverted.
- **(d) CR citations:** all four cited rules exist in `rules-structure.json` and match the modeling —
  611.1 (continuous effect), 611.3 (static ability generates continuous effect), 301.5d (Equipment
  grants an ability to the equipped creature via "gains"/"has"), 207.2 (italicized/reminder text has
  no game function; justifies reminder-text stripping).

## Projection decision (initiative 03)

N/A — no new discriminator introduced. The branch reuses pre-existing discriminators
(`gainAbility`, `EnchantedOrEquipped`, `keywordAbility`; `GainAbilityEffect.cs` already exists),
used across many existing fixtures. No new effect/cost type, trigger event, or restriction, so the
PortWalk projection ratchet does not apply.

## Glossary gaps

None.

## Process notes

The diff also adds `libs/magic-ast/Parsing/Parsers/Static/Rules/BareKeywordListGrantRule.cs`
(a parser rule producing the fixture shape). Parser correctness is out of the judge's scope
(NUnit's job); its doc-comment citations (611.1/611.3/301.5d/207.2) were cross-referenced and
all hold.

ALL PASS
