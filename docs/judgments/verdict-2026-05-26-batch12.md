# MAST batch 12 verdict (autonomous run 8/10)

**Result:** PASS. NUnit 430/0/430. Corpus 7,824 → **8,160** (+336, +1.13% absolute). Biggest delta since batch 5.

## What landed (3 families, all closed cleanly)

| Family | What | Cards |
|---|---|---|
| A — ETB surveil + parenthetical mana | TriggeredAbilityParser.ExtractTrailingReminder + parenthetical activated dispatch (closed long-deferred cluster #1!) | 3 |
| B — Additional cost (sacrifice) | ClauseSplitter prefix peeler + AttributeExtractor.TryExtractAdditionalCosts | 3 |
| C — Landfall ability-word | Generic ability-word prefix peeler (Landfall/Threshold/Delirium/Revolt/Enrage all now ride this path) | 3 |

**Sub-agents:** 5 total (1 helper-novel + 1 helper-mech + 3 mechs).

## Key architectural finding (helper-novel pushed back twice on the briefing)

Helper-novel found that BOTH "novel" AST extensions already exist:
- `AdditionalCostsAttribute : CardAttribute` was already on `Card.Attributes` (with full `AdditionalCost` record).
- `Ability.AbilityWord: string?` already existed as a nullable field on the base record (one existing fixture, SpellSnuff, used it).

Zero new AST types this batch. Big architectural win — helper-novel saved us from inventing duplicate infrastructure.

Plus: incidental `TextSpan.End` `[JsonIgnore]` fix (computed property was being serialized redundantly).

## Generic ability-word infrastructure

Family C's mech added a **single generic prefix peeler** at classifier + parser level. Adding Threshold, Delirium, Revolt, Enrage, Fateful hour, etc. as ability-words is now a one-line addition to `_abilityWords`. Future yield-cluster work in this category drops to ~5 minutes per ability-word.

## Merge note

Two mechs (etbSurveil + landfall) both touched `TriggeredAbilityParser.cs` and `AbilityClassifier.cs`. Manual conflict resolution required — both sets of changes landed (parenthetical activated dispatch + ability-word prefix peeler). Clean conflict resolution; no lost work.

## Top-5 yield clusters now

| Rank | Marginal | Exemplar | Notes |
|---|---|---|---|
| 1 | 24 | Affinity (still deferred — complex cost-modifier) | |
| 2 | 18 | `Exile target land.` | Simple spell rule extension |
| 3 | 18 | `This creature enters with X +1/+1 counters on it.` | ETB-with-counters new shape |
| 4 | 17 | Soulshift | New keyword (integer-parameterized like Bushido) |
| 5 | 17 | Wither | New keyword (no params, similar to Infect) |

The bicycle-parens (cluster #1) is **OUT of top-5** — closed by batch 12. Landfall is OUT (closed). ETB surveil OUT.

## Closing

**2 batches remaining.** Cumulative across 5-12: **+1,114 cards** (7,046 → 8,160, **+3.76% absolute** over 8 batches). +336 this batch — biggest delta of the autonomous run. Per-batch average: ~140.
