# MAST judge — DELTA verdict: PB-5 — CandyTrail conjunction

**Date:** 2026-06-16
**Slice:** PB-5 — CandyTrail conjunction
**Scope:** 1 regenerated gold (uncommitted working tree) + whitelist removal
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WOE/CandyTrail.json` — PASS.
  - **Re-point (corrupt Input fixed):** old gold was the wrong card entirely (a "Bargain" Sorcery "Create a Food token..." carrying a `Kind: unparsed` node). Working tree re-points Input to the real Candy Trail: `ManaCost {1}`, `TypeLine "Artifact — Food Clue"`, OracleText "When this artifact enters, scry 2.\n{2}, {T}, Sacrifice this artifact: You gain 3 life and draw a card." — Scryfall-exact, matches oracle-cards.json.
  - **(a) Target axis structured:** the activated-ability body "You gain 3 life and draw a card" parses as TWO structured effects via effect-conjunction — `gainLife{Amount:3, Player:You}` + `drawCards{Count:1, Player:You}`. Both conjuncts present; the Run-1 failure mode (dropping the gain-3-life conjunct / one residual) is avoided. Correct node/axis, faithful to the card (CR 119 gain life, CR 120 draw).
  - **(b) No new residual (primary criterion):** zero `unparsed` / `EffectType:"unparsed"` / `CostType:"unparsed"` nodes anywhere in `Output.Oracle.Abilities`. Discriminator set is fully structured (scry, gainLife, drawCards, mana/tap/sacrifice costs).
  - **(c) No regression:** ETB `scry 2` modeled as triggered (When/Enters, IsSelf artifact filter) + `scry` effect Count 2 (CR 701.22); activated cost `{2}` + `tap` + `sacrifice this` (CR 701.16) with Food/Clue subtypes captured in TypeLine; co-occurring effects preserved. All glossary terms present (Scry, Sacrifice, Draw, Food Token, Clue Token).
  - **Whitelist:** `WOE/CandyTrail` debt entry removed from `whitelist-unparsed.json` — correct, the unparsed node is gone.

## Out-of-scope residual remaining

None. This gold is now fully structured (zero IUnparsed); no other-axis debt remains on it.

## Process notes

Glossary/rules data live at `libs/mtg-rules/Data/_03_Primary/Datasets/` in this checkout (SKILL references the atlas-flow-test path); cross-referenced there.
