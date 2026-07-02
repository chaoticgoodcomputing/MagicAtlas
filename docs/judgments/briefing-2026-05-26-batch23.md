# Batch 23 briefing — 2026-05-26

Two parallel families. Rule citations verified against `rules-structure.json`.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Unearth keyword | #4 | 13 | new `UnearthEffect`, KeywordDefinition |
| B: Mana-symbol reminder line skip | #3 + #5 | ~26 combined | `ClauseSplitter` pre-filter |

Skipping Affinity (#1, tarpit) and Fear (#2, AST shape decision deferred).

---

## Family A: Unearth keyword (cluster #4, +13 yield)

**Failure signal:** Oracle line `Unearth {1}{R} (Activated reminder text…)` — Unearth is not registered. Cost-parameterized keyword (mana cost). The 702.84a reminder text is canonical but MAST records keyword + cost only.

### Verified rule citation
- **702.84 Unearth** — "Unearth is an activated ability that functions while the card with unearth is in a graveyard. 'Unearth [cost]' means '[Cost]: Return this card from your graveyard to the battlefield. It gains haste. Exile it at the beginning of the next end step. If it would leave the battlefield, exile it instead of putting it anywhere else. Activate only as a sorcery.'"

### Cards in this family
Sample: cards with `Unearth {N}{C}` line:
- **Hellspark Elemental** — `Unearth {R}`
- **Viashino Slaughtermaster** — `Unearth {R}`
- **Cunning Sparkmage** — verify
- **Extractor Demon** — `Unearth {3}{B}`
- **Fledgling Imp** (or similar) — `Unearth {B}`

### AST type
- **`UnearthEffect`** at `libs/magic-ast/AST/Effects/Keyword/UnearthEffect.cs`. `[OracleEffect("unearth")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Required field `Cost: ManaCost`. **Direct mirror of `BestowEffect` / `EchoEffect` / `KickerEffect`**.

### Parser surface
- New `KeywordDefinition Unearth` in `KeywordDefinitions.cs`. `HasParameter = true`, `ParameterType = ManaCost`, `RuleReference = "702.84"`. Mirror Kicker (batch 19).
- Add to `OracleParsers.cs` ParameterizedKeyword `.Or()` chain (near Kicker/Echo).
- Add to the `All` collection.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Unearth",
  "Reminder": { "Text": "(...reminder...)" },
  "Effects": [{
    "EffectType": "unearth",
    "Cost": { "CostType": "mana", "Symbols": [/* per the cost */] }
  }]
}
```

### Anti-patterns
- Do NOT model the "return-from-graveyard / gains haste / exile at end step" engine flow.
- Do NOT confuse with Flashback (different mechanic: cast-from-graveyard).

---

## Family B: Mana-symbol reminder line skip (clusters #3 + #5, ~26 yield combined)

**Failure signal:** Standalone oracle lines like `({R/W} can be paid with either {R} or {W}.)` (hybrid mana, cluster #3) and `({G/P} can be paid with either {G} or 2 life.)` (Phyrexian, cluster #5) appear on cards with hybrid / Phyrexian mana costs. These lines are **not abilities** — they're reminder text describing how the mana symbols can be paid. The parser currently treats each as a clause and emits `UnparsedAbility`.

### Verified rule citation
- **107.4** — defines the mana symbols including hybrid `{W/U}`, `{W/B}`, etc. and Phyrexian `{W/P}`, `{U/P}`, etc. The reminder text explains how each symbol can be paid (e.g., `{G/W}{G/W}` can be paid by `{G}{G}`, `{G}{W}`, or `{W}{W}`).

The reminder is purely explanatory — the mana cost's structured symbols already represent the payability flexibility.

### Cards in these clusters
- Hybrid (cluster #3): Boros Recruit, Mourning Thrull, Dimir Guildmage, Wilt-Leaf Liege, Wild Cantor, etc.
- Phyrexian (cluster #5): many Phyrexian cards (New Phyrexia, ONE).

### Parser surface
**`libs/magic-ast/Parsing/ClauseSplitter.cs`** — extend to filter out clauses that match the mana-symbol reminder shape. The clauses are pre-split, so we can drop them before they reach the ability-classification stage.

Pattern to detect (case-insensitive):
```regex
^\s*\(\s*\{[^}]+\}\s+can\s+be\s+paid\s+with\s+.+?\)\s*\.?\s*$
```

Detection cases:
- Hybrid: `({R/W} can be paid with either {R} or {W}.)` — covers `{W/U}`, `{W/B}`, `{U/B}`, `{U/R}`, `{B/R}`, `{B/G}`, `{R/G}`, `{R/W}`, `{G/W}`, `{G/U}` (10 hybrid pairs).
- Phyrexian: `({B/P} can be paid with either {B} or 2 life.)` — covers `{W/P}`, `{U/P}`, `{B/P}`, `{R/P}`, `{G/P}` (5 Phyrexian colors).
- Monocolored hybrid (potentially): `({2/W} can be paid with two generic mana or {W}.)` — verify if cluster contains these and decide.

When the regex matches, the clause is dropped — produces no `Ability` entry. The card's `Oracle.Abilities` list shrinks by one per stripped reminder. Existing parsed abilities remain unchanged.

### Gold AST shape
Cards with hybrid/Phyrexian mana costs have NO entry in `Oracle.Abilities` corresponding to the reminder line. Example for Boros Recruit:
- Oracle text: `({R/W} can be paid with either {R} or {W}.)\nFirst strike`
- Gold abilities: `[StaticAbility { KeywordSource: "First strike", Effects: [...] }]` — only the First strike entry; the reminder is dropped.

### Cards to fixture (5)
Pick 5 cards whose ONLY non-reminder ability already parses (Flying, First strike, Lifelink, etc.):
- **Boros Recruit** — hybrid + First strike.
- **Phyrexian Rager** — Phyrexian + ETB drawCard + lifegain (verify ETB parses).
- **Mourning Thrull** — hybrid + Flying + lifegain trigger (verify).
- **Wild Cantor** — hybrid + sacrifice mana ability.
- Pick one or two more cleanly. Bail on cards with siblings that don't parse.

### Anti-patterns
- Do NOT emit a no-op StaticAbility carrying just the reminder. The mana cost already carries the symbol structure; the reminder line is purely cosmetic.
- Do NOT broaden the regex beyond the documented mana-symbol reminder shape. Other reminders (keyword reminders like Echo's `(At the beginning of...)`) are kept because they attach to actual abilities.
- Do NOT touch `OracleParser.cs` (orchestrator) directly — the filter goes in `ClauseSplitter`.

---

## Cross-family notes

- Files touched are disjoint: A writes `KeywordDefinitions.cs`, `OracleParsers.cs`, new AST file. B touches `ClauseSplitter.cs`.
- The Family B change is descriptive-doctrine pure: removes a clause that has no ability content. Backward compat preserved — every card that previously had only parsed abilities still has the same gold.
- **Pre-dispatch rule-citation check:** Family A cites 702.84 (verified). Family B cites 107.4 (verified). Mechs should reference these verbatim; further citations should be cross-checked against `rules-structure.json` before being baked into source-code doc-comments.
