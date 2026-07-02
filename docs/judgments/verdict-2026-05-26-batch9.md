# MAST batch 9 verdict (autonomous run 5/10)

**Result:** PASS. NUnit 384/0/384. Corpus 7,563 → **7,611** (+48, +0.16% absolute). Lines 44.65% → 44.81%.

## What landed

| Family | Cards |
|---|---|
| A — Skip-untap (`SkipUntapEffect` + StaticAbilityParser sentence) | 3 (Tawnos's Weaponry, Spirit Shield, Endoskeleton) |
| B — Bushido (`BushidoEffect` integer-param + OracleParsers combinator) | 3 (Devoted Retainer, Nezumi Ronin, Samurai Enforcers) |

**Sub-agents:** 3 total (1 helper-novel + 2 mechs). Smaller batch — only 6 fixtures.

## Notable design notes

- **Bushido combinator chose standalone-per-keyword over factory.** Bushido mech found `Crew` already established the integer-parameterized pattern in ParameterizedKeyword chain (using `OracleToken.Number`). Future integer keywords (Annihilator, Modular, Soulshift, Vanishing) follow the same shape; factory extraction worthwhile at ~6 of them.
- **Skip-untap mech added 2 sibling-shape parser surfaces** for the modifyPT-as-long-as line carried by ATQ artifacts: `TryParseModifyPTEffect` in ActivatedAbilityParser + new `ModifyPTAsLongAsSpellRule.cs`. Both well-bounded; all 5 sibling-allowance criteria met.

## Top-5 yield clusters now

| Rank | Marginal | Exemplar | Notes |
|---|---|---|---|
| 1 | 30 | Bicycle land parens | Tokenizer work, still deferred |
| 2 | 23 | Affinity for artifacts | Complex cost-modifier, still deferred |
| 3 | 23 | `Equipped creature has flying.` | Bare Equipment-grant (no PT mod) — Static |
| 4 | 22 | `Zombie tokens you control have flying.` | Token-controller subtype filter + grant — Static |
| 5 | 22 | Self-by-name spell damage (Open Fire) — already partially covered | Spell-side; existing Take Down rule extension |

## Closing

**5 batches remaining.** Cumulative across 5+6+7+8+9: **+565 cards** (7,046 → 7,611, ~3.97% absolute over 5 batches). Per-batch yield: ~110 average, ~48 this batch (smaller scope). Yield curve is flattening as we work down toward exotic mechanics and tokenizer-level work.
