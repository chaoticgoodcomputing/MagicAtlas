# Versioning and incremental recompute: a content-hash stamp, a cycle-radius-bounded dirty set, and atomic snapshot swaps

## Status

**Proposed (2026-06-15) — design-only.** Every numbered decision below is marked **PROPOSED — pending human ratification**; nothing here is implemented and no code lands on this ADR. It draws the version semantics that the spec ([`docs/scratch/alignment-session/07_versioning-incremental-recompute.md`](../scratch/alignment-session/07_versioning-incremental-recompute.md)) requires *before* the three implementation tracks (§Implementation plan) can proceed independently. It is the first ADR in the repo-root `docs/adr/` line; the per-library ADR lines ([magic-ast](../../libs/magic-ast/docs/adr/), [mast-query](../../libs/mast-query/docs/adr/), [mast-interaction](../../libs/mast-interaction/docs/adr/), [atlas-flows](../../libs/atlas-flows/docs/adr/)) continue to own decisions local to one library; this line owns **cross-cutting** decisions that bind three libraries at once — which versioning-and-recompute is.

This is a **priority-7, lowest** initiative: design now, implement when the data says so (§Trigger condition). Building it before initiatives 03/05 settle the schema would be wasted motion. The deliverable today is this document.

## Context

Three gaps, stated by the spec, all rooted in one missing fact — *what version produced this artifact*:

1. **No schema version on stored artifacts.** [`CardOutputAST`](../../libs/magic-ast/CardOutputAST.cs) carries `Name`, `TypeLine`, `Oracle`, `Attributes`, `Faces` — and **no version field at all**. Derived artifacts (port graphs, edges, cycles, the Postgres materialization) carry none either. Correctness today rests entirely on *"regenerate everything every time."* When a node shape changes, a stored AST is silently stale and nothing detects it.
2. **No incremental recompute.** Every parser change forces a full interaction re-walk — projection → flow edges → cycle enumeration via [`PortGraphEngine`](../../libs/mast-interaction/PortGraphEngine.cs). Acceptable at today's ~10–50k edges and minutes-scale runs; untenable at full-corpus coverage (~38k cards) with continuous concurrent batches, where it couples batch cadence to the slowest global recompute.
3. **Cross-version snapshots unmanaged.** The materialization path is a Flowthru flow that writes a JSON artifact (e.g. `AtlasPoints`), which [`AtlasSeeder`](../../apps/atlas-api/Seed/AtlasSeeder.cs) loads into Postgres tables in the `atlas` schema ([`AtlasDbContext`](../../apps/atlas-api/Data/AtlasDbContext.cs)). The seeder is **idempotent-by-emptiness** — *"if any rows exist in a given table, that table is skipped"* — with **no atomic-swap story**. A re-materialization that truncates-then-refills exposes readers to a half-written, mixed-version corpus.

Two existing facts shape every decision:

- **`AstSchema` already has the version primitive.** [`AstSchema`](../../libs/magic-ast/Schema/AstSchema.cs) carries a monotonic `int SchemaVersion` (*"bumped when the export's shape changes"*) **and** a `string? SchemaHash` — SHA-256 of the canonical, hash-excluded body, computed by [`SchemaExport.ComputeHash`](../../libs/magic-ast/Schema/SchemaExport.cs) over a deterministically-sorted projection. The schema *contract* is already content-hashed; what is missing is stamping *instances* (parsed cards and everything derived from them) with that handle.
- **The cycle-finder is a bounded-length DFS.** [`PortGraphEngine.FindCycles`](../../libs/mast-interaction/PortGraphEngine.cs) enumerates elementary cycles by a depth-first walk with `maxLength` hops, rooted at each port's lowest-identity node, pruning on `string.CompareOrdinal(toId, startId) > 0` and an `onPath` set. The corpus run is bounded at **≤5 hops** ([ADR 0002 §8](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md): *"a sac→death→token→doubler→refuel loop is five hops, ~3s at corpus scale"*). **This bound is the lever** that makes a tight, provably-conservative invalidation neighborhood possible (§2).

