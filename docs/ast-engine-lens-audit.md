# MAST AST — engine-lens structural audit

**Date:** 2026-05-24
**Scope:** every AST family under `libs/magic-ast/AST/`
**Lens:** if we were to build a rules-execution engine from this AST, what would be wrong?

> MAST describes cards; it does not execute them. The proposals below are *descriptive
> AST shapes* meant to capture what oracle text **says**. Engine concerns (priority,
> the stack, replacement-effect interaction order, layering) are explicitly out of
> scope. The engine-builder lens is used as a forcing function for structural
> precision — not as license to import runtime semantics.

## TL;DR

- **`Condition` is a single record holding raw text.** It is the single largest
  descriptive gap in the AST. `Duration` is a polymorphic union with named
  variants; `Condition` should mirror that shape. Intervening-if (603.4),
  static-ability prerequisite ("During your turn"), "as long as", "if you do
  not" and "unless" all collapse into one untyped string today.
- **`ObjectReference` is enum-kind + filter; designated/derived/relational
  references have no home.** `Designated` was added as an escape hatch for
  "your commander" / "the monarch"; same-set-exclusion ("another target") is
  encoded only in the enum name, not as a structural relation; "the player who
  controls this permanent" has no shape at all.
- **`ObjectFilter.Characteristics: List<string>?` is the universal sponge.**
  It currently absorbs (a) genuine current-state predicates we lack ("nontoken",
  "tapped"), (b) lifecycle predicates that already have a structured home
  (`History`), (c) reference designations that should live on
  `ObjectReference` ("your commander", "this Vehicle"), and (d) post-hoc fork
  branches ("who didn't discard a card"). Four problems, one string list.
- **`IOptionalEffect` is asymmetric.** It carries `IfYouDo` but not
  `IfYouDoNot`. The asymmetry forces fork-on-may-choice patterns (Gwen Stacy
  II, "each player who didn't", "if you don't, lose 3 life") into other escape
  hatches. The trait is otherwise well-shaped.
- **`HistoryPredicate` lives buried inside `ObjectFilter`.** It is a
  thing-that-happened-to-an-object, structurally the same kind of node as a
  `TriggerEvent`. It belongs at the same level as them, not as a sub-property
  of a current-state filter.

## Per-family audit

### Effects

**Existing shape.** A `[PolymorphicBase("EffectType")]` discriminated union over
~45 concrete records spanning ten subdirectories (CardFlow, Combat, Control,
Core, Counter, Damage, Keyword, Modification, Replacement, Resource, Timing,
TokenCopy, ZoneChange). Three traits — `IOptionalEffect`, `IDurativeEffect`,
`IPreventableEffect` — are mixed in via interface and surface
`IsOptional`/`IfYouDo`, `Duration?`, `UnlessClause?` on every concrete effect.

**Gaps / smells.**

- **`IOptionalEffect` has no `IfYouDoNot` branch.** Concrete gap surfaced by
  *The Death of Gwen Stacy II* — "Each player may discard a card. Each player
  who doesn't loses 3 life." The fixture encodes "who didn't" as a
  `Characteristics` free-text on the second effect's player filter. The same
  shape recurs broadly: "Draw a card. If you don't, lose 1 life." (Browbeat
  patterns), "Sacrifice a creature. If you don't, this deals 5 damage to you"
  (numerous downside-on-refusal cards). Rule 117.7 governs may/can't semantics;
  Rule 608.2 describes the implicit branch at resolution.
- **Every effect is `IOptionalEffect`, `IDurativeEffect`, `IPreventableEffect`,
  but most cannot meaningfully carry all three.** Trample-with-`UnlessClause`
  is nonsense oracle text. The trait union privileges parser convenience over
  descriptive precision. The traits are doing the job a single
  `EffectEnvelope { May, Duration, Unless, Body }` wrapper could do once.
- **`UnparsedEffect` and `CompositeEffect` are siblings of all the structured
  effects.** Composite is structural; Unparsed is a parse-failure escape hatch.
  Co-locating them in the same union conflates "the AST does not know how to
  describe this" with "the AST describes a sequence". This is minor but bites
  any consumer that wants to traverse only structured nodes.
- **`Duration.AsLongAsDuration { string Condition }`** carries free-text where
  every other `Duration` subtype is structured. "As long as you control a
  Forest" is the canonical case and is already representable as a structured
  predicate ("you control X").
- **`Duration.UntilLeavesBattlefieldDuration { string? Object }`** uses a
  free-text identifier for "[which object] leaves". This should be an
  `ObjectReference`, not a string.

**Proposed structural changes.**

- Lift the three effect traits into a single envelope record:
  ```
  EffectEnvelope { Effect Body; bool IsOptional; Effect? IfYouDo; Effect? IfYouDoNot;
                   Duration? Duration; UnlessClause? Unless; }
  ```
  Then `Effect` becomes a pure description of the action, and the
  fork/duration/preventable wrappers attach where they actually apply.
  Backwards-compatible alternative: add `IfYouDoNot` to `IOptionalEffect` as
  the smaller blast radius patch (Rule 117.7).
