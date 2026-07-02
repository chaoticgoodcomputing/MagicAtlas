# MAST TDD briefing — batch 5 (autonomous run 1/10)

**Coverage entering this batch:** 7,046 / 29,614 cards (23.79%). NUnit 284/0/284 green.

**Run shape:** 3 families across 3 non-overlapping parser files, hybrid helper-novel + helper-mech waves.

## Family A — Spell composite buff (cluster 1, +37 marginal)

**Failure signal:** parser bails on spell-side `Target creature gets +N/+M and gains <keyword> until end of turn.` shape.

**Parser file:** `libs/magic-ast/Parsing/Parsers/Spell/Rules/` — new rule file `ModifyPTAndGainKeywordSpellRule.cs`. Parallel-safe (rule-per-file architecture in Spell).

### Cards (5 mechanical, no new AST)
1. **Rig for War** — `{1}{R}` Instant — `Target creature gets +3/+0 and gains first strike and reach until end of turn.`
2. **Tread Upon** — `{1}{G}` Instant — `Target creature gets +2/+2 and gains trample until end of turn.`
3. **Daring Leap** — `{1}{W}{U}` Instant — `Target creature gets +1/+1 and gains flying and first strike until end of turn.`
4. **Unnatural Predation** — `{G}` Sorcery — `Target creature gets +1/+1 and gains trample until end of turn.`
5. **Colossal Might** — `{R}{G}` Sorcery — `Target creature gets +4/+2 and gains trample until end of turn.`

### Relevant rules
- **Rule 113.3a** — "An object can have multiple abilities" / spell can have multiple effects.
- **Rule 614 (Continuous effects on PT)** — until-end-of-turn duration.

### AST types in scope
- `SpellAbility` with `Effects: [ModifyPTEffect, GainKeywordEffect or similar, ...]`
- `ModifyPTEffect` — exists; supports `Duration: UntilEndOfTurn`.
- For "gains <keyword>": likely `GainAbilityEffect` (gives the keyword as a granted ability) or `CompositeEffect` wrapping individual keyword grants.
- For multi-keyword "and gains flying and first strike" — handle as either: (a) one effect with multi-keyword list, or (b) multiple chained gain effects.

### Expected generalization
One new `[SpellRule]` file that recognizes the shape `Target creature gets +N/+M and gains <keyword(s)> until end of turn` and emits the appropriate effect bundle. Two-effect SpellAbility (ModifyPT + keyword grant), both with `Duration: UntilEndOfTurn`.

### Anti-patterns
- Don't model "until end of turn" as a separate ability; it's a duration on each effect.
- Don't fork into N separate methods for each keyword variant — the keyword name varies but the shape is one.

---

## Family B — Aura composite buff (cluster 2, +34 marginal)

**Failure signal:** parser bails on Aura static `Enchanted creature gets +N/+M and has <keyword>.` shape.

