# Port labels are a deterministic ontology projected from the AST

## Status

Proposed (2026-06-03). Refines [ADR 0001](0001-the-interaction-line.md) §2 (the port is the labelling unit) and §3–4 (the grammar). **Grilled, vocabulary-panelled, and ADR-panelled.** A three-seat grilling fixed the §3 vocabulary (`etb`→`replace`, `owned`→`controlled`, `dies` as a destination qualifier). A subsequent three-seat ADR review (rules / design-soundness / adversarial) returned *spine sound, two structural revisions required before code* — both now folded: the accounting model gained a **firability axis** and the vocabulary gained a **resource-kind axis with positional binding** (see §8, §3). Motivated by the first union-graph judge run (`docs/judgments/interaction-novel-loops-2026-06-03.md`). Precursors landed on `feat/mast-improvements`; no projection engine yet.

## Context

[ADR 0001](0001-the-interaction-line.md) fixed the **port** — an addressable ability sub-tree in a role — as the unit that interacts, but left the label scheme open. The first cut hand-coded one recognizer per role (`DeathTrigger`→`death-payoff`, `CreateToken`/`TokenReplacement`→`token-doubler`, `SacrificeCost`→`sac-outlet`). Materialising the union graph surfaced five forces:

- **Coarse labels manufacture false edges.** `death-payoff` collapsed "when **this** creature dies" with "whenever **another** creature dies" — 644 false-GREEN cycles. `token-doubler` collapsed an emitter (Chatterfang) with a pure multiplier (Doubling Season). The label was less precise than the interaction it named.
- **Hand-coded recognizers don't scale or lint.** One C# recognizer per role covers a handful of shapes, collides as a hot file, and hand-picks its string — so labels drift and nothing checks a label against the card's behaviour.
- **The AST is already typed.** A port's role is a typed sub-tree — `Trigger{Event,Filter}`, `Cost{CostType,Filter}`, `Effect{EffectType,…}`. The deciding facets are structured fields, not prose; a label can be derived, not chosen.
- **A label is a query, and fidelity is bounded by the parse.** `ltb:creature:to-graveyard:controlled` is a mast-query pattern over that sub-tree. But "this creature dies" and "a creature dies" parse identically today, so an AST-derived label is only as precise as the AST.
- **A topological loop is not a combo.** Three further things gate a real interaction the edge layer can't see: whether a consumed resource is **refunded** in the cycle (quantity, not type); whether the loop can actually **fire** each iteration (boolean gates, rate limits — not resources at all); and whether the objects a loop **creates** carry the abilities it depends on. None is on the cards in play.

## Decision

### 1. A port label is a deterministic projection of its AST sub-tree — projection-primary

`PortLabel(node)` is a **pure, total function**: per node-kind it projects a fixed facet sequence through fixed vocabulary maps, joined with colons. Same sub-tree → same label, always; no heuristics, no hand-authored strings. It is **per port** (a multi-effect ability → one labelled port per effect) and a **build output** (content-hashed), never git source. The label **falls out of the AST** — every card has a definitive label map; the query view (§2) is this projection read backwards, not a competing scheme.

### 2. The label and the query are one fact in two directions

The colon-path is a **hierarchy**, which reconciles "labels fall out of the AST" with "labels are queries": a port carries **one canonical leaf** (its most-specific projection) and **satisfies every prefix** on its root-to-leaf path. `query(L) = { node : L is a prefix of PortLabel(node) }` — the projection's preimage. **Projection is primary, query derived**: the collision-lint (§10) needs one canonical label per node to collide on; total coverage needs a total function; retiring recognizers needs automatic labelling — none survives a curated-query model. `mast-query` is the backward half (`Matches(L, node) ≡ PortLabel(node).startsWith(L)`); we build the projection and reuse matching.

