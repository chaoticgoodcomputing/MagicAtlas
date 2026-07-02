# MAST batch 5 — judge briefing

**Date:** 2026-05-25
**Scope:** 4 candidates across 2 parser surfaces (SpellAbilityParser + StaticAbilityParser)
**Doctrine:** orchestrator-writes-gold via helper sub-agent; strict NUnit 100% green to merge.

---

## Candidate 1: Mind Spring (VariableEffect / draw X cards)

**Oracle:** `Draw X cards.`
**Card:** `{X}{U}{U}` Sorcery.

### Relevant rules
- **Rule 107.3b** — "If the variable cost X (or {X}) is in the mana cost of a spell, its controller chooses the value of X as part of casting the spell."
- **Rule 121.1 Draw a card** — drawing N cards repeats the basic "draw a card" step N times.

### Anti-patterns
- The {X} in mana cost AND the X in "Draw X cards" refer to the same chosen value (Rule 107.3b/c). Use `VariableQuantity { Name: "X" }` in both — same descriptive identity, no binding mechanism (MAST is descriptive).
- Don't encode "Draw X cards" as `Count: { QuantityType: "literal", Value: "X" }` — that's an anti-pattern conflating literal-number with variable.

---

## Candidate 2: Culling Mark (must-block, spell-resolution)

**Oracle:** `Target creature blocks this turn if able.`
**Card:** `{2}{G}` Sorcery.

### Relevant rules
- **Rule 509.1c** — block requirements (the defender-side rule that drives "must block if able"). Same rule MustBlockEffect (created in Batch 4) cites.
- **Rule 113.3a** — spell abilities; a sorcery's resolution carries the requirement-imposing instruction.

### Anti-patterns
- MustBlockEffect already exists (Batch 4). The new shape here is using it inside a SpellAbility — the Target is a specific creature (not All), the Duration is `untilEndOfTurn` (encoded as "this turn" in oracle text).
- Don't conflate with MustBeBlockedEffect — that's attacker-side.
- The duration "this turn" must use existing `UntilEndOfTurnDuration` per Rule 700.6.

---

## Candidate 3: Mirror Gallery (legend-rule suppression)

**Oracle:** `The "legend rule" doesn't apply.`
**Card:** `{5}` Artifact.

### Relevant rules
- **Rule 704.5j** — "If a player controls two or more legendary permanents with the same name, that player chooses one of them, and the rest are put into their owners' graveyards. This is called the 'legend rule.'"
- The Mirror Gallery effect suppresses this state-based action entirely.

### Anti-patterns
- Don't model as a `Characteristics: ["legend rule doesn't apply"]` free-text on something — it's a meta-rule suppression and needs structural representation.
- Don't use existing keyword/effect types (it's not a duration, not a damage prevention) — needs a new structured effect.

### Suggested new AST type
- **`LegendRuleSuppressionEffect`** (Rule 704.5j). Discriminator `legendRuleSuppression`. Likely scope-limited via the `Target` field — defaults to "all legendary permanents" but could narrow ("doesn't apply to creature tokens you control" — see The Master, Multiplied's pattern, currently deferred). For Mirror Gallery: no scoping, applies globally. Place at `libs/magic-ast/AST/Effects/Replacement/` (it's structurally a state-based-action suppression, closest to a replacement effect on the legend-rule check).

---

## Candidate 4: Telekinetic Sliver (All-X-have-quoted-ability grant)

**Oracle:** `All Slivers have "{T}: Tap target permanent."`
**Card:** `{2}{U}{U}` Creature — Sliver.

### Relevant rules
- **Rule 113.6** — "Effects can add or remove abilities of objects. ... If multiple effects modify the abilities of an object, those effects are applied in timestamp order."
- **Rule 702.* Sliver** — note: Sliver isn't an ability, it's a creature subtype.
- **Rule 113.10** — "Granted abilities are full-fledged abilities of the gainer."

### Anti-patterns
- Don't model "All Slivers" as `Characteristics: ["All Slivers"]` — that's a typed filter (subtype). Use `ObjectFilter { Subtypes: ["Sliver"] }` with `Kind: Each`.
- Use the existing `GainAbilityEffect.GainedAbility: Ability` shape (introduced in Batch 3) — the inner ability is a real `ActivatedAbility` with a tap cost + tap-target effect, parsed via dispatch to ActivatedAbilityParser (mirrors Find the Path's Aura grant).
- The "All Slivers" target form may differ from the "Enchanted [type]" Aura form recognized by the existing StaticAbilityParser.TryParseGrantedAbility — the subject classification (`ClassifyGrantTarget`) needs to handle `"All [Subtype]s"` in addition to enchanted/equipped.

---

## Suggested AST work for the helper

- **`LegendRuleSuppressionEffect`** for Mirror Gallery. New file, mirrors HasteEffect shape (no parameters, just trait fields).

All other picks use existing AST nodes.

## Suggested mechanical work for the per-card sub-agents

- **Mind Spring:** extend `SpellAbilityParser` with a "Draw X cards" rule (similar to existing `TryParseDrawCardsSimpleEffect` but recognizes `X` as `VariableQuantity.X`).
- **Culling Mark:** extend `SpellAbilityParser` to recognize `Target creature blocks this turn if able.` → SpellAbility wrapping MustBlockEffect with single-target + UntilEndOfTurnDuration.
- **Mirror Gallery:** extend `StaticAbilityParser` to recognize "The \"legend rule\" doesn't apply." → StaticAbility wrapping LegendRuleSuppressionEffect.
- **Telekinetic Sliver:** extend `StaticAbilityParser.ClassifyGrantTarget` to handle "All [Subtype]s" form → ObjectReference { Kind: Each, Filter: { Subtypes: [Sliver] } }; reuse the existing TryParseGrantedAbility dispatch.
