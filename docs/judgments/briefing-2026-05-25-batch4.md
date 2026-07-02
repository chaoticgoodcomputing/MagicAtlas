# MAST batch 4 — judge briefing

**Date:** 2026-05-25
**Scope:** 2 candidates (intentional small batch for new-doctrine pilot)
**Doctrine:** orchestrator-writes-gold via helper sub-agent; strict NUnit 100% green to merge.

---

## Candidate 1: Mental Modulation (ComplexTargeting / multi-effect spell)

**Oracle:**
```
This spell costs {1} less to cast during your turn.
Tap target artifact or creature.
Draw a card.
```

**Card metadata:** `{1}{U}` instant, mono-blue.

### Relevant rules

- **Rule 117.6 Cost reductions** — "Casting a spell may require paying additional or alternative costs, or it may allow paying less." Cost-reduction effects that apply only sometimes (e.g., "during your turn") gate via a Condition; the AST already has `CostReductionEffect` with an optional condition.
- **Rule 701.26a Tap** — "To tap a permanent, turn it sideways from an upright position." The existing `TapEffect` carries a target plus optional `Count` quantity; this card uses single-target with a type-disjunction filter.
- **Rule 109.1 Object** — type disjunctions on a target ("artifact or creature") express the legal-target set as the union of two card types. Use `ObjectFilter.CardTypes` as a multi-element list (the existing convention from Demolish's destroy work).
- **Rule 121.1 Draw a card** — "A player draws a card by taking the top card of their library and putting it into their hand."

### Anti-patterns

- Don't encode "during your turn" as a free-text characteristic — it's a structured `Condition` on the static cost-reduction ability (mirrors the existing Zurgo "During your turn, has indestructible" pattern from the stabilization batch).
- Don't model the tap target with `Characteristics: ["artifact or creature"]` free-text — it's a type-disjunction, structurally `CardTypes: ["artifact", "creature"]` (same convention as Demolish's destroy).
- The "Draw a card" effect is a `DrawCardsEffect` with `Count: 1, Player: You` — don't invent a new shape.

### Glossary gaps

None — every term used is in `glossary.json`.

---

## Candidate 2: Grand Melee (ConditionalEffect / new MustBlockEffect)

**Oracle:**
```
All creatures attack each combat if able.
All creatures block each combat if able.
```

**Card metadata:** `{3}{R}` enchantment.

### Relevant rules

- **Rule 508.1d Attack requirements** — "The active player checks each creature they control to see whether it's affected by any requirements (effects that say a creature attacks if able, or that it attacks if some condition is met)." `MustAttackEffect` already exists for the single-creature case; this card extends it to "All creatures" — a different target shape (`ObjectReference.Kind = Each` with a creatures filter).
- **Rule 509.1c Block requirements** — symmetric to 508.1d for the defender side. "The defending player checks each creature they control to see whether it's affected by any requirements (effects that say a creature must block, or that it must block if some condition is met)." Needs a new `MustBlockEffect` AST node, parallel to `MustBeBlockedEffect` (which is on the attacker side — different concept).

### Anti-patterns

- Don't conflate `MustBlockEffect` with `MustBeBlockedEffect`:
  - `MustBeBlockedEffect` (Rule 509.1c) — attacker-side: "I must be blocked" (this creature imposes a block requirement on the defender).
  - `MustBlockEffect` (Rule 509.1c) — blocker-side: "I must block" (this creature must declare a block when able).
  - They're DIFFERENT requirements on different objects, even though both cite 509.1c. The judge will FAIL a fixture that conflates them.
- Target shape: `Each` with a creature filter — NOT `Self`. The MustAttackEffect today defaults to `Self`; the helper may need to verify it accepts `Each` (the AST field is `Target: ObjectReference`, so it should accept any reference kind).
- "All creatures" applies globally, not just to creatures you control. Filter must NOT carry a `Controller: You` qualifier.

### Glossary gaps

None.

---

## Suggested AST work for the helper

- **`MustBlockEffect`** (Rule 509.1c, blocker-side block requirement). Discriminator `mustBlock`. Same field shape as `MustAttackEffect` (Target: ObjectReference, plus the trait fields). Place under `libs/magic-ast/AST/Effects/Combat/`.

That's the only new type needed. Mental Modulation uses entirely existing nodes.

---

## Suggested mechanical work for the per-card sub-agents

- **Mental Modulation:** extend `SpellAbilityParser.TryParseEffects` to recognize the three-effect sequence. Type-disjunction tap target ("Tap target X or Y") needs a new branch in either `SpellAbilityParser` or `TapEffect` recognition. CostReduction-during-your-turn handler may exist from stabilization batch (Chandra's Incinerator did similar work).
- **Grand Melee:** extend `StaticAbilityParser` to recognize "All creatures [verb] each combat if able" with two arms (attack → `MustAttackEffect` w/ Each target; block → `MustBlockEffect` w/ Each target).

Mechanical sub-agents must NOT touch fixtures or AST types — those are the helper's territory.
