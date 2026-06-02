# The interaction line: a judged edge-grammar over labelled ports

## Status

Proposed (2026-06-01) — foundational; no implementation yet.

## Context

Downstream of MAST (which *describes* cards, [ADR 0004](../../../magic-ast/docs/adr/0004-ast-engine-line.md)) and mast-query (which *matches* shapes, [ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)) sits the consumer both ADRs named and deferred: the interaction project. mast-query [ADR 0001](../../../mast-query/docs/adr/0001-queries-are-runnable-fixtures.md) explicitly hands it "the join layer (cross-query captures, `emits` / `listens` event matching, `intersects` overlap) and the induced resource ontology." This ADR opens that project and fixes its topology before any code is written.

It is a **purely analytical** engine. It will never hold board state, a hand, or the stack; it never *resolves* anything. The only computation that resembles execution is symbolic resource accounting summed around a cycle — arithmetic, not simulation. It references MAST and mast-query shapes; it never extends them. Its product is **edges** — a relationship index over the card corpus. Cycles (combos) are one query run over those edges, a knock-on, not the point.

Four forces shape the design:

- **Cards are multi-role.** Chatterfang is simultaneously a sacrifice outlet (its activated cost), a token doubler (its replacement), and sacrifice fodder (it is a creature) — three roles through three sub-trees of one card. The unit that interacts is therefore contested.
- **Generalisation is the whole value-add.** Going from "card A interacts with card B" to "**family** A interacts with **family** B" is the only thing that exceeds Commander Spellbook (CSB), whose corpus is user-submitted *card pairs*. A system that only learns card pairs cannot beat the source it learned from.
- **Partial parse.** ~34% of cards fully parse; `IUnparsed` regions are everywhere. Three-valued match ([ADR 0001](../../../mast-query/docs/adr/0001-queries-are-runnable-fixtures.md)) must propagate all the way to cycles.
- **Blast radius.** A wrong parser rule mis-parses some cards. A wrong **interaction rule** is a quantified claim over the whole corpus — it mis-connects *thousands* of card pairs. The soundness bar here is higher than MAST's.

## Decision

Seven pinned parts.

### 1. Analytical scope, downstream-only

No state, no resolution, no priority. Dependencies flow one way: `mast-interaction → mast-query → MAST schema export`, never the reverse. The deliverable is the edge graph; cycle-finding is a downstream consumer of it. The engine **overgenerates by design** — it emits *candidate* interactions for verification, never proofs, because MAST describes and does not execute.

### 2. The labelling unit is the **port**, not the card

A **port** is an addressable ability sub-tree in a role, with a computed `{ consumes, emits-resources, emits-events, listens }` projection. A card is a **bag of ports**; `card ∈ family F` is the *projection* "some port of the card matches F" — adequate for atlas/search, never for the graph.

Card-level labelling fails concretely. Chatterfang's outlet sacrifices **Squirrels** specifically (`SacrificeCost` filter = Squirrel), and the loop closes **only because** its doubler emits **Squirrels**, refilling that exact requirement — a generic token doubler would not close it. A card-level label cannot represent "this sub-tree's filter is Squirrel," so it cannot evaluate the join. Ports with their filters can.

This makes a **port-identity scheme** a hard prerequisite (the AST assigns no node identity today): a port is keyed by `(card, ability-path, role)` or a canonical-subtree hash ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md) canonical serialisation).

### 3. Topology: a grammar that generates a parse

Five layers, with a sharp **authored / derived** line:

| Layer | What | Authored & judged? |
|---|---|---|
| Ports | ability sub-trees with emit/consume/listen sets | derived (project cards through the port primitives) |
| Families | query-defined labels over **ports** | query **authored**; membership derived |
| Family edges | directed `label → label` + `{ resource, edge-family, join }` | **authored & judged** — the source-of-truth artifact |
| Port-instance graph | port→port edges where a family edge's join holds | derived (materialised view) |
| Cycles | loops + net-resource accounting + certainty tier | derived (a query over the graph) |

