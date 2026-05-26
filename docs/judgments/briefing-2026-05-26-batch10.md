# MAST TDD briefing — batch 10 (autonomous run 6/10)

**Entering coverage:** 7,611 / 29,614 (25.70%). NUnit 384/0/384.

Both families MECHANICAL — no new AST types this batch.

## Family A — Bare keyword grant to filter/anchor (clusters 3 + 4, +45 marginal combined)

**Parser file:** StaticAbilityParser (monolithic).

### Shapes
- `(Enchanted|Equipped) creature has <keyword>.` (anchor-target grant, e.g., `Equipped creature has flying.`)
- `<Subtype> tokens you control have <keyword>.` (filter-target grant on tokens)

### Cards (5 fixtures, helper-mech)
Pre-curate:
```bash
# Equipped/Enchanted bare grant
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^(Enchanted|Equipped) creature has [a-z][a-z ]+\\.$"))' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -5
# Token-subtype grant
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("tokens you control have"))' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -5
```

Pick 5: 3 Equipment/Aura with bare-grant + 2 with token-subtype-grant.

### Gold AST
For Equipment/Aura:
```json
{
  "Kind": "static",
  "Effect": {
    "EffectType": "gainAbility",
    "Target": { "Kind": "EnchantedOrEquipped" },
    "GainedAbility": { "Kind": "static", "KeywordSource": "Flying", "Effect": { "EffectType": "evasion", ... } },
    "IsOptional": false
  }
}
```
(Use the correct keyword-effect type — Flying maps to EvasionEffect, Haste to HasteEffect, etc.)

For token-grant:
```json
{
  "Effect": {
    "EffectType": "gainAbility",
    "Target": {
      "Kind": "Each",
      "Filter": {
        "CardTypes": ["creature"],
        "Subtypes": ["Zombie"],
        "Controller": "You",
        "Characteristics": ["token"]
      }
    },
    "GainedAbility": { ... flying ... }
  }
}
```

### Parser surface (mech)

Existing `TryParseEnchantedPTAndKeyword` (batch 5) handles `Enchanted/Equipped X gets +N/+M and has KW`. Need a NEW or extended recognizer for the bare `<filter> has/have <keyword>` shape (no PT mod conjunction).

Regex sketch: `^(?:(?<anchor>Enchanted|Equipped) (?<type>creature) has|(?<filter>.+?) (?:you control )?have) (?<kw>[a-z][a-z ]+?)\.$`

Two-arm dispatch:
- anchor arm → `Target: EnchantedOrEquipped`
- filter arm → parse the filter (subtype + creature + controller + token predicate)

---

## Family B — Self-by-name spell damage extension (cluster 5, +22 marginal)

**Parser file:** SpellAbilityParser (rule-per-file).

### Shape
`<CardName> deals <N> damage to <target>.` — spell-side dealDamage where source is the spell's own name.

### Cards (3-4 mechanical, helper-mech)
Pre-curate:
```bash
jq -r '.[] | select(.oracle_text != null and .type_line != null) | select(.type_line | test("Instant|Sorcery")) | select(.name as $n | .oracle_text | test("^\($n) deals \\d+ damage to "))' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

Sample candidates: Open Fire (`Open Fire deals 3 damage to any target.`), Lightning Bolt (note: this is just `Lightning Bolt deals 3 damage to any target` — verify).

### Gold AST
```json
{
  "Kind": "spell",
  "Effects": [
    {
      "EffectType": "dealDamage",
      "Source": { "Kind": "Self" },
      "Amount": { "QuantityType": "literal", "Value": N },
      "Target": { "Kind": "AnyTarget" },  // or appropriate target
      "IsOptional": false
    }
  ]
}
```

### Parser surface

Existing self-by-name rules (`TryParseSelfDealsDamageToFilteredCreatureEffect`, `TryParseSelfDealsDamageToTypeDisjunctionEffect`) handle specific filter shapes. This is the BARE `any target` case. Either extend the existing self-deals-damage rule(s) or add a new rule. Check `Spell/Rules/` for the existing files.

---

## Dispatch plan

**Wave 1 (1 helper-mech only — both families mechanical):**
- `[sub:helper-mech]` (Sonnet): 5 Family A + 3-4 Family B fixtures. ~8-9 total.

Alternative: 2 helper-mechs in parallel, one per family. Use 2 if context budget allows; 1 is fine since both are mechanical.

**Wave 2 (2 parallel mechs):**
- `[sub:mech]` Family A: StaticAbilityParser bare-grant recognizer.
- `[sub:mech]` Family B: SpellAbilityParser self-by-name extension.

**Yield ceiling:** ~67 cards combined.
