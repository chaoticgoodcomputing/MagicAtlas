# Two-layer cycle engine: enumerate over the bounded label graph, instantiate-and-tier per candidate shape

## Status

**Proposed (2026-06-16) — landed behind an equivalence gate on `alignment/two-layer-cycle-engine`, NOT merged.** The behaviour-preserving refactor itself is implemented and proven byte-identical to the reference engine (§The equivalence gate); the three numbered **decisions** below — the display cutoff `K`, the memoization/incremental-recompute policy, and the retire-or-keep call on the old `FindCycles` — are each marked **PROPOSED — pending human ratification**. Nothing on those three is decided here; this ADR proposes and records them for human review. The branch is held for that review and is not merged.

This is the second ADR in the repo-root `docs/adr/` line ([0001](0001-versioning-and-incremental-recompute.md) is versioning-and-incremental-recompute). Like 0001, this line owns a **cross-cutting** decision: the label-cycle set is a derived index that 0001 versions, so the engine half (here) and the versioning half (0001) are deliberately complementary. The per-library ADR lines ([mast-interaction](../../libs/mast-interaction/docs/adr/) especially — [0001 the-interaction-line](../../libs/mast-interaction/docs/adr/0001-the-interaction-line.md), [0002 port-labels](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md)) continue to own decisions local to one library.

## Context

The cycle finder is the engine's expensive step. [`PortGraphEngine.FindCycles`](../../libs/mast-interaction/PortGraphEngine.cs) enumerates elementary cycles by a depth-first walk over the **per-instance** graph — one node per port instance, keyed on `PortNode.Identity` (`card::label`). Elementary-cycle enumeration is exponential worst-case **in the node count**, and the per-instance node count scales with the corpus (~100k port instances over ~38k cards). Today the corpus run is held tractable only by the ≤5-hop length bound (ADR-0001 §2 quotes *"~3s at corpus scale"*), but that bound is a band-aid over a node count that is fundamentally the wrong size.

The banked design ([`libs/mast-interaction/docs/two-layer-cycle-engine.md`](../../libs/mast-interaction/docs/two-layer-cycle-engine.md)) reframes this as an analytical-chemistry problem: **ports are atoms** (a finite, grammar-bounded vocabulary of canonical `PortLabel` leaves), **cards are molecules** (combinatorial, corpus-scale). The expensive enumeration should run over the **atoms** — the distinct-label graph — not the molecules. The measurement that licenses this is in the design doc and re-quoted by [ADR-0001 §3](0001-versioning-and-incremental-recompute.md): the `PortLabelCensus` flow finds **545 cycle-relevant labels over 29,615 cards (54× dedup)** at full scale. The cycle graph is **~545 nodes, bounded by the grammar — never corpus-scale.** The bad case (label space ≈ card count) is decisively ruled out.

Three facts shape every decision:

- **The label graph is a sound over-approximation.** The colon-match / prefix-preimage projection ([mast-interaction ADR-0002 §2/§6](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md): *"the colon-match proposes a superset… the operator only ever prunes"*) guarantees that every real instance interaction's labels connect in the label graph. So every real instance cycle projects to a closed walk in the label graph, and **no real cycle can be dropped** by enumerating shapes there first. This is the load-bearing soundness property — the whole refactor rests on it.
- **The verdict is instance-dependent, the shape is not.** Whether a candidate shape is a real GREEN/AMBER/RED loop turns on the concrete `Subject` filters (the Squirrel ⊄ creature straddle — same label, the operator decides) and the §8 firability/balance/productivity floors, which read co-costs and producers that may sit off the ring. So shape-finding (Layer 1) is label-level and cheap; tiering (Layer 2) is instance-level and precise, but runs only over the handful of card-sets a candidate produced.
- **The cycle set is the canonical derived index (ADR-0001 §3, banked design).** *"all cycles a card is in"* = `card → its labels → label-cycles → instantiate`. Incremental recompute keys on `(label, version)`; a card whose projection changes dirties only the labels it touches plus their cycle neighborhood — *tighter* than the per-card neighborhood ADR-0001 §2 currently bounds. That doc owns the versioning/invalidation/atomic-swap half; this ADR owns the engine half.

## What this branch implements (the behaviour-preserving refactor)

Two layers, in [`PortGraphEngine`](../../libs/mast-interaction/PortGraphEngine.cs):

