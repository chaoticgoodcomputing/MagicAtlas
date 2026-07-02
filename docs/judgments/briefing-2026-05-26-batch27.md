# Batch 27 briefing — 2026-05-26

Two parallel families. Rule citations pre-verified.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Ascend keyword | #4 | 12 | new `AscendEffect`, KeywordDefinition |
| B: "You control enchanted creature" | #2 | 13 | extend StaticAbilityParser using existing `GainControlEffect` |

Skipping cluster #1 (Fear — disjunction-of-filters AST decision still deferred), cluster #3 (As-enters-choose-type — replacement-effect shape decision deferred), cluster #5 (Unleash — embedded `+1/+1 counter + can't-block conditional` is more involved).

---

## Family A: Ascend keyword (cluster #4, +12 yield)

**Failure signal:** Oracle line `Ascend (If you control ten or more permanents, you get the city's blessing for the rest of the game.)` — Ascend is not registered. Parameterless keyword.

### Verified rule citation
- **702.131 Ascend** — has two flavors:
  - 702.131a: Spell ability (on instants/sorceries) — fires once at resolution.
  - 702.131b: Static ability (on permanents) — always-active condition check.
  - 702.131c: "city's blessing" is a designation with no rules meaning beyond as marker for other effects.

MAST records the keyword's presence. The "control ten or more permanents → blessing" condition + engine bookkeeping for the city's blessing flag is engine territory.

### Cards in this family
Ixalan-block cards (`Rivals of Ixalan` set introduced Ascend):
- **Storm the Vault** — ramp+ascend.
- **Pride of Conquerors**, **Pious Interdiction** variants.
- Find via:
  ```bash
  jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Ascend|\\nAscend(?!s)"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
    tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
  ```

### AST type
- **`AscendEffect`** at `libs/magic-ast/AST/Effects/Keyword/AscendEffect.cs`. `[OracleEffect("ascend")]`. Inherits the four standard trait interfaces. **No fields.** Mirror `ConvokeEffect`/`DelveEffect`/`ImproviseEffect` (all recent parameterless keywords).

### Parser surface
- New `KeywordDefinition Ascend` in `KeywordDefinitions.cs`. `HasParameter = false`, `Category = Static` (per 702.131b — permanents). `RuleReference = "702.131"`.
- Add to `All` collection.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain (near other parameterless keywords).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Ascend",
  "Reminder": { "Text": "(If you control ten or more permanents, you get the city's blessing for the rest of the game.)" },
  "Effects": [{ "EffectType": "ascend" }]
}
```

### Anti-patterns
- Do NOT model the "ten or more permanents" condition.
- Do NOT model the city's-blessing flag.
- Do NOT distinguish spell-Ascend vs static-Ascend in the AST — MAST records the keyword; the rules-engine differentiates by 702.131a/b based on card type.

---

## Family B: "You control enchanted creature" (cluster #2, +13 yield)

**Failure signal:** Oracle line `You control enchanted creature.` — Aura body that grants the controller control of the enchanted permanent. `GainControlEffect` exists; no parser recognizes the "You control enchanted X" pattern.

### Verified rule citation
- **612 Continuous effects that change control** — sub-rules govern temporary and indefinite control changes.
- **702.5 / 303 Aura** — Auras attach via Enchant; abilities of the form "You control enchanted [type]" use `ObjectReference.EnchantedOrEquipped()` for the attached object.

### Cards in this family
Control-changing Aura cards:
- **Mind Control** — `You control enchanted creature.`
- **Persuasion**, **Dominate**, **Confiscate**, **Volition Reins**, etc.

Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^You control enchanted") or (.oracle_text | contains("\nYou control enchanted")))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### Existing infrastructure
- `GainControlEffect` — at `libs/magic-ast/AST/Effects/Modification/GainControlEffect.cs`. Verify field shape.
- `StaticAbility.AffectedObjects` for "enchanted X" subject (from batch 25 + 26 patterns).
- `ObjectReferenceKind.EnchantedOrEquipped` for the target.

### Parser surface
Extend `StaticAbilityParser.cs` with a `TryParseGainControlOfEnchanted` (or similar) method. Match `^You control enchanted (creature|permanent|land|artifact|enchantment)\.?$`. Emit:
```csharp
new StaticAbility {
  Effects = [new GainControlEffect {
    Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
    // verify other GainControlEffect fields (e.g., Controller defaults to You; Duration null = indefinite)
  }]
}
```

Inspect `GainControlEffect` to determine its required fields. Likely `Target: ObjectReference` and possibly `NewController: PlayerReference` (default You) + `Duration: Duration?` (default indefinite for Aura body).

### Gold AST shape
```json
{
  "Kind": "static",
  "Effects": [{
    "EffectType": "gainControl",
    "Target": { "Kind": "EnchantedOrEquipped" }
  }]
}
```

(Verify exact field names; the effect may have additional optional fields.)

### Anti-patterns
- Do NOT model temporary-control rules (612.4 etc.).
- Do NOT distinguish "you control" (controller-side) vs "target player controls" — this batch handles "you control" (Aura controller) only.

---

## Cross-family notes

- **Disjoint files.** A: `AST/Effects/Keyword/AscendEffect.cs` + `KeywordDefinitions.cs` + `OracleParsers.cs`. B: `StaticAbilityParser.cs`.
- **Pre-verified rule citations:** A = 702.131; B = 612 (control-changing effects) + 702.5 (Aura).
- Both families work with existing AST scaffolding for the most part.
