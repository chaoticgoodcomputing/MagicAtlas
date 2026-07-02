# Batch 33 briefing — 2026-05-27

Single mech-hybrid family. All AST infrastructure exists.

| Family | Cluster | Yield | Line freq | Surface |
|---|---|---|---|---|
| A: Lord grants keyword to subtype | #5 | 11 | 22 | extend StaticAbilityParser using existing GainAbilityEffect |

Skipping cluster #1 (Start your engines! / Max Speed design), #2 (As-enters-choose-type — queued next batch, needs new effect type), #3 (Unleash — embedded conditional), #4 (Painlands — pay-life-or-tapped).

---

## Family A: "<Subtype> you control have <Keyword>." (cluster #5, +11 yield, 22 lines)

**Failure signal:** Oracle lines like `Goblins you control have haste.`, `Creatures you control have flying.`, `Warlocks you control have menace.` — lord-style static abilities granting a keyword to all controlled members of a subtype. `GainAbilityEffect` exists. No parser surface recognizes this shape today.

### Verified rule citations
- **702 keyword abilities** (each keyword cited in its respective subrule).
- **613.1c Layer 6** — granted abilities (continuous effects that add or remove abilities).
- Per `feedback_mast_describes_not_executes`: AST records "filter → granted keyword"; the layer-6 application + per-permanent broadcast is engine territory.

### Cards in this family
Lord-style cards:
- **Tahngarth, Talruum Hero** (or **Madrush Cyclops** / **Cyclops of Eternal Fury**) — `Creatures you control have haste.`
- **Selenia, Dark Angel** — `Creatures you control have vigilance.`
- **Levitation** — `Creatures you control have flying.`
- **Hag of Noxious Nightmares** — `Warlocks you control have menace.`

Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^[A-Z][a-z]+s you control have (haste|menace|flying|trample|vigilance|lifelink|deathtouch|first strike|double strike|hexproof|reach|defender)\\.$"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -15
```

### Existing infrastructure
- `GainAbilityEffect { Target: ObjectReference, GainedAbility: Ability, ... }` — at `libs/magic-ast/AST/Effects/Modification/GainAbilityEffect.cs`. Recursive — the granted ability is a full `Ability` node (a StaticAbility carrying a keyword).
- `StaticAbility.AffectedObjects: ObjectFilter?` — for "which objects this static applies to" (batch 25/26 pattern).
- `KeywordExpander` (look it up) — likely maps keyword names ("haste", "flying") to their canonical AST representations (HasteEffect, EvasionEffect, etc.).

### AST shape decision
Two reasonable shapes for "Goblins you control have haste":

**Option 1 (filter-on-AffectedObjects, target-on-effect = each):** 
```json
{
  "Kind": "static",
  "AffectedObjects": { "Subtypes": ["Goblin"], "Controller": "You" },
  "Effects": [{
    "EffectType": "gainAbility",
    "Target": { "Kind": "Each" /* or whatever matches existing convention for "all affected objects" */ },
    "GainedAbility": { "Kind": "static", "KeywordSource": "Haste", "Effects": [{ "EffectType": "haste" }] }
  }]
}
```

**Option 2 (filter-on-target):**
```json
{
  "Kind": "static",
  "Effects": [{
    "EffectType": "gainAbility",
    "Target": { "Kind": "Each", "Filter": { "Subtypes": ["Goblin"], "Controller": "You" } },
    "GainedAbility": { ... }
  }]
}
```

Inspect existing GainAbilityEffect fixtures (look at MassAnthemSpellRule / similar) to determine which convention is established. Pick whichever matches. Document choice in manifest.

### Parser surface
Extend `StaticAbilityParser.cs` with `TryParseSubtypeGrantsKeyword` (or generalize the existing `TryParseBareKeywordGrant`, which the briefing notes handle "Enchanted creature has X" but should be checked for whether it also handles `<filter> you control have <keyword>`).

Match pattern: `^<filter-noun-plural> you control have <keyword>\.?$` (case-insensitive). Examples:
- "Goblins you control have haste." → subtype "Goblin", keyword "haste".
- "Creatures you control have flying." → CardType "creature", keyword "flying".
- "Warlocks you control have menace." → subtype "Warlock", keyword "menace".

Build the AST:
- Parse the filter noun (singularize plural; classify as Subtype if creature-type, CardType if `creatures` / `artifacts` / etc.).
- Look up the keyword's canonical effect (use the keyword-expansion infrastructure that KeywordDefinitions provides, or hand-build per the keyword name).
- Wrap in `GainAbilityEffect.GainedAbility` as a StaticAbility.

### Anti-patterns
- Do NOT model the engine-side layer-6 continuous effect application.
- Do NOT distinguish "Goblins" (creature subtype) from "Wizards" (also creature subtype) — they both go to `Subtypes`.
- Do NOT confuse with "Equipped creature gains haste" (Aura/Equipment body; uses EnchantedOrEquipped, batch 5 pattern) — Lord-grants are global filters, not Aura attachments.

### Keyword scope
Restrict this batch to bare-keyword grants: haste, menace, flying, trample, vigilance, lifelink, deathtouch, first strike, double strike, hexproof, reach, defender. Defer "have flying and haste" multi-keyword grants. Defer "have '<full ability>'" grants (e.g., "Goblins you control have 'When this creature dies, draw a card.'").

### Cards to fixture (3)
- 1-2 creature-subtype variants (Goblins / Warlocks / Rats).
- 1 generic creature-type variant ("Creatures you control have …").

Pre-validate sibling abilities.

---

## Cross-family notes

- Single-family batch.
- **Pre-verified rule citations:** 613.1c (layer-6 granted abilities) + the keyword's own subrule.
- AST shape choice (Option 1 vs Option 2) should match existing GainAbilityEffect fixture conventions — document in manifest.