The interaction engine is **already over-approximate by design** ([ADR 0001 §1](../../libs/mast-interaction/docs/adr/0001-the-interaction-line.md): *"overgenerates by design"*; [ADR 0002 §6](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md): *"the colon-match proposes a superset… the operator only ever prunes"*). An invalidation policy that over-approximates the dirty set is therefore **in keeping with the engine's existing epistemic posture**, not a new compromise: recompute too much, never too little.

## Decision

Six pinned parts. The four spec design-points are decisions 1–4; decision 5 is the trigger condition; decision 6 is the equivalence test that *is* the safety argument for incrementality.

### 1. Version stamp: a content hash, carried on every artifact, rejected on mismatch under Strict

**PROPOSED — pending human ratification.**

The stamp is a **content hash, not a monotonic integer** — specifically a `SchemaStamp { int SchemaVersion; string SchemaHash; }` value pair, where `SchemaHash` is exactly the SHA-256 handle [`AstSchema`](../../libs/magic-ast/Schema/AstSchema.cs) already computes. The `int SchemaVersion` rides along as a **human-readable, monotonic dewey** for diffs and logs; the **hash is the authority** for any equality/staleness decision.

- **Why hash-primary, not int-primary.** A monotonic int requires a human to remember to bump it on every shape change — exactly the discipline that fails silently (the bug we are fixing is *silent* staleness). A content hash is **computed from the artifact's own definition**, so it cannot drift out of sync with the thing it stamps: the machinery that produces `AstSchema.SchemaHash` already does this for the schema contract, deterministically (sorted collections, canonical compact JSON, hash-excluded body). We **reuse that exact mechanism** rather than invent a parallel one. The int is advisory; the hash is load-bearing.
- **Two layers of hash, both stamped.** A derived artifact's staleness has two independent causes — the *schema shape* changed, or *this card's parse* changed — so the stamp carries both:
  - **`SchemaHash`** — the [`AstSchema`](../../libs/magic-ast/Schema/AstSchema.cs) hash, identifying the node-model shape. One value for the whole corpus in a given build.
  - **`ContentHash`** — the SHA-256 of the canonical-serialized artifact body (the same canonicalization discipline: sorted, compact, hash-field excluded). Per-card for an AST; per-derived-artifact otherwise. This is the `version` in the spec's *invalidation keyed on `(card, version)`* (§2).
- **Where it lives in the serialized JSON.** A new top-level `Stamp` property on [`CardOutputAST`](../../libs/magic-ast/CardOutputAST.cs) (`SchemaStamp { SchemaVersion, SchemaHash, ContentHash }`), and the **same `Stamp` shape on every derived artifact** — the port graph, the edge set, the cycle set, and each Postgres-materialized row-batch (carried as a snapshot-level column/sidecar, §3, not per-row, to avoid bloating ~38k×N rows). `ContentHash` over an AST is computed with the hash field excluded, mirroring `SchemaExport.ComputeHash`'s `schema with { SchemaHash = null }` step exactly.
- **How `MagicASTJsonOptions.Strict` treats a mismatch: REJECT, never silently migrate.** [`MagicASTJsonOptions.Strict`](../../libs/magic-ast/MagicASTJsonOptions.cs) already *"disallows unmapped members… respects required constructor parameters"* — its whole stance is *a malformed artifact is a loud failure, not a silent best-effort.* Version mismatch joins that stance: deserializing an artifact whose `SchemaHash` ≠ the running build's `AstSchema.SchemaHash` throws (a typed `StaleArtifactException` carrying both hashes), it does not coerce or migrate in place. Migration is a separate, explicit orchestration step (§4) — never a side effect of a read. `MagicASTJsonOptions.Web` (lenient, for external data) keeps reading anything, because external data was never stamped by us; the Strict↔Web split already encodes *our artifacts are checked, foreign data is tolerated*, and we extend it rather than redraw it.

### 2. Invalidation keyed on `(card, ContentHash)`, with the dirty set bounded by the cycle-search radius

**PROPOSED — pending human ratification.**

A batch that changes the parse of a set of cards `Δ` (detected as *the cards whose recomputed `ContentHash` differs from the stamped one*) recomputes **only the dirty neighborhood**, defined precisely as:

