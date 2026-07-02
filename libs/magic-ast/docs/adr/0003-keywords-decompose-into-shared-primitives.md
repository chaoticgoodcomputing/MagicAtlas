# Keywords decompose into shared mechanical primitives; identity is retained as a typed label

## Status

Accepted (2026-05-29) — implementation staged across batches

## Context

MAST exists to serve **clustering**: discovering that mechanically-similar cards are similar by giving them similar subtrees. Flying and Menace should collide because both are evasion; Equip and Reconfigure should collide because both attach a permanent to a creature.

A class of keyword abilities was modeled as opaque marker effects carrying only a cost — `EquipEffect`, `CyclingEffect`, `EchoEffect`, `BestowEffect`, `ReconfigureEffect`, `KickerEffect` — each an `Effect` with a `Cost` field, typically wrapped in a `static` ability. This had three faults:

1. **Wrong ability category.** Equip (CR 702.6a) and Cycling (CR 702.29a) are *activated* abilities; Echo (CR 702.30a) is a *triggered* ability. Encoding them as `"Kind": "static"` is descriptively false — the CR 113.3 category is itself a fact about the card, not an execution detail.
2. **Cost in the wrong rules slot.** An activation cost lived in an `Effect.Cost` field rather than `ActivatedAbility.Costs`; an unless-pay cost rather than an `UnlessClause`; an additional casting cost rather than the existing `AdditionalCost` type.
3. **Un-clusterable black boxes.** The AST already has an `AttachEffect` primitive (CR 701.3). Yet `EquipEffect`, `ReconfigureEffect`, and explicit `AttachEffect` were three disjoint subtrees for the same mechanic — `AttachEffect`'s own comment codified the split as intentional. The three things clustering most needs to collide were guaranteed never to.

The pattern was defended by the descriptive-not-engine doctrine. That doctrine was mis-drawn: it was read as "don't decompose," when its real boundary is "don't model engine *execution*."

## Decision

A keyword expands into its **true rules decomposition**:

- the correct `AbilityKind` (Equip/Cycling → activated, Echo → triggered, Kicker/Bestow → static),
- its cost routed to that ability's existing cost slot (`ActivatedAbility.Costs`, `UnlessClause`, `AdditionalCost`/`AlternativeCost`),
- its mechanical action expressed as the **existing shared primitive** (`AttachEffect`, `DrawCardsEffect`, `SacrificeEffect`, …),

so mechanically-similar cards converge on the same subtree. The opaque marker effect types are deleted.

The boundary of the descriptive doctrine is fixed as **mechanical content vs. engine execution**:

- **Mechanical content** — *what the ability does* in shared primitives (attach to a creature, draw a card, sacrifice unless you pay) — **is decomposed**, because shared primitives are the cluster axis. Decomposition depth is arbitrated by the **cluster-value test**: decompose a clause iff doing so creates a cluster axis a consumer would query.
- **Engine execution** — legality checks, zone-change mechanics, continuous-effect application ("while attached, this isn't a creature"), the conditional resolution of a kicked spell — **stays implicit**.

**Keyword identity is retained as a typed label on the decomposed structure, never as a substitute for it.** `KeywordSource` (string today, the `KeywordAbility` enum per ADR 0001 going forward) rides on the correctly-categorized ability. This label is load-bearing in two directions — the **production / reference duality**:

- *Production:* the label lets two decompositions that share a primitive still be recognized as the same keyword family (Equip vs. an ad-hoc attach).
- *Reference:* other cards filter on the keyword as a class — Strong Back's "*Equip abilities* you activate … cost {3} less." If the identity did not survive decomposition, "Equip abilities" would be inexpressible: only anonymous `ActivatedAbility + AttachEffect` nodes would remain. The identity that survives decomposition **must be the same identity a filter matches on**, which is the decisive reason to make it the typed `KeywordAbility` enum rather than a bare string.

Shared structure beats merged structure (the [ADR 0002](0002-timing-decomposition.md) pattern, reapplied): where two distinct rules objects reference the same concept, they share the **value type** and keep their own wrappers. Aura attachability (`EnchantRestrictionEffect.LegalTargets`, CR 702.5 — a continuously-checked static restriction) and Equipment attachment (`AttachEffect.Target` inside the Equip activated ability, CR 702.6a — a target on activation) are distinct wrappers that both compose `ObjectFilter`; clustering ("attaches to a land") is a consumer projection over that shared filter, not a merged node. A vanilla Aura gets **no synthesized `AttachEffect`** — its text only states "Enchant [quality]"; the attachment action is the casting rules (CR 303.4), and inventing a node would fabricate structure the text never says.

## Considered options

- **Keep opaque keyword markers (status quo).** Rejected: miscategorizes the ability, misfiles the cost, and defeats the clustering objective the AST exists for.
- **Decompose but discard keyword identity (pure mechanical form).** Rejected: makes "Equip abilities cost less" and keyword-family clustering inexpressible. The reference half of the duality requires the surviving label.
- **Merge Aura `Enchant` restriction and Equipment attach into one node.** Rejected: they are different rules objects (continuous legality vs. target-on-activation); merging repeats the duration/delayed-trigger conflation ADR 0002 removed. Share `ObjectFilter`, keep wrappers.
- **Synthesize `AttachEffect` for Auras for symmetry.** Rejected: fabricates structure absent from oracle text.
- **Build an `IAttachmentRestriction` trait now so a walker finds attachability uniformly.** Deferred: premature abstraction with no clustering consumer yet demanding it; the shared `ObjectFilter` makes adding it later non-breaking.

## Consequences

- Breaking change: `EquipEffect`, `CyclingEffect`, `EchoEffect`, `BestowEffect`, `ReconfigureEffect`, `KickerEffect` are removed; affected fixtures re-encode to true-kind + primitive + `KeywordSource`. Equip → `ActivatedAbility{ KeywordSource:"Equip", Costs:[…], Effects:[AttachEffect{Target: creature you control}], Restrictions:[Sorcery frame] }`.
- A reviewer may now reject a node that miscategorizes a keyword's ability kind, files a cost in an `Effect`, or hides mechanical content behind an opaque marker.

### Named follow-ups (surfaced by the Strong Back fixture)

1. **`CostReductionEffect` needs an `AppliesTo`.** It is self-only today ("reduces the cost to cast *this spell*") with stringly-typed `BasedOn`/`Condition` (ADR 0001 interior free text). It cannot express "reduce the cost of [a filtered class of *other* abilities/spells]." Strong Back clauses 2–3 are unrepresentable until this lands.
2. **A filter over abilities/spells keyed on `KeywordAbility`** (the reference half of the duality) — "Equip abilities," "Aura spells" — including controller and target predicates.
3. **Extend `ObjectFilter` with the relational axes the rules use** — `AttachedTo` (Strong Back clause 4; KorSpiritdancer currently fakes it with stringly-typed `CountQuantity.CountOf`), and a **player** arm so "Enchant player" (Curses, CR 702.5) has an honest `LegalTargets`.