The **family-edge graph is a grammar of interactions** — small, hand-curated, rules-judged, in git, dozens-to-hundreds of edges. The **port-instance graph is the parse of the corpus through that grammar** — large, derived, a build artifact, regenerated, never hand-edited. You author and judge the grammar; you query the parse. This is MAST's own shape one level up: author parser rules → generate ASTs.

The derived graph is a snapshot content-hashed to `(MAST-version, query-set, family-edge-set)`. Three-valued logic propagates down it: a port over an `IUnparsed` region is `Unknown`; an edge touching it is `Unknown`; a cycle through it is *candidate-pending-parse* — a MAST-coverage signal, not an interaction result.

### 4. Two edge families, joined differently

- **Flow edges** — A emits resource/event R, B consumes R. Join = `intersects(filter_A, filter_B)`, the MAST-owned `ObjectFilter` overlap operator ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)). Covers mana, tokens, counters, and events-as-resources (death, ETB, LTB, sacrifice, cast…).
- **Modifier edges** — A rewrites how B's port behaves (doublers, replacements, cost modification, trigger amplification). Join = an interception match ("does B emit an object/event A intercepts?"). Chatterfang's replacement is a modifier edge against *every* token creator.

The join is **typed by edge family** and **evaluated per port-pair at expansion** — never a blanket cartesian product of the two families. The `intersects` prune is what keeps the parse honest.

### 5. Interactions are runnable fixtures

Inheriting [mast-query ADR 0001](../../../mast-query/docs/adr/0001-queries-are-runnable-fixtures.md): family definitions and family edges are **declarative JSON data**, versioned, diffable, judged, three-valued. A family edge is two mast-query patterns plus a resource label and a join — it adds no new matching machinery, only the deferred join layer. Port labels and the instance graph are *derived*, materialised offline, never authored.

### 6. Triage has two axes

