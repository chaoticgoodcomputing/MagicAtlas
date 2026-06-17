# Dedicated-surface designs — the deferred hard cards (2026-06-17)

Implementation-ready specs for the cards the fan-out pilot deferred because they need bespoke
parser-surface design (not batch-cards). Grounded in the actual AST surfaces (verified present), so
implementation is a focused vertical slice per card, not open-ended design. See
[`parser-coverage-pilot.md`](parser-coverage-pilot.md) for why these were deferred (each failed twice
under fan-out: unanchored-regex overfit, then free-text residual).

## Rings of Brighthearth (124 combos)

> Whenever you activate an ability, if it isn't a mana ability, you may pay {2}. If you do, copy that
> ability. You may choose new targets for the copy.

**Reuses (verified present):** `ConditionalPayEffect` ("you may pay [cost]. If you do, [IfYouDo]" — see
`Deathgreeter`, `Nim Deathmantle`) and `CopyEffect` (which already has `MayChooseNewTargets: bool?` and a
`Target: ObjectReference`).

**Gold Output shape:**
```
TriggeredAbility {
  Trigger:      { Timing: Whenever, Event: ActivatesAbility, Filter: { Controller: You } },
  InterveningIf: <triggering ability is NOT a mana ability>,
  Effects: [ ConditionalPayEffect {
      Cost:   ManaCost {2},
      IfYouDo: CopyEffect { Target: <the triggering ability>, MayChooseNewTargets: true } } ]
}
```

**New surfaces (3, small):**
1. `TriggerEvent.ActivatesAbility` — new enum value (+ its `PortWalkProjection`/`known-coarse` entry if it
   forms an interaction edge; likely coarse for now). CR 603.2 / 602.
2. **Is-mana-ability intervening-if condition.** A real `[ConditionKind("triggeringAbilityIsMana")]`
   record (NOT a free-text `other` residual — that was the batch-4 FAIL). Carries a bool so the negation
   ("isn't") is structured. CR 605 (mana abilities).
3. **"that ability" reference.** `CopyEffect.Target` needs an `ObjectReference` for the triggering
   activated/triggered ability on the stack (CR 113 — an ability is an object). Add an
   `ObjectReferenceKind.TriggeringAbility` (or a filter `CardTypes:["ability"]` on `It`). Prefer a named
   kind for clarity.

**Parser rule:** ONE new `[TriggeredRule]`/`[TriggerConditionRule]` matching the full clause, **anchored**
(`^…$`) — the two prior FAILs were an unanchored `\byou activate an ability\b` that matched as a substring
inside more-specific triggers and dropped their filters. After implementing, run the parser over the 11
corpus cards containing "activate an ability" and confirm none are mislabeled.

**Note:** This card is the strongest argument for the `FANOUT §1.4` `[QualifierAxis]`/trigger-condition
reflection registry — the overfit hazard is structural, not a worker mistake.

## The One Ring (58 combos)

> When this enters, if you cast it, you gain protection from everything until your next turn.
> {T}: Add a burden counter on The One Ring, then draw a card for each burden counter on it.
> At the beginning of your upkeep, you lose 1 life for each burden counter on The One Ring.

**Gold Output shape:** three abilities —
1. ETB triggered with an **intervening-if "if you cast it"** → `GainAbility(ProtectionFromEverything)` to
   You, `Duration: UntilYourNextTurn`.
2. Activated `{T}:` → `[PutCounterEffect(burden, on Self), DrawCardsEffect(Count: CountQuantity over
   burden counters on Self)]`.
3. Upkeep triggered → `LoseLifeEffect(Count: CountQuantity over burden counters on Self)`.

**New surfaces (3, small):**
1. **"if you cast it" intervening-if** — a cast-this-object condition (akin to the merged
   `CastThisTurnPredicate` from Aetherflux, but a boolean intervening-if on the ETB). New `ConditionKind`
   or reuse the cast-history predicate. CR 603.4.
2. **Protection from everything** — a `ProtectionEffect`/`GainAbility` with an "everything" quality (CR
   702.16 protection). Likely a new keyword-ability variant `Protection { From: Everything }`.
3. **Burden-counter count** — `PutCounterEffect` with a custom counter name "burden" + `CountQuantity`
   over named counters on Self (the counter-count quantity machinery exists; confirm a named-counter
   filter). The draw/lose-life scaling reuse existing `DrawCardsEffect.Count` / `LoseLifeEffect` with the
   count quantity.

**Note:** `protection from everything` likely recurs (Sphere of Safety-adjacent, etc.), so the protection
surface is reusable beyond this card.

## Carried FAILs (simpler — orchestrator fixes, branches preserved)

- **Hapatra, Vizier of Poisons** (`mast-tdd/parse-hapatra-vizier`): gold clean; the shared
  `TriggeredRuleHelpers.cs` change is an unanchored overfit mislabeling siblings. Fix = anchor the matcher
  (same class as Rings; fold into the §1.4 registry work).
- **Ulalek, Fused Atrocity** (`mast-tdd/parse-ulalek-fused-atrocity`): `CopyEffect` target filter drops
  the "other" qualifier (CR 109.5); fix = add `ExcludeSelf: true` to the copy filter (the `ExcludeSelf`
  machinery is already wired through `ObjectFilterRelations`).

## Recommended implementation order

1. The `FANOUT §1.4` reflection-seam registry first (it makes Rings' anchored trigger-condition new-file
   and retires the overfit class that blocks Rings + Hapatra).
2. Rings (3 surfaces, mostly reuse) → land it + Hapatra (same anchor pattern).
3. The One Ring (protection-from-everything is the one genuinely-new keyword).
4. Ulalek `ExcludeSelf` fix (trivial, independent).
