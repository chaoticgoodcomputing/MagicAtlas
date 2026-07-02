# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (delta-judge, task `terravore`)
**Branch:** `mast-tdd/2026-07-02-terravore`
**Base:** `c9b1439a35f44d0207b28446529176c13106f531`
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JUD/Terravore.json` — PASS.
  The characteristic-defining P/T ("Terravore's power and toughness are each equal to the
  number of land cards in all graveyards") is modeled as a `static` ability with a single
  `definePT` effect: `Characteristic: "Both"` (faithful to "power and toughness are **each**
  equal to"), `Value` = a `count` `Quantity` whose `CountOf` is `{ CardTypes: ["land"],
  Zone: "Graveyard" }`. Matches CR 604.3 / 604.3a — a static CDA that defines power/toughness
  as a game-state quantity. Describe-not-execute: it states what the `*` equals with no timing
  baked in. The "in all graveyards" scope is encoded by `Zone: Graveyard` with **no**
  Controller/Owner axis (the established all-graveyards convention, mirroring
  `ExileAllRule.GraveyardPattern`). Discriminator/shape match the existing `definePT`
  convention (cf. ZendikarIncarnate: `definePT` + count over `{CardTypes:[land], Controller:You}`).

## Delta checks

- (a) **Target line structured correctly** — right node (`definePT`, the CDA P/T node),
  right discriminator (`Characteristic: Both`), faithful count filter; describe-not-execute;
  no baked-in timing. PASS.
- (b) **No new free-text / unparsed residual** — count is fully structured via `ObjectFilter`
  (`CardTypes` + `Zone`); no `unparsed` Kind/EffectType, no rules-bearing free-text string. PASS.
- (c) **No regression** — new fixture; Trample sibling preserved as its own `static` /
  `keywordAbility` ability (matches ChargingBinox convention); `creatureStats` attribute retains
  `*`/`*` variable P/T; manaCost/colors/colorIdentity intact. PASS.
- (d) **Citation cross-reference** — CR 604.3 (and 604.3a) exist in `rules-structure.json` and
  describe exactly a static characteristic-defining ability that sets power/toughness. PASS.

## Projection decision (initiative 03)

N/A — no new discriminator introduced. `definePT`, the `count` `Quantity`, `ObjectFilter.Zone`,
`Zone.Graveyard`, and `CardTypes` all pre-exist in the AST; the parser change
(`StaticRuleHelpers.BuildObjectCountFilter` gaining a "`<type> cards in all graveyards`" arm)
reuses existing filter axes. The exhaustiveness ratchet does not fire and no new
`PortGraph`/`PortWalkProjection` or `known-coarse-projections.json` entry is required.

## Glossary gaps

_none_

## Process notes

Oracle text confirmed verbatim against `oracle-cards.json`
("Trample\nTerravore's power and toughness are each equal to the number of land cards in all
graveyards."). The parser arm strips the "card(s)" head noun before `ClassifyTypeNounPhrase`
and drops the Controller axis for the all-graveyards scope — both sensible and consistent with
the fixture gold. Non-fixture parser edits are out of scope for rules-accuracy judging.
