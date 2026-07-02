# Batch 21 briefing — 2026-05-26

Two novel-shape families:

| Family | Cluster | Yield | Files |
|---|---|---|---|
| A: ETB-energy (`you get {E}{E}`) | #2 | 20 | new `GainEnergyEffect`, new triggered rule |
| B: Delve keyword | #5 | 14 | new `DelveEffect`, KeywordDefinition |

Skipping Affinity (tarpit), Fear (AST shape decision), and become-target-sacrifice (Family C — defer one batch to avoid TriggeredAbilityParser file pressure across two parallel mechs).

---

## Family A: ETB-energy "you get {E}{E}" (cluster #2, +20 yield)

**Failure signal:** Oracle line `When this creature enters, you get {E}{E} (two energy counters).` — trigger detection works (ETB self), but no `[TriggeredRule]` knows the effect verb "you get {E}…" (energy counters). No `GainEnergyEffect` AST exists.

### Cards in this family
1. **Aether Hub** — `{T}, Pay {E}{E}: …`-style + energy generators. Look for clean ETB-energy printings.
2. **Bristling Hydra**, **Servant of the Conduit**, **Era of Innovation**, and other Kaladesh/Aether Revolt cards.

Sample exemplars: River Herald Scout-style ETBs. All 20 cluster cards share the canonical "When this creature enters, you get {E}{E}" / "you get {E}" shape (the variation is the count).

### Relevant rules
- **107.10 Energy symbol** — `{E}` represents one energy counter. The symbol can appear in cost or effect contexts.
- **122 Counters** — generic counter rules. Energy counters live on players, not permanents.
- **702.66 Delve / 702.84 / 702.116** — none govern energy. Energy is a generic player counter (cf. glossary; defined as "a player resource accumulated as energy counters").
- Per `feedback_mast_describes_not_executes`: AST records the effect verb + count + recipient; the energy-counter-on-player bookkeeping is engine territory.

### AST type
- **`GainEnergyEffect`** at `libs/magic-ast/AST/Effects/Resource/GainEnergyEffect.cs`. `[OracleEffect("gainEnergy")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Required field `Amount: Quantity` (use `LiteralQuantity` for fixed counts). Optional field `Player: PlayerReference` defaulting to "you" — model the implicit "you" subject as null or as `PlayerReference.You()`; check existing conventions in `AST/Effects/Resource/`.

Mirror the resource-effect shape of existing `GainLifeEffect` / `LoseLifeEffect` if those exist; or `DrawCardsEffect` for the `Amount + Player` field convention.

### Parser surface
- New `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/GainEnergyTriggeredRule.cs`. Receives post-trigger effect text, matches `^you get (\{E\})+ \(.+\)?\.?$` (or a count-explicit variant like `you get {E}{E}` → count = 2). Emits `GainEnergyEffect { Amount: <count>, Player: <implicit you> }`.
- Consider whether the same rule should also handle the spell version (`Untap target permanent. You get {E}.` style splits — out of scope for this batch; restrict to ETB triggered shapes only).

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "Enters", "Filter": { "CardTypes": ["creature"] } },
  "Effects": [
    { "EffectType": "gainEnergy", "Amount": { "QuantityType": "literal", "Value": 2 } }
  ],
  "Reminder": { "Text": "(two energy counters)" }
}
```

(Verify Quantity discriminator spelling — `literal` vs `Literal` vs `LiteralQuantity` — by inspecting existing `LiteralQuantity` JSON in fixtures.)

### Cards to fixture (5)
Find 5 clean ETB-energy printings via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("When this creature enters, you get \\{E\\}"))) | .name + " | " + .oracle_text' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -20
```

Pick single-line printings where possible.

### Anti-patterns
- Do NOT model energy as a permanent counter — energy counters live on players (rule 122 generic, energy is a player resource).
- Do NOT collapse "you get {E}{E}" into "you get 2 energy counters" semantically — model the AST verb + count, keep the literal symbol-count interpretation in the parser.

---

## Family B: Delve keyword (cluster #5, +14 yield)

**Failure signal:** Oracle line `Delve (Each card you exile from your graveyard while casting this spell pays for {1}.)` — `Delve` is not registered. No `DelveEffect` AST.

### Cards in this family
1. **Treasure Cruise** — `Draw three cards. Delve (...)` (multi-line)
2. **Dig Through Time** — `Look at the top seven cards of your library. ... Delve (...)` (multi-line)
3. **Murderous Cut** — `Destroy target creature. Delve (...)`
4. **Become Immense** — `Target creature gets +6/+6 until end of turn. Delve (...)`
5. **Logic Knot** — `Counter target spell unless its controller pays {X}. Delve (...)`

All 14 cluster cards share the identical Delve reminder text. Helper picks 3 whose sibling spell-body lines already parse (Treasure Cruise's "Draw three cards" likely via `DrawCardsSimpleRule`; Murderous Cut's "Destroy target creature" via `DestroyTargetSimpleRule`).

### Relevant rules
- **702.66 Delve** — "Delve is a static ability that functions while the spell with delve is on the stack. 'Delve' means 'For each generic mana in this spell's total cost, you may exile a card from your graveyard rather than pay that mana.'"
- Per `feedback_mast_describes_not_executes`: AST records keyword presence; the cost-substitution mechanic is engine territory.
- Mirror Convoke (`ConvokeEffect`, batch 5) — same shape (cost-modifier keyword, parameterless, presence-only).

### AST type
- **`DelveEffect`** at `libs/magic-ast/AST/Effects/Keyword/DelveEffect.cs`. `[OracleEffect("delve")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. **No fields.** Mirror `ConvokeEffect` exactly.

### Parser surface
- New `KeywordDefinition Delve` in `KeywordDefinitions.cs` (parameterless). `RuleReference = "702.66"`.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain (near other parameterless keywords).
- Add to the `All` collection.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Delve",
  "Reminder": { "Text": "(Each card you exile from your graveyard while casting this spell pays for {1}.)" },
  "Effects": [{ "EffectType": "delve" }]
}
```

### Anti-patterns
- Do NOT model the "exile cards → pay generic mana" substitution flow. The keyword's presence is the whole AST record.
- Do NOT confuse Delve with Kicker. Delve modifies cost-payment (substitution); Kicker adds an optional cost. Different mechanics.

---

## Cross-family notes

- Files touched are disjoint: Family A writes `AST/Effects/Resource/GainEnergyEffect.cs` + `Parsing/Parsers/Triggered/Rules/GainEnergyTriggeredRule.cs`. Family B writes `AST/Effects/Keyword/DelveEffect.cs` + `KeywordDefinitions.cs` + `OracleParsers.cs`.
- The Resource/ directory may not exist yet (verify); if not, place `GainEnergyEffect` somewhere sensible (e.g., `AST/Effects/Resource/` or `AST/Effects/Core/`) and document in the manifest.