- **AST-intrinsic — the novelty engine.** Computed from the corpus itself, combo-free: **unported nodes** by frequency (the `IUnparsed` analog — a node type with no port projection is a black hole) and **unbridged resources** by yield ceiling `|emitters(R)| × |consumers(R)|` (the `directYield` analog; an *upper* bound — `intersects` narrows it, the same over-count MAST's directYield triage already learned to read as a ceiling). The steady-state driver. The **vertical slice is one resource**: port its emitters, port its consumers, bridge them. This axis is the *only* source that can find edges CSB does not know.
- **CSB-extrinsic — a prior, never a grader.** The `variants.json` bulk export (snapshot-pinned) supplies popular and emergent interactions "deeper than surface rules." Its `features` seed the ontology (roles → ports, results → cycle-outputs), its `templates` are family patterns pre-written in prose, its `popularity` orders work, its `?q=<card>` drives per-card deep-dives. **CSB is a ceiling if used as the edge source** — you cannot exceed an oracle you learn from. It prioritises and seeds; the rules-judge grades.

### 7. A two-tier test pyramid with an independent rules-judge

- **Property test (per primitive)** — sample N ports from each of families A, B on either side of an edge and grade **agreement** between the engine's per-cell prediction and the judge's ruling — *not* "all N×N interact" (a sound broad primitive has legitimate `intersects`-holes in its product; demanding universal interaction rejects it). Asymmetric bar: **zero false positives** (over-claim — blast radius), false negatives tracked as coverage backlog. Sample **adversarially** toward family boundaries (uniform sampling has no power against over-claims). Every failure is **attributed** (edge logic vs A-membership vs B-membership). The engine fills the matrix free; the judge audits a sample; `Unknown` cells are excluded; CSB pairs seed required-positive cells.
- **Reconstruction test (per known loop)** — a known combo's hand-annotated resource-flow must reconstruct from the grammar. A **dark hop** (an annotated hop no primitive derives) is the gap signal. Outcomes route work: **GREEN** (certain), **AMBER** (closes only through an `Unknown` region → a MAST-loop task, not ours), **RED** (dark even assuming full parse → a missing primitive, ours). No-ratchet: every gold green to land.
- **The judge** is rules-grounded against the same sources `mast-judge` uses (glossary.json + rules-structure.json), **independent of CSB** (no train-and-grade circularity), **stricter than `mast-judge`** (blast radius), and runs a **regression** across all golds on every addition. The judge owns exact CR citations; this ADR cites rule *areas* illustratively.

Metrics: **port + bridge coverage** (intrinsic, bulk) is the primary observability; **known-combo reconstruction rate** is the calibration that licenses trust in de-novo cycles.

## Considered options

- **Label cards, not ports.** Rejected: loses role attribution on multi-role cards, cannot evaluate filter joins (the Squirrel-specificity case), and manufactures false self-loops.
- **Family edges as descriptive metadata; expand by cartesian product.** Rejected: a blanket A×B is the false-edge explosion. The join must be evaluated per port-pair.
- **CSB as benchmark / grader.** Rejected on two counts: circular (a source used to both train and grade overfits) and a ceiling (cannot exceed the oracle). CSB is a prioritisation and sampling prior; the rules-judge grades.
- **"All N×N must interact" as the property-test bar.** Rejected: rejects sound broad primitives whose family product legitimately excludes some pairs. Grade agreement; be strict on false positives only.
- **Absorb the engine into MAST or mast-query.** Rejected (consistent with [0004](../../../magic-ast/docs/adr/0004-ast-engine-line.md) / [0008](../../../magic-ast/docs/adr/0008-the-query-line.md)): re-imports consumer concerns those lines expelled; the induced resource ontology has no honest home upstream.
- **A real (state-holding) rules engine.** Rejected by scope: analytical only. Overgeneration is accepted and pruned by accounting + judge; outputs are candidates for verification.

## Consequences

- `libs/mast-interaction/` is created (`MagicAST.Interaction.csproj`), depending on mast-query and the MAST schema export — never the reverse.
- New infrastructure: a **port-identity scheme**. Everything else reuses MAST + mast-query.
- MAST must **build** the `intersects` / subsumption operator over `ObjectFilter` — slated by [ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md) but **not yet implemented** (`ObjectFilter` is today a plain data record with no overlap method). It is the flow-edge join predicate and therefore this project's **critical-path prerequisite**: until it exists, no flow edge can be evaluated, and the reconstruction tests cannot run. It is a MAST change, landed through the MAST TDD loop and `mast-judge` — not this loop — and it is non-trivial (overlap over ~25 axes, including the relational `SharesColorWith` / `ExiledWith` / `AttachedTo` / `History` axes).
- The frozen reference corpus gains a **pinned CSB `variants.json` snapshot** (refreshed as a reviewable diff, never a live dependency) and reference combos. The **canonical worked gold is Chatterfang, Squirrel General × Pitiless Plunderer** — a mana- and fodder-neutral free loop whose closure turns on Chatterfang sacrificing *Squirrels* and its doubler emitting *Squirrels*, exercising port-granularity, a flow edge (death → Treasure → mana), and a modifier edge (token-creation → doubler) in one fixture.
- Derived artifacts (port labels, the instance graph) are build outputs, content-hashed, not source-of-truth in git.
- A follow-on `interaction-tdd-loop` skill and an `interaction-judge` mirror the existing MAST trio; their contracts are deferred to their own docs.
- Thematic **archetypes can be induced as dense clusters over the family-edge graph** (aristocrats ≈ the sac-outlet ↔ death-payoff ↔ drain neighbourhood), unifying the interaction and clustering consumers of mast-query rather than duplicating them. The family graph, its edges, and any induced labels are **materialised as Postgres rows served by `atlas-api` over GraphQL — the path `AtlasPointRow` already proves** — *not* added to the `atlas-flows` similarity map, which is a sibling embedding pipeline that explicitly disclaims label/archetype overlays.
