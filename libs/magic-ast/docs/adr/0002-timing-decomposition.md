# Timing is four rules objects over one shared clock primitive

## Status

Accepted (2026-05-29) — implementation pending

## Context

"Timing" in oracle text is not one concept. The Comprehensive Rules slice it into four objects, each in a different section:

- **Legality / permission** — *when may I take this action* (cast, activate). CR 117, 307.1, 601, 602.5; Flash CR 702.8a.
- **Duration** — *how long a continuous effect lasts*. CR 611.2.
- **Delayed triggered ability** — *a one-shot scheduled to fire later*. CR 603.7.
- **Condition / window** — *a state predicate gating a trigger or static* ("as long as", "while"). CR 603.4 and general conditions.

MAST had drifted into conflating these. The clearest symptom: the `Duration` union carried both genuine 611.2 scopes (`UntilEndOfTurnDuration`) and 603.7 delayed triggers (`AtBeginningOfNextEndStepDuration`, `AtBeginningOfNextCleanupStepDuration`, `AtEndOfCombatDuration`). [Kari Zev, Skyship Raider](../../../../tests/magic-ast-tests/Data/HandParsedCards/AER/KariZevSkyshipRaider.json) showed it live — "Sacrifice it at the beginning of the next end step" was encoded as a `sacrifice` effect with `Duration: atBeginningOfNextEndStep`, i.e. a duration impersonating an ability. Separately, the turn-timeline vocabulary (the beginning/end of each step) was encoded twice: as members of the `TriggerEvent` enum (`BeginningOfUpkeep`, `EndOfTurn`, …) and again as `Duration` variants.

Two goals drove the decision: (1) each node must be **honest to the rules object** it represents, and (2) concepts built from the same underlying rules primitive must **share** that primitive (DRY) rather than re-encode it.

## Decision

Introduce **`GameTime`** — a point on the turn timeline (phase/step + edge + this/next + whose turn). It is a value type, not an effect or ability. Exactly three rules objects compose it, because exactly three reference the clock:

- **Time triggers** ("At the beginning of your upkeep") — `TriggerCondition`'s firing point.
- **Delayed triggers** ("…at the beginning of the next end step") — CR 603.7.
- **Duration endpoints** ("until end of turn").

The other two rules objects deliberately **do not** compose `GameTime`, and that asymmetry is the honesty guard:

- **Legality** references a named **timing window / frame** (`Instant | Sorcery | …`), not a clock point. Sorcery speed = main-phase ∧ stack-empty ∧ priority; the distinguishing conjunct (stack empty) is not expressible as a `GameTime`. The card node names the frame; expansion is engine territory.
- **Condition** references game state, not a clock point.

Concrete structural changes:

- **`TriggerEvent` loses its clock members.** They migrate into a `GameTime` arm so `TriggerCondition.Event` becomes a union `event | GameTime`. `TriggerEvent` shrinks to genuine events (zone changes, combat, casts). ~22 fixtures migrate.
- **`DelayedTriggeredAbility` is a new effect-owned node**, created by a `createDelayedTrigger` effect (CR 603.7). It is *not* added to `AbilityKind` (which maps to CR 113.3's printed categories) and reuses `TriggerCondition`, so its firing point is the same `event | GameTime` union — covering both the "at" (clock) and "when/whenever" (event) delayed forms the rule names.
- **`Duration` keeps only 611.2 scopes.** Its delayed-trigger variants are removed (they become delayed triggers). Its endpoint is a union `GameTime | Condition` (e.g. "for as long as this artifact remains tapped"); start is implicit at resolution.

## Considered options

- **Cheap path — bare `At: GameTime` on the delayed trigger, leave `TriggerEvent` untouched.** Rejected: re-introduces the two-representations smell (clock points as enum members *and* as `GameTime`) and cannot model "when/whenever" delayed triggers at all, so the problem recurs within a few card families.
- **Make `DelayedTriggeredAbility` a first-class `AbilityKind`.** Rejected: it would be the only ability kind that can never appear at the top level of a card; it is structurally a sub-object of the effect that creates it.
- **Reuse `TriggeredAbility` with a `Delayed: true` flag.** Rejected: blurs "triggers off a printed event" with "scheduled by a resolving effect."
- **Treat "only as a sorcery" as a `GameTime` window.** Rejected: a `GameTime` cannot carry the stack-empty / priority conjuncts that define sorcery vs instant speed; legality is a frame reference, not a clock point.

## Consequences

- Breaking change to the serialized AST: `TriggerEvent` clock members relocate, the `Duration` union drops its delayed-trigger variants, and a new `createDelayedTrigger` effect + `DelayedTriggeredAbility` node appear. Gold fixtures migrate with it (~22 touch trigger clock members; delayed-trigger fixtures such as Kari Zev and Armor of Thorns re-encode).
- One representation of any clock point — printed trigger, delayed trigger, or duration endpoint all reference the same `GameTime`. The DRY win the decision exists to secure.
- The four-object split is now load-bearing: a reviewer can reject a node that puts a delayed trigger in a `Duration`, a clock point in a frame, or "stack empty" in a `GameTime`.
