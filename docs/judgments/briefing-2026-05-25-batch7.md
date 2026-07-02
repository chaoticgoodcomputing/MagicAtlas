# MAST batch 7 — judge briefing

**Date:** 2026-05-25
**Scope:** 4 candidates
**Doctrine:** orchestrator-writes-gold via helper; NUnit 100% to merge.

---

## Candidate 1: Vanishing Verse
**Oracle:** `Exile target monocolored permanent.` **Cost:** `{W}{B}` Instant.
**Rules:** Rule 701.10 (exile); Rule 105.3 ("An object is monocolored if it has exactly one color"). Mirror of IsMulticolored.
**Suggested AST work:** new `ObjectFilter.IsMonocolored: bool?` (parallel axis to IsMulticolored).
**Gold:** SpellAbility wrapping ExileEffect with Target.Filter `{ CardTypes: ["permanent"], IsMonocolored: true }`.

---

## Candidate 2: Flashfreeze
**Oracle:** `Counter target red or green spell.` **Cost:** `{1}{U}` Instant.
**Rules:** Rule 701.6 (counter); color disjunction on filter (multiple entries in `Colors[]` = disjunction).
**Gold:** SpellAbility wrapping CounterSpellEffect with Target.Filter `{ CardTypes: ["spell"], Colors: ["R", "G"] }`.
**Anti-patterns:** Don't use IsMulticolored — those are independent axes. Color list with multiple entries IS the disjunction.

---

## Candidate 3: Irresistible Prey
**Oracle:** `Target creature must be blocked this turn if able.\nDraw a card.` **Cost:** `{G}` Instant.
**Rules:** Rule 509.1c (must-be-blocked); Rule 121.1 (draw).
**Gold:** TWO SpellAbilities (the `\n` separates per per-clause-one-ability):
1. SpellAbility wrapping `MustBeBlockedEffect { Target: Target(creature), Duration: UntilEndOfTurnDuration }`.
2. SpellAbility wrapping DrawCardsEffect.
**Anti-patterns:** Don't conflate with MustBlock (already taught — that's blocker-side; this is attacker-side requirement on a creature).

---

## Candidate 4: Crystal Grotto
**Oracle:** `When this land enters, scry 1.\n{T}: Add {C}.\n{1}, {T}: Add one mana of any color.` **Cost:** `""` (land). **Type:** Land.
**Rules:** Rule 603.6c (ETB triggers); Rule 605 (mana abilities); Rule 107.4d (colorless mana symbol).
**Gold:** Three abilities:
1. Triggered: `{ Trigger: When, Enters, Filter: { CardTypes: ["land"] }, Effects: [ScryEffect{Count:1}] }`.
2. Activated: `{ Costs: [TapCost], Effects: [AddManaEffect { Mana: "{C}" }], IsManaAbility: true }`.
3. Activated: `{ Costs: [ManaCost{Symbols:[generic 1]}, TapCost], Effects: [AddManaEffect { Mana: "any color", AnyColor: true }], IsManaAbility: true }`.
**Anti-patterns:** Don't model "Add one mana of any color" as a free-text Mana field — use `AnyColor: true` plus a sensible Mana string. The existing AddManaEffect supports this via `AnyColor: bool`.