- Promote `Duration.AsLongAsDuration.Condition` from `string` to the structured
  `Condition` union proposed in the Conditions section below.
- Promote `UntilLeavesBattlefieldDuration.Object` from `string?` to
  `ObjectReference?`.
- Pull `UnparsedEffect` out of the `Effect` union and into a sibling
  `IParseFailure` marker — Effects that *could not be parsed* are
  structurally different from Effects that *were not yet implemented*.

**Blast radius.** Effect envelope refactor is large — every concrete effect
record changes shape and every fixture serializing `IsOptional`, `Duration`,
`UnlessClause` would re-shape. `IfYouDoNot`-only patch is small (interface +
implementations + Gwen Stacy fixture). `Duration` patches are small (3-4
fixtures reference `AsLongAsDuration`; `UntilLeavesBattlefieldDuration` rarely
used).

### Triggers

**Existing shape.** `TriggeredAbility { Trigger: TriggerCondition, InterveningIf:
Condition?, Effects, Restrictions }`. `TriggerCondition { Timing, Event, Filter }`
with `TriggerTiming` (When/Whenever/At) and `TriggerEvent` (~30-entry enum
spanning zone change, combat, damage, life, spell/ability, phase, state, counter,
card draw, sacrifice, token creation, search/scry/surveil, Other).

**Gaps / smells.**

- **`TriggerEvent` is an enum where each variant is a structural shape with
  parameters.** "Enters the battlefield" is a `ZoneChange { From, To }`;
  "deals combat damage to a player" is `CombatDamage { ToPlayer: true }`;
  "becomes the target of a spell" is `BecomesTarget { ByObject }`. The enum
  flattens three or four axes (event-class, zone, target-type) into a
  Cartesian product (`DealsCombatDamage` vs `DealsCombatDamageToPlayer` vs
  `DamageDealt` vs `NoncombatDamageDealt`). Adding "deals combat damage to a
  creature" would need yet another enum variant.
- **The same Filter slot describes "which object's event are we matching"
  *and* "what the object looks like".** Rule 603.2 distinguishes the
  triggering object from any restriction on it. `Mindlink Mech` fixture
  encodes "this Vehicle" via `Filter.Characteristics: ["this Vehicle"]` —
  exactly the wrong slot. A self-scoped filter is a reference, not a current-
  state predicate.
- **No structured slot for the relational participants in a trigger.**
  "Whenever a creature you control deals combat damage to a player" has
  three distinct roles (source, victim, controller-of-source). Today they're
  collapsed into one `Filter`.
- **`InterveningIf` lives on `TriggeredAbility` as `Condition?` but the
  underlying `Condition` is raw-text.** Rule 603.4 makes intervening-if
  semantically distinct from a static condition; the shape doesn't matter
  until `Condition` itself becomes structured.
- **`HistoryPredicate` is currently a sub-property of `ObjectFilter`.**
  Question 5: it should rise to the trigger level. "Whenever a creature
  dealt damage by Zurgo this turn dies" is a trigger about *Dies* whose
  matching object is restricted by a history predicate; the predicate is
  about the *trigger* (a windowed lookback) more than about the object's
  current characteristics.

**Proposed structural changes.**

- Replace `TriggerEvent` enum with a `[PolymorphicBase("EventType")]` union
  matching `ReplacementEvent`'s shape:
  ```
  TriggerEvent (abstract)
   ├─ ZoneChangeTrigger { Origin: Zone?, Destination: Zone?, Object: ObjectFilter? }
   ├─ CombatTrigger { Phase: AttackPhase, Source/Target: ObjectReference? }
   ├─ DamageTrigger { Source: ObjectReference?, Recipient: ObjectReference?, IsCombat: bool? }
   ├─ PhaseTrigger { Step: TurnStep, ControllerScope: ControllerFilter? }
   ├─ SpellOrAbilityTrigger { Object: ObjectFilter?, Cast/Activated/Triggers }
   ├─ StateTrigger { TargetingTransition: tap/untap/transform/... }
   ├─ CounterTrigger { CounterType, Placed/Removed }
   └─ OtherTrigger { Description }
  ```
  Note structural parity with `ReplacementEvent` (Rule 603 vs Rule 614 are
  the same shape of "thing-that-happened-to-an-object" from a descriptive
  perspective). This is the unification Q3 hints at.
- Rename the existing `Filter` field on the trigger to make its role
  explicit, or eliminate it by absorbing into the per-variant references.