> **Dirty set `D(Δ)`** = the union, over every port `p` projected from a card in `Δ`, of every artifact (port, edge, cycle) lying within **`R` rules-defined hops** of `p` in the materialized port-instance graph, where **`R = maxLength − 1`** is the cycle-search radius — `maxLength = 5` today, so **`R = 4`**.

The recompute procedure is then: (a) re-project ports for every card in `Δ`; (b) re-materialize edges incident to any port within `R` hops of a changed port; (c) re-run `FindCycles` **seeded only from start-nodes within `R` hops of a changed port**, not the whole graph.

**The bound argument — why `R` hops is a provable over-approximation of the true dirty set.** A cycle that `FindCycles` can surface has at most `maxLength` edges (the `path.Count < maxLength − 1` guard, plus the closing edge). For a *changed* port to affect whether some cycle `C` exists or what tier it carries, that port must be **on `C` or feed an edge of `C`**. Every node on an elementary cycle of length ≤ `maxLength` is within `maxLength − 1 = R` undirected hops of every *other* node on that cycle (walk the rest of the ring). Therefore:

> If a card in `Δ` changes a port `p`, every cycle whose existence or tier `p` can alter contains a node within `R` hops of `p`. Re-seeding `FindCycles` from the `R`-hop ball around every changed port re-derives **every** affected cycle. The `R`-ball is thus **provably ⊇ the true dirty set** of cycles. ∎

This is an over-approximation on **three** counted axes, each acceptable and each in line with the engine's existing overgenerate-then-prune posture:

