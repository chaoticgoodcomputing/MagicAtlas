# Contributing to magic-ast

Magic: The Gathering card text parser and AST.

## Tests

The test harness for this library lives at [`tests/magic-ast-tests/`](../../tests/magic-ast-tests/), not in this directory. Run tests from there.

## Glossary

Terms specific to how MAST treats oracle text it has not (yet) fully structured. See [ADR 0001](docs/adr/0001-free-text-is-frontier-only.md) for the decision behind them.

**Free text**:
A fragment of oracle text retained verbatim in the AST instead of being structured into nodes.
_Avoid_: "raw text" / "passthrough" used loosely; reserve "unparsed" for the `Unparsed*` nodes specifically.

**Frontier free text**:
Free text whose declared *type* announces it is unstructured — a discriminated-union variant a consumer can branch on statically (`UnparsedAbility`, `UnparsedEffect`, `OtherHistoryPredicate`). Legitimate and idiomatic: the parser's honest "I stopped here" marker, the role error nodes play in Roslyn or tree-sitter.

**Interior free text**:
A `string` / `List<string>` field on a node that otherwise presents as structured, where the field's type does *not* reveal the value may be unstructured (`ObjectFilter.Characteristics`, `SpellAbility.Instructions`, `HistoryPredicate.Timeframe`). Debt — "stringly typed" — because a consumer cannot branch on it without re-parsing text the parser already held.

**Type-honesty**:
The rule deciding which free text is acceptable: a slot that may hold unstructured content must have a *type* that says so (a union with a residual arm), regardless of where it sits in the tree. Frontier free text is type-honest; interior free text is not.

**Residual arm**:
The typed `Other` / `Unparsed` variant of a discriminated union that carries the literal phrase when no structured variant matches (e.g. `OtherHistoryPredicate`, `OtherCharacteristic`). The honest home for "not yet structured" — counted and reported, so it stays a deferral rather than becoming a destination.

**Characteristic**:
A constraint on an `ObjectFilter` beyond its structured axes — a discriminated union of `KeywordCharacteristic` (a keyword-ability requirement) and the `OtherCharacteristic` residual. The frontier-honest replacement for the former `Characteristics: List<string>` bag (ADR 0001).

**KeywordAbility**:
The canonical identity of a parameterless Magic keyword ability (CR 702), as an enum — the type-honest alternative to bare keyword strings. Currently seeded for `KeywordCharacteristic`; intended to absorb the other keyword-as-string sites (`AbilityAdder`, `KeywordSource`) over subsequent batches.

## Timing

The rules slice "timing" into four distinct objects, each governed by a different CR section. MAST keeps them apart and lets them share one primitive (`GameTime`) where — and only where — they genuinely reference the same clock. See [ADR 0002](docs/adr/0002-timing-decomposition.md).

**GameTime**:
A point on the turn timeline — a phase/step boundary qualified by edge (beginning/end), relation (this/next), and whose turn. The shared primitive composed by the three rules objects that reference the clock: time-triggers, delayed triggers, and duration endpoints (CR 500-series turn structure).
_Avoid_: "timing" used generically; encoding a `GameTime` for anything that isn't a clock point.

**Timing window** (legality frame):
A named legality class — "instant speed", "sorcery speed" — that a permission references (CR 117/307.1; Flash, CR 702.8a). It is *not* a `GameTime`: sorcery speed bundles non-clock predicates (stack empty, priority) that a clock point cannot hold, and those are the conjuncts that distinguish it from instant speed. Legality references the frame by name; the frame's expansion is engine territory.
_Avoid_: modeling "only as a sorcery" / "as though it had flash" as a `GameTime` or a phase enumeration.

