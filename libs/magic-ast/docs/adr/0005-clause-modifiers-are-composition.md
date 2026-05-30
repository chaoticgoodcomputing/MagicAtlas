# Clause modifiers are composition, not universal effect interfaces

## Status

Accepted (2026-05-29) — implementation staged across batches

## Context

Three trait interfaces — `IOptionalEffect` (`IsOptional` + `IfYouDo`/`IfYouDoNot`), `IPreventableEffect` (`UnlessClause`), `IDurativeEffect` (`Duration`) — were implemented by essentially every effect node: **227 / 226 / 225** implementers respectively, **224 implementing all three**. That is a blanket mixin, and it fails twice:

- **Honesty.** Every effect advertises that it can be optional, carry an "unless [player] pays" rider, and carry a duration. Most can't: `CombatDamageTimingEffect : IPreventableEffect` implies "first strike unless you pay"; `EvasionEffect`/`IndestructibleEffect : IOptionalEffect` implies "you may have flying." The type lies about the node's shape, so `is IPreventableEffect` means nothing.
- **DRY.** A C# interface property must be re-declared in every implementing record. So `IsOptional`, `IfYouDo`, `IfYouDoNot`, `Duration`, `UnlessClause` are hand-copied into ~224 record files — the interface mechanism *is* the duplication.

The traits are **clause-level modifiers** oracle text attaches to an effect, not intrinsic properties of every effect type.

## Decision

Modifier legality follows the **CR 608 (one-shot effect) vs CR 611 (continuous effect)** dichotomy, which is the reviewable test for which modifiers an effect may carry:

- **One-shot action effects** (draw, destroy, sacrifice, deal damage, create token) — performed on resolution. May be **optional** and may carry an **unless-rider**. Cannot carry a duration.
- **Continuous effects** (modify P/T, grant ability, evasion, timing modification) — persist (CR 611). May carry a **duration**. Cannot be optional or carry an unless-rider.

Mechanisms:

- **Optional and unless become composition wrappers**, present iff the oracle text has the clause:
  - `OptionalEffect { Inner: Effect, IfYouDo?: Effect, IfYouDoNot?: Effect }` — wrapper *presence* is the "you may"; there is no `IsOptional` bool. `IfYouDo`/`IfYouDoNot` ride here because "if you do" is meaningless without a "you may" (CR 117.7).
  - `PreventableEffect { Inner: Effect, Unless: UnlessClause }` — the "… unless [player] pays …" rider.
  - Two wrappers, not one decorator: "you may; if you do" and "unless you pay" are distinct clause shapes that almost never co-occur, and typed wrappers let a consumer query `is PreventableEffect` directly. A single decorator with four optional fields just rebuilds a smaller bag that permits incoherent combinations (an `IfYouDo` with no "may").
- **Duration becomes a property of a continuous-effect base**, not an interface: `abstract ContinuousEffect : Effect { Duration?: Duration }`, extended only by continuous effects. The field is defined once (DRY) and exists only where a continuous effect can have a duration (honesty).
- The three trait interfaces are removed.

## Considered options

- **Keep the interfaces, restrict implementers.** Rejected: an interface still forces each implementing record to re-declare the field — the duplication is unfixed.
- **One decorator carrying all modifiers.** Rejected: rebuilds the bag at smaller scale and permits incoherent field combinations.
- **A shared base record carrying all five fields.** Rejected: still universal, so honesty is unfixed (one-shots would still expose `Duration`, continuous effects `UnlessClause`).

## Consequences

- Large but mechanical migration: ~224 effect records lose the five-field block. `OptionalEffect`/`PreventableEffect` wrappers and a `ContinuousEffect` base are added. Fixtures re-encode — optional/unless effects gain a wrapper layer; durative continuous effects keep the field but re-parent onto the base.
- The serializer must handle a two-level `Effect → ContinuousEffect → concrete` polymorphic hierarchy. Fallback if it can't: a narrowed `IDurativeEffect` implemented only by continuous effects (accepting the smaller, honest-subset duplication).
- The 608/611 family is now a reviewable test: a node may be rejected for carrying a modifier its family can't bear (a durative one-shot, a preventable static).
- Wrappers nest only in the rare optional-and-unless co-occurrence.
- Staged golds: an optional-with-fork, an unless-rider (Mana Leak), and a durative continuous grant.