- Promote `HistoryPredicate` from a sub-property of `ObjectFilter` to a
  first-class restriction expressible at the trigger level *and* the filter
  level (it's legitimately useful in both — see References section).

**Blast radius.** Large. `TriggerEvent` is referenced by every triggered
ability fixture (~30 cards). However the migration is mechanical: each enum
variant maps 1:1 to a polymorphic variant. The blast radius is exactly the
cost of structuring an under-modeled axis once.

### References

**Existing shape.** `ObjectReference { Kind: ObjectReferenceKind, Filter:
ObjectFilter? }`. 18-variant enum covering Self, Target, It, You, Opponent,
EachOpponent, EachPlayer, AnyTarget, Another, Each, Controller, Owner,
DefendingPlayer, EnchantedOrEquipped, Chosen, ThatPlayer, EachOtherPlayer,
**Designated**. `ObjectFilter` is a flat record with cardTypes / subtypes /
supertypes / colors / controller / characteristics / zone / power+toughness+MV
comparisons / `HistoryPredicate? History` / sourceSpan.

**Gaps / smells.**

- **`ObjectReferenceKind` enum mixes three orthogonal axes.** A reference
  has a *cardinality* (Self/It/Each/Another/AnyTarget), an *anchor* (target,
  designated, chosen, enchanted/equipped), and a *role* (controller, owner,
  defending player, that player). "Each opponent" is cardinality+role;
  "another target creature" is cardinality+anchor+exclusion; "your commander"
  is anchor+role. The enum forces every combination into a flat name.
- **`Designated` is an escape hatch.** Q2. Today: `ObjectReference { Kind:
  Designated, Filter: { Characteristics: ["your commander"], Zone: CommandZone } }`
  (Road of Return). The free-text characteristic carries the entire designation.
  The actual structural concepts hidden inside `Designated`:
  - **The monarch** (Rule 716) is a *per-game player designation*, not an
    object reference. "The monarch" as a reference is "the player who holds
    that designation".
  - **The commander** (Rule 903) is a *card carrying a designation*, scoped
    to a player. "Your commander" is "the card with the Commander designation
    whose owner is you".
  - **Enchanted/Equipped** (already an enum variant) is structurally the same
    pattern — *the object my Aura/Equipment is attached to*. It is a
    *derived reference*, not a designation.
- **No structural support for derived references.** "The player who controls
  this permanent" appears throughout Auras and saga chapters. Today it would
  collapse into either `Controller` (loses the "of this permanent" link) or
  `Characteristics: ["the player who controls..."]`.
- **"Another target creature" loses its exclusion semantically.** `Another`
  is one enum variant, but the semantic content is "Target, with implicit
  self-exclusion". Rule 115.6 ("another" excludes the source object) is
  structural — the exclusion is an attribute of the target relation, not
  a different kind of reference.
- **`ObjectFilter.Characteristics` is the universal sponge.** Q6. Audit of
  current uses across fixtures:
  - `"nontoken"` — genuine missing structured concept (Rule 109.1
    distinguishes tokens from other objects). Belongs as a typed
    `IsToken: bool?` or a token-status predicate.
  - `"flying"`, `"reach"` — structurally `HasAbility` predicates. Belong
    as `RequiredAbilities: IReadOnlyList<AbilityRef>`.
  - `"tapped"`, `"untapped"`, `"attacking"`, `"blocking"` — these are
    *current-state status* predicates (Rule 110.5). Belong as a typed
    `StatusFlags` set or per-flag bool.
  - `"who didn't discard a card"` — branch-on-prior-choice. Belongs on
    `IOptionalEffect.IfYouDoNot` (see Effects), *not* on a reference filter.
  - `"your commander"`, `"this Vehicle"`, `"this spell"`, `"this card"` —
    designation/identity references. Belong on `ObjectReference` as a
    typed designation, not as a current-state filter.
  - `"from anywhere other than exile"` — a zone-set exclusion. Belongs as
    `ExcludedZones: List<Zone>?` or as a structured `ZoneSet` field.
  - `"nonlegendary"` — a supertype negation. Belongs as a typed
    `ExcludedSupertypes` or as a properly-modelled negation predicate.
    Currently appears in Mindlink Mech as `Supertypes: ["nonlegendary"]`,
    which is also wrong (Legendary is the supertype; "nonlegendary" is its
    negation).
  - `"in any number of target players' graveyards"` — collapsed structure
    from Gwen Stacy III. The actual shape: target N players, scope the
    filter to *their* graveyards. Need a `Owner: ObjectReference` slot on
    `ObjectFilter` (zone-of-whom).

**Proposed structural changes.**

- **Split `ObjectReference` into a richer model.** A first draft:
  ```
  ObjectReference (abstract)
   ├─ SelfReference                       — "this creature", "this spell"
   ├─ NamedReference { Name: string }     — "Amy Pond" (partner-with), "Urza"
   ├─ TargetReference {
   │     Filter: ObjectFilter,
   │     ExcludesSelf: bool,              — captures "another"
   │     IsAnyTarget: bool                — Rule 115.4 "any target"
   │   }
   ├─ DerivedReference {
   │     Anchor: ObjectReference,
   │     Relation: ReferenceRelation      — Controller / Owner / Attached /
   │   }                                    DefendingPlayer / ThatPlayer
   ├─ DesignationReference {
   │     Designation: GameDesignation,    — Commander | Monarch | Initiative |
   │     Scope: ObjectReference?          —   Ringbearer | Day/Night-side
   │   }
   ├─ BackReference {
   │     Antecedent: AntecedentTag        — It | They | Them | That player
   │   }                                  — Rule 113.10
   └─ QuantifiedReference {
        Quantifier: All | Each | EachOther | Another,
        Inner: ObjectReference
      }
  ```
  Notes:
  - Cardinality (`Each`, `Another`) becomes its own axis via
    `QuantifiedReference`, not flattened into the kind name.
  - Designations (Rule 716 monarch, Rule 903 commander, Rule 715 saga lore,
    Rule 720 initiative, Rule 702.157 Ring-bearer) get a typed enum, not
    `Characteristics: [string]`.
  - `EnchantedOrEquipped` becomes `DerivedReference { Anchor: Self, Relation:
    Attached }`, freeing the relation set to grow (Imprinted, Haunted, etc.).
  - `Another` becomes a flag on `TargetReference`, capturing Rule 115.6
    same-set exclusion as a structural attribute of the relation, not a
    different kind.
- **Decompose `ObjectFilter.Characteristics` into typed predicates.**
  Promote each of the current uses to a typed slot:
  ```
  ObjectFilter {
    CardTypes, Subtypes, Supertypes,
    ExcludedSupertypes,                   — captures "nonlegendary"
    Colors, ExcludedColors,                — captures "nonblack"
    Controller: ControllerFilter?,
    Owner: ObjectReference?,               — captures Gwen Stacy III
    IsToken: bool?,                        — captures "nontoken"
    Status: StatusFlags?,                   — tapped/untapped/attacking/blocking
    RequiredAbilities: List<AbilityRef>?,
    ForbiddenAbilities: List<AbilityRef>?,
    Zones: ZoneSet?,                        — replaces single Zone
    ExcludedZones: ZoneSet?,                — captures "anywhere other than exile"
    Power/Toughness/ManaValueComparison,
    History: HistoryPredicate?,            — already present
    Other: string?                          — last-resort, scoped to one slot
  }
  ```
  `Characteristics: List<string>` survives as `Other: string?` — singular,
  bounded, and visibly an escape hatch in glossary/diff review.
- **Lift `HistoryPredicate` to be reusable.** Today it's a sub-property of
  `ObjectFilter`. It should be addressable wherever a windowed lookback
  applies — on `TriggerEvent` ("Whenever a creature that attacked this
  turn..."), on `ObjectFilter`, and inside `Condition` predicates ("if a
  permanent died this turn"). The discriminated union itself is already
  well-shaped; we want broader use sites, not new variants.
- **Extend `HistoryPredicate` with a structured `Timeframe`.** Today
  `Timeframe: string` (e.g. "this turn"). Promote to a small union:
  ```
  Timeframe (abstract)
   ├─ ThisTurn
   ├─ SinceYourLastTurn
   ├─ ThisCombat
   ├─ SinceLastEndStep
   └─ OtherTimeframe { Description }
  ```
  This is the same pattern `Duration` already uses.
- **Add concrete `HistoryPredicate` variants for recurring cases:**
  `EnteredTheBattlefieldThisTurnPredicate` (Rule 603.6d ETB lookback),
  `WasCastThisTurnPredicate` (storm-style ancestry, Rule 702.40),
  `AttackedThisTurnPredicate`, `BlockedThisTurnPredicate`,
  `DiedThisTurnPredicate`. Each is a description of "this object's
  recent lifecycle", structurally identical to the existing
  `DealtDamageByPredicate` and `CrewedPredicate`.

**Blast radius.** Largest of the audit. Roughly every fixture touches
`ObjectReference` and `ObjectFilter`. Recommend a staged migration:
(1) `IsToken: bool?` + `Status` + `Owner: ObjectReference?` extracted from
`Characteristics` first; (2) `DesignationReference` introduced alongside the
existing `Designated` enum, with deprecation; (3) `QuantifiedReference` and
`DerivedReference` last, since they touch the most fixtures.

### Costs

**Existing shape.** `[PolymorphicBase("CostType")]` union over Mana, Tap,
Untap, Sacrifice, Discard, PayLife, Exile, RemoveCounters, TapPermanents,
Composite. Plus `AdditionalCost`, `AlternativeCost`, `CostReduction` as
oracle-text-level records carried on `CardAttribute`s.

**Gaps / smells.**

- **No `PayEnergyCost`, `PayManaFromTreasureCost`-style "non-mana resource"
  costs.** Energy (Rule 106.11), Tickets (Unfinity), experience counters
  (Rule 122.1c) all surface as activated-ability costs in real cards.
  Future work, low priority — but flag it because the existing `Cost`
  union does not telegraph the extension axis.
- **`AdditionalCost.Condition: string?`** — same raw-text-condition smell
  flagged elsewhere (also in `CostReduction.Condition` and
  `CostReductionEffect.Condition`). Three separate `string? Condition`
  fields scattered across the AST.
- **`SacrificeCost.Filter: ObjectFilter`** is correctly structured, but the
  paired `Quantity` mixes literal counts with "X of them" without a
  cardinality discriminator. Real edge case: "Sacrifice three creatures, no
  two of which share a creature type" — currently no shape exists for
  inter-element constraints in a multi-object cost.

**Proposed structural changes.**

- Once `Condition` is structured (see Conditions), retrofit all three
  `string? Condition` sites. Same patch, three benefits.
- Add `PayResourceCost { ResourceKind, Amount }` for energy / experience /
  tickets — only when first real card needs it.

**Blast radius.** Small. The `Condition` retrofit lands automatically with the
`Condition` family change.

### Conditions

**Existing shape.** `Condition { Text: string }`. Used as `InterveningIf?` on
`TriggeredAbility` (Rule 603.4) and `Condition?` on `StaticAbility` (Rule
604), plus `ModeSelectionOverride.Condition` (modal "unless you control..."
patterns). Open TODO in the source: "Add structured condition representation
as parsing matures."

**Gaps / smells.**

- **Three of the most-quoted oracle-text patterns are raw-text strings
  today.** This is Q1, and it is the single biggest descriptive gap.
- **`Condition` has no shape parity with `Duration`.** `Duration` is a clean
  discriminated union; `Condition` is a single record with one string. The
  two concepts are highly similar in oracle text (both modify scope of an
  effect, both attach to many sites).

**Proposed structural changes.**

- Make `Condition` a polymorphic union mirroring `Duration`:
  ```
  Condition (abstract)
   ├─ ComparisonCondition {
   │     Lhs: Quantity,                   — "if you have 7 or more cards in hand"
   │     Op: ComparisonOperator,
   │     Rhs: Quantity
   │   }
   ├─ ControlsCondition {
   │     Player: ObjectReference,         — "if you control a Forest"
   │     Filter: ObjectFilter,
   │     Quantity: Quantity               — at-least-one by default
   │   }
   ├─ LifeTotalCondition {
   │     Player: ObjectReference,
   │     Op: ComparisonOperator,
   │     Value: Quantity                  — "if your life total is 10 or less"
   │   }
   ├─ HasCardsInZoneCondition {
   │     Player: ObjectReference,
   │     Zone: Zone,
   │     Op: ComparisonOperator,
   │     Value: Quantity                  — "if you have no cards in hand"
   │   }
   ├─ PhaseCondition {
   │     Step: TurnStep,                  — "during your turn"
   │     Player: ObjectReference?         —   (Zurgo II)
   │   }
   ├─ HistoryCondition {
   │     Predicate: HistoryPredicate      — reuse the existing union;
   │   }                                    "if a creature died this turn"
   ├─ BooleanCondition {
   │     Op: And | Or | Not,              — composite conditions
   │     Operands: List<Condition>
   │   }
   ├─ ObjectHasCondition {
   │     Object: ObjectReference,         — "if this creature has flying"
   │     Predicate: ObjectFilter          —   (filter-as-predicate)
   │   }
   └─ OtherCondition { Description }     — escape hatch, singular and bounded
  ```
  Rule citations: 603.4 (intervening if), 604 (static), 117.7 (may/if you do),
  502/504/506 (phase/step references for `PhaseCondition`).
- Carry concrete-card examples in the source XML-doc for each variant
  (matching CONTRIBUTING.md's "name a card that proves the shape" rule):
  - `ComparisonCondition` — Death's Shadow (X = your life total)
  - `ControlsCondition` — Llanowar Wastes-style "if you control a Forest"
  - `LifeTotalCondition` — Vito, Thorn of the Dusk Rose
  - `HasCardsInZoneCondition` — Hellbent abilities
  - `PhaseCondition` — Zurgo (during your turn), Bloodthirsty Aerialist
  - `HistoryCondition` — Morbid abilities ("if a creature died this turn")
  - `BooleanCondition` — modal "unless" overrides (Will Kenrith)
  - `ObjectHasCondition` — Mishra-style "if this creature has flying"

**Blast radius.** Medium. `StaticAbility.Condition` and
`TriggeredAbility.InterveningIf` are referenced across most static and
triggered fixtures (Zurgo, Sanctum, Vito, Niambi, Mystic Remora). The
migration is mechanical (each existing `Condition.Text` becomes
`OtherCondition { Description }` initially, then structured per-card as the
parser learns each variant).

### Quantities

**Existing shape.** `[PolymorphicBase("QuantityType")]` union of Literal,
Variable (X/Y/Z), Derived (with `DerivedKind` enum: Power/Toughness/ManaValue/
LifeTotal/CardsInHand/CardsInGraveyard/DamageDealt/LifeGained/LifeLost/Other),
Count (string), UpTo, Calculated (Expression: string, Operation: string,
Rounding: string).

**Gaps / smells.**

- **`CountQuantity.CountOf: string` + `Filter: string?`** — two raw-text
  fields where structure already exists. `CountOf` is consistently "the
  number of X you control / in your graveyard / etc."; the filter is
  an `ObjectFilter`. The structured equivalent is
  `CountQuantity { Filter: ObjectFilter }` — and "you control" / "in your
  graveyard" lives on the filter.
- **`CalculatedQuantity { Expression: string, Operation: string?, Rounding:
  string? }`** is three raw-text fields capturing "half X rounded down",
  "twice that many". The operations and rounding are a small closed set.
- **`DerivedQuantity.Source: string?`** loses the relationship to other
  objects. "Equal to the power of target creature" — the structured shape
  is `DerivedQuantity { DerivedFrom: Power, Source: ObjectReference }`.
- **`DerivedKind.Other`** plus `DerivedQuantity.Source: string` — this is
  where unmodeled derivations land. Examples in fixtures: "number of tokens
  created" (Chatterfang). That specific shape is meta — it references the
  triggering event itself. Probably belongs on `ReplacementModifier` rather
  than `DerivedQuantity`.

**Proposed structural changes.**

- `CountQuantity { Filter: ObjectFilter }` — drop `CountOf` string,
  drop `Filter: string?`.
- `CalculatedQuantity`:
  ```
  CalculatedQuantity {
    Operation: ArithmeticOp,              — Half | Double | Triple | Plus | Minus
    Base: Quantity,
    Rounding: RoundingMode?               — Up | Down | None
  }
  ```
  enum-ify the closed sets.
- `DerivedQuantity.Source: ObjectReference?` (not string).
- For event-meta references like "that many" (Chatterfang), introduce
  `EventReferenceQuantity { EventReference: EventReference }` rather than
  overloading `DerivedKind.Other`. The shape is structurally about the
  triggering event, not about an object characteristic.

**Blast radius.** Small to medium. `CountQuantity` is well-shaped already
once the strings become structured; fixtures referencing it (Chatterfang,
Quirion Dryad) need light touch-ups.

### Durations

**Existing shape.** Clean `[PolymorphicBase("DurationType")]` union:
UntilEndOfTurn, UntilYourNextTurn, AsLongAs (with `string Condition`),
Permanent, UntilLeavesBattlefield (with `string? Object`), UntilEndOfCombat,
AtBeginningOfNextEndStep.

**Gaps / smells.**

- **`AsLongAsDuration.Condition: string`** — raw text. Lands automatically
  with the `Condition` union refactor.
- **`UntilLeavesBattlefieldDuration.Object: string?`** — should be
  `ObjectReference?`.
- **Missing variants the rules clearly support:** `UntilEndOfYourNextTurn`,
  `UntilThisIsCastOrDies`, `ForAsLongAs[X has Counters]`. Add as concrete
  cards land.

**Proposed structural changes.** Already covered under Effects. Otherwise
this is one of the better-shaped families and is the *model* for what
`Condition` should look like.

**Blast radius.** Small.

### Abilities

**Existing shape.** Polymorphic union over `Ability`: Activated, Spell, Static,
Triggered, Modal, Saga, LevelUp, Unparsed. `Ability` base carries
`AbilityWord?`, `KeywordSource?`, `Reminder: Parenthetical?`.

**Gaps / smells.**

- **`ModalAbility.AbilityKind => AbilityKind.Spell;`** — the modal node lies
  about its kind ("Modal modifies another ability type"). Modal is
  structurally a *wrapper* over a set of inner abilities. The fact that the
  enum cannot represent this is a hint that the discriminator is the wrong
  shape for the wrapper. Same problem will surface for any future wrapper
  ability (Splice, Aftermath, Adventure containers).
- **`ModalOption.Ability: Ability`** — recursive `Ability` works, but the
  field name reads as "an entire ability" when really it's an "alternative
  effect bundle". Possibly `Body: Effect` rather than `Ability: Ability`
  for the inner option, matching how `SagaChapter.Body` works.
- **`UnparsedAbility`** is a parse-failure escape sibling to all real
  abilities. Same conflation flagged on `UnparsedEffect`.
- **No structured slot for "ability granted by another ability".** Gain-an-
  ability effects (`GainAbilityEffect.AbilityText: string?`) carry the gained
  ability as a raw string. Rule 113.2 ("a card's text") implies the granted
  ability is itself an `Ability`. Today the granted ability is text. (See
  Rory Williams fixture: `"AbilityText": "suspend"` — that's a keyword being
  passed as a string into a field that *should* hold a structured ability.)
- **`TriggeredAbilityRestriction.Conditional`** enum variant exists but is
  redundant with `InterveningIf` (one duplicates the other's role).

**Proposed structural changes.**

- Drop the `Modal` enum-variant pretense — make `ModalAbility` clearly a
  wrapper:
  ```
  ModalAbility {
    Wraps: AbilityKindHint,                 — Spell | Triggered | Activated
    ModeSelection,
    Modes: List<ModalOption>
  }
  ```
  where `Wraps` tells consumers what enclosing ability kind owns the
  decision. Or, simpler: leave `AbilityKind.Modal` and accept that some
  abilities have a `Modal` envelope, structurally the way `EffectEnvelope`
  is proposed for effects.
- Promote `GainAbilityEffect.AbilityText` to `Ability` (recursive).
  Carry the parser failure as a separate raw fallback only when the
  granted text is unparseable.
- Drop `TriggeredAbilityRestriction.Conditional` — it's covered by
  `InterveningIf` once `Condition` is structured.

**Blast radius.** Medium for `GainAbilityEffect` (touches every "creatures
you control gain X" fixture). Small for the others.

### Replacement events

**Existing shape.** `[PolymorphicBase("EventType")]` over ReplacementEvent
with CounterPlacement, Damage, Death, Destruction, LifeChange, TokenCreation,
Untap, ZoneChange, Generic. `ReplacementEffect { Event, OriginalEventOccurs,
Replacement, Modifier? }`.

**Gaps / smells.**

- **Structural overlap with TriggerEvent.** Q3 raises this. A *triggered
  ability* fires *after* a thing happens; a *replacement effect* fires
  *instead of* a thing happening. They share the same descriptive shape:
  "this is the event in question". The current AST already mirrors them
  (the replacement event union has Death/Damage/ZoneChange; the trigger
  enum has Dies/DamageDealt/Enters). Two unions for the same descriptive
  concept.
- **`DamageEvent.DamageType: string?`** is one of three places "combat vs
  noncombat" is encoded as a string (the others are `TriggerEvent.NoncombatDamageDealt`
  vs `DamageDealt` and `CombatDamageTimingEffect`). Enum-ify.
- **`GenericEvent { Description }`** is the unparsed-escape sibling.
- **`ReplacementModifier`** is structurally relevant for "twice that many"
  patterns. Worth confirming it carries an `EventReference` not just a
  scalar modifier.

**Proposed structural changes.**

- Unify `TriggerEvent` and `ReplacementEvent` into a single `GameEvent`
  discriminated union shared between `TriggeredAbility.Trigger.Event` and
  `ReplacementEffect.Event`. This is the structural payoff of the
  Triggers-section proposal.
- `DamageEvent.DamageType: DamageKind` enum (Combat / Noncombat / Any).

**Blast radius.** Large, but high payoff — collapses two parallel hierarchies
into one. Stage after Triggers refactor.

### Card attributes

**Existing shape.** `[PolymorphicBase("Kind")]` over CardAttribute with
ManaCost, Colors, ColorIdentity, CreatureStats, Loyalty, Defense,
AdditionalCosts, AlternativeCosts, CostReductions, Layout. Plus
`PowerToughnessValue` polymorphic with Fixed/Variable/Derived.

**Gaps / smells.**

- **Well-shaped overall.** This is the cleanest family in the AST and is a
  good model for the "collection of optional shapes hung on a node" pattern.
- **`PowerToughnessValue.Derived` has `DerivedFrom: string?`** — same string
  smell as `DerivedQuantity.Source`. Promote to a structured derivation.
- **No `RulesText` attribute carrying ability-word/flavor-word metadata.**
  Currently `AbilityWord` lives on `Ability`; that's fine. But cards with
  "ability words" not tied to any single ability (rare) have no home.

**Proposed structural changes.** Minimal. Tighten `DerivedPTValue.DerivedFrom`.

**Blast radius.** Small.

## Cross-cutting themes

- **Raw-text escape hatches are scattered across the AST.** `Condition.Text`,
  `AsLongAsDuration.Condition`, `UntilLeavesBattlefieldDuration.Object`,
  `AdditionalCost.Condition`, `CostReduction.Condition`,
  `CostReductionEffect.Condition`, `TimingModificationEffect.Condition`,
  `ExileEffect.ReturnCondition`, `EvasionEffect`-via-`Filter.Characteristics`,
  `ObjectReference { Designated }`-via-`Filter.Characteristics`,
  `ModeSelectionOverride.Condition`, `DerivedQuantity.Source`,
  `CountQuantity.CountOf`+`Filter`, `CalculatedQuantity.Expression`+`Operation`+
  `Rounding`, `DamageEvent.DamageType`, `GainAbilityEffect.AbilityText`,
  `GenericEvent.Description`, `OtherHistoryPredicate.Description`. The
  `Condition`-related ones consolidate to one fix; the
  `Characteristics`-related ones consolidate to the ObjectFilter split; the
  `Description`/`Expression` ones land per-family. Roughly 20 sites, 3
  consolidation paths.
- **Predicate-vs-event overlap.** `TriggerEvent`, `ReplacementEvent`, and
  `HistoryPredicate` are three views of "thing-that-happened-to-an-object":
  forward (waiting for it), instead-of (intercepting it), backward (looking
  back at it). Today these are three independent shapes. Unifying them is
  the largest structural payoff in this audit.
- **Missing target-set semantics.** `Another`/`AnyTarget`/`Each` are
  flattened into the reference-kind enum; structural target-set operators
  (same-set exclusion, set cardinality, "no two of which share X") have no
  shape.
- **Enum-vs-union axis disagreements.** `ObjectReferenceKind` (enum) and
  `TriggerEvent` (enum) are doing a discriminated-union's job; `Duration`,
  `Effect`, `Cost`, `Quantity`, `Ability`, `ReplacementEvent`, `CardAttribute`
  use polymorphic unions. The enum sites are where future axes can't be
  added cleanly.
- **Trait-mixin asymmetry.** `IOptionalEffect` carries half a fork
  (`IfYouDo`, no `IfYouDoNot`); `IDurativeEffect` + `IPreventableEffect` are
  applied uniformly to every concrete effect even where they don't make
  sense. Effect-envelope pattern resolves both.

## Recommended next steps

Ranked smallest-blast-radius first:

1. **Add `IfYouDoNot: Effect?` to `IOptionalEffect`.** One interface change,
   N implementations, one fixture (Gwen Stacy II) re-shapes. Removes the
   most embarrassing `Characteristics` free-text in the test suite.
2. **Promote `Condition` from `record { Text: string }` to a polymorphic
   union mirroring `Duration`.** Start with `OtherCondition { Description }`
   as the catch-all so existing fixtures migrate mechanically. Introduce
   structured variants one card at a time per the MAST TDD loop.
3. **Promote raw-text sub-fields to structured references in-place:**
   `UntilLeavesBattlefieldDuration.Object` → `ObjectReference?`,
   `DerivedQuantity.Source` → `ObjectReference?`,
   `GainAbilityEffect.AbilityText` → `Ability`.
4. **Decompose `ObjectFilter.Characteristics` by typed predicate.** Add
   `IsToken: bool?`, `Status: StatusFlags?`, `Owner: ObjectReference?`,
   `ExcludedZones: List<Zone>?`, `ExcludedSupertypes`. Keep `Other: string?`
   as the visible last-resort. Update fixtures incrementally as the parser
   learns each predicate.
5. **Introduce `DesignationReference` and `DerivedReference` alongside the
   existing `Designated` enum variant.** Deprecate `Designated`-via-
   `Characteristics` once both new shapes carry their current uses.
6. **Lift `HistoryPredicate` to be a sibling concept** addressable on
   triggers, filters, and conditions, with a structured `Timeframe` union
   replacing the current `Timeframe: string`.
7. **Replace `TriggerEvent` enum with a polymorphic union matching
   `ReplacementEvent`'s shape, then unify them into a shared `GameEvent`.**
   Largest blast radius; do last; gives the cleanest descriptive symmetry.
8. **Refactor effect traits into a single `EffectEnvelope` wrapper.**
   Largest single-touch change to the Effects family; defer until 1-7 have
   shown stable usage.

## Out of scope / explicit non-goals

- **Stack-order, layering, and priority semantics.** Several issues in the
  audit (interaction order of replacement effects, intervening-if check
  timing, "as long as" duration scoping) crossed into engine territory. The
  AST should describe *what the card says*; *when it applies* is the
  engine's problem.
- **Target legality / "still legal" tracking.** Rule 608.2b live re-checking
  of targets is not the AST's concern.
- **Cost-payment workflow / mana-pool reasoning.** `IPreventableEffect`
  records *the presence* of an "unless" clause; how a player decides to pay
  is engine territory. Resisted the temptation to model decision points.
- **Multiplayer designations beyond what cards reference.** Captured monarch
  (Rule 716), commander (Rule 903), initiative (Rule 720), Ring-bearer (Rule
  702.157) because cards in the fixture set name them. Skipped team / Two-
  Headed Giant designations since no fixture surfaces them yet.
- **Continuous-effect "characteristic-defining ability" classification (Rule
  604.3).** A real engine partitions abilities by which layer they apply at;
  MAST should not pre-bake that. The AST should describe the effect; the
  consumer classifies.
- **Sub-types-as-creature-types vs sub-types-as-spell-types disambiguation.**
  Already correctly delegated to `TypeLineAST.Subtypes` and the rules-
  defined subtype tables. Not the AST's problem to enforce.
- **Replacement-effect chaining order (Rule 616).** Out of scope. AST
  records the presence of multiple replacement effects on a card; chaining
  is an engine concern.