- **Layer 1 — candidate shapes (cheap).** `LabelCycleHops` builds adjacency over **distinct labels** (an edge `A→B` iff some instance `PortEdge` runs from a label-`A` port to a label-`B` port — the deduped atom graph) and enumerates its elementary cycles with **no length bound** (the node count is grammar-bounded). It returns the set of label HOPS `(fromLabel, toLabel)` lying on any candidate cycle.
- **Layer 2 — instantiate + tier (precise).** `FindCyclesByLabelGraph` collects the **admissible instance edges** — those whose `(From.Label → To.Label)` hop appears in a candidate shape — and runs the **identical** per-instance elementary-cycle DFS (`EnumerateInstanceCycles`, factored out of the original `FindCycles`) over exactly that subset, with every §8 floor/prune still evidenced against the FULL edge set. The operator-tiering, the one-shot/bridge/counter prunes, the co-cost/balance/productivity/tap-renewal floors are unchanged — only the *node set the enumeration walks* moved.

The original `FindCycles` is **retained as the reference implementation** (its retire/keep is Decision 3) and now delegates to the shared `EnumerateInstanceCycles` with `searchEdges == allEdges`, so the reference and the two-layer engine share one DFS body — no drift between them.

### The equivalence gate (the safety proof)

The refactor is only allowed to land if it is **behaviour-preserving**, so the proof is executable and runs in CI on every batch:

- **Sentinel set** ([`PortWalkTwoLayerEquivalenceTest`](../../tests/magic-ast-tests/Tests/Interaction/PortWalkTwoLayerEquivalenceTest.cs)) — for every initiative-03 sentinel (62 cards/combos), `FindCyclesByLabelGraph` is asserted **byte-identical in the full cycle set AND in tiers** to `FindCycles`. ✅ green.
- **Bench eligible combos** ([`TwoLayerEquivalenceTest`](../../tools/bench/MagicAtlas.Bench/TwoLayerEquivalenceTest.cs)) — for every Commander-Spellbook eligible combo (33), the same byte-identical assertion. ✅ green.

**Why the equivalence is provable, not merely observed.** Every real instance elementary cycle `C` projects to a closed walk `W` in the label graph. Every edge of a directed closed walk lies on some elementary directed cycle of that graph (decompose the walk). Layer 1 enumerates all elementary label-cycles, so every hop of `W` is recorded — hence **every instance edge of `C` is admissible**. `C`'s edges are therefore all present in Layer 2's restricted DFS, which is the *same* elementary-cycle finder, so it surfaces `C` (and, being elementary, no spurious extra). The admissible set is thus provably ⊇ the edges of every real cycle and ⊆ all edges, so the restricted enumeration is exactly the unrestricted one. The two tests confirm this on real data; the argument says it must hold generally. This mirrors ADR-0001 §6 — the equivalence test *is* the safety argument made executable.

## Decision

Three pinned decisions, each **PROPOSED — pending human ratification**. The refactor + equivalence gate above are implemented; these three are the calls left for humans.

### 1. The display cutoff `K` (cards) — PROPOSED: `K = 5`, applied as a post-enumeration cards-based filter

**PROPOSED — pending human ratification.**

The old hop-length bound is demoted to a **display/query filter measured in distinct CARDS, not hops** (`displayMaxLengthInCards` on `FindCyclesByLabelGraph`; default `int.MaxValue` = unbounded enumeration). The enumeration itself runs over the unbounded label graph; `K` filters the *instance* cycles afterward, keeping only those spanning ≤ `K` distinct cards. A test ([`CardsDisplayFilterTest`](../../tools/bench/MagicAtlas.Bench/CardsDisplayFilterTest.cs)) proves the bound is in cards: a multi-hop two-card cycle survives `K=2`; every cross-card cycle is dropped at `K=1`.

