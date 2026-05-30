# The AST/engine line: reference not resolution, topology not annotation

## Status

Accepted (2026-05-29) — implementation staged across batches

## Context

MAST describes cards; it does not execute them. That doctrine ([memory: "MAST describes, does not execute"]) was directionally right but operationally fuzzy — it kept failing to settle concrete cases. The exile zone (CR 406) forced the issue. A card like **Azula, Cunning Usurper** has one ability that exiles cards and a *separate* ability that refers to "cards exiled with Azula" (a CR 406.6 linked ability). Two tempting models appeared:

1. Thread a **variable/binding** from the exile ability to the permission ability, tagging each exiled card so the permission can find "its" cards.
2. Give the permission a **filter** that names the set by reference ("cards exiled with [this source]"), leaving the exile ability untouched.

The contrast with **Transcendent Dragon** — whose counter-exile-then-cast is *one* bundled triggered ability, with no priority window — showed that option 1 actively destroys information: a binding couples Azula's two abilities and erases the window that exists *because* they are separate.

## Decision

The descriptive doctrine is given two operational rules:

**Reference, not resolution.** MAST encodes a reference exactly as the card words it ("cards exiled with Azula", "the exiled cards", "target creature you control"). It does not pre-resolve the reference into the concrete set of objects it denotes at any moment. Maintaining the CR 406.5 piles, honoring CR 406.6 linkage, knowing a fresh Azula does not inherit a dead one's exiles — all of that is the engine evaluating the reference against game state.

**Topology, not annotation.** Structural shape carries meaning the AST must not duplicate as a flag. A permission bundled inside one ability's resolution (Transcendent Dragon — atomic, no window) versus exposed as a separate persistent ability (Azula — a window exists) is encoded by ability count and nesting alone. Represent the structure faithfully and the timing consequence is implied; the engine derives it.

The reusable test: **if a datum exists only because an implementer would need it to track state, it is engine; if it is written in the card's words, it is AST.**

Corollaries fixed by this ADR:

- **Linked exile (CR 406.6)** is mirrored *however the card establishes the link*: a printed marker (Grolnok's croak counter → `ExileEffect.WithCounters` + a `Counters` filter) or a source reference (Azula's "exiled with Azula" → a plain exile + an `ExiledWith` filter on the permission). The exile ability gains structure only when the card prints a marker.
- **Play-from-exile** is a family distinguished by *where the permission lives*, which is also *what timing it has*:
  - bundled one-shot, immediate, no window → `CastWithoutPayingEffect` inside the resolving ability (Transcendent Dragon);
  - persistent static permission, window → a new `MayPlayFromExile` effect on a static ability (Azula, Grolnok);
  - duration-bounded → `ImpulseEffect` / `MayPlayFromExile` + `Duration` (impulse, Roku).
- An exiled card's **fate** is always modeled by composed follow-up nodes (sibling effect, delayed trigger, linked return, or `MayPlayFromExile`), never by a string on the exile. `ExileEffect.ReturnCondition` (stringly, unused) is deleted.

## Considered options

- **Variable-threading (option 1).** Rejected: reconstructs the engine's pile bookkeeping, invents a binding the text never states, and couples abilities whose separateness is mechanically load-bearing (it would erase Azula's priority window).
- **Window/atomicity flags.** Rejected: topology already encodes them; a flag is a second source of truth that can disagree with the structure.
- **One unified play-from-exile node (merge `ImpulseEffect`).** Rejected: impulsive draw is a cohesive one-shot worth one node; the cluster axis "cards playable from exile" is recovered by consumer projection over `{ImpulseEffect, MayPlayFromExile}`, the same call made for attachment in [ADR 0003](0003-keywords-decompose-into-shared-primitives.md).

## Consequences

- New `MayPlayFromExile` effect (static-scoped permission): `Cards` (an exile-zone `ObjectFilter` + linkage), `Actions` (`PlayLands`/`CastSpells`), optional `WhoseTurn`/`AsThoughFlash`/`ManaSpend` riders.
- `ObjectFilter` grows reference-valued relational axes — `ExiledWith` (source reference), `Counters` (marker predicate) — alongside `AttachedTo` ([ADR 0003](0003-keywords-decompose-into-shared-primitives.md)). Negation axes (`IsToken`, `ExcludedCardTypes`) follow as needed.
- `ExileEffect.ReturnCondition` removed (zero fixtures use it); `ExileEffect.Duration` audited for the same reason.
- The line is now a reviewable test: a node may be rejected for pre-resolving a reference into a set, or for annotating a window/order that its topology already encodes.
- Staged golds: `AzulaCunningUsurper` (separate static permission), `TranscendentDragon` (bundled one-shot) — the pair isolates topology as the variable.
