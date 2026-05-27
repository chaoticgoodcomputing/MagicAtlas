# Batch 24 briefing — 2026-05-26

Two parallel families. Rule citations pre-verified against `rules-structure.json`.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: ETB self-deals damage to any target | #3 | 13 | new TriggeredRule using existing DealDamageEffect |
| B: Improvise keyword | #5 | 13 | new `ImproviseEffect`, KeywordDefinition (mirror Convoke) |

Skipping cluster #1 (Affinity tarpit), cluster #2 (Fear — AST shape deferred), cluster #4 (cost-reduction for type spells — defer pending CostReductionEffect design).

---

## Family A: ETB-self-deals-damage-to-any-target (cluster #3, +13 yield)

**Failure signal:** Oracle line `When this creature enters, it deals 1 damage to any target.` — trigger detection works (ETB self, fixed in batch 20). No `[TriggeredRule]` knows the effect "it deals N damage to any target." `DealDamageEffect` AST exists. Spell-side `SelfDealsDamageToAnyTargetRule` exists (batch 10) but is spell-context; need the triggered analog.

### Verified rule citation
- **603 Triggered abilities** — general triggered-ability machinery (sub-rules 603.1–603.10).
- **119.3 / 120 Damage** — damage-dealing event semantics. `DealDamageEffect` already encodes the verb.
- **115.1 Targets** — "any target" filter for `target creature, player, or planeswalker` (see also 115.4 covering "any target" shorthand).

Per `feedback_mast_describes_not_executes`: AST records source + amount + target filter. Damage resolution is engine.

### Cards in this family
Pre-validate via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("When this (creature|artifact) enters, it deals [0-9]+ damage to any target"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

Likely candidates: small Pingers / Bolt-on-ETB creatures.

### Existing infrastructure
- `DealDamageEffect` — `[OracleEffect("dealDamage")]`. Source + Amount + Target. Verify field shape from `libs/magic-ast/AST/Effects/Damage/DealDamageEffect.cs`.
- Triggered enters-trigger detection (batch 20's ParseObjectFilter unification).
- `SelfDealsDamageToAnyTargetRule` (batch 10, spell-side) — look at this for the effect-text matching pattern.

### Parser surface
New `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SelfDealsDamageToAnyTargetTriggeredRule.cs`. Receives post-trigger effect text. Matches `^it\s+deals\s+(\d+)\s+damage\s+to\s+any\s+target\.?$`. Emits `DealDamageEffect { Source: ObjectReference.It(), Amount: LiteralQuantity, Target: ObjectReference { Kind: Target, Filter: AnyTarget } }`.

Subject is `It()` (the entering creature) — pronoun-reference convention used by `ModifyPTTriggeredRule`. Target is "any target" — check the canonical filter shape for "any target" in existing fixtures (likely a special filter discriminator or characteristic set covering creature/player/planeswalker).

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "Enters", "Filter": { "CardTypes": ["creature"] } },
  "Effects": [{
    "EffectType": "dealDamage",
    "Source": { "Kind": "It" },
    "Amount": { "QuantityType": "literal", "Value": 1 },
    "Target": { "Kind": "Target", "Filter": { /* "any target" canonical shape */ } }
  }]
}
```

(Verify "any target" filter shape against the spell-side `SelfDealsDamageToAnyTargetRule` gold output.)

### Anti-patterns
- Do NOT duplicate `SelfDealsDamageToAnyTargetRule`'s body — share the effect-builder helper if one exists, otherwise factor a small static helper.
- Do NOT model the damage prevention / replacement logic. Just the verb + amount + target.

---

## Family B: Improvise keyword (cluster #5, +13 yield)

**Failure signal:** Oracle line `Improvise (Your artifacts can help cast this spell. Each artifact you tap after you're done activating mana abilities pays for {1}.)` — Improvise is not registered. Parameterless cost-modifier keyword, exact thematic sibling of Convoke.

### Verified rule citation
- **702.126 Improvise** — "Improvise is a static ability that functions while the spell with improvise is on the stack. 'Improvise' means 'For each generic mana in this spell's total cost, you may tap an untapped artifact you control rather than pay that mana.'"

(Convoke's analog is 702.51; Improvise relates closely but is its own rule entry.)

### Cards in this family
Aether Revolt (AER) / Kaladesh-block artifact-themed cards:
- **Reverse Engineer**, **Maverick Thopterist**, **Battle at the Bridge**, **Heart of Kiran** (no — Heart is Crew), etc.

Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Improvise|\\nImprovise"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### AST type
- **`ImproviseEffect`** at `libs/magic-ast/AST/Effects/Keyword/ImproviseEffect.cs`. `[OracleEffect("improvise")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. **No fields.** Direct mirror of `ConvokeEffect` (or `DelveEffect` from batch 21 — same shape).

### Parser surface
- New `KeywordDefinition Improvise` in `KeywordDefinitions.cs`. `HasParameter = false`, `Category = Static`, `RuleReference = "702.126"`. Mirror Convoke / Delve.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain near Convoke / Delve.
- Add to `All` collection.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Improvise",
  "Reminder": { "Text": "(Your artifacts can help cast this spell. Each artifact you tap after you're done activating mana abilities pays for {1}.)" },
  "Effects": [{ "EffectType": "improvise" }]
}
```

### Anti-patterns
- Do NOT model the "tap artifacts → pay mana" mechanic. Keyword presence only.
- Do NOT confuse with Convoke (which taps creatures) or Delve (which exiles graveyard cards).

---

## Cross-family notes

- **Touched files are disjoint.** A: new `Triggered/Rules/SelfDealsDamageToAnyTargetTriggeredRule.cs`. B: new `AST/Effects/Keyword/ImproviseEffect.cs` + `KeywordDefinitions.cs` + `OracleParsers.cs`.
- **No new AST types in Family A** (DealDamageEffect exists).
- **Pre-verified rule citations:** A cites 603 + 119.3 + 115.1 (descriptive references). B cites 702.126 (verified).
- Continue the rule-citation pre-check discipline from batch 23.
