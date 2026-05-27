# Batch 22 briefing — 2026-05-26

Three focused mech families. All AST infrastructure already exists; each family is a tight parser-side extension.

| Family | Cluster | Yield | Surface | Touches |
|---|---|---|---|---|
| A: Become target → sacrifice | #3 | 14 | TriggeredAbilityParser + new TriggeredRule | trigger detection + new effect rule |
| B: PT-buff "for each X you control" | #4 | 13 | StaticAbilityParser | extend existing PT-mod rule with CountQuantity |
| C: Counter target spell unless pays {N} | #5 | 13 | Spell/Rules/CounterSpellRule | extend regex from `{[A-Za-z]}` to also accept `{N}` (numeric) |

Skipping cluster #1 (Affinity tarpit) and cluster #2 (Fear — AST shape decision deferred). Touched files are disjoint across families.

---

## Family A: Become target → sacrifice (cluster #3, +14 yield)

**Failure signal:** Oracle line `When this creature becomes the target of a spell or ability, sacrifice it.` — `TriggerCondition.Event.BecomesTarget` enum value EXISTS (`AST/Triggers/TriggerCondition.cs`), but `TriggeredAbilityParser.Parse`'s trigger-detection branch has no "becomes the target" matcher, and no `[TriggeredRule]` knows the effect "sacrifice it" / "sacrifice this creature."

### Cards in this family
1. **Drift of Phantasms** — has multiple abilities; verify siblings.
2. **Submerged Boneyard**, **Spire Phantasm**, etc.
3. Pre-validate by querying corpus:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("becomes the target of a spell"))) | .name' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -15
```

### Relevant rules
- **603.6 Triggered abilities** — events that trigger include "[object] becomes the target of [spell/ability]" (the canonical "target trigger" shape).
- **701.16 Sacrifice** — "To sacrifice a permanent, its controller moves it from the battlefield directly to its owner's graveyard."
- Per `feedback_mast_describes_not_executes`: AST records the trigger event + effect verb + target. The targeting / stack interactions are engine territory.

### Existing infrastructure
- `TriggerCondition.Event.BecomesTarget` enum value (already defined).
- `SacrificeEffect` AST type (already defined in `AST/Effects/ZoneChange/SacrificeEffect.cs`) — carries `Target: ObjectReference`.

### Parser surface
Two changes in different files:

**1. Trigger detection** — `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs`. Add a branch in `TryParseTriggerCondition` (or wherever the trigger phrasing matcher lives) for `becomes the target of a spell or ability` → `TriggerCondition { Event: BecomesTarget, Filter: { CardTypes: ["creature"], References: [Self] } }` (or the existing convention for self-subject triggers; check how "this creature attacks/dies/enters" emits and mirror).

**2. Sacrifice effect rule** — new `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SacrificeTriggeredRule.cs`. Matches post-trigger effect text `^sacrifice (it|this creature|this permanent)\.?$` and emits `SacrificeEffect { Target: Self }` (or `It()`, matching the pronoun-reference convention used by `ModifyPTTriggeredRule` for "it gets").

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "BecomesTarget", "Filter": { "CardTypes": ["creature"] } },
  "Effects": [{ "EffectType": "sacrifice", "Target": { "Kind": "It" } }]
}
```

(Verify the trigger filter shape — "what becomes the target" vs "what's the trigger object" — by inspecting how dies/enters triggers populate `Trigger.Filter`.)

### Anti-patterns
- Do NOT model the spell/ability stack interaction. Just record the trigger event.
- Do NOT add a new AST type — `SacrificeEffect` exists.

---

## Family B: PT-buff "for each X you control" (cluster #4, +13 yield)

**Failure signal:** Oracle line `This creature gets +1/+0 for each artifact you control.` — static PT modifier with a per-permanent-count quantity. `CountQuantity` exists in the AST. `ModifyPTEffect` exists. Existing static parser doesn't recognize the "for each X you control" quantity in PT-mod context.

### Cards in this family
1. **Tempered Steel** variants; search for "for each artifact you control" / "for each creature you control" etc.
2. Pre-validate via corpus query.

### Relevant rules
- **613 Continuous effects** — variable static modifications calculated at need (each `for each` clause re-evaluates).
- Per `feedback_mast_describes_not_executes`: AST stores the per-filter count quantity. Engine recalculates.

### Existing infrastructure
- `CountQuantity` (Quantity discriminator). Check if it carries `Filter: ObjectFilter` to specify "what to count" — likely yes. Inspect `libs/magic-ast/AST/Quantities/CountQuantity.cs`.
- `ModifyPTEffect` with `PowerModifier: Quantity`, `ToughnessModifier: Quantity`. Currently most fixtures use `LiteralQuantity` (`+N/+N`). Replacing with `CountQuantity` is the descriptive move.