**Delayed triggered ability**:
A triggered ability created by a *resolving effect* rather than printed on the card (CR 603.7) — e.g. "Sacrifice it at the beginning of the next end step." Effect-owned: it lives under the effect that creates it, never appears in `AbilityKind` (which maps to CR 113.3's *printed* categories), and reuses `TriggerCondition` so its firing point is the same `event | GameTime` union a printed trigger uses.
_Avoid_: encoding it as a `Duration` on an effect (the conflation ADR 0002 removes); promoting it to a top-level ability kind.

**Duration**:
The scope of a *continuous effect* (CR 611.2) — how long it lasts. Its endpoint is a union of a `GameTime` ("until end of turn") and a state predicate ("for as long as this artifact remains tapped"); start is implicit at resolution. Distinct from a delayed trigger, which schedules a one-shot at a `GameTime` and is an ability, not a scope.
_Avoid_: putting clock-point *triggers* (`at the beginning of the next …`) in the `Duration` union; conflating "until X" (endpoint) with "while/during X" (condition gating a trigger or static).

## Keywords & clustering

MAST exists to serve **clustering** — mechanically-similar cards should produce similar subtrees. That goal, not raw fidelity, decides how keywords are modelled. See [ADR 0003](docs/adr/0003-keywords-decompose-into-shared-primitives.md).

**Keyword decomposition**:
Expanding a keyword ability into its true rules form — correct `AbilityKind`, cost in the kind-appropriate slot, mechanical action as the existing shared primitive — so it converges on the same subtree as other cards with that mechanic (Equip and Reconfigure both → `ActivatedAbility` + `AttachEffect`). The opaque-marker alternative (an `Effect` carrying only a `Cost`) is rejected: it miscategorises the ability and is un-clusterable.
_Avoid_: a keyword-named `Effect` that stands in for the ability; `"Kind": "static"` on an activated/triggered keyword.

**Mechanical content vs. engine execution**:
The boundary of the descriptive-not-engine doctrine. *Mechanical content* (what the ability does, in shared primitives) is decomposed because it is the cluster axis; *engine execution* (legality checks, zone-change mechanics, continuous-effect application) stays implicit. Decomposition depth is arbitrated by the **cluster-value test**: decompose a clause iff doing so creates a cluster axis a consumer would query.
_Avoid_: citing "descriptive, not engine" to justify *not* decomposing mechanical content.

**Keyword identity** (production/reference duality):
The keyword label (`KeywordSource`; the `KeywordAbility` enum going forward) retained *on* the decomposed structure, never *instead of* it. It is load-bearing twice: in **production** it marks which keyword family a decomposition belongs to; in **reference** it is what other cards filter on ("*Equip abilities* … cost {3} less"). The identity that survives decomposition must be the same identity a filter matches — the reason it must be typed, not a bare string.
_Avoid_: discarding the keyword name after decomposition; matching keyword classes by free-text name.

**Shared primitive**:
A value type that several distinct rules objects compose, rather than each re-encoding it — `GameTime` for the clock ([ADR 0002](docs/adr/0002-timing-decomposition.md)), `ObjectFilter` for object/attachment criteria. Wrappers stay distinct where the rules behaviour differs (Aura `EnchantRestrictionEffect.LegalTargets` vs. Equipment `AttachEffect.Target`); clustering is a consumer projection over the shared primitive, not a merged node.
_Avoid_: merging two rules objects because they share a primitive; fabricating a primitive the oracle text never states (e.g. an `AttachEffect` on a vanilla Aura).

**Keyword-as-proxy** (marker collapse):
A keyword is a proxy for a canonical, parameterizable AST subtree; for a parameterless marker (deathtouch, trample, vigilance…) the subtree is just the primitive `KeywordAbilityEffect { Keyword: KeywordAbility }`. The 71 zero-payload marker effect records collapse into that one node keyed on the `KeywordAbility` enum, which is the single keyword identity reused across the card's own ability, a granted ability (`GainAbilityEffect.GainedAbility`), a characteristic filter (`KeywordCharacteristic`), and a keyword-ability filter (`AppliesTo`). See [ADR 0006](docs/adr/0006-keyword-markers-collapse-to-one-effect.md).
_Avoid_: a distinct empty record per keyword; folding *parameterized* keywords into the same node (their payload is real — keep them distinct, or decompose per [ADR 0003](docs/adr/0003-keywords-decompose-into-shared-primitives.md)).

## The AST/engine line

What MAST records versus what a consumer (a hypothetical rules engine) computes — the recurring test for where a datum belongs.

**Reference, not resolution**:
MAST encodes a reference exactly as the card words it ("cards exiled with Azula", "the exiled cards", "target creature you control"); it does not pre-resolve that reference into the concrete set of game objects it currently denotes. Working out *which* cards are exiled-with-Azula right now — maintaining the CR 406.5 piles, honoring CR 406.6 linkage, knowing a fresh Azula doesn't inherit a dead one's exiles — is engine work.
_Avoid_: threading a synthetic binding/variable between two abilities to "pre-link" them; baking a card instance's tracked state into the AST.

**Topology, not annotation**:
Structural shape already carries meaning the AST must not duplicate as a flag. Whether a permission lives bundled inside one ability's resolution (Transcendent Dragon — atomic, no priority window) or in a separate persistent ability (Azula — a window exists) is encoded by the ability count and nesting alone. Represent the structure as written and the timing consequence is implied; the engine derives it.
_Avoid_: flags for windows/atomicity/ordering that faithful topology already expresses.

**Linked exile** (CR 406.6):
The relationship between an ability that exiles cards and another referring to "the exiled cards" / cards "exiled with [this]". MAST mirrors *however the card establishes the link*: a printed marker (Grolnok's croak counter → `ExileEffect.WithCounters` + a `Counters` filter) or a source reference (Azula's "exiled with Azula" → a plain exile + an `ExiledWith` filter on the permission). The exile ability gains structure only when the card itself prints the marker.
_Avoid_: marking the exile ability when the card doesn't (Azula); conflating marker-tracked (cross-incarnation) with source-linked (instance-only).

## Effect modifiers

How "you may…", "…unless you pay…", and "…until end of turn" attach to effects. They are *clause-level modifiers*, present only where the rules allow — never a universal interface on every effect. See [ADR 0005](docs/adr/0005-clause-modifiers-are-composition.md).

**Clause modifier**:
A decoration oracle text attaches to an effect — optionality ("you may"), an unless-rider ("unless [player] pays"), or a duration ("until end of turn"). Modelled so the field exists *iff the clause does*, and defined *once*, not re-declared per effect type.
_Avoid_: a trait interface implemented by every effect (it both lies about capability and forces the field into every record).

**One-shot vs. continuous** (the modifier-legality test):
The CR 608 / CR 611 dichotomy decides which modifiers an effect may carry. A **one-shot** effect (draw, destroy, sacrifice) is performed on resolution — it may be optional and may carry an unless-rider, but never a duration. A **continuous** effect (modify P/T, grant ability, evasion) persists — it may carry a duration, but is never optional and takes no unless-rider.
_Avoid_: a durative one-shot ("draw a card until end of turn") or a preventable static ("flying unless you pay").

**Effect modifier mechanisms**:
`OptionalEffect { Inner, IfYouDo?, IfYouDoNot? }` (wrapper *presence* is the "you may"; no bool) and `PreventableEffect { Inner, Unless }` are composition wrappers applied to one-shot effects. `Duration` is a property of an abstract `ContinuousEffect` base that only continuous effects extend.
_Avoid_: one decorator carrying all modifiers (rebuilds the bag); putting `Duration` on an interface (re-imports the per-record duplication).

## Conditions

A game-state predicate — "you control a Forest", "seven or more cards in your graveyard", "enchanted creature is black". One discriminated union (`Condition`), never a string. See [ADR 0007](docs/adr/0007-conditions-are-one-union.md).

**Condition**:
A predicate over game state, modelled as a discriminated union (`CountCondition`, `HistoryCondition`, … + an `OtherCondition` residual) that *composes the existing primitives* — `ObjectFilter`, `Comparison`, `HistoryPredicate` — rather than introducing new ones. The single home for every "if …", "as long as …", "unless …"-style predicate, replacing the former stringly `Condition { Text }` record and the bare `string? Condition` fields. Encoded as written; the engine evaluates it ([reference, not resolution](#the-astengine-line)).
_Avoid_: a stringly condition field; a new condition primitive (conditions are a *consumer* of `ObjectFilter`/`Comparison`/`HistoryPredicate`); building every arm up front instead of seeding `CountCondition` + `OtherCondition` and growing worst-first.

## Conventions

**Prefer structure; defer honestly.** When oracle text resists structuring:

- Route it to an existing structured field if one fits. Most `ObjectFilter` concepts (card types, colors, zone, comparisons, controller) already have first-class homes — do not re-encode them as free-text characteristics.
- If no structured form exists yet, emit a **residual arm** (`Other*` / `Unparsed*`), never a bare-string interior field.
- A new card family's gold fixture MAY use a residual arm, but MUST NOT add a new bare-string interior field when a structured form is reachable.

**Drive debt down worst-first.** Open, multi-domain string-bags (e.g. `ObjectFilter.Characteristics`) outrank bounded single slots (e.g. `HistoryPredicate.Timeframe`). The residual report (see [ADR 0001](docs/adr/0001-free-text-is-frontier-only.md)) surfaces which cards still carry interior free text.
