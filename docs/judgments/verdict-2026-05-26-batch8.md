# MAST batch 8 verdict (autonomous run 4/10)

**Result:** PASS. NUnit 372/0/372. Corpus 7,455 → **7,563** (+108, +0.37% absolute). Lines 43.84% → 44.65%.

## What landed

| Family | Cards |
|---|---|
| A — `This X can't be blocked.` (new `CantBeBlockedEffect` + Static sentence dispatch) | 3 |
| B — Morph keyword (new `MorphEffect` + OracleParsers ParameterizedKeyword) | 3 |
| C — Mass-anthem spell `Creatures you control get +N/+M until end of turn` (new `MassAnthemSpellRule`) | 5 |

**Sub-agents:** 5 total (1 helper-novel + 1 helper-mech + 3 mechs).

Mass-anthem mech also extended AbilityClassifier with a routing guard (`until end of turn` suffix differentiates spell-anthem from static Lord-anthem). Bounded sibling addition.

## Top-5 yield clusters now

| Rank | Marginal | Exemplar | Next-batch fit |
|---|---|---|---|
| 1 | 30 | `({T}: Add {W} or {U}.)` | tokenizer/parens-handling — keep deferred |
| 2 | 24 | Bushido (integer-parameterized keyword) | needs novel combinator shape — defer or build |
| 3 | 24 | `You may choose not to untap this creature during your untap step.` | new effect (skip-untap) — novel |
| 4 | 23 | Affinity for artifacts (cost-reduction) | complex cost-modifier mechanic — defer |
| 5 | 23 | `Equipped creature has flying.` | Equipment composite (`Equipped X has KW.`) — extends batch 5 Aura composite to Equipment |

## Closing

**6 batches remaining.** Cumulative across 5+6+7+8: +517 cards (7,046 → 7,563). Sustained ~104-189 per batch with the yield curve flattening toward smaller residuals.
