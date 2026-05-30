# Parameterless keyword markers collapse into one KeywordAbility-keyed effect

## Status

Accepted (2026-05-29) — implementation staged across batches

## Context

Of ~154 keyword effect records, **71 carry zero payload** beyond the ([ADR 0005](0005-clause-modifiers-are-composition.md)) trait fields — pure markers (`DeathtouchEffect`, `TrampleEffect`, `VigilanceEffect`, `HasteEffect`, `CascadeEffect`, `PersistEffect`, …) whose only information is their type identity. The other 83 carry a real payload (a cost, an N, a quality, a filter).

A keyword's identity is needed in more than one place. The same `Deathtouch` appears as:

- a card's own ability (Fynn, the Fangbearer),
- an ability **granted** to another object (Gift of the Deity: "enchanted creature … has deathtouch"),
- a **characteristic filter** (Fynn: "a creature you control **with deathtouch**").

Modeling markers as 71 distinct empty record types both duplicates the records and fragments that one identity across unrelated types. The `KeywordAbility` enum ([ADR 0001](0001-free-text-is-frontier-only.md)) already exists for exactly this — seeded with `Flying, Reach, Shadow`.

## Decision

A keyword is a **proxy for a canonical, parameterizable AST subtree**. For a parameterless marker, that subtree is just the primitive itself — `KeywordAbilityEffect { Keyword: KeywordAbility }`.

- **Collapse the 71 zero-payload markers** into the single `KeywordAbilityEffect`, growing `KeywordAbility` to cover them. A `DeathtouchEffect {}` and `KeywordAbilityEffect { Keyword: Deathtouch }` carry identical information, so this is zero information loss.
- **`KeywordAbility` becomes the one canonical keyword identity**, referenced wherever a keyword appears: the card's own ability (`KeywordAbilityEffect`), a granted ability (`GainAbilityEffect.GainedAbility` wrapping the subtree), a characteristic filter (`KeywordCharacteristic`), and the keyword-ability filter (`AppliesTo`, [ADR 0003](0003-keywords-decompose-into-shared-primitives.md)). This makes a primitive like deathtouch referenceable *outside* cards that print the keyword — the point of the collapse beyond DRY.
- **Parameterized keywords (83) keep distinct records.** Their payload is real; forcing them into a `KeywordAbilityEffect{Keyword, Cost?, Value?, Filter?}` bag would rebuild the optional-bag smell rejected throughout. The cost-bearing subset (Equip, Cycling, Echo, Bestow, Kicker) instead decomposes per [ADR 0003](0003-keywords-decompose-into-shared-primitives.md).
- **Cluster-value decomposition stays open.** A behavioral marker pair (Persist + Undying → a shared "dies → return with a counter" primitive) may later be decomposed per ADR 0003's cluster-value test. The collapse does not block it: promoting `KeywordAbilityEffect{Persist}` to a `TriggeredAbility` later is the same migration as from `PersistEffect{}`, and strictly better than leaving an empty record that also doesn't cluster.

## Considered options

- **Keep 71 distinct marker records.** Rejected: pure duplication, and it fragments the keyword identity across unrelated types so a grant/filter can't reference "the same thing."
- **One unified `KeywordAbilityEffect` covering parameterized keywords too** (optional `Cost`/`Value`/`Filter`). Rejected: an optional-field bag that permits nonsense combinations and erases keyword-specific rules shape.

## Consequences

- `EffectType` loses 71 string variants; 71 record files are deleted; `KeywordAbility` grows from 3 to ~74 members. Fixture migration is mechanical find-and-replace (`{"EffectType":"vigilance"}` → `{"EffectType":"keywordAbility","Keyword":"Vigilance"}`), no card's meaning changes.
- `Ability.KeywordSource` (string, ~398 fixtures) becomes redundant with `Keyword` on these abilities, making the `KeywordSource → KeywordAbility` migration ([ADR 0001](0001-free-text-is-frontier-only.md) follow-up) reachable.
- The boundary is sharp and reviewable: an effect with *any* payload (evasion, combatDamageTiming, protection, firebending) is untouched; only zero-payload markers fold.
- Staged golds: Akroma, Angel of Wrath (boundary — what folds vs. what stays); Ozai, the Phoenix King (comma-line markers + parameterized firebending + conditional self-grant of flying/indestructible); Fynn, the Fangbearer (own marker + characteristic-filter reference); Gift of the Deity (granted-keyword reference).