**Parser file:** `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (monolithic). Only Static-touching family this batch.

### Cards (3 mechanical, no new AST)
1. **Primal Visitation** — `{3}{R}{G}` Aura — `Enchant creature\nEnchanted creature gets +3/+3 and has haste.`
2. **Swashbuckling** — `{1}{R}` Aura — `Enchant creature\nEnchanted creature gets +2/+2 and has haste.`
3. **Goblin War Paint** — `{1}{R}` Aura — `Enchant creature\nEnchanted creature gets +2/+2 and has haste.`

### Relevant rules
- **Rule 303.4 (Auras)** — Auras have an Enchant ability granting them their attachment target.
- **Rule 113.3 (Abilities granted by other abilities)** — "has X" wording grants the keyword to the enchanted permanent.

### AST types in scope
- `StaticAbility` (existing). `KeywordSource: "Enchant"` for the enchant restriction line is already handled.
- Body: a StaticAbility with composite effect or two effects: `ModifyPTEffect { Target: EnchantedOrEquipped, PowerModifier, ToughnessModifier }` + a keyword grant on EnchantedOrEquipped.
- `GainAbilityEffect` (exists per GLOSSARY.md) is the natural fit for "has <keyword>".
- Use `CompositeEffect` (exists, discriminator `composite`) to bundle the two effects in a single StaticAbility.

### Expected generalization
Extend StaticAbilityParser to recognize the `<subject> gets +N/+M and has <keyword>.` shape and emit a composite. Subject is `EnchantedOrEquipped` for "Enchanted creature" and similar.

### Anti-patterns
- Same as Family A — don't fork by keyword name.
- The grant is permanent (no duration) here, unlike Family A.

---

## Family C — Equip + Convoke keywords (clusters 3 + 5, +43 + +33 = +76 marginal combined)

**Failure signal:** two new keyword effect types missing from the AST. Both have well-defined reminder-text patterns parsed by the keyword combinators.

**Parser file:** `libs/magic-ast/Parsing/Combinators/OracleParsers.cs` — extend `SimpleKeyword` (Convoke, no params) and `ParameterizedKeyword` (Equip, cost-bearing). Same file as last batch's parser-mech for Defender/Cycling — well-grooved pattern.

### Cards
**Equip (5 fixtures, helper-novel territory — needs new EquipEffect AST):**
1. **Bonesplitter** — `{1}` Equipment — `Equipped creature gets +3/+0.\nEquip {1} ({1}: Attach to target creature you control. Equip only as a sorcery.)`
2. **Trusty Machete** — `{1}` Equipment — `Equipped creature gets +2/+1.\nEquip {2} (...)`
3. **Barbed Battlegear** — `{3}` Equipment — `Equipped creature gets +3/+1.\nEquip {3} (...)`
4. **Torch Gauntlet** — `{2}` Equipment — `Equipped creature gets +2/+0.\nEquip {2} (...)`
5. **Vulshok Morningstar** — `{3}` Equipment — `Equipped creature gets +3/+3.\nEquip {3} (...)`

**Convoke (3 fixtures, helper-novel territory — needs new ConvokeEffect AST):**
1. **Sundering Vitae** — `{2}{G}` Instant — `Convoke (Your creatures can help cast this spell. ...)\nDestroy target artifact or enchantment.`
2. **Pack's Favor** — `{2}{G}` Instant — `Convoke (...)\nTarget creature gets +3/+3 until end of turn.` (note: composite, may overlap with Family A — pick a different simpler Convoke if needed)
3. **Living Totem** — `{3}{G}` Creature — `Convoke (...)\nWhen Living Totem enters, put a +1/+1 counter on another target creature you control.`

### Relevant rules
- **Rule 702.6 (Equip)** — "{cost}: Attach to target creature you control. Activate only as a sorcery."
- **Rule 702.51 (Convoke)** — "Your creatures can help cast this spell. Each creature you tap while casting this spell pays for {1} or one mana of that creature's color."
- **Glossary (Equip):** "A keyword ability that lets a player attach an Equipment to a creature they control."
- **Glossary (Convoke):** "A keyword ability that lets you tap creatures rather than pay mana to cast a spell."

### AST types to add
- **`EquipEffect`** — discriminator `equip`. Required field: `Cost: Cost` (polymorphic, mirroring CyclingEffect / WardEffect pattern). Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Source: `libs/magic-ast/AST/Effects/Keyword/EquipEffect.cs`.
- **`ConvokeEffect`** — discriminator `convoke`. No parameters (rules-engine handles the tap-for-mana mechanic). Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Source: `libs/magic-ast/AST/Effects/Keyword/ConvokeEffect.cs`.

### Expected generalization
- For Equip: extend `ParameterizedKeyword` in `OracleParsers.cs` — mirror Cycling's combinator (keyword token + mana symbol consumption + optional reminder).
- For Convoke: extend `SimpleKeyword` in `OracleParsers.cs` — mirror Defender's combinator (bare keyword + optional reminder).

### Anti-patterns
- Don't expand EquipEffect to include the rules semantics ("attach to target creature", "only as a sorcery"). That's rules-engine derivation per `feedback_mast_describes_not_executes`.
- Don't try to model the cost-reduction semantics of Convoke. Same reason.

---

## Dispatch plan

**Wave 1 (3 parallel sub-agents):**
- `[sub:helper-novel]` (Opus) — adds EquipEffect + ConvokeEffect + 8 fixtures (5 Equip + 3 Convoke). Branch `mast-tdd/helper-novel-batch5-2026-05-26`.
- `[sub:helper-mech]` (Sonnet) — Family A: 5 Spell composite fixtures. Branch `mast-tdd/helper-mech-spellComposite-batch5-2026-05-26`.
- `[sub:helper-mech]` (Sonnet) — Family B: 3 Aura composite fixtures. Branch `mast-tdd/helper-mech-auraComposite-batch5-2026-05-26`.

**Wave 2 (3 parallel sub-agents, after wave 1 merges + glossary regen):**
- `[sub:mech]` (Sonnet) — Family A: new SpellRule file for "modifyPT + gain keyword + duration" shape. Branch `mast-tdd/mech-spellComposite-batch5-2026-05-26`.
- `[sub:mech]` (Sonnet) — Family B: StaticAbilityParser extension for "X gets +N/+M and has KW". Branch `mast-tdd/mech-auraComposite-batch5-2026-05-26`.
- `[sub:mech]` (Sonnet) — Family C: OracleParsers extensions for Equip (ParameterizedKeyword) + Convoke (SimpleKeyword). Branch `mast-tdd/mech-equipConvoke-batch5-2026-05-26`.

**Yield ceiling:** ~104 cards (37+34+33+? — Equip cluster #3 is 43 marginal but may flip many more via the "Equipped creature gets" sibling parsing that already exists).