- **Why a CARDS bound, not a hops bound.** A combo is a relationship between *cards* a deck-builder assembles; an intra-card ability chain can add hops without adding cards (a single multi-ability card lengthens the ring but doesn't make the combo "bigger" to a human). Bounding by distinct cards is the unit the **product viz and query surface** actually care about — *"show me 2- and 3-card combos"* — and it decouples display reach from the AST's hop granularity, which is an implementation detail that shifts as the parser sharpens.
- **Why `K = 5` proposed.** It is the cards-equivalent of the bench's current hop bound of 5 ([`ComboRecallRunner.LengthBound`](../../tools/bench/MagicAtlas.Bench/ComboRecallRunner.cs): *"a full sac→death→token→doubler→refuel loop spans five hops — the Ashnod's Altar × Pitiless × Chatterfang archetype"*). Five hops there ≈ a 3-card archetype, so `K=5` cards is **generous** — it never tightens today's reach and leaves headroom for larger combos as coverage grows. The number is a starting proposal, not a defended constant: humans should set it against the product's intended combo-size ceiling.
- **What is NOT proposed.** Whether the bench/flow should switch its hop bound to this cards bound (today `ComboRecallRunner` still uses hop-5 and the equivalence tests compare the *unbounded* enumerations, isolating the engine refactor from the filter). Migrating the bench/flow display path to `K` cards is a follow-on, recorded under Decision 3's migration.

### 2. Memoization / incremental-recompute policy — PROPOSED: the label-cycle set is the `(label, version)`-keyed derived index of ADR-0001, recomputed incrementally

**PROPOSED — pending human ratification.**

The label-cycle set (Layer 1's output) is **the** canonical derived index, built once per corpus version and queried many times, with incremental recompute keyed on `(label, version)` — exactly the index ADR-0001 §3 names. Concretely:

- **Memoize Layer 1, recompute Layer 2 on demand.** The label graph and its elementary cycles (the candidate shapes) are corpus-version-stable derived artifacts: cache them under the corpus `ContentHash` stamp (ADR-0001 §1). Layer 2 (instantiate + tier) is a bounded per-candidate computation re-run when a query needs the precise tiers for a specific card-set — it need not be globally materialized.
- **Invalidation keys on `(label, version)`, tighter than ADR-0001 §2's `(card, R-hops)`.** A card whose re-projection changes its label set dirties **only the labels it touches plus their cycle neighborhood in the (small) label graph** — not the `R`-hop instance ball. Because the label graph is bounded (~545 nodes), the label-neighborhood is tiny and cheap to recompute. This is the *tighter* invalidation ADR-0001 §3 anticipates; it supersedes the `(card, R-hops)` instance-ball bound for the cycle index specifically (the instance ball remains correct, just looser).
- **The equivalence test extends ADR-0001 §6.** The incremental-≡-from-scratch gate ADR-0001 §6 fixes is satisfied here by the same byte-identical machinery: a `(label, version)`-incremental recompute of the label-cycle set must equal a from-scratch enumeration, asserted on the sentinel set in CI.
- **This decision is explicitly DEFERRED to ADR-0001's trigger (§5).** ADR-0001 is design-only until a full re-walk exceeds 10 min or the corpus exceeds 20k cards. This memoization policy lands **with** that initiative, not before — building it now, before the schema/`SchemaHash` stabilizes, would churn (ADR-0001's own rejected "implement now" option). What this ADR proposes is the *keying* (`(label, version)`) and the *memoize-Layer-1 / recompute-Layer-2-on-demand* split; the *when* stays bound to ADR-0001 §5.

### 3. Retire or keep the old `FindCycles` — PROPOSED: KEEP as the equivalence oracle for now; retire only after a soak

**PROPOSED — pending human ratification.**

`FindCycles` (per-instance) is **kept** as the reference implementation and the equivalence oracle, **not retired in this branch**. The proposal for its eventual disposition:

- **Keep now.** The two-layer engine's whole safety argument is *"byte-identical to the reference."* Deleting the reference deletes the oracle. While the engine is new, `FindCycles` is the ground truth the equivalence gate compares against on every batch — retiring it would make the gate self-referential (comparing the new engine to itself). The shared `EnumerateInstanceCycles` body means keeping both costs no duplication of the §8 logic.
- **Retire later, after a soak.** Once `FindCyclesByLabelGraph` has run green against `FindCycles` across (a) every batch for an agreed soak window and (b) the full corpus (not just sentinels + bench), the reference can be retired: `FindCycles` becomes a thin wrapper or is deleted, callers ([`PortWalkSentinelSnapshotTest`](../../tests/magic-ast-tests/Tests/Interaction/PortWalkSentinelSnapshotTest.cs), [`ComboRecallRunner`](../../tools/bench/MagicAtlas.Bench/ComboRecallRunner.cs)) move to the two-layer entry point, and the equivalence test is repurposed to *incremental ≡ from-scratch* (Decision 2 / ADR-0001 §6) rather than *two-layer ≡ per-instance*.
- **What humans decide.** The soak length and the corpus-wide-equivalence bar that licenses retirement, and whether the eventual retirement deletes `FindCycles` or keeps it as a debug oracle. This ADR does not set those; it proposes keep-now-retire-after-soak and records the conditions.

## Considered options

- **Bound the per-instance enumeration harder (lower hop cap) instead of moving to the label graph.** Rejected: the hop cap is a band-aid over a node count that is the wrong size (corpus-scale). It trades reach for tractability — exactly the compromise the atom/molecule reframing removes. The label graph is bounded by the grammar regardless of corpus size; the hop cap fights a symptom.
- **Enumerate over the label graph and tier at the LABEL level (skip instantiation).** Rejected: the verdict is instance-dependent (the Squirrel ⊄ creature straddle, the §8 floors reading off-ring co-costs/producers). A label-level tier would lose the operator's precision — the very thing [mast-interaction ADR-0002 §7](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md) ("the label names, the operator decides") forbids. Layer 2 must instantiate.
- **Land the refactor without the byte-identical gate (trust the soundness argument).** Rejected: the task's own STOP-criterion. A behaviour-changing engine refactor that slips a tier flip past review is exactly the regression the gate exists to catch; the proof must be executable, on real data, in CI — not a paragraph.
- **Make the display bound hops, matching today's `LengthBound`.** Rejected: hops are an AST-granularity detail that shifts as the parser sharpens; cards are the unit the product and humans reason in. The cards bound decouples display reach from parse granularity (Decision 1).
- **Retire `FindCycles` immediately (it's redundant once two-layer is green).** Rejected for v1: it is the equivalence oracle; deleting it makes the safety gate self-referential. Keep through a soak (Decision 3).
- **Materialize Layer 2 globally (precompute every candidate's tiers).** Rejected as the v1 memoization: Layer 2 is a bounded per-candidate computation cheap to run on demand; globally materializing it re-introduces corpus-scale storage for marginal query benefit. Memoize the cheap, version-stable Layer 1; recompute Layer 2 per query (Decision 2).
- **Johnson + SCC decomposition for Layer 1 now.** Deferred (not rejected): the design doc names it as the escalation lever *if the label graph + SCCs ever grow past CPU tractability.* The measurement (545 nodes, small SCCs) says that is distant; the simple rooted-DFS enumeration is adequate today. GPU-parallel tiering is a further, noted-not-committed lever.

## Consequences

- **`PortGraphEngine` gains** `FindCyclesByLabelGraph` (the two-layer entry point), `EnumerateInstanceCycles` (the shared DFS body), and `LabelCycleHops` (Layer 1). `FindCycles` is retained, delegating to the shared body. No §8 logic is duplicated or changed.
- **Two equivalence tests are added** and run in CI on every batch — the sentinel-set half (mast-tests) and the bench-eligible-combos half (bench). Both are byte-identical-or-fail. A `CardsDisplayFilterTest` proves the display bound is in cards.
- **The display bound is demoted to cards** (Decision 1); the enumeration is unbounded. Callers that want a bound pass `K` cards; the default is the full set.
- **No incremental-recompute code lands here** (Decision 2 defers to ADR-0001 §5's trigger). What lands is the *engine shape* that makes the `(label, version)` keying possible.
- **`FindCycles` is not retired** (Decision 3). The branch is held for human review of the three PROPOSED decisions and is **not merged**.
- **Nothing on the three decisions is enacted** beyond what the equivalence-gated refactor needs. The artifact of this initiative is this document + the behaviour-preserving engine + the gate.

## Implementation status (this branch)

- [x] Layer 1 — `LabelCycleHops` over the distinct-label graph, unbounded length.
- [x] Layer 2 — `FindCyclesByLabelGraph` instantiate-and-tier over admissible instance edges; shared `EnumerateInstanceCycles`.
- [x] `FindCycles` retained as the reference, delegating to the shared body.
- [x] Equivalence gate — byte-identical tiers on the sentinel set AND the bench eligible combos. **Green.**
- [x] Length-bound demoted to a cards-based display filter; proven by test.
- [x] Full build + suite green (mast-tests, bench).
- [ ] Decision 1 — ratify `K` (proposed 5 cards) and migrate the bench/flow display path. **PROPOSED.**
- [ ] Decision 2 — ratify the `(label, version)` memoization policy; implement at ADR-0001 §5's trigger. **PROPOSED.**
- [ ] Decision 3 — ratify keep-now-retire-after-soak; set the soak/corpus-wide bar. **PROPOSED.**
