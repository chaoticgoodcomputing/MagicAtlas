# Batch 19 briefing — 2026-05-26

Two parallel novel-shape families, both keyword additions following established patterns:

| Family | Cluster | Yield | Pattern | Mirror template |
|---|---|---|---|---|
| A: Flanking | #4 | 15 | Parameterless static keyword | Exalted, Vigilance (parameterless) |
| B: Kicker {cost} | #5 | 14 | Mana-cost-parameterized keyword | Bestow, Echo (cost-parameterized) |

Skipping cluster #1 (Affinity), cluster #2 (attack-trigger +N/+N — investigation deferred), cluster #3 (Fear — needs AST shape decision for color/type-disjunction blocker exception).

Each family is small enough that a single Sonnet hybrid (helper-novel + mech in one worktree) handles AST type + KeywordDefinition registration + OracleParsers chain + 3 fixtures.

---

## Family A: Flanking (cluster #4, +15 yield)

**Failure signal:** Oracle line `Flanking (Whenever a creature without flanking blocks this creature, the blocking creature gets -1/-1 until end of turn.)` — `Flanking` is not registered as a keyword. Reminder text is canonical for the 702.25a triggered semantics, but MAST records keyword presence only (engine handles the triggered behavior).

### Cards in this family
1. **Burning Shield Askari**
2. **Benalish Cavalry**
3. **Mtenda Herder**
4. **Zhalfirin Commander**
5. **Cadaverous Knight**

All 15 cluster cards share the identical oracle line. Pick the 3 cleanest single-keyword printings (no complex sibling abilities).

### Relevant rules
- **702.25 Flanking** — "Flanking is a triggered ability. 'Flanking' means 'Whenever a creature without flanking blocks this creature, the blocking creature gets -1/-1 until end of turn.'" Same descriptive doctrine as Exalted / Bushido (cf. `feedback_mast_describes_not_executes`): keyword effect is parameterless, the trigger expansion is engine territory.

### AST type
- **`FlankingEffect`** at `libs/magic-ast/AST/Effects/Keyword/FlankingEffect.cs`. `[OracleEffect("flanking")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. **No fields.** Mirror `ExaltedEffect` exactly.

### Parser surface
- New `public static KeywordDefinition Flanking` entry in `libs/magic-ast/Keywords/KeywordDefinitions.cs` (don't forget the `All` list).
- Add to `OracleParsers.cs` SimpleKeyword chain (the parameterless keyword section, near Indestructible/Exalted).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Flanking",
  "Reminder": { "Text": "(Whenever a creature without flanking blocks this creature, the blocking creature gets -1/-1 until end of turn.)" },
  "Effects": [{ "EffectType": "flanking" }]
}
```

---

## Family B: Kicker {cost} (cluster #5, +14 yield)

**Failure signal:** Oracle line `Kicker {N}{C} (You may pay an additional {N}{C} as you cast this spell.)` — Kicker not registered. Parameterized by a mana cost.

### Cards in this family
1. **Krosan Druid** — `Kicker {4}{G}`
2. **Sphinx of Lost Truths** — `Kicker {1}{U}`
3. **Sergeant-at-Arms** — `Kicker {2}{W}`
4. **Strength of Night** — `Kicker {B}` (mono mana)
5. **Overload** — `Kicker {2}` (pure generic mana)

Cluster has 14 candidates; pre-validate each. **Defer the `Kicker {A} and/or {B}` multi-cost form** (rule 702.33b) — that's a separate parser surface. Restrict this batch to single-cost `Kicker {cost}`.

### Relevant rules
- **702.33a Kicker** — "Kicker is a static ability that functions while the spell with kicker is on the stack. 'Kicker [cost]' means 'You may pay an additional [cost] as you cast this spell.'"
- **702.33b** — `Kicker {A} and/or {B}` is the same as two kicker abilities (defer; one shape per batch).
- **702.33c Multikicker** — "Multikicker [cost]" means "[cost], any number of times." Defer.

### AST type
- **`KickerEffect`** at `libs/magic-ast/AST/Effects/Keyword/KickerEffect.cs`. `[OracleEffect("kicker")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Required field `Cost: ManaCost`. **Direct mirror of `BestowEffect`** (look at recent batch 14).

### Parser surface
- New `public static KeywordDefinition Kicker` entry in `KeywordDefinitions.cs`. `HasParameter = true`. Mirror `Bestow`/`Echo` pattern.
- Add to `OracleParsers.cs` SimpleKeyword/ParameterizedKeyword chain (near `Bestow` / `Echo` for mana-cost-parameterized keywords).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Kicker",
  "Reminder": { "Text": "(You may pay an additional {N}{C} as you cast this spell.)" },
  "Effects": [{
    "EffectType": "kicker",
    "Cost": {
      "CostType": "mana",
      "Symbols": [/* mana symbols */]
    }
  }]
}
```

### Anti-patterns
- Do NOT model the "if it was kicked" linked-ability behavior (702.33e/f) — that's engine territory and a separate rules feature.
- Do NOT collapse `Kicker {A} and/or {B}` into a single effect with two costs. Defer to a follow-up batch.
- Do NOT confuse Kicker with `AdditionalCostsAttribute` (batch 12) — Kicker is OPTIONAL ("you may pay"), additional costs are MANDATORY ("As an additional cost..."). The two live in different AST namespaces.

---

## Cross-family notes

- **No file overlap on AST.** Family A writes `FlankingEffect.cs`; Family B writes `KickerEffect.cs`.
- **`KeywordDefinitions.cs` overlap.** Both families add entries to the same file and update the `All` list. Insert in different sections (parameterless vs parameterized) — diff-conflict is unlikely but possible. Land Family A first (smaller), then Family B; auto-resolve any trivial conflicts.
- **`OracleParsers.cs` overlap.** Same `.Or()` chain — but new entries go in different parts (Flanking near other parameterless keywords; Kicker near other cost-parameterized). Like Bestow + Persist + Echo from batches 13/14/15 — these merges have been clean historically.
- Land A first, then B. If A's `OracleParsers.cs` edits make B conflict, B rebases on main and re-runs.
