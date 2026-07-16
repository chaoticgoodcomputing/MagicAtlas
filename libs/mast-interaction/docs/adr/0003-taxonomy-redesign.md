# Taxonomy redesign — an Event/State/Behavior kind system with typed-feature-structure ports

## Status

**Proposed (2026-07-16).** Refines [ADR 0001](0001-the-interaction-line.md) (ports and the edge families
survive intact) and supersedes [ADR 0002](0002-port-labels-are-deterministic-ast-projections.md) **§3**
(the single ordered colon-chain) and **§6's** bridge-primacy; ADR 0002's foundational principles —
deterministic projection, label-as-query, totality, the §8 accounting axes, the collision-lint — all
carry forward. **Grilled twice:** an interaction-judge pass returned the sacrifice event hierarchy
CR-correct (6/6 PASS, PROCEED, with two conditions folded into §5/§6); an independent fresh-context
design review verified the draft against the code and returned ADJUST with five amendments, all adopted
(§6, §7, §9, Migration). Grounded on **two worked golds** — Chatterfang × Pitiless Plunderer (GREEN) and
Deadeye Navigator × Peregrine Drake (AMBER) — which between them exercise every decision below (Appendix).

**Not yet implemented.** The projection, engine, and fixtures still reflect ADR 0002 until the Migration
stages complete; nothing ships from this document before its Stage gates pass.

## Context — why the ontology reopened

This ADR governs layers 2–3 (port projection, interactions) of the end-to-end system topology — see
[docs/design/system-topology.md](../../../../docs/design/system-topology.md) for the full pipeline
(sources → parse → ports → interactions → Flowthru/seed → API → website), the accretion loop's place
beside it, and the shared anchor-by-hand/generalize-by-structure/gate-by-judges architecture the parse
and interaction layers have in common.

ADR 0002 fixed labels as deterministic AST projections over a soft (hierarchical) colon-vocabulary, with
the query as the projection read backwards. It held well for union-graph reconstruction. Two pressures
reopened it:

