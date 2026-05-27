# Batch 31 briefing — 2026-05-27

Single mech-hybrid family. Pre-verified rule citation.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Plot keyword | #5 | 11 | new `PlotEffect`, KeywordDefinition (mirror Kicker / Bestow / Unearth) |

Skipping cluster #1 (Fear), cluster #2 (Start your engines! — Max Speed design needed), cluster #3 (As-enters-choose-type), cluster #4 (Unleash).

---

## Family A: Plot keyword (cluster #5, +11 yield)

**Failure signal:** Oracle line `Plot {1}{G} (You may pay {1}{G} and exile this card from your hand. Cast it as a sorcery on a later turn without paying its mana cost. Plot only as a sorcery.)` — Plot is not registered. Cost-parameterized keyword from Outlaws of Thunder Junction (OTJ).

### Verified rule citation
- **702.170 Plot** — "Plot is a keyword ability that functions while the card with plot is in a player's hand. 'Plot [cost]' means 'Any time you have priority during your main phase while the stack is empty, you may exile this card from your hand and pay [cost]. It becomes a plotted card.'"

Per `feedback_mast_describes_not_executes`: AST records keyword + cost. The plot/exile/cast-later flow is engine territory.

### Cards in this family
OTJ + Murders at Karlov Manor cards with Plot:
- Find via:
  ```bash
  jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Plot \\{"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
    tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
  ```

### AST type
- **`PlotEffect`** at `libs/magic-ast/AST/Effects/Keyword/PlotEffect.cs`. `[OracleEffect("plot")]`. Inherits the four standard trait interfaces. Required field `Cost: ManaCost`. **Direct mirror of `KickerEffect` / `BestowEffect` / `UnearthEffect` / `EchoEffect`**.

### Parser surface
- New `KeywordDefinition Plot` in `KeywordDefinitions.cs`. `HasParameter = true`, `ParameterType = ManaCost`, `Category = KeywordCategory.Static`, `RuleReference = "702.170"`. Mirror Kicker / Unearth.
- Add to `All` collection.
- Add Plot combinator to `OracleParsers.cs` ParameterizedKeyword `.Or()` chain (near Kicker / Unearth / Echo).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Plot",
  "Reminder": { "Text": "(You may pay {1}{G} and exile this card from your hand. Cast it as a sorcery on a later turn without paying its mana cost. Plot only as a sorcery.)" },
  "Effects": [{
    "EffectType": "plot",
    "Cost": { "CostType": "mana", "Symbols": [/* per the cost */] }
  }]
}
```

### Anti-patterns
- Do NOT model the "becomes a plotted card" / cast-later mechanic.
- Do NOT confuse with Flashback (different — cast-from-graveyard for a cost), Suspend (delayed-cast on time counters), or Foretell (similar but different exile/reveal timing).
- Do NOT distinguish "Plot {cost}" from any potential "and/or" variants — those are deferred (similar to Kicker's batch 19 deferral of multi-cost forms).

### Cards to fixture (3)
Pick clean printings — Plot is often paired with Disguise or other OTJ keywords; pick cards where siblings already parse.

---

## Cross-family notes

- Single-family batch.
- **Pre-verified rule citation:** 702.170 (Plot). Not 702.169 (Disguise) or 702.171.
- The "Cast" word in the reminder is a verb, not the Cast rule (701.5). Just a doc-comment note.
