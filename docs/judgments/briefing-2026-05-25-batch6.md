# MAST batch 6 — judge briefing

**Date:** 2026-05-25
**Scope:** 5 candidates
**Doctrine:** orchestrator-writes-gold via helper sub-agent; strict NUnit 100% green to merge.
**Multi-effect-per-clause:** Boiling Blood is the corpus's first MAST fixture that exercises the multi-sentence-single-line bundling doctrine (`feedback_mast_multi_effect_per_clause`). The helper must produce ONE SpellAbility with TWO Effects for that card.

---

## Candidate 1: Boiling Blood (multi-effect spell)

**Oracle:** `Target creature attacks this turn if able. Draw a card.`
**Card:** `{2}{R}` Sorcery.

### Relevant rules
- **Rule 508.1d** — attack requirements; "Target creature attacks this turn if able" imposes one. (`MustAttackEffect` exists.)
- **Rule 121.1** — draw a card.
- **CR 113.3a** — spell abilities are plural; multiple effects can resolve under one ability.

### Anti-patterns
- The two sentences are on ONE line. Per `feedback_mast_multi_effect_per_clause`, this is ONE SpellAbility with TWO Effects, NOT two separate SpellAbilities. Contrast Mental Modulation, which had `\n`-separated lines (so it was correctly split into separate SpellAbilities).
- The MustAttack target here is `Target` with a creature filter (single-target) PLUS Duration `untilEndOfTurn` ("this turn"). Different from Grand Melee's `Each` target with no duration.

### Suggested AST shape
`SpellAbility { Effects: [ MustAttackEffect { Target: Target(creature), Duration: UntilEndOfTurnDuration }, DrawCardsEffect { Count: 1, Player: You } ] }`

---

## Candidate 2: Neutralizing Blast (multicolored counter)

**Oracle:** `Counter target multicolored spell.`
**Card:** `{1}{U}` Instant.

### Relevant rules
- **Rule 701.6** — counter (move from stack to graveyard).
- **Rule 105.5** — "An object is multicolored if it has two or more colors." Structurally, multicolored is its own filter axis (count of colors >= 2).

### Anti-patterns
- Don't model "multicolored" as a `Characteristics: ["multicolored"]` free-text — it's a typed predicate (parallel to `IsColorless` in `ObjectFilter`).
- Add `ObjectFilter.IsMulticolored: bool?` field, parallel to `IsColorless`.

### Suggested new AST field
- `ObjectFilter.IsMulticolored: bool?` — `true` matches objects with two or more colors. Document `Rule 105.5`.

---

## Candidate 3: Gravkill (type-disjunction exile)

**Oracle:** `Exile target creature or Spacecraft.`
**Card:** `{3}{B}` Sorcery.

### Relevant rules
- **Rule 701.10** — exile.
- **Rule 109.1** — type disjunction on target; same convention as Demolish's destroy (`ObjectFilter.CardTypes` multi-element list).

### Anti-patterns
- Spacecraft is a card type (introduced in Aetherdrift). Use `CardTypes: ["creature", "spacecraft"]` (lowercase, multi-element).
- Don't conflate with subtype filters.

---

## Candidate 4: Clash of Wills (counter unless X paid)

**Oracle:** `Counter target spell unless its controller pays {X}.`
**Card:** `{X}{U}` Instant.

### Relevant rules
- **Rule 701.6** — counter.
- **Rule 117.7 UnlessClause** — "[effect] unless [player] pays [cost]" — already structurally modeled via `IPreventableEffect.UnlessClause`.
- **Rule 107.3** — X is variable; the cost paid to prevent the counter is the same X as in the spell's mana cost.

### Anti-patterns
- The `UnlessClause` must use `Player: ObjectReference.Controller` (referring to the spell's controller, antecedent of "its controller"). Don't free-text this.
- The `UnlessClause.Cost` is `PayManaCost` with `VariableQuantity.X` — same X identity as the spell's mana cost.

---

## Candidate 5: Citanul Hierophants ("Creatures you control" mana grant)

**Oracle:** `Creatures you control have "{T}: Add {G}."`
**Card:** `{3}{G}` Creature — Druid.

### Relevant rules
- **Rule 113.6/113.10** — granted abilities are full-fledged abilities of the gainer.
- **Rule 605** — mana abilities (the granted ability is a mana ability).

### Anti-patterns
- "Creatures you control" is `ObjectReference { Kind: Each, Filter: { CardTypes: ["creature"], Controller: You } }`. NOT free-text.
- The granted ability is `ActivatedAbility { Costs: [TapCost], Effects: [AddManaEffect { Mana: "{G}" }], IsManaAbility: true }`. Mirrors Find the Path's Aura grant shape but with a different subject filter.

### Parser-side note
- `StaticAbilityParser.ClassifyGrantTarget` already handles "All [Subtype]s" (Telekinetic Sliver) and "Enchanted/Equipped" (Find the Path). Adding "Creatures you control" / "[CardType]s you control" is a third subject form.

---

## Suggested AST work for the helper
- `ObjectFilter.IsMulticolored: bool?` — new field, parallel to `IsColorless`. Doc-comment cites Rule 105.5.

## Suggested mechanical work per card
- **Boiling Blood:** extend `SpellAbilityParser.TryParseEffects` to handle multi-sentence single-line input (split by `". "` boundary, parse each fragment as an effect, return the list). New `TryParseMustAttackTargetEffect` for the "Target creature attacks this turn if able" shape (Spell-side, mirrors the existing static "All creatures attack..." but with `Target` + `UntilEndOfTurnDuration`).
- **Neutralizing Blast:** extend `SpellAbilityParser.MapColorWord` / `BuildSpellFilter` to recognize "multicolored" → `IsMulticolored: true`.
- **Gravkill:** new `TryParseExileTypeDisjunctionEffect` in `SpellAbilityParser`, mirrors the existing `TryParseDestroyTargetTypeDisjunctionEffect`.
- **Clash of Wills:** extend `TryParseCounterSpellEffect` to recognize the "unless its controller pays {X}" tail → `UnlessClause { Player: Controller, Cost: PayMana(VariableQuantity.X) }`.
- **Citanul Hierophants:** extend `StaticAbilityParser.ClassifyGrantTarget` with a third form: `"Creatures you control"` / `"[CardType]s you control"` → `ObjectReference { Kind: Each, Filter: { CardTypes: [...], Controller: You } }`.