### Parser surface
Extend the relevant PT-mod path in `StaticAbilityParser.cs` (look for "gets +N/+M" detection, probably `TryParseBareKeywordGrant` or a separate static-PT rule). When the modifier has the shape `+N/+M for each <filter> you control`, emit `ModifyPTEffect { PowerModifier: CountQuantity { Per: <filter>, Multiplier: N }, ToughnessModifier: CountQuantity { Per: <filter>, Multiplier: M } }` — exact field names depend on CountQuantity's shape.

### Gold AST shape
```json
{
  "Kind": "static",
  "Effects": [{
    "EffectType": "modifyPT",
    "Target": { "Kind": "Self" },
    "PowerModifier": {
      "QuantityType": "count",
      "Of": { "CardTypes": ["artifact"], "Controller": "You" },
      "Multiplier": 1
    },
    "ToughnessModifier": { "QuantityType": "count", "Of": { ... }, "Multiplier": 0 }
  }]
}
```

(Verify `CountQuantity` discriminator + field names; the structure above is approximate.)

### Anti-patterns
- Do NOT use a free-text string for the "per X" filter. Use the structured `ObjectFilter` carried by `CountQuantity`.
- If the modifier is `+0` on one side, still emit `CountQuantity` with `Multiplier: 0` (or skip the side and use `LiteralQuantity 0` — pick whichever matches existing fixture conventions).

---

## Family C: Counter target spell unless pays {N} (cluster #5, +13 yield)

**Failure signal:** Oracle line `Counter target spell unless its controller pays {1}.` (or `{2}`, `{6}` — generic mana amounts). `CounterSpellRule` exists and handles the `unless its controller pays {X}` form, but the regex `\{(?<unlessx>[A-Za-z])\}` only matches a single letter inside the braces — missing the generic-numeric case `\{[0-9]+\}` and probably the `{N}{C}` colored-cost case too.

### Cards in this family
1. **Mana Leak** — `Counter target spell unless its controller pays {3}.`
2. **Force Spike** — `Counter target spell unless its controller pays {1}.`
3. **Memory Lapse**, **Spell Pierce**, etc.

### Relevant rules
- **701.6 Counter** — "To counter a spell or ability means to remove it from the stack." Already cited in `CounterSpellRule:11`.

### Parser surface
Single file edit: `libs/magic-ast/Parsing/Parsers/Spell/Rules/CounterSpellRule.cs:37`. Extend the regex from:
```
(?:\s+unless\s+its\s+controller\s+pays\s+\{(?<unlessx>[A-Za-z])\})?\.?$
```
to also accept generic numeric mana costs (and ideally arbitrary `ManaCost` strings):
```
(?:\s+unless\s+its\s+controller\s+pays\s+(?<unless>\{[^}]+\}(?:\{[^}]+\})*))?\.?$
```

The `<unless>` capture is the full mana-cost string; parse it via `ManaCostParser` (used elsewhere in the codebase). Plumb the parsed `ManaCost` into the `UnlessClause`.

Inspect `UnlessClause` — does it accept a parsed `ManaCost` or a raw string? If it stores `Cost: Cost` (polymorphic), wrap the mana cost in a `ManaCost` instance.

### Gold AST shape (for the unless clause)
```json
{
  "Kind": "spell",
  "Effects": [{
    "EffectType": "counter",
    "Target": { "Kind": "Target", "Filter": { "CardTypes": ["spell"] } },
    "UnlessClause": {
      "Player": { "Kind": "TargetController" },
      "Cost": {
        "CostType": "mana",
        "Symbols": [{ "Kind": "generic", "GenericAmount": 1 }]
      }
    }
  }]
}
```

(Verify `UnlessClause.Player` discriminator value for "the targeted spell's controller" — should be `TargetController` or similar.)

### Anti-patterns
- Do NOT keep the X-only regex when extending. Extend to accept any mana-cost string, not just `{X}` or `{N}` — `{1}{U}` cards (Disenchant variants) should also pass.
- Do NOT add a new effect type; `CounterEffect` (or whatever the existing one is called) handles this.

---

## Cross-family notes

- **Touched files are disjoint.** A: TriggeredAbilityParser + new Triggered/Rules/. B: StaticAbilityParser. C: Spell/Rules/CounterSpellRule.
- **No new AST types in any family.** All scaffolding exists (`BecomesTarget` enum value, `SacrificeEffect`, `CountQuantity`, `UnlessClause`, `CounterEffect`).
- Each family can run as a single mech-hybrid (fixtures + parser) in parallel.