1. **Undirected ball over a directed graph.** `FindCycles` traverses *directed* edges; the `R`-ball is computed **undirected** (a changed port can be *upstream* of a cycle's start node, reachable only by walking edges backward). Undirected reachability is a strict superset of the directed dirty set — safe, slightly wide.
2. **Per-edge tier vs whole-cycle verdict.** A changed port can flip a *per-edge* `Reliability`/`Overlap` ([`AddRulesEdge`](../../libs/mast-interaction/PortGraphEngine.cs)) without touching cycle topology, and the cycle-level floors (`CoCostsSatisfied`, `Balanced`, `Productive`, `TapRenewed`, the one-shot/bridge/counter prunes — [ADR 0002 §8](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md)) read co-costs and producers *that may sit a hop or two off the ring*. The `R`-ball already contains the ring **and** its 1-hop co-cost/producer fringe, because those fringe nodes are themselves within `R` of a ring node — but to be safe against the co-cost map (`CoCostMap` joins costs sharing an effect, an intra-card relation), we **also dirty every port on any card in `Δ`** (not just the changed port), absorbing same-card co-cost ripples for free.
3. **Modifier edges fan out.** A doubler/replacement intercept ([`Materialize` step 4](../../libs/mast-interaction/PortGraphEngine.cs)) connects an emit to *every* matching intercept — a high-degree node. The `R`-ball naturally includes this fan-out; it does mean a change near a Chatterfang-class modifier dirties more than average, which is correct (it *does* affect more cycles) and still bounded by `R`.

**Consequence for the keying.** Invalidation keys on `(card, ContentHash)`: a card whose recomputed `ContentHash` is unchanged contributes **nothing** to `Δ`, even if it was re-parsed (re-parse is cheap and idempotent — §4). Only a genuine parse change enters `Δ` and seeds the `R`-ball. The neighborhood is `R`-bounded, the radius is read **directly from `maxLength`** so the two can never drift, and the over-approximation is stated, counted, and justified rather than hoped.

### 3. Atomic materialization: versioned snapshots behind a single `current` pointer swap

**PROPOSED — pending human ratification.**

Readers must **never** see a mixed-version corpus. The mechanism is **versioned snapshot tables plus an atomic pointer swap**, replacing the [`AtlasSeeder`](../../apps/atlas-api/Seed/AtlasSeeder.cs) idempotent-by-emptiness/truncate-and-refill model:

- A materialization writes into **freshly-created, version-suffixed tables** (e.g. `atlas.atlas_points__<SchemaHash-prefix>`, or a numbered snapshot id) — never into the live tables a reader is querying. The write is invisible to readers because they bind to the *pointer*, not the suffixed name.
- A single **`current` pointer** (a one-row `atlas.snapshot_pointer` table, **or** a Postgres updatable view / `search_path` swap / table-rename inside one transaction) names the snapshot readers see. The swap is **one atomic DDL/DML transaction**: point `current` at the new snapshot, commit. Postgres MVCC guarantees every reader sees either the wholly-old or the wholly-new snapshot — never a torn mixture.
- The snapshot's `Stamp` (§1) is recorded on the snapshot-pointer row, so *"which version is live"* is a single query, and the staleness gate (spec, tests §3) can assert *the live snapshot's `SchemaHash` equals the current build's*.
- Old snapshots are retained for **one** generation (cheap rollback: re-point `current` back) and garbage-collected beyond that — this is **not** historical archaeology (out of scope), just a safe swap with a rollback step.

Rationale: the swap must be atomic *at the boundary the reader observes*, and the only such boundary Postgres gives us cheaply is a single committed transaction. A pointer indirection turns *"rematerialize the corpus"* from a long, observable, torn write into a long invisible write **plus** an instant visible flip. This is the database-shaped dual of decision 1's *reject-don't-coerce*: readers get a coherent version or the previous coherent version, never a coercion.

### 4. Migration policy: re-parse from oracle text, never migrate stored JSON

**PROPOSED — pending human ratification.**

When a node shape changes, stored ASTs are regenerated by **re-parsing from oracle text**, not by transforming stored JSON.

- **Oracle text is the durable source; a stored AST is a cache.** The parser ([`CardParser`](../../libs/magic-ast/Parsing/CardParser.cs)) is a pure function of oracle text + the current grammar. Re-parsing reproduces the *current* schema's AST by construction — it can never produce a shape the current code can't read, which a hand-written JSON migration always can. The spec states this directly: *"Re-parse is likely correct — oracle text is the durable source — making 'migration' a re-parse orchestration problem."* We adopt it as the decision, not a leaning.
- **This makes migration a re-parse *orchestration* problem, which is exactly what decision 2 cheapens.** Because re-parse is the migration, a schema bump invalidates the whole corpus's `SchemaHash`, and decision 2's `(card, ContentHash)` keying turns that into: *re-parse every card, but only the cards whose `ContentHash` actually changed enter `Δ` and trigger downstream recompute.* A schema change that doesn't alter most cards' serialized shape costs a full re-parse (cheap, ~minutes) but a **small** downstream re-walk — the design's whole payoff. There is **no JSON-migration code to write, test, or keep in sync with the schema** — that maintenance burden simply does not exist.
- **No "historical AST archaeology."** Old artifacts are regenerable from oracle text (spec, out-of-scope); we keep no migration ladder and no old-shape readers. The one generation of retained snapshots (§3) is for rollback, not history.

The cost we accept: re-parsing 38k cards on every schema bump even when only the node *shape* (not most cards' content) changed. At minutes-scale parse time this is well within budget, and decision 2 ensures the *expensive* part (interaction re-walk) stays proportional to genuine `ContentHash` churn, not to the re-parse.

### 5. Trigger condition: implement when a full re-walk exceeds **10 minutes** OR the parsed corpus exceeds **20,000 cards**

**PROPOSED — pending human ratification.**

This initiative is design-only until **either** threshold trips, recorded so the implementation start is data-driven, not a vibe:

- **`X = 10 minutes` of full interaction re-walk** (projection + flow edges + `FindCycles` at corpus scale). Reasoning: the spec pegs *today's* runs at *"minutes-scale"* and *"~10–50k edges,"* with the ≤5-hop corpus `FindCycles` at *"~3s at corpus scale"* ([ADR 0002 §8](../../libs/mast-interaction/docs/adr/0002-port-labels-are-deterministic-ast-projections.md)). A full re-walk that crosses **10 minutes** is the point at which it stops fitting inside a single interactive TDD-loop iteration and starts **serializing concurrent batches** behind one global recompute (the spec's stated failure mode). 10 min is ~2× a generous current "minutes-scale" run — close enough to act before it bites, far enough not to build speculatively.
- **`Y = 20,000` parsed cards.** Reasoning: the full-corpus target is *"~38k cards"* (spec). Re-walk cost grows super-linearly in corpus size (the flow-edge step is a filtered cartesian over emits×consumes — [`Materialize`](../../libs/mast-interaction/PortGraphEngine.cs) — and cycle enumeration grows with graph density), so the re-walk-minutes curve bends up well before 38k. **20k ≈ the halfway mark to full coverage** and a conservative point at which the `X` threshold is likely to trip imminently; crossing it is the leading indicator that licenses starting the build *before* the minutes wall is hit. Whichever of `X`/`Y` trips first is the trigger.

Both numbers are **measurements to record**, not guesses to defend: the implementation should begin by instrumenting the current full re-walk (wall-clock + parsed-card count per batch) so the threshold crossing is observed, logged, and the start decision is auditable.

### 6. The equivalence test is the safety argument — incremental must equal from-scratch, in CI, on every batch

**PROPOSED — pending human ratification.**

Incrementality is only sound if it provably matches a full re-walk. The **invalidation-correctness test** (spec, tests §2) is therefore a **first-class, non-negotiable gate**, fixed now even though it runs later: on the sentinel set (initiative 03), change one card's AST, then assert *the incrementally-recomputed edge/cycle set is byte-identical (by `ContentHash`) to a from-scratch full re-walk.* This equivalence **is** the safety argument for the `R`-ball over-approximation (§2) — it empirically confirms the bound is `⊇` the true dirty set on real data — and it **must run in CI on every batch**, alongside the version round-trip test (an old-stamped artifact is rejected, never silently consumed — §1) and the staleness gate (initiative 01 preflight fails if any committed derived artifact's `SchemaHash` lags the current schema — §3).