1. **The frontend is a taxonomy oracle.** The card explorer's "what feeds X / what X feeds" columns are a
   *direct* rendering of the emit↔consume relation. Where the union graph tolerated a coarse family edge
   (a combo either reconstructs or it doesn't), the explorer surfaces every family adjacency as a
   user-visible claim — a coarse `token→cast` combo-ring edge renders as "Chatterfang feeds Aang," which
   is plainly wrong. The frontend made taxonomy imprecision *legible* in a way the batch pipeline never did.
2. **A single worked case (Chatterfang's sacrifice) cracked open the event model.** Deciding how "Sacrifice
   X Squirrels" should render forced the question of whether a sacrifice is a *consume* (`sac:` cost + a
   curated `sac→ltb` bridge) or an *emit of an event* in a subsumption hierarchy — and the answer
   generalized far past sacrifice.

The through-line: ADR 0002 names ports by **mechanism/role**; this redesign names them by the
**event/state/behavior that flows**, so that subsumption — not curation — carries most cross-resource
matching. The deepest finding (design review, confirmed in code): **the AST already carries a
typed-feature-structure system** (`ObjectFilter` = attribute set; `ObjectFilterRelations` + `TypeOntology`
= per-facet lattice matching with trilean open-world semantics); the colon-*string* was the lossy step —
structure was serialized to text too early, and everything since has been fighting the strings.

## Decision

### 1. The kind system: Event / State / Behavior

The taxonomy root is a three-kind system (FRP lineage — Elliott & Hudak):
- **EVENT** — discrete occurrences with payloads: zone-changes (the primitive, `from→to`), cast,
  damage-dealt, dice-rolled, combat-presence, life-change. Events are what triggers subscribe to.
- **STATE** — pools/stores folded over event streams (`state = fold(events)`): mana, life, cards,
  counters, and per-permanent **availability** (tap/untap).
- **BEHAVIOR** — continuous time-varying values: modifications, statics, keyword grants, evasion,
  "as long as" effects.

The ability layer maps onto it exactly: an **activated ability is a function** (costs = arguments, pull);
a **triggered ability is a subscription** (an event filter, push); a **static ability is a behavior**; a
**replacement is middleware** on the event stream — CR 614.5's applies-once is the standard interceptor
invariant, so the no-self-bootstrap property is a theorem of the pattern, not a house rule.
**Push vs pull consumption is first-class**: argument-consumes (costs; §8 conjunction) and
subscription-consumes (triggers; any-match) are distinct kinds, which the engine already treats
differently and the model now names. Control-flow wrappers (`conditional`/`optional`/`composite`/
`rollResultsTable`/`becomesPermanent`) are **combinators** — the §8 gating layer, not root members.

### 2. Port shape: an is-a stem plus an unordered attribute set

A port is `side : stem [attribute-set]`:
- The **stem** is reserved for genuine is-a descent — `<side>:<supergroup>:<card-type>` (e.g.
  `emit:removal:creature`), plus keyword chains where is-a truly holds
  (`modification:evasion:forestwalk`).
- **Everything else is an unordered attribute set on the leaf** — `subtype`, `control`, `from`/`to` zone,
  `manner`, `p/t`, `color`, `token`, `qty`, `duration`, … orthogonal axes, never colon-nested. Each stem
  **licenses** a declared key set (TFS appropriateness; `mana[toughness=1]` is ill-typed).
- **Derived categories are named attribute-constraints**, never stem nodes: `fodder := [p/t=1/1]` (or
  `[toughness=1]`), each pinned to exactly one query. Slang is first-class exactly when AST-derivable.
- **One oracle clause may project several ports** (a port is a query over the AST); dual
  consume+emit projections are the norm for costs that raise events (§5).

### 3. Matching: per-facet lattice subsumption with implicit subtree capture

`captures(Q, E)` holds iff Q's stem is an ancestor-or-equal of E's stem in the is-a lattice, **and** every
attribute constraint in Q subsumes E's value in that attribute's own value lattice (`control: self ⊆
controlled ⊆ any`; zones; the TypeOntology for types/subtypes). Unconstrained attributes capture
everything; absent attributes are **open-world unknowns** resolved by the operator trilean
(Subsumes/Intersects/Overlaps), never boolean failures. Querying `removal:creature` therefore captures its
entire subtree — every deeper stem and every attribute combination — by construction; ADR 0002's
positional `**` glob is retired. **Matching operates on the structured port; the colon-label becomes a
generated serialization/display format** (determinism and the collision-lint transfer to
(stem, attribute-set) identity).

### 4. Supergroups are slang-anchored views; the root is complete

**Removal / Deployment / Modification** are named filters over the event stream, anchored on the
battlefield/stack endpoint of the zone-change primitive (Removal = it's the FROM; Deployment = it's the
TO; both carry explicit `from`/`to` — graveyard hate, discard, and mill are Removal with non-play
endpoints). Resources (Mana/Cards/Life/Counters) are STATE stores; Structure (phases, extra
turns/combats, availability) is the clock. Every canonical family and every `PortWalkProjection`
discriminator has a placement: dice-rolled and combat-presence are Events; tap/untap is the availability
store; a counter is a store whose P/T effect is a derived Behavior; a copy is a deployment event with
provenance. Damage is a Life-store verb whose *lethality* is a derived edge into Removal.

### 5. Events vs objects; dual ports; the sacrifice remodel

Objects (creatures, permanents, tokens) **flow**; events **happen to them** — and a cost that removes an
object raises an event. A sacrifice cost projects **both**:
`consume: creature[subtype=…, control=you, qty=…]` (the fodder — retained, because the §8
balance/conjunction reads off it; the event's subject is a type descriptor, not a pool decrement) **and**
`emit: removal:creature[from=battlefield, to=graveyard, manner=sacrificed, …]`. The event sits at the
narrowest rung of **sacrifice ⊂ dies ⊂ leaves-the-battlefield** (CR 701.21a · 700.4 · 603.6/603.10a),
expressed by facets — `to=graveyard` = dies, bare = LTB, `manner=sacrificed` = sacrifice — so all three
consumer rungs (LTB-, dies-, and "when sacrificed"-triggers) match by subsumption and **the curated
sac→dies bridge is retired**. Parser asks: the `manner` attribute and a `Sacrificed` trigger event.

### 6. The residual layer — subsumption replaces the arms' structural half only

Generic `captures(Q,E)` absorbs the *structural* content of the engine's per-arm switch (blink/cast/
reanimate unify as `deployment → deployment-consume`), but three knowledge classes cannot live in a
filter lattice and are **declared residual rules**:
1. **Attribute polarity** — per-attribute, fixed vs **producer-chooses**; choice values match
   existentially (`emit:mana[color=any•choice]` pays `[color=black]` → GREEN).
2. **Match policy per consume kind** — argument-consumes demand **cover** (the supplied object must
   provably satisfy the parameter: sac, recast; CR 400.7 token-can't-be-self refusals live here);
   subscription-consumes accept **intersect**, with the operator tiering.
3. **Identity guards** — the same-card/`:self` witnesses (self-watching triggers, self-blink, spell
   self-copy/recast). Implementations stay in code; every guard is registered with its witnessing golds.
Plus **4. irreducible cross-resource bridges** — edges no lattice relates (untap→mana, AMBER ceiling,
CR 107.4) — which remain curated, tier-ceilinged, and cited.
Standing acceptance tests: the Chatterfang×Pitiless mana hop stays GREEN; the four same-card false-loop
classes stay dead; Deadeye×Peregrine's E1/E2 stay AMBER.

### 7. Provenance and certainty

Every projected attribute instance carries **asserted vs derived (over-approximated)** provenance. A
derived attribute (`to=graveyard` under CR-614 redirects; the subtype→type lift) may gate feasibility but
**caps Reliability at Unknown** — over-approximation stays principled: the projection over-proposes, the
operator/board prunes. The **null-Subject default-GREEN is dead**: a port with no subject filter has an
*unknown* subject — the edge may be proposed, but it floors AMBER unless a confirmed rule (§8) or a
same-card witness certifies it. Nothing uncited is GREEN.

### 8. The schema is a derived rollup — schema-by-accretion

There is no centrally-authored rule file. **Per-gold interaction fixtures are the only hand-authored
artifact**: each derivation run (one combo → one gold, subagent-parallel) records its edges with their
certifying mechanisms, declares any *new* rules with itself as witness, and asserts its own acceptance
tests. **The central fixture is a generated, content-hashed rollup** unioning all declared rules —
polarity table, match policies, guard registry, bridges — every entry carrying `witnesses:`. The loop:
**(a)** read the rollup (reuse known rules, never re-derive) → **(b)** derive the gold, declare only the
new → **(c)** regenerate the rollup. Conflicts **fail the build** (judge resolves; the resolution is
itself an evidence-cited entry). Rules climb a **promotion ladder** — `observed` (1 witness) →
`corroborated` (N) → `confirmed` (rules-judge) — and the ladder feeds tiering: an edge certified only by
an observed rule caps at AMBER; only confirmed rules participate in GREEN. The rollup is the runtime rule
source for novel pairs, and a rule-miss is the discovery-prioritization signal for which combo to derive
next. Bootstrap: the two worked golds.

**The rollup's artifacts.** One generation pass over (scaffold + golds) builds a single internal model
and emits four content-hashed files; the lean pair is a **projection** (`strip(provenance)`) of the
verbose pair, so they cannot drift:
1. **`port-topology.json`** — the port universe: stems (is-a parent + Event/State/Behavior kind), the
   licensed-attribute matrix, value lattices, derived-category aliases, slang views. Every entry carries
   `status: declared | witnessed` — declared-only entries come from the Stage-0a scaffold, so this file
   doubles as the scaffold-vs-reality diff the topology sweeps consume ("which declared ports have no
   witness; which witnessed ports were never predicted").
2. **`port-interactions.json`** — the generalized connection rules for novel pairs: the residual layer
   (polarity, match policies, guards-by-name, bridges + tier ceilings) with each rule's promotion status
   (`observed`/`corroborated`/`confirmed` — the tier cap). Subsumption itself is not listed — it is
   derivable from artifact 1's lattices. This is also the artifact the frontend's explorer columns
   ultimately consume (exact port-to-port matching, retiring coarse family adjacency).
3. **`port-topology.cited.json` / `port-interactions.cited.json`** — the verbose twins: identical
   entries + provenance (`witnesses: [gold-id#edge]`, judge verdicts, CR citations, conflict-resolution
   records). The diagnostics surface — consumed by atlas-diag ("why does this edge exist?" → witness →
   verdict in one trace) and by the doc generator.
Entry IDs are stable across regeneration (name-derived, never positional) so citations, judge records,
and diagnostics survive rebuilds; a non-empty conflict set fails the generation pass.

### 9. Tractability guardrail

Matching stays in the polynomial **EL profile**: conjunctive attribute-subset + per-value lattices; no
disjunction and no general negation in the query language. The **closed negation vocabulary** is exactly
the filter model's atomic, per-axis, decider-confined family: `ExcludedCardTypes` · `ExcludedSubtypes` ·
`ExcludedSupertypes` · `ExcludedColors` · `IsColorless` · `ComparisonOperator.NotEqual` (deciders return
Unknown when present on either side) · `:another`/exclude-self (handled by the §6 guard layer, not the
lattice). At corpus scale, candidate generation is **stem-bucketed** (the is-a lattice yields finite
bucket keys) before structured matching enters the emits×consumes hot loop.

### 10. Accounting: §8 is (parameterized) synchronous dataflow

The resource accounting is formally SDF balance (Lee & Messerschmitt): per edge
`q(src)·p(e) = q(snk)·c(e)`; the smallest solution is the repetition vector; an infinite combo is a
consistent cycle with unbounded repetition — Chatterfang's free loop is a balance solution with q = 1.
Variable rates (`X`, `that-many`) make this **parameterized** SDF; quantity bindings across ports (the
sac cost, the removal qty, and the +X/−X magnitude share one `X`) are first-class.

## Consequences

- **Engine:** the `FlowFeasible` per-arm switch shrinks to `captures(Q,E)` + the §6 residual registry;
  blink/cast/reanimate-specific arms retire into the generic deployment match; the sac→dies bridge
  retires into §5 subsumption. This is a **topology change** (today's bridge is consume→consume; the
  event emit moves edge endpoints) — every reconstruction snapshot regenerates, and the two-layer cycle
  engine's label-string quotient must be re-proven over (stem, attribute-set) identity.
- **Parser:** four asks — the `manner` attribute (§5), a `Sacrificed` trigger event (§5), per-cost-component
  spans (logged), and the Deadeye blink slice (`deployment[manner=blink]`).
- **Dumps/API/frontend:** `CardPortRow` gains structured attributes (jsonb, like spans); atlas-web
  matches by attribute-subset (fixing the Aang-class column imprecision at the root) and consumes the
  taxonomy + rollup live — extending the existing derive-canonicality-from-the-API pattern, retiring the
  last hardcoded frontend duplicate.
- **Docs:** the rollup is the single machine-readable source; a doc generator emits the taxonomy
  reference (stems, licensed keys, lattices, aliases, rules-with-witnesses).

## Migration (staged, gated)

**Strategy: aim the taxonomy, accrete the rules ("Path 2, amended").** Two candidate strategies were
weighed: (1) *decentralized transliteration* — encode the current interaction knowledge into the new
format as-is, recompile, and judge the resulting topology; (2) *scaffold-first* — sketch the target
topology (from the slang corpus + CR glossary), then migrate rules toward it, adjusting in-flight. The
decision is (2), amended by three findings: **(a)** the existing fixture corpus
(`tests/magic-ast-tests/Fixtures`, ~1,760 files) stratifies by layer, and almost none of it transliterates:
~96% is **parse-layer substrate** (`HandParsedCards`/`KeywordExpansions`/quarantines — the ASTs the new
ports project FROM; preserved, touched only by the named parser asks, span-only) plus the **lattice-decider
golds** (`FilterRelations` — carried forward, they test exactly the §3 machinery); the interaction-grammar
content that actually transforms is THREE small files (`known-families.json`, `families.json`,
`blood-artist-engine.json`), whose content is **absorbed as witnesses** — the first two encode the
Chatterfang ring (in ADR-0002 glob syntax and in the even older pre-0002 recognizer vocabulary — proof this
corpus already survived one taxonomy migration by content-absorption, not format-porting), and the third IS
a ready-made third gold (Ruthless Knave × Blood Artist, the GREEN `creature ⊆ creature` cover-policy
witness contrasting the Squirrel-straddle AMBER). So per-gold fixtures are written fresh regardless, making
the scaffold nearly free; **(b)** the current shape is known
insufficient (the frontend's topology-shape bugs; unparsed staples like Deadeye's blink; ~38% L2
coverage), so faithful transliteration would reproduce an inadequate map and the post-hoc judge sweep
would lack a null hypothesis to falsify; **(c)** scaffolding does NOT reintroduce central authority —
the shared **vocabulary/shape** (Decision §1–§4) was always centrally designed and judged, while the
**rules** stay evidence-accreted per §8: no rule exists without a witnessing gold. Derivation runs
**re-derive each combo fresh against the scaffold — never transliterate the old arms** (the arms' own
fidelity is separately checked by Stage 3 shadow mode). Evaluation is **dual-loop**: per-gold judges
(§8) plus periodic **rollup-vs-scaffold topology sweeps**, where a divergence either falsifies the
scaffold (it is a revisable hypothesis, not an authority) or flags a bad derivation.

- **Stage 0a — the topology scaffold (target map).** A hypothesis-status, versioned fixture: the
  systematic slang-page + CR-glossary sweep (each term → an AST-derivable view/alias over
  stems+attributes, or explicitly rejected as non-derivable); the enumerated vocabularies (manner
  tokens, zone lattice, licensed-attribute matrix per stem); and falsifiable connectivity predictions
  (e.g. every sac outlet reaches all three trigger rungs; token emitters reach sac outlets) — the null
  hypotheses for the topology sweeps. *Gate:* a judge pass over the scaffold's derivability claims.
- **Stage 0b — formats + rollup generator.** Author the per-gold interaction-fixture schema + rollup
  generator with loud conflict detection; seed from the two worked golds **plus the absorbed third**
  (Ruthless Knave × Blood Artist from `blood-artist-engine.json` — the GREEN cover-policy witness); retire
  `known-families.json`/`families.json` once their content is witnessed in the rollup. *Gate:* rollup
  builds; all three golds' assertions pass.
- **Stage 1 — parser asks.** `manner` attribute, `Sacrificed` trigger event, per-cost spans, the Deadeye
  blink slice — vertical slices via the mast-tdd loop. *Gate:* gold-fixture suite green; span-only regen
  discipline.
- **Stage 2 — structured PortNode.** Stem + attributes + provenance emitted **alongside** the string label
  (dual-emit); the label becomes a generated serialization. *Gate:* label round-trip —
  serialize(structure) reproduces today's labels byte-for-byte across the corpus.
- **Stage 3 — engine shadow mode.** `captures(Q,E)` + residual registry runs in parallel with the old arms
  on the full corpus; diff edges + tiers. Stem-bucketed candidates; quotient re-proof. *Gate:*
  reconstruction golds + bench:recall + a judged sweep of **every tier change** (including the §7
  null-Subject GREEN drops); the corpus-edge-diff gate is explicitly non-authoritative.
- **Stage 4 — cutover.** Retire subsumption-expressible arms; guards/bridges remain as the registry.
  Regenerate resource graph + D1–D4 dumps; reseed the API. *Gate:* CardAtlas contract tests + atlas-diag
  spot-checks on both golds' cards.
- **Stage 5 — frontend.** Structured attributes + rollup consumption; attribute-subset column matching.
  *Gate:* the Chatterfang and Deadeye card pages render the worked-gold edge sets exactly.

## Open questions

- **Duality scope (O3):** which cost/effect families after sacrifice get the consume+event-emit dual
  first (destroy, exile, mill, discard) — resolved incrementally, one gold at a time, through the §8 loop.
- **Naming:** *Deployment* vs *Arrival*; *Structure* vs *Tempo/Sequencing*. Bikeshed-class; settle at
  Stage 0a.
- **Bridge audit (O6):** completes empirically — each bridge either falls to subsumption during a gold
  derivation or earns its registry entry with witnesses.
- **Stage-0 file formats:** the concrete fixture/rollup schemas (execution detail, first work item).
- ~~Migration strategy (transliterate-then-judge vs scaffold-first)~~ — **decided**: scaffold-first,
  amended ("aim the taxonomy, accrete the rules") — see the Migration section's strategy preamble and
  Stage 0a.

## Provenance

- **interaction-judge** (2026-07-16): sacrifice⊂dies⊂LTB 6/6 PASS, PROCEED — conditions (fodder consume
  retained; manner facet; over-approximation posture) folded into §5–§7. CR: 701.21a, 700.4, 603.6,
  603.10a, 614.5/614.6, 111.1, 205.3m, 400.7, 107.4.
- **Fresh-context design review** (Fable, 2026-07-16): ADJUST — five amendments, all adopted (§6 residual
  layer, §7 provenance, §4 root completeness, Migration Stage 3, §9 negation inventory).
- **Prior art:** typed feature structures / HPSG (stem+attributes, appropriateness, open-world);
  description logics (EL vs ALC tractability boundary); Ranganathan's Colon Classification (faceted vs
  enumerative); FRP — Elliott & Hudak (Event/Behavior kinds); event sourcing (state-as-fold);
  pub-sub/interceptor patterns (subscription filters, middleware applies-once); synchronous dataflow —
  Lee & Messerschmitt (balance equations, repetition vectors; PSDF for variable rates).
- **Origin:** the atlas-web Card Explorer flow-adjacency work (the Chatterfang "renders/connects wrong"
  investigation) — see the atlas-diag session artifacts.

---

## Appendix — observations log (chronological grounding)

The unedited accumulation this Decision was promoted from, kept verbatim for provenance — including the
worked golds and the review verdicts in the order they landed. Where an entry conflicts with the Decision
sections above, **the Decision governs** (e.g. O12's colon-nested qualifier sketch predates O14's
attribute-set split; O18's "fourth axis" lean predates O19's kind system).

### O1 — Event subsumption: `sacrifice ⊂ dies ⊂ leaves-the-battlefield` (judge-PASSED)
An interaction-judge review (all 6 claims PASS, PROCEED) grounded the hierarchy in CR:
- **sacrifice ⊂ dies** — CR 701.21a (sacrifice moves a permanent *from the battlefield directly to its
  owner's graveyard*) + 700.4 (*dies* = put into a graveyard from the battlefield). A "whenever a creature
  dies" trigger fires on a sacrifice.
- **dies ⊂ LTB** — CR 700.4 + 603.6 + 603.10a (a death is a battlefield→graveyard zone change; LTB watchers
  fire on it).
- **`sacrifice` also feeds "whenever you sacrifice"** natively — CR 603.10a lists sacrifice-triggers,
  dies-triggers, and LTB-triggers as look-back observers of the *same* event.

Today only **dies ⊂ LTB** is encoded (`DeathTrigger` = `ltb:…:to-graveyard`, the destination as an `ltb`
qualifier). `sacrifice` is a *separate `sac:` role* wired to dies-triggers only by a curated rules-bridge
(`sac → ltb:…:to-graveyard`). **Implication:** fold sacrifice into the `ltb` hierarchy as its narrowest
rung and retire the bridge — subsumption then reaches *all three* consumer rungs, including "when
sacrificed," which the dies-only bridge misses.

### O2 — Costs/effects are frequently DUAL (emit an event AND consume a resource)
The sacrifice cost both **emits** the LTB event (subject = fodder type — what triggers *see*) and
**consumes** the fodder permanent (what is *removed*). The judge's load-bearing condition: the emit's
subject is a **type descriptor, not a pool decrement** — dropping the `sac:` consume loses the §8
balance/multi-cost-conjunction floor and re-admits the Ruthless-Knave false-GREENs. **Implication:** the
redesign must let one clause project **both** a resource-consume and an event-emit, and keep them on
separate axes (flow/balance vs existence/certainty). This likely generalizes: "destroy," "exile," "mill,"
"draw-as-a-cost," etc. are candidates for the same consume+emit duality.

### O3 — Name by event/resource, not by action-mechanism
`sac`, `ltb`, `etb`, `cast`, `attacks` are **mechanism/action** roles. The subsumption in O1 only falls out
cleanly once sacrifice is expressed as *an LTB event with a manner*, i.e. named by the **event** it
produces. **Implication (thesis):** prefer event/resource-centric leaves so hierarchy is structural. A
"cost" is then a *consume of a resource* + optionally an *emit of the event that paying it produces*, not a
first-class role. Open: how far to push this without exploding the vocabulary (see O8).

### O4 — The facet grammar needs a `manner` slot
To keep dies a proper generalization of sacrifice while letting "when sacrificed" match *only* sacrifices,
the leaf needs a cause/manner facet **after** destination:
`role : subject : [destination] : [manner] : [scope] : [exclusion]`
(e.g. `ltb:creature:squirrel:to-graveyard:sacrificed:controlled`). The judge flagged that the rungs are
**not** naive subject-prefixes (subtype sits between subject and destination) — they are realized by the
**glob** operator (`ltb:**:to-graveyard:**`) plus the **operator** deciding subject-subsumption. So this is
a real grammar extension, authored so glob patterns generalize correctly. Manner tokens to enumerate:
`sacrificed`, `destroyed`, `combat` (combat-damage death), `state-based`, ….

### O5 — Two axes are being conflated: engine-ROLE vs frontend-FAMILY
`ResourceFamilies.Of(label)` derives the coarse family from the **role prefix** (`sac:` → `sacrifice`).
That family drives the resource graph + the explorer columns; the **role** drives the §8 engine. These are
different consumers with different needs: the engine wants the `sac:` role (conjunction/balance); the
explorer wants "Chatterfang consumes a *creature/token*, and emits a *death*." Deriving the family from the
role forces one to serve both and mislabels the card page. **Implication:** make family a first-class,
possibly event/subject-derived axis, decoupled from the engine role. (This is the concrete fork blocking
the Chatterfang card-page fix: fodder-consume family = `creature` [faithful, non-canonical] vs `token`
[drives columns, wrong for non-token fodder].)

### O6 — Subsumption vs curated bridges — draw the line deliberately
The engine has two cross-resource mechanisms: **facet-prefix subsumption** (dies⊂LTB, structural) and
**curated rules-bridges** in `PortGraphEngine` (`sac→ltb`, untap-lands→mana, blink→etb, spell-recursion→cast,
…). O1 shows at least one bridge (`sac→ltb`) is really a *missing subsumption rung*. **Implication:** audit
every curated bridge — which are genuine game-rule shortcuts (a *different* resource enabling another) vs
subsumption the vocabulary should express directly. Hypothesis: bridges should be reserved for
cross-*resource* enablement (untap→mana), and same-event narrowings (sacrifice→death) should be subsumption.

### O7 — Over-approximation is principled, and must stay principled
`sacrifice ⊂ dies` is over-approximate: a CR-614 graveyard replacement (Rest in Peace) sends the sacrificed
permanent to exile, so it *leaves the battlefield* (absolute) but does *not* die (the `to-graveyard`
destination is over-asserted). This is the **same** prune-able status the current `sac→ltb` bridge already
carries ("the label names; the operator decides," ADR 0002 §6/§7). The redesign must preserve: the
*projection over-proposes the destination, the operator/board prunes*. Related straddle: the subtype→creature
lift (`Squirrel` → `creature:squirrel`) is a matching over-approximation — CR 205.3m/308.1 leave
`Squirrel ⊄ creature` Unknown for a bare-subtype filter, so the operator floors it to AMBER (never a false
GREEN). Both belong in the redesign's "over-approximate axes" register.

### O8 — Single source of truth + generated docs
The taxonomy is spread across ~5 places with no generated reference:
`PortWalkProjection.cs` (projected role/effect/cost/trigger sets), `PortLabel.cs` (the facet builders),
`ResourceFamilies.cs` (families + canonical set), ADR 0002 (the spec), and a **partial duplicate in the
frontend** (`mock.ts` `GROUPS` / the live-canonical set derived from `resourceFamilyRows`). There is an
exhaustiveness ratchet (`PortWalkExhaustivenessTests`) forcing each AST discriminator through a projection
decision, but **no centralized definition of the families and their subsumption hierarchies**, and no
generated facet-grammar reference. **Implication:** the redesign should land a single machine-readable
taxonomy (families, rungs, manner tokens, subsumption edges) that both the engine and the frontend consume,
plus a doc generator. The user explicitly wants standardization + doc generation here.

### O9 — Canonical family set is due for review under the event model
Current 17: `mana token sacrifice death etb recur dice damage life blink copy cast combat untap tap counter
phase`. Under O1/O3, `sacrifice` is not a peer resource — it's a *manner* of `death`, which is a rung of
LTB; `etb` is the enter dual of LTB; `recur` (return-to-battlefield/hand) is zone-change too. **Implication:**
a zone-change umbrella (`ltb` / `etb` / `recur` as directions of one axis) may collapse several "families"
into one hierarchy, which would also fix the explorer's family membership questions at the root.

### O10 — "Fodder" is a strategic gloss, not a resource: separate OBJECTS from EVENTS
"Fodder" (what a sacrifice consumes) is Magic slang for expendable creatures — **not AST-derivable** (a
creature's fodder-ness is a game-plan judgment, absent from the card text) and not a 1/1-specific notion.
What a sacrifice actually consumes is a **creature** (the permanent named by the `sac:` subject filter —
Ashnod's `creature`, Chatterfang `creature:squirrel`); what the doubler emits is **creature tokens**
(`emit:token:creature:squirrel`), never `emit:fodder`. The label names what an object *is*, not the role a
player assigns it. This exposes a cleaner cut: distinguish **object-resources** (the things that flow around a
loop — creatures/permanents, mana, tokens, counters) from **events** (what happens to them — enters,
leaves-battlefield / dies / sacrificed, cast). A sacrifice = `consume(object: creature)` + `emit(event: LTB)`;
the creature flows, the sacrifice/death is the event feeding payoffs. **Refines** O2 (the duality is
object-consume + event-emit), O3 (name by object *and* by event, on separate axes), O5 (the fodder-consume
family is `creature`, the object — reframing the fork), and O9 (`creature`/`permanent` is an object-resource
the canonical family set omits; `sacrifice`/`death`/`etb` are events, not peer object-families). Corollary
(the enabling principle, ADR 0002 §2): a single oracle clause legitimately projects **multiple** ports —
Chatterfang's one sacrifice clause is a consume AND an emit — because a port is a *query* against the AST, and
one sub-tree satisfies many queries. The many-to-one (text → labels) is the design, not a smell.

### O11 — Slang categories are first-class when AST-derivable: `fodder` as a P/T-derived subset
**Corrects O10's over-dismissal.** The bar for a port term is **AST-derivability, not CR-canonicity** — MAST
is a *categorization* system, so a slang category earns its place if it's a definable **query**, not a
game-plan judgment. "Fodder" fails only as vague "creatures you'd sacrifice"; pinned to structure it is
legitimate and useful: `fodder` := a **1/1 creature** (or, for the Skullclamp reading, a **toughness-1**
creature — one a −1-toughness effect kills), read off the `createToken`/creature P/T the AST already carries.
It is a **subset leaf** on the object axis (`emit:token:creature:squirrel:…:fodder ⊆ emit:token`), consistent
with the soft hierarchy (a port satisfies its prefixes), so it adds precision without breaking coarse
`emit:token` matching. **Motivating case — Skullclamp** ("+1/−1; whenever equipped creature dies, draw two
cards; Equip {1}") is a `consume:fodder` engine: it turns toughness-1 creatures into cards. **Chatterfang's
1/1 Squirrels are `emit:…:fodder`**, so the taxonomy surfaces **Chatterfang × Skullclamp** structurally —
which bare `emit:token`/`consume:creature` labels miss, being too coarse to know these are the *small*
creatures Skullclamp wants. **Caveat — polysemy:** a sac outlet wants *any* creature, Skullclamp wants
*toughness-1*, an aristocrat wants *anything that dies*; so each derived category must be **pinned to exactly
one query** (or carry a small family: `fodder` = 1/1, `fodder:t1` = toughness-1), never a catch-all.
**Implication:** the object axis (O10) carries **derivable sub-categories** (P/T, subtype) as subset leaves —
this is a primary source of the taxonomy's synergy-surfacing value, and the redesign should treat such
categories as named, singly-defined query aliases layered under the structural labels.

### O12 — Initial top-level supergroups (slang-anchored)
First structural scaffold for the taxonomy root. Slang supplies the equivalence classes players reason in
(mtg.fandom.com/wiki/List_of_Magic_slang); "removal" is the flagship — a broad class no single CR term names.
The supergroups sit on the event axis + the resource axis (O10) plus a clock axis, and the **object axis
(subject + derived categories, O11) crosses through them** — a full leaf is roughly
`<side> : <supergroup> : <object-subject> : <qualifiers(destination/manner/scope)> : <quantity>`.

**A · Object zone-transitions (events on objects):**
1. **Removal** — an object in play (battlefield OR stack) will no longer be in play. Subsumes by
   **destination** (`→graveyard` = *dies* / `→exile` / `→hand` = *bounce* / `→library` / *off-stack* =
   *countered*) and **manner** (*sacrificed*, *destroyed*, *combat*, *state-based*). **This tree IS O1**:
   `removal:creature:…:to-graveyard:sacrificed` = Chatterfang's sac; bare `removal:creature` (LTB) /
   `…:to-graveyard` (dies) match it by glob. Sacrifice⊂dies⊂LTB is a slice of Removal.
2. **Deployment** — an object enters play (→battlefield/stack). Subsumes cast, ETB, token-creation,
   reanimation/recursion, blink-return, copy. Removal's dual (flicker = Removal→Deployment; recursion =
   removal-then-redeployment).
3. **Modification** — an object altered in place (stays in play): P/T (modify/set/switch), keyword/ability
   grants, type/color/control change. (Slang: pump, anthems.)

**B · Resources (fungible pools):**
4. **Mana** (ramp/fixing/rituals) · 5. **Cards** (draw/dig/tutor/mill/discard — the hidden hand/library/
   graveyard resource) · 6. **Life** (gain/loss/drain; **damage** resolves here as life/toughness loss) ·
   7. **Counters** (+1/+1, proliferate, energy).

**C · Structure (the clock):**
8. **Structure** — phases, steps, extra turns, extra combats, untap, priority (tempo/untappers/time-walk).

**Known straddles (edges, to resolve — not blockers):** *Damage* is a Life verb whose *lethality* is a
derived edge into Removal, not its own supergroup. *Counters* straddle Modification (a +1/+1 counter changes
P/T) and Resources (proliferate/energy pool) — likely own supergroup + a modification edge. *Sacrifice-as-cost*
= a Removal emit that also decrements the object pool (the O2 duality restated). **Open naming:** Deployment
vs Arrival/Development; Structure vs Sequencing/Tempo.

### O13 — Removal needs a source-zone facet; zone-change (from→to) is the primitive
Removal's first cut ("an object in play will no longer be in play," O12) is battlefield/stack-anchored, but
graveyard hate removes an object from a NON-play zone — **Soul-Guide Lantern** ("When this artifact enters,
exile target card from a graveyard") is `removal:card:from-graveyard:to-exile`. Generalize: a Removal leaf
carries an explicit **from-zone** facet — `removal:<object>:from-<zone>:to-<zone>:<manner>:<scope>` — covering
battlefield→graveyard (dies), battlefield→exile, battlefield→hand (bounce), graveyard→exile (gy hate),
hand→graveyard (discard), library→graveyard (mill), stack→graveyard (countered). Deeper: **zone-change
(from → to) is the primitive event**; Removal and Deployment (O12) are slang supergroups anchored on the
battlefield/stack endpoint — Removal = battlefield/stack is the FROM, Deployment = it's the TO — and
non-play-endpoint zone-changes (gy hate, discard, mill) are Removal in the broad "answer/disrupt" sense.
**Open:** do Removal/Deployment stay separate supergroups sharing a from/to facet pair, or collapse into one
Zone-change supergroup with the slang as derived views?

### O14 — `:` is is-a taxonomy; attributes are an unordered qualification set (not colon-nested)
ADR 0002 §3 flattens everything into one ordered colon-chain (`role:subject:[destination]:[scope]:[exclusion]`),
conflating genuine hierarchy with orthogonal filters and forcing a canonical order + glob to match subsets.
Split them:
- **Taxonomy stem (`:`, ordered, genuine is-a):** `<side>:<supergroup>:<card-type>` — e.g. `emit:removal:creature`,
  `emit:deployment:creature`. Card TYPE (permanent ⊃ creature/artifact/…) is the is-a spine; a bare
  `removal:permanent` really is a prefix of `removal:creature`. Keyword grants may nest here where is-a holds
  (`modification:evasion:forestwalk`; forestwalk ⊂ landwalk ⊂ evasion).
- **Attributes (an unordered qualification SET on the leaf):** `subtype`, `control`, `from-zone`, `to-zone`,
  `manner`, `p/t`, `color`, `token`, `qty`, … ORTHOGONAL axes, not deeper taxonomy. A Squirrel is-a creature in
  the *general* tree, but in a *port* "creature" is the type and "squirrel" filters *which* creature, alongside
  "controlled." Notate as a set — `emit:removal:creature[subtype=squirrel, control=you, from=battlefield,
  to=graveyard, manner=sacrificed]` — never `removal:creature:squirrel:…:controlled`.
- **Matching = attribute-subset, order-free:** an emit satisfies a consume iff the consume's constraints ⊆ the
  emit's attributes (Blood Artist `consume:removal:creature[]` matches `emit:removal:creature[subtype=squirrel,
  manner=sacrificed]`). Cleaner and more robust than ADR 0002's positional glob.
- **Derived categories (O11) are named attribute-constraints, NOT stem nodes:** `fodder` := `[p/t=1/1]` (or
  `[toughness=1]`). This also **corrects the O13 table**: the sac consumes ANY controlled Squirrel, so its
  filter is `[subtype=squirrel, control=you]` with *no* fodder constraint; `fodder` is an attribute of the
  *emitted* tokens (and of what Skullclamp *wants*), not a requirement of the sac.

### O15 — Prior art: the design is a typed-feature-structure system; stay in the tractable profile
Research pass (2026-07-16) against three literatures, to catch down-the-road issues:

1. **Typed feature structures / unification grammars (HPSG).** O14's design — a type hierarchy (the is-a
   stem) + attribute-value pairs, matched by subsumption — IS the TFS formalism: types ordered by
   information content, attributes inherited down the hierarchy, matching = unification/subsumption. Two
   TFS lessons to adopt: **(a) appropriateness conditions** — each stem type licenses a declared set of
   attribute keys (`removal:*` licenses `from/to/manner`; `mana` licenses `color/qty`; nonsense like
   `mana[toughness=1]` is ill-typed) — this is the machine-readable schema O8's doc-generator should emit;
   **(b) absent-attribute semantics** — an unspecified attribute means *unknown/any*, and matching against
   it is an OPEN-world judgment, not a failed lookup. (The operator's Subsumes/Intersects/Overlaps trilean
   already implements exactly this — e.g. the Squirrel⊄creature straddle floors to AMBER.)
2. **Description logics.** Subsumption in EL-family logics (conjunction + existential only) is
   **polynomial**; adding general boolean operators (ALC) makes it **EXPTIME**. Our matching — conjunctive
   attribute-subset + per-value lattices, no disjunction/negation in queries — sits in the tractable EL
   profile. **Guardrail:** keep it there. The one existing negation (`:another` / exclude-self) is a bounded
   exception to confine; resist general boolean attribute constraints in the query language.
3. **Faceted classification.** Ranganathan's **Colon Classification** (1933!) is the ancestor of the
   colon-label idea, and its core finding is O14's split: rigid enumerative hierarchies pigeonhole subjects;
   orthogonal facets compose. ADR-0002's single ordered chain was drifting enumerative; O14's stem+facets is
   the analytico-synthetic correction.

**Answer to the prefix-capture question:** yes — by construction, once capture is defined per-facet, not as
string prefix. `query(Q) captures emit(E)` iff (stem of Q is an ancestor-or-equal of stem of E in the is-a
lattice) ∧ (each attribute constraint in Q subsumes E's value in that attribute's own value lattice —
`control: self ⊆ controlled ⊆ any`, zones, the TypeOntology for subtypes; unconstrained = captures all).
So `removal:creature` implicitly captures every deeper stem and every attribute combination — `removal:
creature:*` for free — and ADR-0002's positional `**` glob (needed only because attributes sat inside the
ordered chain) is retired. Unknown-vs-constrained mismatches resolve by trilean, not boolean.

### O16 — The AST already carries the feature structure; the label string is the lossy step
Mesh check against current code: the O14/O15 model is *closer* to the existing machinery than ADR-0002's
strings are. `ObjectFilter` (Subtypes, Controller, IsToken, …) **is** the attribute set;
`ObjectFilterRelations.Subsumes/Intersects` over the `TypeOntology` **is** the per-facet lattice matching with
trilean open-world semantics; `PortNode` already carries the filter as its `Subject`. The lossy step is
`PortLabel` **flattening** the filter into an ordered colon-string (`LiftCardTypes`, facet joins) and then
*re-matching on the string* with globs — projecting structure down to text and parsing it back. Structural
changes the redesign implies:
1. **Match on the structured port, not the string.** PortNode grows explicit stem + attribute fields (subject
   filter, from/to zone, manner, qty binding); `FlowFeasible`/the engine's arms become instances of ONE generic
   `captures(Q,E)` (O15) plus the §8 accounting — the per-arm switch shrinks to the genuinely cross-resource
   bridges (O6).
2. **The colon-label becomes a serialization/display format** (and the dump/query surface), generated FROM the
   structure — never the matching substrate. Collision-lint and determinism guarantees transfer unchanged.
3. **Appropriateness schema as data** (O15): a single machine-readable taxonomy file — stems, licensed
   attribute keys per stem, per-attribute value lattices, derived-category aliases (O11), slang supergroup
   views (O12) — consumed by the projection, the engine, the doc generator (O8), AND the frontend (which
   already derives canonicality live from the API; this replaces the last hardcoded frontend duplicate).
4. **Dumps/API**: CardPortRow gains structured attributes (jsonb, like spans) so atlas-web matches by
   attribute-subset instead of family-string equality — fixing the column-precision problems (Aang) at the
   root.
5. **Migration posture**: projection + engine first behind the existing gold-fixture gates (labels regenerate
   deterministically from structure), resource-graph/dumps regen after, frontend last. The AST itself needs
   little: filters/zones/quantities are already typed fields; the main parser asks are the O4 manner facet and
   per-cost spans (already logged).

### O17 — Fresh-context review verdict: ADJUST (five bounded amendments)
An independent fresh-context review (Fable, 2026-07-16) verified O1–O16 against the actual code and rendered
**ADJUST** — central diagnosis confirmed (the AST is already a TFS; the colon-string is the lossy step;
label re-parsing/positional facet reads in the engine are the evidence), but one systematic over-claim and
several gaps block a PASS. Verdicts: A(root) ADJUST · B(grammar) ADJUST · C(prior art) PASS w/ correction ·
D(sacrifice) PASS w/ two notes · E(mesh) ADJUST. The five amendments, now adopted into this log:

1. **`captures(Q,E)` needs a declared RESIDUAL layer — subsumption replaces the arms' *structural* half
   only** (amends O16.1). Reading the engine arms shows three knowledge classes a filter lattice cannot
   express, each purchased with a judged false-positive class:
   (i) **producer-choice attribute polarity** — `emit:mana[color=any]` feeds `pay:mana[color=black]` GREEN
   because the *producer chooses* the color (`ManaColorFeeds`); naive constraint-⊆-value floors the
   Chatterfang × Pitiless mana hop to AMBER. Per-attribute polarity (fixed vs producer-chooses, existential
   matching for choice values) must be declared in the schema.
   (ii) **per-role match policy** — sac/recast require type-*cover* (deliberately stricter than Intersects),
   etb is deliberately lenient, CR 400.7 token-can't-be-self refusals.
   (iii) **object-identity guards** — the same-card + `:self` checks (self-watching damage, self-blink,
   spell self-copy/recast) that killed the recurring 1-card false-loop class.
   Acceptance tests: Chatterfang × Pitiless mana hop stays GREEN; the four same-card-guard false loops stay dead.
2. **Restate ADR-0002 §6's invariant for attribute data** (amends O7/O14): every projected attribute carries
   **asserted vs over-approximated provenance**; an over-approximated attribute (`to=graveyard` under CR-614
   redirects; the subtype→type lift) may gate feasibility but **caps Reliability at Unknown** — otherwise
   generic matching GREEN-certifies over-assertions (the 644-false-GREEN class by a data-shaped door). Fold
   the null-Subject default-GREEN keep/kill decision into the same section.
3. **The supergroup root has orphans** (amends O12): `dice` fits no meta-axis; `tap` and combat-presence
   (`attacksorblocks`) are only implicitly Structure. Place them (or declare a named fourth bucket —
   candidate: a "Stochastic/Presence" review against `ResourceFamilies.Canonical` + all three
   `PortWalkProjection` discriminator sets, line-by-line, before Decision). Parser ask: **no `Sacrificed`
   trigger event exists** — the O1 "when sacrificed" rung is parser-gated; add it beside the O4 manner facet.
4. **Migration is a topology change, not a relabel** (amends O16.5): today's sac→dies bridge is
   **consume→consume**; the emit-side removal event moves edge *endpoints*, so every reconstruction
   gold/cycle shape regenerates, the two-layer cycle engine's label-string quotient (byte-identical
   equivalence gate) must be **re-proven** over stem+attribute-set identity, and the corpus-edge-diff gate is
   known non-authoritative on broad changes. Promotion gate = reconstruction golds + bench:recall + a judged
   GREEN sweep. Perf: the emits×consumes product needs **stem-bucketed candidate generation** (the is-a
   lattice gives finite bucket keys — strictly better than globs) before structured matching enters the hot loop.
5. **O15 negation-inventory correction**: `:another` is not the only negation — the filter model carries an
   atomic-negation family (`ExcludedCardTypes/Subtypes/Supertypes/Colors`, `IsColorless`, `NotEqual`
   comparisons), each per-axis-decided and in-profile. The Decision inventories these, not just `:another`.

---

### The triggering case (worked, for reference)

Chatterfang, Squirrel General — full-text labeling under O10–O14 (stem `side:supergroup:card-type` +
attribute set `[…]`; labels illustrative pending final facet names):

| Oracle text | Side | Stem | Attributes |
|---|---|---|---|
| `Forestwalk (…reminder…)` | emit | `modification:evasion:forestwalk` | `[subject=self]` — inert; reminder text = no port |
| `If one or more tokens would be created under your control,` | intercept | `deployment` | `[token, control=you]` — the replaced creation event |
| `those tokens plus that many 1/1 green Squirrel creature tokens are created instead.` | emit | `deployment:creature` | `[token, subtype=squirrel, color=green, p/t=1/1, control=you, qty=that-many]` (p/t=1/1 ⇒ **fodder**) |
| `{B},` | consume | `mana` | `[color=black, qty=1]` |
| `Sacrifice X Squirrels` (dual, O2) | consume | `creature` | `[subtype=squirrel, control=you, qty=X]` — fodder pool decrement (**§8**; **no** fodder attr — any Squirrel) |
| `Sacrifice X Squirrels` | emit | `removal:creature` | `[subtype=squirrel, control=you, from=battlefield, to=graveyard, manner=sacrificed, qty=X]` |
| `Target creature gets +X/-X until end of turn.` | emit | `modification:creature` | `[stat=p/t, delta=+X/-X, target=any, duration=eot, qty=X]` — −X toughness ⇒ **lethal edge** → derived Removal (cf. Skullclamp) |

Match check: Blood Artist / Pitiless (`consume:removal:creature[]`, any creature death) satisfy the sac emit
because their `[]` ⊆ its attributes; the spurious `emit:cast` rows (Aang) never arise. `X` binds across the sac
cost, the removal qty, and the +X/−X magnitude (§8); `that-many` binds the doubler emit to the intercept.

### The flagship gold, redefined (Chatterfang × Pitiless Plunderer under O1–O17)

The ADR-0001 canonical gold, re-derived end-to-end — it exercises every decision, including all three
O17.1 residual rules. Loop: sac a Squirrel → Pitiless makes a Treasure → the doubler adds a Squirrel to
that creation → the Treasure pays the `{B}`. Ports (`•choice` = producer-choice polarity, `•derived` =
over-approximated provenance, `•fodder` = O11 alias):

**Pitiless Plunderer:** P1 consume `removal:creature[control=you, from=battlefield, to=graveyard,
exclude=self]` · P2 emit `deployment:artifact[token, subtype=treasure, control=you, qty=1]`.
**Treasure token:** T1 consume `tap[self]` (the O17.3 orphan) · T2 consume `artifact[subtype=treasure,
token, self, qty=1]` · T3 emit `removal:artifact[…, manner=sacrificed]` · T4 emit
`mana[color=any•choice, qty=1]`. **Chatterfang:** C1–C6 as tabled above (C4 = fodder consume, C5 =
removal emit with `to=graveyard•derived`).

| Edge | Match | Certifying mechanism |
|---|---|---|
| E1 C5→P1 | stems equal; P1 attrs ⊆ C5; Squirrels ≠ Pitiless | **subsumption (bridge retired)** + identity guard; `•derived` caps Reliability (Rest-in-Peace prune) |
| E2 P2→C1 | `deployment ⊇ deployment:artifact`; `[token, control=you] ⊆` P2 | **Modifier edge** — never closes a loop alone (CR 614.5) |
| E3 C1⇒C2 | `qty=that-many` bound to intercepted count | card-defined |
| E4 C2→C4 | at-creation types `creature[squirrel, token]` **cover** C4's filter | **per-role match policy** (cover, not Intersects) |
| E5 T4→C3 | `[color=black]` vs `[color=any•choice]`: producer picks black, GREEN | **polarity** (existential on choice values) |

§8 per iteration (X=1): Squirrels −1(C4) +1(C2) = 0 · mana −{B}(C3) +1-chosen-black(T4) = 0 · Treasures
+1−1 = 0 · no hard gates; T1's tap is not cross-iteration (fresh token each loop). Free loop.

Deltas vs the ADR-0002 gold: E1 flips from a curated consume→consume bridge to a real emit→consume flow
edge (the O17.4 topology change, on the gold itself); the sac is dual (C4+C5 — §8 reads off C4); the three
GREEN-preserving judgments (polarity/cover/identity) move from buried engine arms to declared schema; the
over-approximation becomes visible provenance; the modifier edge survives untouched.

### Second gold — Deadeye Navigator + Peregrine Drake (orthogonal validation)

A mechanically-unrelated U(/W control-flicker) combo, chosen to stress the axes the aristocrats gold never
touches. Infinite ETBs + mana-positive: Deadeye (soulbonded to Drake) pays `{1}{U}` → blinks Drake → Drake's
ETB untaps 5 lands → repeat. Current parse state (real data): Peregrine parses clean (`etb:creature:self` +
`emit:untap`, 19 combos); **Deadeye's blink is UNPARSED** (`emit:gainability`, 0 combos) — a live parse gap the
taxonomy gives a target for. Worked on the correct mechanics:

**Deadeye:** `consume:mana[blue,1]+[generic,1]` · emit `removal:creature[from=battlefield, to=exile,
manner=blink, exclude=self]` (exile half) · emit `deployment:creature[manner=blink, exclude=self]` (return
half — load-bearing). **Peregrine:** consume `deployment:creature[self]` (ETB) · emit
`structure:untap[subtype=land, control=you, qty=up-to-5]`.

| Edge | Match | Arm kind |
|---|---|---|
| E1 Deadeye `deployment:creature[blink]` → Drake `consume:deployment:creature[self]` | stems equal (blink *is* a deployment); structural half = subsumption; Drake self-watches, Deadeye blinks "a creature" → Overlaps → **AMBER** | **subsumption + identity-guard residual** |
| E2 Drake `structure:untap[land]` → Deadeye `consume:mana[{1}{U}]` | untap ≠ mana, no lattice relates them → **AMBER** ("up to five"→firability; colors unknown, CR 107.4) | **irreducible cross-resource bridge (stays curated)** |

**Why it validates the model:**
- **E1 splits an existing arm exactly as O16.1/O17.1 predicted.** `BlinkSatisfiesEnter` collapses: its structural
  half becomes the generic `deployment → deployment-consume` match (which ALSO covers cast/reanimate — the
  blink-specific arm is *retired* and blink/cast/reanimate unify under `deployment`); its residual half (is the
  blinked object the ETB's self-watched object?) cannot collapse and correctly floors to AMBER. Structural →
  subsumption; residual → stays.
- **E2 is the other arm kind** — a genuinely cross-resource bridge (Structure enabling Resource) subsumption
  can't express; it stays curated and AMBER-by-construction. The two edges together exhibit BOTH residual kinds
  the review named (O17.1), on one combo.
- **Object/event symmetry (O10)** in the flesh: `dies` = consume-a-removal ↔ `etb` = consume-a-deployment.
- **The untap orphan (O17.3) appears on a real combo** — `structure:untap` is exactly the unplaced family; the
  gold confirms the orphan is real, not hypothetical, and belongs to a Structure/tap sub-axis.
- **Tier outcome differs correctly**: both edges AMBER → soundly-conditional loop (works with blue sources, not
  card-certifiable), reached by the SAME machinery that GREENs Chatterfang. Color never touches the stem
  (mono-U here, B/G there) — the structural evidence that the taxonomy is color-agnostic.

Caveat: mono-blue, not literally U/W; the UW two-color / combat-blink variant is Brago, King Eternal (which
would also exercise the combat-presence orphan).

### O18 — Root-completeness sweep (O17.3) + open-items ledger
All 17 `ResourceFamilies.Canonical` mapped against the O12 root:

**Clean (11):** mana/life→Resources; damage→Resources·Life (+lethal edge→Removal); token→attribute `[token]`
on a Deployment (not a family); sacrifice→Removal`[manner=sacrificed]`; death→Removal`[to=graveyard]`;
etb→Deployment(consume); recur→Deployment`[from=graveyard]`; blink→Removal`[to=exile]`+Deployment; cast→
Deployment`[to=stack]`; phase→Structure.

**Needs a decision (6):**
- **dice → TRUE ORPHAN.** Neither zone-transition nor fungible pool — a roll is an *ephemeral event* producing
  a number, feeding "whenever you roll" triggers. Candidate: a **fourth meta-axis, Events/Occurrences** (roll;
  arguably damage-as-event, combat-damage-dealt) for triggerable happenings that move no object and bank no
  pool; alternative is modeling a roll as a transient Resource (loses the occurrence nature). LEAN: fourth axis.
- **tap / untap** — Structure · *availability* sub-axis (surfaced on the Deadeye gold; place it).
- **combat** — Structure (phase/extra-combat) + a **combat-presence** event (`attacksorblocks`); name the
  presence event.
- **counter** — straddle: Modification (`+1/+1`→P/T) vs Resources (proliferate/energy pool). LEAN: own Resource
  with a Modification edge.
- **copy** — mostly Deployment (a copy enters); spell-copy is a stack event (place explicitly).

**Not root members — the §8 gating layer (name, don't file):** the projection's control-flow wrappers
`conditional`/`composite`/`optional`/`rollResultsTable`/`becomesPermanent` gate/fan-out/grant over OTHER ports;
they belong to firability, orthogonal to the resource/event root.

### Open-items ledger (review amendments by state, honest)
- **O17.1 residual-layer schema** — VALIDATED by both golds (polarity, cover-policy, identity-guard, irreducible
  bridge all appeared), NOT drafted as machine-readable data. *The largest creative work remaining.*
- **O17.2 provenance + null-Subject default-GREEN** — noted (`•derived` on golds); representation unspecified and
  the default-GREEN keep/kill is a LIVE decision.
- **O17.3 root completeness** — this sweep; dice/tap-untap/combat-presence/copy/counter decisions above.
- **O17.4 migration harness** — shape named (topology change, quotient re-proof, reconstruction-gold+bench:recall
  +judged-GREEN gate); not staged.
- **O17.5 negation inventory** — correction noted; `Excluded*`/`IsColorless`/`NotEqual` not yet enumerated per-axis.

### O19 — Event-FP re-derivation of the top layer: Event / State / Behavior kinds
Research pass (2026-07-16) mapping event-based functional programming onto the root; three literatures land:
1. **FRP (Elliott & Hudak)** — two primitives: **Events** (discrete occurrences with payloads) and
   **Behaviors** (continuous time-varying values). This is the missing KIND system: Events = zone-changes,
   cast, damage-dealt, **dice-rolled** (the O18 orphan DISSOLVES — a roll is just an event with a numeric
   payload and no object; the "fourth axis" was Events all along), combat-presence. Behaviors = the
   Modification supergroup precisely (statics/anthems/"as long as"/P-T layers).
2. **Event sourcing** — state = `fold(initial, events)`; state is derived, never primitive. Resources are
   **Stores/accumulators** folded over event streams. Places two O18 stragglers: **tap/untap** = an
   availability store on a permanent (untap = a state-restoration event); **counter** = a store whose P/T
   effect is a derived Behavior (the straddle resolves into store + projection).
3. **Stream processing / pub-sub** — the port sides map one-to-one: emit = publish; trigger-consume =
   **subscription with a content filter** (our attribute matching IS content-based filtering); cost-consume =
   **function argument** (an activated ability is a function call: args=costs, return=effects); intercept =
   **middleware** — and CR 614.5's applies-once is the standard interceptor invariant, so no-self-bootstrap is
   a known theorem of the pattern, not a house rule. **Push vs pull consumption becomes first-class**: costs
   (pull/conjunction — §8 function application) vs triggers (push/any-match) are structurally distinct kinds
   of Consume, which the engine already treats differently but the model never named.
4. **SDF balance equations (Lee & Messerschmitt) = §8's formal home.** `q(src)·p(e) = q(snk)·c(e)` per edge;
   smallest solution = the repetition vector; a consistent cycle = a schedulable loop. An infinite combo IS a
   consistent SDF cycle with unbounded repetition — Chatterfang's free loop is a balance solution with q=1.
   Rank/deadlock/scheduling theory applies directly. Caveat: X-costs and `that-many` are variable rates →
   **parameterized SDF** (PSDF), cite the extension honestly.

**Re-derived top layer:** a KIND system above (not replacing) the O12 supergroups —
`EVENT` (zone-change, cast, damage, dice, combat) / `STATE` (mana, life, cards, counters, availability) /
`BEHAVIOR` (modification). The O12 slang supergroups survive as **named filters over the event stream**
(Removal/Deployment = views over zone-change — answering O13's open collapse question: one zone-change event
type, supergroups as views). Ability layer: activated=function(pull) · triggered=subscription(push) ·
static=behavior · replacement=middleware. The O18 gating layer = stream **combinators**
(conditional/optional/composite). §8 = (P)SDF balance over the cycle.

**Resolves from O18:** dice (event, no new axis) · tap/untap (availability store) · counter (store + behavior
projection) · copy (a deployment event with provenance=copy) · control-flow wrappers (combinators, named).
**Remaining from O18:** combat-presence naming (an event — trivial now) · the residual-layer schema (O17.1,
unchanged) · provenance/default-GREEN (O17.2) · migration staging (O17.4) · negation enumeration (O17.5).

### O20 — The residual-layer schema is a DERIVED ROLLUP, not a central authority (schema-by-accretion)
Decision-shaped (user-directed, 2026-07-16). Interactions are captured by deriving combos into flagship
golds across many subagent runs — so a centrally-authored schema is both a fan-out bottleneck and
philosophically wrong (speculative rules, written before evidence). Instead:

- **Per-gold interaction fixtures are the only hand-authored artifact.** Each derivation run writes one
  gold: its edges (each naming its certifying mechanism — subsumption / polarity / match-policy / guard /
  bridge), any NEW rules it introduces (with itself as witness), and machine-checkable assertions (the gold
  IS its own acceptance test).
- **The central fixture is a generated rollup** (content-hashed, never hand-edited): the union of all
  declared rules across golds — polarity table, match-policy table, guard registry (impl stays code, but
  every entry cites witnessing golds), bridge list — each entry carrying `witnesses:` provenance.
- **The loop:** (a) a new derivation READS the rollup first (known mechanisms are reused, never re-derived);
  (b) it writes its gold + declares only genuinely new interactions; (c) the rollup REGENERATES. Repeat.
  Subagent-parallel by construction — workers touch only their own fixture; the rollup is the union step
  (merge conflicts land only on the generated file, which regenerates).

Three load-bearing semantics:
1. **Conflicts fail loudly.** Contradictory rules across golds → the rollup build FAILS (never
   last-writer-wins), forcing a judge pass whose resolution is recorded as an evidence-cited entry
   (stateless-invariants-over-ratchets, applied to the schema itself).
2. **Promotion ladder — rules are earned:** `observed` (1 witness) → `corroborated` (N) → `confirmed`
   (rules-judge pass). Integrates with tiering: an edge certified only by an observed rule CAPS AT AMBER;
   only confirmed rules participate in GREEN certification. The schema's own certainty feeds tier assignment.
3. **The rollup is the runtime rule source for novel pairs** (MAST's purpose is unreported combos): novel
   edges matched via corroborated/confirmed rules tier normally; an edge needing a rule that doesn't exist
   falls back coarse/AMBER — and that miss is the SIGNAL for which combo to derive next. Step (a) doubles as
   the discovery prioritizer.

Supersedes the central-authority options (pure data file / typed-central-C#) from the earlier schema
discussion; retains their content model (R1 polarity, R2 match-policy keyed by consume kind, R3 guards-in-
code, R4 bridges, R5 provenance/defaults) as the rollup's SECTIONS. Bootstrap: seed the rollup from the two
existing worked golds (Chatterfang×Pitiless: polarity may-be-choice + argument-cover; Deadeye×Peregrine:
self-watch guard + untap→mana bridge).

### O21 — Review closeout: combat-presence, the negation inventory, the null-Subject decision
Closes the three bounded O17 items that needed only enumeration or a decision:

- **Combat-presence (O17.3, last root straggler):** under O19 it is simply an **Event** —
  `event:combat-presence` (a permanent attacks or blocks; today's `attacksorblocks` consume is a
  subscription to it; `emit:additionalcombat` re-raises the phase that produces it). Root is now complete:
  every canonical family and every `PortWalkProjection` discriminator has a placement.
- **Negation inventory (O17.5), enumerated from `ObjectFilter`/`ObjectFilterRelations`:**
  `ExcludedCardTypes` · `ExcludedSubtypes` · `ExcludedSupertypes` · `ExcludedColors` (each a per-axis
  exclusion list with its own decider) · `IsColorless` (boolean "no colors") · `ComparisonOperator.NotEqual`
  (deciders return **Unknown** when NotEqual is on either side — safely non-committal) · the `:another` /
  exclude-self facet (an identity-flavored exclusion, handled by the O17.1 guard layer, not the filter
  lattice). All are **atomic, per-axis, decider-confined** — none introduces general boolean structure, so
  the EL-profile guarantee (O15) holds. The Decision lists exactly this set as the closed negation vocabulary.
- **Null-Subject default-GREEN (O17.2): KILL.** Decision: a port with no subject filter is an *unknown*
  subject, not a universally-compatible one — feasibility may still propose the edge, but Reliability caps
  at Unknown (→ AMBER floor) unless a confirmed rollup rule (O20) or a same-card witness certifies it.
  Rationale: consistent with open-world semantics (O15), with attribute provenance (O17.2), and with the
  O20 ladder ("nothing uncited is GREEN"). Migration note: this is a deliberate tier-tightening; the
  shadow-mode diff (O22) must list every edge that drops from GREEN, each re-judged rather than waved through.

### O22 — Migration staging (O17.4), gated
*(Promoted verbatim into the body's **Migration** section; see above. The historical open-questions and
provenance lists that closed out the log were likewise promoted into the body's **Open questions** and
**Provenance** sections — the O5 fork resolved as `creature`-with-`[token]`-attribute per O14, and the
O9/O17.2/O17.3/O17.4 items resolved per O19/O21/O22.)*
