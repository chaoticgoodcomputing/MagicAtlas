# Conditions are one discriminated union over shared primitives

## Status

Accepted (2026-05-29) — implementation staged across batches

## Context

A *condition* — a predicate over game state — had **three** representations in the AST:

- `Condition { Text: string }`, a stringly record: `TriggeredAbility.InterveningIf` (17 fixtures), `StaticAbility.Condition` (19), `ModalAbility.Condition`, `DrawCardsEffect.Condition`.
- bare `string? Condition`, a stringly field: `AsLongAsDuration` (17), `CostReductionEffect`, `TimingModificationEffect`, `AdditionalCost` (×2).
- `EvasionCondition`, an already-structured but narrow type: `EvasionEffect.UnblockableCondition`, `CantAttackEffect`.

The two stringly forms are the **largest remaining interior-free-text surface** under [ADR 0001](0001-free-text-is-frontier-only.md). And the corpus shows conditions are highly regular, not free-form prose — "you control a Giant", "two or fewer other lands", "seven or more cards in your graveyard", "you attacked this turn", "enchanted creature is black". Every one composes a primitive that already exists: `ObjectFilter` (controller/zone/types), `Comparison` (`{Operator, Value}`), `HistoryPredicate` (itself a union with an `OtherHistoryPredicate` residual).

## Decision

`Condition` becomes one discriminated union (`PolymorphicBase("ConditionType")`, the same pattern as `Effect`/`Cost`/`Duration`), composing the existing primitives, with a residual arm. All eight stringly sites unify onto it; the bare-string fields become `Condition`-typed.

Seeded **worst-first** ([ADR 0001](0001-free-text-is-frontier-only.md)) rather than building every arm up front:

- `CountCondition { Filter: ObjectFilter, Count: Comparison }` — the dominant pattern ("you control N+ X", "7+ cards in your graveyard", "2 or fewer lands").
- `OtherCondition { Text: string } : IResidual` — the frontier-honest catch-all, so every site is type-honest immediately.
- `HistoryCondition { Predicate: HistoryPredicate }`, an object-state arm ("enchanted creature is black"), and `CompoundCondition { Op, Conditions[] }` are added as the card families that need them land; the residual report drives the order.

Conditions encode the predicate **as written**; the engine evaluates it against game state ([ADR 0004](0004-ast-engine-line.md) reference-not-resolution) — a `CountCondition` is "you control a Forest", never a pre-resolved boolean.

`EvasionCondition` is already structured and type-honest, so it stays out of scope — flagged for later consolidation into this union, not part of this debt.

## Considered options

- **Keep the stringly forms.** Rejected: the largest ADR 0001 debt, and three representations of one concept.
- **Build all condition arms up front.** Rejected: speculative; ADR 0001 mandates a residual arm now and structured arms grown worst-first.
- **Introduce a new condition primitive.** Rejected: a condition is a *consumer* of `ObjectFilter`/`Comparison`/`HistoryPredicate`, not a new primitive — no new value type is needed.

## Consequences

- `Condition { Text }` and the four bare `string? Condition` fields all become the `Condition` union; ~50+ fixtures re-encode. Migration of the dominant arm is mechanical; the rest land in `OtherCondition` and trend down via the residual report.
- Object-state conditions already staged ("enchanted creature is black/green" on Gift of the Deity, "six or more unspent mana" on Ozai) land in `OtherCondition` until the object-state / resource arm is built — counted, not hidden.
- `ModalAbility.Condition` is `required`, so it becomes a non-null union member (`CountCondition` or `OtherCondition`).
- Staged golds (in-place migrations): Anurid Murkdiver (`AsLongAsDuration` bare-string → `CountCondition` graveyard threshold), Wizard's Lightning (`StaticAbility.Condition { Text }` → `CountCondition` control-a-filter).
