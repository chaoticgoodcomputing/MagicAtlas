# MAST batch 8 — judge briefing

**Date:** 2026-05-25
**Scope:** 5 candidates
**Doctrine:** orchestrator-writes-gold via helper; NUnit 100% to merge.

---

## Candidate 1: Celestial Purge
**Oracle:** `Exile target black or red permanent.` **Cost:** `{1}{W}` Instant.
**Rules:** 701.10 (exile); color disjunction via `Colors[]` multi-element list.
**Gold:** SpellAbility wrapping ExileEffect with Target.Filter `{ CardTypes: ["permanent"], Colors: ["B", "R"] }`.

---

## Candidate 2: Ultimate Price
**Oracle:** `Destroy target monocolored creature.` **Cost:** `{1}{B}` Instant.
**Rules:** 701.7 (destroy); 105.3 (monocolored — reuse `IsMonocolored` from Batch 7).
**Gold:** SpellAbility wrapping DestroyEffect with Target.Filter `{ CardTypes: ["creature"], IsMonocolored: true }`.

---

## Candidate 3: Abandon Attachments (IfYouDo continuation)
**Oracle:** `You may discard a card. If you do, draw two cards.` **Cost:** `{1}{U/R}` Instant.
**Rules:** 117.7 (may/if-you-do); 121.1 (draw); 700.7 (discard).
**Gold:** ONE SpellAbility (multi-sentence single line — per `feedback_mast_multi_effect_per_clause`):
```
SpellAbility { Effects: [
  DiscardCardsEffect {
    Count: literal 1, Player: You, IsOptional: true,
    IfYouDo: DrawCardsEffect { Count: literal 2, Player: You }
  }
] }
```
**Anti-patterns:** DON'T split into two SpellAbilities — the period+space is in-clause, not `\n`. The IfYouDo trait on IOptionalEffect captures the continuation; that's existing infrastructure.

---

## Candidate 4: Excise
**Oracle:** `Exile target attacking creature unless its controller pays {X}.` **Cost:** `{X}{W}` Instant.
**Rules:** 701.10; 117.7 (UnlessClause).
**Gold:** SpellAbility wrapping ExileEffect with:
- Target.Filter `{ CardTypes: ["creature"], Characteristics: ["attacking"] }`
- UnlessClause `{ Player: Controller, Cost: ManaCost(variable X) }`
**Anti-patterns:** "attacking" is a state characteristic — `Characteristics: ["attacking"]` is acceptable here (it's a runtime status, not a structural type predicate). DO use Characteristics for this. The UnlessClause cost uses the same VariableQuantity X as the spell's mana cost.

---

## Candidate 5: Thought Reflection (replacement)
**Oracle:** `If you would draw a card, draw two cards instead.` **Cost:** `{4}{U}{U}{U}` Sorcery.

Wait — the oracle text checks out as Enchantment, not Sorcery. Helper should verify type. Either way, this is a **continuous static** ability with a **replacement effect**.

**Rules:** 614 (replacement effects); 121 (draw a card).
**Gold:** StaticAbility wrapping ReplacementEffect with:
- ReplacementEvent: a draw event (likely `ZoneChangeEvent` with library→hand semantics, OR a new draw-specific event subtype)
- ReplacementModifier: replace with "draw two cards"

This is the most ambitious card in the batch — replacement effects haven't been deeply exercised. The existing AST has `ReplacementEffect` and `ReplacementEvent` (with `ZoneChangeEvent`, `DamageEvent`, etc.). Need to check whether a draw event is already representable.

**Anti-patterns:** Don't model the replacement as `IfYouDo` — IfYouDo is continuation, not substitution. Replacement is structurally different (the "instead" keyword is load-bearing).

If the existing ReplacementEvent union doesn't cover draw-a-card, the helper may need a `DrawCardEvent` (new). Flag to surface.

---

## Suggested AST work for the helper
- **`DrawCardEvent : ReplacementEvent`** (Rule 121.1 + 614 replacement) — likely needed for Thought Reflection. Place at `libs/magic-ast/AST/Effects/Replacement/DrawCardEvent.cs`. Discriminator `drawCard`. Check first if any existing event type covers "would draw a card".

## Suggested mechanical work
- **Celestial Purge:** extend the existing exile-spell parser (e.g. add a recognizer for "Exile target X or Y permanent" — color disjunction on permanent type).
- **Ultimate Price:** add "Destroy target monocolored [type]" recognizer (mirrors VanishingVerse's exile).
- **Abandon Attachments:** the helper bundles into one SpellAbility with IfYouDo. Parser needs to recognize "You may discard a card. If you do, draw [N] cards." as a single effect-with-continuation. Likely needs multi-sentence handling in SpellAbilityParser.
- **Excise:** extend exile parser for "Exile target attacking creature unless its controller pays {X}". Combines characteristic + UnlessClause.
- **Thought Reflection:** new ReplacementEffect recognizer in StaticAbilityParser. Pattern: "If you would [event], [replacement] instead."
