# Batch 28 briefing — 2026-05-26

Two parallel families. Rule citations pre-verified.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Combat-damage-to-player → put +1/+1 counter | #5 | 12 | TriggeredAbilityParser trigger detection + existing PutCountersTriggeredRule |
| B: Start your engines! / Speed | #4 | 12 | new `StartYourEnginesEffect` (or similar), KeywordDefinition |

Skipping cluster #1 (Fear, AST shape deferred), cluster #2 (As-enters-choose-type, deferred), cluster #3 (Unleash, embedded conditional).

---

## Family A: "Whenever this creature deals combat damage to a player, put a +1/+1 counter on it." (cluster #5, +12 yield)

**Failure signal:** Trigger detection for `deals combat damage to a player` is missing in `TriggeredAbilityParser.cs`. `TriggerCondition.Event.DealsCombatDamageToPlayer` enum value EXISTS but no branch matches the phrasing. The effect side ("put a +1/+1 counter on it") is likely handled by `PutCountersTriggeredRule`.

### Verified rule citation
- **702.21 Trample / 510 Combat Damage Step** — combat damage is a defined game event. Triggers that fire on "deals combat damage to a player" use the rule 510.2 framework (the damage event).
- **603.6 Triggered abilities** — generic trigger machinery.
- **122 Counters** — generic counter framework.

Per `feedback_mast_describes_not_executes`: AST records the trigger event (DealsCombatDamageToPlayer) + the effect (PutCountersEffect on Self). Damage assignment and counter-placement bookkeeping are engine territory.

### Cards in this family
- **Boneyard Wurm** (or other Wurm/Beast cards with this trigger)
- **Spike Cannibal** variants
- Find via:
  ```bash
  jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("Whenever this creature deals combat damage to a player, put a \\+1/\\+1 counter on it"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
    tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
  ```

### Existing infrastructure
- `TriggerCondition.Event.DealsCombatDamageToPlayer` — pre-existing enum value.
- `PutCountersEffect` AST and `PutCountersTriggeredRule` — verify rule handles "put a +1/+1 counter on it" with Self subject.

### Parser surface
**1. Trigger detection (TriggeredAbilityParser.cs):** Add a `TryParseDealsCombatDamageTrigger` branch. Match `Whenever <subject> deals combat damage to <player-ref>`. Use `ParseObjectFilter` for the subject (e.g., "this creature" → Self). Map `to a player` / `to an opponent` / `to any player` to the relevant Player filter. Emit `TriggerCondition { Event: DealsCombatDamageToPlayer, Filter: <subject>, ... }`.

**2. Verify PutCountersTriggeredRule:** Read the existing file. Confirm it handles "put a +1/+1 counter on it" with `It()` subject. If not, extend.

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": {
    "Timing": "Whenever",
    "Event": "DealsCombatDamageToPlayer",
    "Filter": { "CardTypes": ["creature"] }
  },
  "Effects": [{
    "EffectType": "putCounters",
    "Target": { "Kind": "It" },
    "CounterType": "+1/+1",
    "Amount": { "QuantityType": "literal", "Value": 1 }
  }]
}
```

(Verify CounterType discriminator and PutCountersEffect field shape against an existing fixture.)

### Anti-patterns
- Do NOT model damage assignment / combat damage step ordering.
- Do NOT model the player choice if multiple players (the trigger fires once per damage event per player; engine handles).

---

## Family B: "Start your engines!" / Speed (cluster #4, +12 yield)

**Failure signal:** Oracle line `Start your engines! (If you have no speed, it starts at 1. It increases once on each of your turns when an opponent loses life. Max speed is 4.)` — Aetherdrift's Speed mechanic. Not registered.

### Verified rule citation
- **702.179 Start Your Engines!** — keyword ability defining the Speed mechanic.
- **702.178 Max Speed** — paired keyword that gates additional abilities by speed value.

### Cards in this family
Aetherdrift (DFT) cards. Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Start your engines!|\\nStart your engines!"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### AST type
- **`StartYourEnginesEffect`** at `libs/magic-ast/AST/Effects/Keyword/StartYourEnginesEffect.cs`. `[OracleEffect("startYourEngines")]`. Parameterless, four trait interfaces. Mirror `ConvokeEffect` / `DelveEffect` / `ImproviseEffect` / `AscendEffect`.

### Parser surface
- New `KeywordDefinition StartYourEngines` (the keyword's text in source: "Start your engines!" — note the exclamation mark). `HasParameter = false`, `Category = Static`, `RuleReference = "702.179"`. CreateExpansion → `new StaticAbility { KeywordSource = "Start your engines!", Effects = [new StartYourEnginesEffect()] }`.
- Add to `All` collection.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain. The exclamation mark may require a custom `Keyword("Start your engines!")` matcher — check how `KeywordDefinitions.cs` handles multi-word keyword names (PartnerWith uses "Partner" + "with" word tokens).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Start your engines!",
  "Reminder": { "Text": "(If you have no speed, it starts at 1. It increases once on each of your turns when an opponent loses life. Max speed is 4.)" },
  "Effects": [{ "EffectType": "startYourEngines" }]
}
```

### Anti-patterns
- Do NOT model the Speed counter mechanic, the "increases when opponent loses life" trigger, or the Max Speed 4 cap.
- Do NOT confuse Start Your Engines! (702.179) with Max Speed (702.178) — different keyword. Defer Max Speed to a future batch.

### Tokenization note
The keyword "Start your engines!" has 3 words + exclamation. Find how multi-word keywords with punctuation are handled (look at existing keywords like "First strike", "Double strike", or compound keywords like "Partner with"). The parser combinator pattern usually expects word-by-word matching with `Keyword("first") + Keyword("strike")`. For "Start your engines!", the pattern would be `Keyword("Start") + Keyword("your") + Keyword("engines") + (potentially "!" as a separate token)`. If the exclamation mark is preserved as part of the word token, adjust.

---

## Cross-family notes

- **Disjoint files.** A: `TriggeredAbilityParser.cs` (trigger detection branch). B: `AST/Effects/Keyword/StartYourEnginesEffect.cs` + `KeywordDefinitions.cs` + `OracleParsers.cs`.
- **Pre-verified rule citations:** A cites 603.6 + 510 + 122; B cites 702.179.
