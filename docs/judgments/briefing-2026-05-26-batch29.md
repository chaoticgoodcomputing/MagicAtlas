# Batch 29 briefing — 2026-05-26

Single family this batch — keeping it tight after the batch 28 Family B revert. Pre-verified rule citation.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Evolve keyword | #2 | 13 | new `EvolveEffect`, KeywordDefinition |

Skipping cluster #1 (Fear — AST shape deferred), cluster #3 (As-enters-choose-type — deferred), cluster #4 (Unleash — embedded conditional), cluster #5 (Start your engines! — needs MaxSpeed GrantedAbility design from batch 28 follow-up).

---

## Family A: Evolve keyword (cluster #2, +13 yield)

**Failure signal:** Oracle line `Evolve (Whenever a creature you control enters, if that creature's power is greater than this creature's power and/or that creature's toughness is greater than this creature's toughness, put a +1/+1 counter on this creature.)` — Evolve is not registered. Parameterless keyword (like Persist, Flanking, Exalted, Ascend).

### Verified rule citation
- **702.100 Evolve** — "Evolve is a triggered ability. 'Evolve' means 'Whenever a creature you control enters, if that creature's power is greater than this creature's power and/or that creature's toughness is greater than this creature's toughness, put a +1/+1 counter on this creature.'"
- Per `feedback_mast_describes_not_executes`: MAST records the keyword's presence; the entering-creature trigger + power-comparison + counter-placement flow is engine territory (analogous to how Persist, Bestow, Bushido, and other triggered-mechanic keywords are kept presence-only).

### Cards in this family
Gatecrash (GTC) cards introduced Evolve:
- **Cloudfin Raptor** — `Flying\nEvolve (...)`
- **Crocanura**, **Drakewing Krasis**, **Experiment One**, **Gyre Sage**, etc.

Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Evolve|\\nEvolve(?=\\s)"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -15
```

### AST type
- **`EvolveEffect`** at `libs/magic-ast/AST/Effects/Keyword/EvolveEffect.cs`. `[OracleEffect("evolve")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. **No fields.** Direct mirror of `AscendEffect`/`DelveEffect`/`ImproviseEffect`/`FlankingEffect` — the recent parameterless-keyword cohort.

### Parser surface
- New `KeywordDefinition Evolve` in `KeywordDefinitions.cs`. `HasParameter = false`, `Category = KeywordCategory.Static`, `RuleReference = "702.100"`. CreateExpansion → `new StaticAbility { KeywordSource = "Evolve", Effects = [new EvolveEffect()] }`.
- Add to `All` collection.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Evolve",
  "Reminder": { "Text": "(Whenever a creature you control enters, if that creature's power is greater than this creature's power and/or that creature's toughness is greater than this creature's toughness, put a +1/+1 counter on this creature.)" },
  "Effects": [{ "EffectType": "evolve" }]
}
```

### Anti-patterns
- Do NOT model the entering-creature trigger as a separate TriggeredAbility. The keyword IS the abbreviation; the reminder text describes engine behavior.
- Do NOT model the power-toughness comparison as a Condition node.
- Do NOT confuse Evolve with Unleash (similar P/T counter mechanic, different trigger model).
- **Critical lesson from batch 28:** if the keyword reminder describes an inner mechanic with VARIABLE content per card (like Max Speed's `[Ability]` operand), modeling presence-only would lose load-bearing content and FAIL. Evolve's reminder is FIXED text (every Evolve card has the same reminder) — presence-only is correct. Verify the reminder text is identical across the 5-fixture pick before committing.

### Cards to fixture (3)
Pre-validate siblings (Flying, Trample, etc. should all parse). Pick clean printings.

---

## Cross-family notes

- Single-family batch — keeping the loop tight after batch 28's revert.
- **Pre-verified rule citation:** 702.100 — verified verbatim against `rules-structure.json`.