The query operator generalises prefix-match to **glob wildcards** (landed 2026-06-04, `PortLabel.Matches`): `*` matches exactly one facet segment, `**` matches zero or more, so a bare prefix `L` is the special case `L:**`. This is what makes **variable-arity families** collapse cleanly — labels are not fixed-arity (a token-emit's subject is `creature:squirrel` or just `artifact`), so a positional pattern like `emit:token:*:*:controlled` would miss the subtype-less form; `emit:token:**:controlled` covers both. The loop-viz legend groups nodes by their wildcard **family** (the 60+ distinct `emit` labels fold to a handful — `emit:token:**:controlled`, `emit:mana:**`, …), and the **label-grammar figure** (`known-families.json`) is authored directly in these wildcard patterns (`sac:**` → `ltb:**:to-graveyard:**` → `replace:**`), replacing the earlier phantom family names that named no real label. A `PortGraphEngineTest` guards that every grammar family is a pattern real loop ports match, so the figure can't drift back out of sync with the projection.

### 3. The colon-ontology — two axes: an action/object axis and a resource axis

A label is a most-significant-first colon-path over a controlled, **soft (hierarchical)** vocabulary. It carries **two distinct dimensions**, because a card-type taxonomy alone cannot name the resources combos trade in (the panel's second FATAL):

**(a) Action + object** — `role : subject : [destination] : [scope] : [exclusion]`:

| Facet | Tokens |
|---|---|
| **role** | triggers `ltb` `etb` `cast` `attacks` · costs `sac` `pay` `tap` · effects `emit` `destroy` `draw` `counter` `modify` · replacements **`replace`** (CR 614) |
| **subject** (the *object* a port acts on) | `creature` `artifact` `permanent` `land` … (+ subtype via the type-ontology, e.g. `creature:squirrel`) |
| **destination** (`ltb` qualifier) | `:to-graveyard` (= *dies*, CR 700.4) · `:to-hand` · `:to-exile` · `:to-library` · bare = any-zone |
| **scope** | `self ⊆ controlled ⊆ any` · `opponent ⊆ any` · `owned` = ownership only (orthogonal, CR 108.3) · `:another` = exclude-self |

**(b) Resource** — what *flows*, lifted from the existing `ResourceKind` enum ([Interaction.cs:9](../../Interaction.cs#L9)) as a first-class axis, because mana/counters/life are not card types: `mana:<color>`, `token:<object>`, `counter:<kind>`, `life`, `loyalty`, plus the event-resources (`death`/`etb`/`ltb`/`sacrifice`/`cast`). **Positional resources carry their bearer**: a `+1/+1` counter on a specific permanent is `counter:plus-one-plus-one@<bearer-filter>`, *not* a fungible pool — fixing `Resource.Subject = null` ([Interaction.cs:26](../../Interaction.cs#L26)), the root of the "counter on Ballista ≡ counter anywhere" collision.

**(c) Colon vs parameter.** A facet is a **colon segment** iff its value-space is a controlled vocabulary that participates in prefix-subsumption (dropping it broadens the query correctly) — role, subject (card-type:subtype, via the ontology), scope, destination, resource-kind, mana color. A facet whose value-space is **arbitrary** — an open-ended object reference or a linked variable — rides *outside* the prefix-path as a **parameter** (`@…`), so it never participates in or pollutes prefix-matching: a counter's **bearer** is `@<bearer-filter>` for a *permanent* counter, and is **absent** for a *player* resource (energy, poison, experience — `counter:energy` has no host). The choice is decided by the **facet's value-space, not the instance value**: `self` is `:self` as a *scope* (a controlled enum) yet `@self` as a *bearer* (an arbitrary-object facet whose value here happens to be the source). A controlled-vocabulary **set** within one segment uses `+` (`mana:blue+red`, `artifact+creature`). **Multiplicity is never a facet** — it is the §8 quantity (`{B}{B}` → `pay:mana:black` quantity 2).

**Deferred (named extension):** *designation* and *turn-history* resources — monarch, the initiative (CR 725/730), storm count — are **out of scope for v1**. They are not consume/emit resources that net; they are held flags that can be stolen, or per-turn tallies with no card-local AST. They need a designation/turn-state model orthogonal to the flow model, not a bolt-on facet.

"Soft" means a facet is appended only when a distinction begins to matter, and shorter labels stay valid prefixes (`ltb:creature:to-graveyard ⊆ ltb:creature`). The facet vocabularies and projection order are the **curated, judge-reviewed ontology**.

### 4. An ability decomposes into consume and emit ports

A port is no longer a whole ability with `Emits`/`Consumes` sets. An ability decomposes into single-role ports — **consume** (trigger/cost) and **emit** (effect) — each carrying a label **and a quantity** (§8), joined by a card-defined edge (§5). **Modal abilities** (`ModalEffect`) project **one port per mode**, tagged with a **mutual-exclusion group** so the accounting (§8) reads them as "at most one fires," never as simultaneous emits. (A *cost-disjunction* — "pay {3} **or** sacrifice a creature" — has no upstream AST node today; `CompositeCost` is AND-only. That is a parser-bounded gap, handled like a modal cost once an `OrCost` node lands upstream.)

The decomposition is **total over structural roles** (§1): a port is projected for *every* cost, trigger, effect, and static — **not gated on resource flow**. A `{0}` cost is a `pay:mana` port of quantity 0, an inert keyword (`evasion:forestwalk`) is an edge-sparse port; both exist because the interaction surface is *structural* (cost-modification, activation/keyword observation), independent of flow magnitude. What is sparse is the **edges**, not the ports — an edge-less port is a harmless isolated node awaiting an interacting card. (This retires the precursor projector's flow-gate, which skipped any ability with no emit/consume/intercept.)

### 5. Edges carry a provenance: card-defined vs rules-defined

Both are first-class directional edges the cycle-finder traverses, differing by **who authors the connection** — orthogonal to ADR 0001 §4's flow/modifier *mechanism* axis:

- **Card-defined** — the ability's own causality *within one ability* (trigger/cost → its effect). **Certain by construction** on the *certainty* axis. It still carries a **quantity** ("create three Golems" = 3), so "card-defined hops are free" means free of operator-tiering, **not** free in the accounting (§8).
- **Rules-defined** — a flow between ports mediated by the game's mechanics (one card's emit → another's consume); the edge the **operator tiers**. "Inter-port" *includes cross-ability edges of the same card* (Chatterfang's emitted Squirrel feeding its own sac).

Provenance is well-defined **only relative to a fixed ability-decomposition**: re-decomposing one ability into two (the multi-effect-per-clause question) reclassifies a card-defined edge as rules-defined and can change a cycle's certainty floor. The decomposition is therefore part of the ontology contract, not a free choice downstream.

### 6. The flow grammar derives; modifier and bridge edges stay curated

Flow edges (`emit:R → consume:R`) **derive** from colon-matching, then the operator prunes — no per-family hand-authoring. The discipline is an **over-approximation invariant, stated as a checkable construction constraint**: *every facet the projection emits must refine an operator axis, and the projection must never emit a facet finer than the operator can decide.* Only then is "the colon-match proposes a superset of what the operator accepts, so the operator only ever prunes" *guaranteed* rather than hoped. **Known violation:** `:self`/`:another` is finer than the operator currently decides — `Intersects` deliberately exempts `ExcludeSelf` from its Unknown-floor ([ObjectFilterRelations.cs:502](../../../magic-ast/AST/References/ObjectFilterRelations.cs#L502)) — so until **both** the parser carries `ObjectReference{Kind:Self}` **and** the operator gates that axis, the constraint is violated and the engine emits false GREEN (the judge's 644). The fix is parser **and** operator, not parser alone.

What stays **curated**: **modifier edges** and **rules-bridges** (a game mechanic with no parsed emit). Modifier edges come in a dual pair: a *doubler/replacement* intercepts another card's **emit** port (Chatterfang on every token creator), and a *cost-modifier* intercepts another card's **cost** port (CR 118.9 "abilities cost `{1}` more" raises a `pay:mana` port's quantity — including a `{0}` port from 0 to 1, which is why §4 projects the zero-magnitude cost port in the first place). The canonical bridge `sac:creature → ltb:creature:to-graveyard` (CR 701.21 → 700.4) is **over-approximate by design**: a CR-614 graveyard-replacement (Rest in Peace: "if a card would be put into a graveyard… exile it instead") means a sacrificed creature can leave to exile and *not die* — the bridge over-proposes, and the operator/board prunes. The prose states it as over-approximate, not as "sacrifice ⟹ dies always."

### 7. The label names; the operator decides

The label is a **lossy** projection — it summarises, it does not decide edges. The join stays with the `ObjectFilter` `Intersects`/`Subsumes` operator ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)): `owned ⊄ controlled`, `controlled ⊆ any`, `creature:squirrel ⊆ creature`, the certainty tier, and (closed 2026-06-04) the self-death Amber downgrade. The string groups; the operator rules. This is the type-side guarantee behind §6's over-approximation: coarse propose, precise refine.

### 8. Cycle validity is four axes: existence × certainty × balance × firability

A topological cycle is a **real infinite combo** only when all four hold — and the accounting layer (this section) owns the last two:

1. **Existence** — every hop's edge holds (operator, §7).
2. **Certainty** — every rules-defined hop is GREEN, not Amber/Red (§5, §7).
3. **Balance** — `net(R) = Σ emit(R) − Σ consume(R) ≥ 0` for every resource. Quantities project from the AST (constant / variable `X` / calculated `"that many"`), kept **out of the label** (type vs magnitude). `net(R) ≥ 0` is **necessary, not sufficient** — it is "resource-balanced," not "self-sustaining." Each hop's quantity is read **through the operator's Reliability tier**: an Amber edge's quantity is unproven (the shared object may never materialise), so an Amber hop floors the *balance* verdict to Amber — balance never out-certifies the edge it rides on.
4. **Firability** — no hop is gated by an unsatisfiable boolean (`TriggeredAbility.InterveningIf`, a target-legality filter) or bound by a rate limit (`TriggeredAbilityRestriction.OnlyOnceEachTurn`, `TriggerCondition.PerTurn`). These are *not resources* and `net(R)` is blind to them: a resource-neutral once-per-turn loop (or a Kiki-Jiki loop needing a legal nonlegendary target) nets zero yet is not infinite. **Any cycle touching such a gate or limit floors to Amber** until the firability is discharged.

`net(R) > 0` on a resource is the loop's **payoff** (its product). A *finite engine* (some `net(R) < 0`) is a real, honestly-bounded synergy, not a combo. Symbolic quantities (`X`, `"that many"`) floor to Amber-balance pending a symbolic pass — overgenerate-then-tighten, as with the operator's `Unknown`.

**Multi-cost conjunction landed (2026-06-04) — a firability refinement (axis 4).** An activated ability is paid as one action (CR 602.2 → 601.2h — *all* its costs together), but a cycle closes through a *single* cost port. So a loop traversing one cost port of an ability is firable only if the ability's **other** cost ports are each satisfiable every iteration — already in the loop, fed by a producer, or free (a tap, whose only limit is the untap rate-limit already covered above). `PortGraphEngine` derives co-costs from the card-defined edges (within an ability every cost drives every effect, §5 — so two costs sharing an effect are siblings) and floors `PortCycle` to Amber (`CoCostsSatisfied = false`) when a resource co-cost has no feeder. This is what lets Chatterfang × Pitiless **certify**: the squirrel loop traverses `sac:creature:squirrel`, whose co-cost `pay:mana:black` is fed by the very Treasure the loop produces (§9) — so the conjunction holds; strip the mana source and the same loop floors to Amber. It lives in the engine's cycle model (`PortCycle.Tier`, beside the balance/firability floors). **The corpus viz now renders that verdict (2026-06-04):** a C# `MaterializeCycles` step runs `PortGraphEngine.FindCycles` (bounded ≤5 hops — a sac→death→token→doubler→refuel loop is five hops, ~3s at corpus scale) and exports each loop with its cycle-level tier, so the right subplot colours edges by the *cycle verdict* (firability + conjunction floored), not the per-edge tier the prior Python re-derivation showed. The Ashnod's Altar × Pitiless × Chatterfang archetype reconstructs **GREEN-certified** (a free sac outlet, all hops Green, firable, co-costs satisfied); the 2-card Chatterfang-`{B}` variant is Amber (the Squirrel ⊄ creature bridge straddle). *Remaining:* the conjunction's "fed" is presence-of-a-producer (necessary); the stronger "fed by the loop itself each iteration" (sufficient for infinite, vs once) is the next tightening, deferred until more combos are in hand to avoid over-fitting this one.

### 9. Token-emits resolve to token objects with their own ports

`emit:token:<subject>` is not a leaf — it instantiates an **object that projects ports** (the mana that refunds a `{B}` lives on the *Treasure*, not on the card that made it). A token-emit is a **handoff**: resolve the subject to a token AST, project *its* ports, splice them in. Sources:

- **predefined** (Treasure, Food, Clue…) — a vendored registry parsed once from CR 111.10 (same derive-don't-rehost posture as the rules).
- **custom** — the inline `createToken.Token` sub-tree.
- **copy** (`IsCopy:true`) — the copied object's ports.
- **vanilla** (stats only) — no *intrinsic* ports; but it can still gain ports **extrinsically** (an anthem — Mondrak, "Squirrels you control have …" via `GainAbilityEffect`). Extrinsic grants are resolved by the *same rules-defined-edge pass* against grant effects whose target filter the token matches — they are **not** on the token's own sub-tree, so §9's intrinsic sources alone would miss the whole aristocrats-anthem archetype.

Resolution is a **walk over a visited-set of resolution keys**, terminating by the same discipline as `InteractionEngine.FindCycles`; a cycle in the token-creation graph (token A makes token B makes token A) resolves to under-classification, never a hang. Delayed triggers (`CreateDelayedTriggerEffect`) are the same mechanism — an effect instantiating a deferred ability sub-tree — and resolve identically. It stays projection-primary (the same `PortLabel` on the resolved AST; only the resolution lookup is new). Omitting it does not create false positives — it **under-classifies** (a refund-through-token infinite reads as finite).

**Landed — predefined source (2026-06-04, commits `570550a3` + the registry generalization).** `PortWalk.ResolvePredefinedTokens` resolves each created predefined token via the `PredefinedTokens` registry (CR 111.10, transformative-derived: Treasure/Gold/Powerstone/Clue/Food/Blood/Map) to its intrinsic activated ability — the self-sacrifice (plus tap / generic-mana / discard) costs driving the resource it emits (Treasure → `emit:mana:any`), attributed to the creator (CR 111.2). The engine's token-flow arm wires the created token into its own self-sac, and a new **colour-aware** mana arm wires the emit to a cost: `emit:mana:any → pay:mana:black` is GREEN by producer choice (§3b†), `emit:mana:green ⊄ pay:mana:black` (no false-GREEN). This closes the Chatterfang × Pitiless **mana sub-loop** — the Treasure now feeds Chatterfang's `{B}`, which previously had *no* feeder and sat in zero cycles, so the combo's mana half was invisible. Corpus after regen: 824 `emit:mana:any`, 53 `emit:drawcards`, 51 `emit:gainlife`. **Remaining:** the custom / copy / extrinsic-anthem sources; and the §8 conjunction that an ability's *multiple* cost ports aren't yet joined — a loop closes through one cost port without requiring the others be fed (the squirrel loop doesn't yet *require* its `{B}` feeder to certify, it only now *has* it).

### 10. A label collision is an AST-precision lint — when the sub-trees differ

Because the label is deterministic, two sub-trees that project the **same** label *and whose canonical serialized sub-trees differ* expose an AST that conflates them — a ranked queue of parser-precision targets (weighted by false edges manufactured). The differ-clause is load-bearing: two cards that are genuinely synonymous (Llanowar Elves ≡ Elvish Mystic → `tap … emit:mana:green`) project the same label from the *same* sub-tree — that is **correct generalization** ([ADR 0001](0001-the-interaction-line.md)'s whole value-add), not a bug. The lint fires only on *same-label / different-sub-tree*. The **interaction-judge enforces** it: a label asserting a role the text does not support is a FAIL — which is how the panels caught `etb`-for-a-replacement and `owned`-for-control.

### 11. The ontology is the feedforward

The colon-vocabulary and projection rules live in `libs/mast-interaction/CONTRIBUTING.md` (terminology section, per the workspace convention) — the soft guide an `interaction-tdd-loop` worker reads before adding a facet, as the parser's `CONTRIBUTING.md` guides parser workers.

## Worked example: Chatterfang × Pitiless

Chatterfang's `Oracle.Abilities` projects six ports:

| AST node | Port | side |
|---|---|---|
| static `evasion` (Forestwalk) | `evasion:forestwalk` | inert |
| static `replacement` `Event:tokenCreation` | `replace:token-creation:controlled`‡ | consume *(intercept)* |
| `Replacement:createToken{1/1 G Squirrel}` | `emit:token:creature:squirrel` | emit |
| activated `Costs[0]:{mana B}` | `pay:mana:black` | consume |
| activated `Costs[1]:{sacrifice X Squirrels}` | `sac:creature:squirrel:controlled` | consume |
| activated `Effects[0]:{modifyPT +X/-X}` | `modify:pt` | inert\* |

Card-defined edges: `replace:token-creation:controlled → emit:token:creature:squirrel`; `{pay:mana:black, sac:creature:squirrel:controlled} → modify:pt`. The loop closes through rules-defined edges and a resolved Treasure token (§9), and accounts to zero (§8):

| Resource | consume | emit | net |
|---|---|---|---|
| Squirrels | sac `X=1` | replacement `that many` = 1 | 0 |
| black mana | `{B}` = 1 | Treasure `emit:mana:any` = 1 | 0† |
| Treasure | mana-refund sacs 1 | Pitiless `emit:token:treasure` = 1 | 0 |
| payoff | — | `modify:pt` −1/−1, repeatable | +1/cycle |

Pitiless's real text is "Whenever **another** creature you control dies…" → `ltb:creature:to-graveyard:controlled:another` (the `:another` is correct; [the Interaction fixture](../../../../tests/magic-ast-tests/Fixtures/Interactions/cards/PitilessPlunderer.json) has corrupted oracle text dropping "another" — a **data bug to fix**, caught by this review). The sac of the Treasure is an *artifact* death, so it correctly does **not** re-trigger Pitiless's `ltb:creature:…` — visible only because the token's sac is an accounted port.

\* `modify:pt` is inert except a lethal `-X` toughness (death via SBA, CR 704), conditional on `X ≥ toughness` → the operator floors that bridge to Amber.
† `emit:mana:any → pay:mana:black` is GREEN by **producer choice** (a Treasure makes any one color; its controller picks black), *not* by `any ⊆ black` (false at the type level). This requires the operator to model choosable-any mana as satisfying a specific demand; absent that, the hop is Amber, not the clean net-0 shown. The shorthand `any ⊇ black` in the prior draft laundered an Amber — the honest reading is "GREEN under producer choice, pending operator support."

‡ The S1 projector found this label is **unscoped today** — `replace:token-creation`. Chatterfang's parsed `Event` is `{EventType:tokenCreation, MinimumQuantity:1}` with no controller, so "under your control" is dropped; the `:controlled` scope rides along once a parser fix carries the event controller (a second parser-precision target, after `ltb:…:self`). The unscoped label over-approximates safely (§6) — the projection surfacing the gap rather than asserting a scope the AST does not hold.

## Considered options

- **`net(R) ≥ 0` as the sufficiency test for infinite.** Rejected (panel FATAL): necessary, not sufficient — blind to boolean gates and rate limits the AST already carries, so it certifies gated/once-per-turn loops as infinite. Firability is a distinct fourth axis (§8).
- **Card-type `subject` as the only resource naming.** Rejected (panel FATAL): cannot name mana/counter/life/loyalty, and drops a counter's bearer (`Subject = null`). A resource-kind axis with positional binding is required (§3).
- **Query-primary labels** (curated patterns; a node has whatever matches). Rejected: no canonical label → no collision-lint; coverage gaps return; it is the recognizer model in a declarative coat.
- **Quantity in the label.** Rejected: fragments the label space and shatters prefix-match for a magnitude that belongs in accounting (§8).
- **Tokens as opaque resources.** Rejected: the refund lives on the token; opacity under-classifies refund-through-token infinites (§9).
- **`dies` as a sibling role of `ltb`.** Rejected by panel: CR 700.4 makes *dies* a destination-special-case of LTB (603.6c); a sibling breaks the subsumption the operator relies on.
- **`owned` for "you control"** / **`etb:*` for a token doubler.** Rejected by panel (both FAIL): control ≠ ownership (CR 108.3/108.4); a CR-614 replacement is not an ETB trigger (CR 614.6 — the entry never happens).
- **Pure-emergent grammar / keep-curated-only.** Rejected for the middle (§6): derive flow, curate modifier + bridges.
- **Designation/turn-state in v1.** Rejected: monarch/initiative/storm need a second modelling paradigm; deferred as a named extension (§3).
- **Opaque/hashed labels, label-drives-edges, one-port-per-ability, rigid enum** — rejected as in the prior draft.

## Consequences

- **`PortProjector` becomes a generic AST-walk + `PortLabel` projection**, retiring the hand-coded recognizers; the port model becomes single-role consume/emit ports (each with a quantity + any mode-group) joined by card-defined edges.
- **The matcher must move from string-equality to prefix-containment.** `InteractionEngine.Materialize` currently groups by exact label (`GroupBy(p => p.Label, Ordinal)`), which contradicts §2's soft-prefix subsumption — a family edge on `ltb:creature` would miss `ltb:creature:to-graveyard:controlled`. Prefix-match (or operator-driven grouping) is the mechanism that makes the hierarchy do work.
- **The engine gains two accounting layers** — cycle balance + firability (§8) — and **token resolution** (§9), the latter needing a **vendored CR-111.10 predefined-token registry** and reuse of `FindCycles` for termination.
- **Two upstream/operator changes are prerequisites, not parser-only:** the `ObjectFilter` operator must gate `ExcludeSelf` (close the [ObjectFilterRelations.cs:502](../../../magic-ast/AST/References/ObjectFilterRelations.cs#L502) exemption) alongside the `ObjectReference{Kind:Self}` parser binding; and `Resource.Subject = null` ([Interaction.cs:26](../../Interaction.cs#L26)) must carry a positional resource's bearer.
- **The flow grammar largely derives**; `known-families.json` shrinks to modifier edges + rules-bridges.
- **Existing labels migrate**: `sac-outlet`→`sac:<type>:…`, `death-payoff`→`ltb:<subject>:to-graveyard:<scope>`, `token-doubler`→`replace:token-creation` (+ emit port). The reconstruction golds, the grammar, the viz palette, and the corrupted Pitiless Interaction fixture update.
- **Validity is a conjunction** (existence ∧ certainty ∧ balance ∧ firability); the cycle tier is the worst across all four, and a cycle is reported as infinite / finite-engine / payoff only after firability and balance are discharged.
- **Label collisions become triage**: the first is `ltb:…:self` vs `ltb:…:controlled`, blocked on the parser+operator self-binding fix. **The S3b corpus run + a 3-seat MAST judge panel (2026-06-04) quantified the live impact: ~144 self-death loops** (Doomed Dissenter, Brindle Shoat, Triplicate Titan, Phyrexian Triniform — all "When this creature dies, create…") reconstruct as **false-GREEN** because the parser collapses "this creature" to `ltb:creature:to-graveyard`, so a cross-card sac bridges to them. They are the dominant corpus false-positive and are prunable **only** by the upstream self-binding (parser + the operator's `ExcludeSelf` gate) — *not* by firability, balance, or the §10 lint, because the dropped "this" makes the self-death and any-death sub-trees byte-identical (no collision to flag). The panel affirmed the projection/operator/provenance/firability architecture sound (2 PROCEED / 1 HALT, the HALT being this scope-completeness point — the upstream prerequisite — not an architectural error). **The self-binding *mechanism* has since landed (2026-06-04):** `ObjectFilter.IsSelf` (the dual of `ExcludeSelf`), the operator gating it in `Subsumes` (a self-only sup is not subsumed by a non-self sub → No, so a cross-card sac→self-death bridge falls to Amber, with conformance fixtures), and `PortLabel` projecting `…:self`. **The parser activation has since landed (2026-06-04, commits `ce6534be` + `ad9c8412`):** `TriggeredRuleHelpers.ParseObjectFilter` now marks "this [type]" → `IsSelf`, *self-only* (guarded against the "this X or another X" disjunction = any-X, e.g. Blood Artist — verified on Basri's Lieutenant). The ~154 desynced self-event golds were migrated via a 16-batch Sonnet mast-tdd-loop workflow + a CR-soundness judge (PASS), then six **keyword** self-death paths that bypass `ParseObjectFilter` (Persist/Undying/Modular/Afterlife/Soulshift/Blitz, CR "this permanent") were self-bound directly with their 12 golds + 4 expansion fixtures. Suite green at each step (3180). **The S3b corpus viz now reflects it**: the self-death triggers project `ltb:creature:to-graveyard:self` (1344 such labels), and **every** cross-card sac→self-death bridge is Amber (1342 edges, 0 false-GREEN) — a net 598 edges flipped GREEN→Amber. The named cases (Doomed Dissenter, Brindle Shoat, Triplicate Titan, Phyrexian Triniform) each carry `:self` with all 48 inbound bridges Amber. *Residual (out of scope, noted):* keyword self-triggers on **non-death** events (Melee→attacks, Evoke→enters) still bypass self-binding — not a death-bridge, so no loop-viz false-GREEN.
- **Deferred, named:** designation/turn-state resources (monarch/initiative/storm); cost-disjunction (`OrCost`) is an upstream MAST gap. Sketching Vivi Ornitier + Liberty Prime surfaced four more bounded extensions: **subject negation** (`cast:noncreature`, from `ExcludedCardTypes`), **OR-of-triggers** ("attacks or blocks" → two consume ports), **pay-unless mode-groups** ("sacrifice it unless you pay {E}{E}" → `pay:counter:energy` XOR `sac:self`, extending §4's modal group to pay-or-suffer), and **dynamic/linked quantities** (`X = power`, board-state-dependent → §8 symbolic-Amber, never certified infinite).
- **Fidelity stays bounded by the AST**: the projection surfaces parse gaps as collisions (§10), missing token abilities as under-classification (§9), and gated/rate-limited firability as Amber (§8) — rather than papering over any of them.