## Considered options

- **Monotonic-int version as the authority (hash advisory).** Rejected: the bug is *silent* staleness, and a human-bumped int is precisely the discipline that fails silently. The hash is computed from the artifact, so it cannot forget to change. Keep the int as a readable dewey only.
- **Migrate stored JSON in place on a shape change.** Rejected: a JSON migration can produce a shape the current parser would never emit, must be written/tested/kept-in-sync per schema bump, and duplicates the parser's job. Oracle text is the durable source; re-parse is the migration (§4).
- **Coerce/best-effort-read a mismatched artifact under Strict.** Rejected: contradicts `MagicASTJsonOptions.Strict`'s existing *loud-failure* stance and re-introduces silent staleness through the back door. Reject with a typed exception; migrate only as an explicit step.
- **Truncate-and-refill the live Postgres tables (today's seeder model).** Rejected: exposes readers to a half-written, mixed-version corpus — the exact gap the spec names. Versioned snapshots + atomic pointer swap (§3).
- **A precise, exact dirty set (transitive closure of *actually-affected* cycles).** Rejected as the v1 keying: computing the exact set requires the very cycle enumeration we are trying to avoid, and gives back only a constant-factor saving over the `R`-ball. The `R`-ball is provably ⊇ the true set, reads its radius straight from `maxLength`, and matches the engine's existing over-approximate posture. The equivalence test (§6) keeps it honest.
- **Unbounded-radius invalidation (re-walk the connected component).** Rejected: a single high-degree modifier node (Chatterfang-class intercept) can make the component nearly the whole graph, collapsing incrementality back to a full re-walk. The `maxLength`-derived `R` bound is what keeps the neighborhood small; an unbounded radius throws that away.
- **Per-row version stamps in Postgres.** Rejected for the materialization: stamping ~38k×N rows bloats every row and buys nothing the snapshot-level `Stamp` (§3) doesn't — a snapshot is internally single-version by construction (it was written by one build).
- **Implement now (don't wait for a trigger).** Rejected by the spec's own priority-7/design-only framing: before 03/05 settle the schema, the `SchemaHash` it keys on is itself unstable, so the machinery would churn. Wait for the measured trigger (§5).

## Consequences

- **`docs/adr/` is created** as the repo-root, cross-cutting ADR line; this is `0001`. Library-local ADR lines are unchanged.
- **magic-ast** gains a `SchemaStamp` value type and a `Stamp` property on [`CardOutputAST`](../../libs/magic-ast/CardOutputAST.cs) (and `CardFaceAST` if faces are independently cacheable), plus a `ContentHash` computation reusing [`SchemaExport.ComputeHash`](../../libs/magic-ast/Schema/SchemaExport.cs)'s canonicalize-then-SHA256 discipline. `MagicASTJsonOptions.Strict` gains stamp-mismatch rejection (a `StaleArtifactException`); `Web` is unchanged.
- **mast-interaction** gains `(card, ContentHash)`-keyed invalidation and an `R`-hop neighborhood walk that reads `R = maxLength − 1` from [`PortGraphEngine`](../../libs/mast-interaction/PortGraphEngine.cs), so the bound tracks any future `maxLength` change automatically. `FindCycles` gains a seeded-start overload (enumerate only cycles touching a given start-node set).
- **atlas-flows / atlas-api** replace the idempotent-by-emptiness seed with versioned snapshot tables + a `current` pointer; the swap is one atomic transaction, with one generation retained for rollback. [`AtlasSeeder`](../../apps/atlas-api/Seed/AtlasSeeder.cs) is restructured around write-new-then-swap rather than skip-if-nonempty.
- **CI** gains three gates (§6): version round-trip (reject stale), invalidation-correctness (incremental ≡ full on the sentinel set), and the staleness preflight (no committed artifact lags the schema). The equivalence test is the load-bearing one — it is the safety proof for the over-approximation made executable.
- **Nothing is built until the trigger (§5) trips.** The artifact of this initiative, today, is this document and the three sized tracks below.

## Implementation plan (sized; for later conversion to issues)

Three **largely independent** tracks once this ADR fixes the version semantics (the spec's stated parallelization). Each is one tracer-bullet vertical slice plus follow-ons.

### Track A — Stamping (magic-ast) — ~S/M

1. `SchemaStamp { int SchemaVersion; string SchemaHash; string ContentHash }` value type; `ContentHash` computed by a `ComputeContentHash(CardOutputAST)` reusing `SchemaExport`'s canonical-compact-SHA256 discipline (hash field excluded). **[tracer]**
2. Add `Stamp` to `CardOutputAST` (+ `CardFaceAST`); stamp on parse.
3. `MagicASTJsonOptions.Strict`: reject on `SchemaHash` mismatch with a typed `StaleArtifactException`; `Web` unchanged.
4. Version round-trip test (old-stamped artifact rejected deterministically).

*Independent of B and C; depends only on the existing `AstSchema.SchemaHash`.*

### Track B — Invalidation (mast-interaction) — ~M/L

1. `(card, ContentHash)` change detection: `Δ` = cards whose recomputed `ContentHash` differs from the stamped one. **[tracer]**
2. `R`-hop undirected neighborhood walk (`R = maxLength − 1`, read from `PortGraphEngine`); dirty = ports/edges/cycles in the ball, plus all same-card ports (co-cost ripple, §2).
3. `FindCycles` seeded-start overload; incremental re-materialize of edges incident to the ball.
4. **Invalidation-correctness equivalence test** on the sentinel set — incremental ≡ full, in CI every batch (§6). *This is the track's acceptance gate.*

*Depends on Track A's `ContentHash`; otherwise independent of C.*

### Track C — Atomic swap (atlas-flows / atlas-api) — ~M

1. Versioned snapshot tables + a `current` pointer (one-row pointer table or updatable view). **[tracer]**
2. Restructure `AtlasSeeder` to write-new-then-swap (one atomic transaction); record the snapshot `Stamp`.
3. Reader binding through the pointer; one-generation retention + rollback; GC beyond.
4. Atomic-swap test: a reader query during rematerialization sees only old-or-new, never mixed (spec, impl criteria §3).

*Depends on Track A's `Stamp` for the snapshot stamp; otherwise independent of B.*

**Cross-track gate (initiative 01 preflight):** the staleness gate — fail if any committed derived artifact's `SchemaHash` lags the current schema — lands once A defines the stamp and is wired into the existing preflight.
