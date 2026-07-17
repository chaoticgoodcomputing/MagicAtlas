# Port-native interaction graph API — design

Status: **Design (2026-07-17)**. Grilled across several rounds; the one empirical risk
(2-hop filtering at scale) has been **spiked and retired** (see §7). Stack decision
**locked: Postgres → EF Core → HotChocolate → Trax → GraphQL** (no graph DB).

## 1. Goal

Model the interaction graph the way the engine already does — **ports are the nodes,
a card is a labeled bag of ports** — and let the client walk `card → ports → edges →
ports` with **zero topology knowledge**. The engine (`PortGraphEngine.Materialize` /
`PortFlowMatcher.Captures`, post-cutover the authoritative matcher) stays the single
source of truth. The client filters, sorts, and paginates; it never re-derives arms,
guards, or subsumption. This kills the `flowArms.ts` / `feeds()` / `FLOW_FEEDERS`
reimplementation and the whole false/over-broad-edge class (Chatterfang→Aang, the
Squirrel-sac firehose) by construction.

## 2. What the grilling settled

1. **Trax is capable.** `[TraxQueryModel]` is a HotChocolate TypeModule; EF navigation
   properties project into nested GraphQL, and `ObjectTypeExtension`/`AddTypeModule`
   allow custom resolver fields. Both the nav-walk and on-demand resolvers are
   expressible. *Feasibility is not the constraint.*
2. **The raw port union is a substrate, not a product.** `InteractionUnion.Materialize`
   (the global union → `card-edges.json`) is **5.5M edges**; **3.6M are cross-card
   GREEN**. A single generic `emit:returntobattlefield` port feeds **4,832** consumes;
   a single `sac:permanent:controlled` receives from **2,971**. Neither tier nor
   correctness prunes this — "a reanimated creature can reliably be sacrificed" is
   GREEN, true, and useless. Serving the raw graph as a walkable map is a firehose.
3. **Relevance is rank + filter + limit, not prune.** Sort by popularity (sinks the
   ~3.6M inert edges to popularity 0, floats the combo-participating ones), filter by
   card/edge attributes, `first: 15`. The mesh becomes navigable without deletion.
4. **Filtering/sorting forces precompute.** HotChocolate filter/sort/page only work over
   an `IQueryable` (they translate to SQL). You cannot push `where`/`order`/`first` into
   an on-demand engine call without fetch-all-then-filter-in-memory. So the edges **must**
   be a materialized EF table. On-demand is off the table — decided by the requirement,
   not a limitation.
5. **The edge is not just `Captures`.** `Materialize` emits flow (`Captures`), the
   untap→mana and **consume→consume** sac→dies bridges, the emit→intercept modifier, and
   set-contextual copy-graft closing edges. Precompute (running `Materialize`) captures
   all of it; an on-demand `Captures`-only resolver would miss the bridges and get copy
   wrong. This is a second, independent reason to precompute.
6. **Combos are first-class; their verdict is not emergent.** A combo *is* a closed walk
   (structure emerges from edges), but its GREEN/firable verdict is §8 whole-cycle
   accounting (Firable · Balanced · Productive) — SQL cannot derive it from pairwise edge
   tiers. So `combo` stays a first-class entity (verdict + result + popularity) that
   **references** its edges; edges carry combo popularity back-annotated onto them. The
   edge graph is the skeleton; the combo carries what the skeleton can't.
7. **The 2-hop "filter by second-degree port" is the killer feature *and* the only real
   risk** — and it dissolves by **denormalization**: precompute each edge's reachability
   as a tag array so the 2-hop nested `EXISTS` becomes a flat GIN array-overlap. Traversal
   moves offline (where the engine already runs); the online query is flat filtered search
   — the stack's sweet spot. Spiked in §7.

## 3. Data model

Four `[TraxQueryModel]` entities. The **edge is a wide, denormalized row** carrying
everything a filter/sort could want, so no online query ever joins or traverses.

- **`PortRow` (node)** — `portId` (content-addressed: hash of `card + label`, stable
  across reseeds), `cardId`, `label`, `side`, `family`, `stem`, `tier`, provenance
  (`oracleLineIndex`, `spans`), and the `subject` facets. Navigation: `card`,
  `outEdges`, `inEdges`.
- **`PortEdgeRow` (denormalized edge)** — the spine:
  - endpoints: `fromPortId`, `fromCard`, `fromLabel`, `toPortId`, `toCard`, `toLabel`
  - edge: `relation` (`flow` | `bridge` | `modifier` | `card-defined`), `tier`
  - **denormalized `toCard` attributes** (for endpoint filters): `toCmc`, `toColors[]`,
    `toEdhrecRank`
  - **`popularity`** (combo-derived — max/sum of the combos this edge participates in;
    0 for inert edges → the relevance sort key)
  - **`targetReaches text[]`** (the 2-hop denormalization — the resource families the
    target port's card reaches/produces; the killer-filter column)
- **`CardRow` (grouping)** — unchanged; ports join via `cardId`.
- **`ComboRow` (first-class cycle)** — verdict (loop-tier), result/payoff, popularity,
  and references to its constituent edges. Unchanged from D4 `combo-instances`; the edge
  graph links to it, not vice-versa.

## 4. Pipeline (reuse existing engine, add a denormalize step)

1. **`Materialize`** already emits the global port→port edges with all relation families
   and pairwise tiers (`card-edges.json` today, an internal artifact).
2. **New denormalize step** — join each edge to: `toCard` attributes (cmc/colors/edhrec
   from the card index), the `targetReaches` tag set (the emit-families of `toCard`), and
   the combo popularity (back-annotated from the combo pass). Emit a normalized,
   content-addressed `port-edges.json`.
3. **Seeder** loads `atlas.port_edges` (like `atlas.ports` today). **Reseed is two-table
   atomic** (ports + edges together) so an edge never dangles against a stale port —
   truncate+reload both in one transaction, or version-swap.

Staleness is reseed-fresh, consistent with the rest of the atlas. Copy set-context: copy
edges are **excluded from the broad pairwise graph** (they're only truthful in a set) and
surfaced via combos.

## 5. API (Trax / HotChocolate)

- `PortEdgeRow` and `PortRow` as `[TraxQueryModel]` → `discover { atlas { portEdges(...) } }`
  with automatic Relay paging, filtering, sorting, projection.
- **Every filter the explorer wants is a flat predicate on one indexed table:**
  - color / mana-value → `where: { toColors: { some: {...} }, toCmc: { lte: 3 } }`
  - relevance → `order: { popularity: DESC }` (or `toEdhrecRank: ASC` for the deckbuilding view)
  - **2-hop reachability** → `where: { targetReaches: { some: { eq: "damage" } } }`
    (the denormalized tag — no live join)
- **Nested-collection paging** on `outEdges`/`inEdges` via `[UsePaging]` resolver fields
  (an `ObjectTypeExtension`), so every hop carries `first:`/`where:` and the dense SCC
  never dumps thousands of rows.
- **Escape hatch**: for a genuinely arbitrary second-degree predicate not covered by a
  denormalized tag, a custom HotChocolate resolver backed by hand-tuned SQL (Trax allows
  this via `ObjectTypeExtension`) — HC for the 90%, hand-written SQL for the rare hard case.
- **Indexes are ours** ("Trax does not own migrations"): `gin (target_reaches)`,
  `btree (from_card, from_label)`, `btree (to_edhrec)`, `btree (to_cmc)`, `btree (popularity)`.

## 6. Client

A dumb walker + a filter UI. Given a card: `card → ports → outEdges/inEdges (with a
filter+sort+first:15) → neighbour ports → their card → …`. Knows only ids, rendering, and
display prefs. **Deletes `flowArms.ts`, `feeds()`, `FLOW_FEEDERS`** and the two-query
`useCardNeighbours` dance. No topology on the client → cannot drift from the engine.

## 7. Spike — the 2-hop filter, validated at full scale (2026-07-17)

Built `atlas.port_edges_spike`: all **5.5M** edges denormalized with `toCmc`,
`toEdhrecRank`, `toColors[]`, and `targetReaches[]` (each target card's emit-families),
GIN-indexed on `targetReaches` + btrees on `from_card`, `to_edhrec`, `to_cmc`.

| Metric | Result |
|---|---|
| COPY 5.5M rows | **7.3 s** |
| Build 4 indexes + ANALYZE | **~10 s** |
| Table + indexes on disk | **1.3 GB** |
| **Q1** card page: Chatterfang token-feeders reaching `life`, cmc≤4, top-15 by EDHREC | **7 ms** (BitmapAnd of `from_card` + GIN `target_reaches`) |
| **Q2** global stress: all edges reaching `damage`, cmc≤3, GREEN, top-15, *no* card prefilter | **39 ms** (ordered index scan on `to_edhrec`, filter-and-stop-at-15) |
| **Q3** the exact user 2-hop example: Chatterfang feeders reaching life/damage, ranked, top-15 | **11 ms** |

**Verdict:** the denormalized tag-filter is single-digit-to-tens-of-ms over the full
5.5M-edge graph. The 2-hop reachability filter — the one feature that looked like it needed
a graph DB or a live nested join — is a flat array-overlap. The stack is validated; no
alternative datastore is warranted.

## 8. Implementation status (2026-07-17)

**Slice 1 landed** (API data layer, end-to-end): `PortEdgeRow` `[TraxQueryModel]` +
`atlas.port_edges` + a seeder (`SeedPortEdgesAsync`) that streams the engine's
`card-edges.json`, denormalizes in-stream via Npgsql binary COPY (`toCmc/toColors/
toEdhrec/targetReaches`), and rebuilds the GIN + btree indexes after load.

**A big finding fell out of the seed:** the engine's raw union is **97% copy-graft
synthetics** ("X copy of Y" nodes). Excluding them (design §4 — copy is set-contextual, it
belongs in combos) collapses the graph from 5.5M edges to **163,170 real-card pairwise
edges** — 34× smaller than the spike's raw count. The size/reseed concern evaporates.

Verified through Trax GraphQL: `portEdgeRows(where: { fromCard, fromLabel, targetReaches:
{some:{eq:"life"}}, toCmc:{lte} }, order:{toEdhrec}, first:15)` returns the correct
aristocrat payoffs (Ayara, Cauldron of Essence, Zimone) in ~ms — confirming HotChocolate
auto-generates the nested list filter for the `text[]` tag column. The 2-hop feature is a
flat filter in production, exactly as designed.

## 9. Open decisions / follow-ups

- **Popularity back-annotation** — the pipeline step that stamps each edge with its combos'
  popularity. Load-bearing for the relevance sort; couples the edge dump to the combo pass.
- **`targetReaches` granularity** — family-level tags (spiked) cover the explorer's real
  cases; keep the resolver escape hatch for exact-second-degree predicates.
- **Content-addressed port ids** — adopt `hash(card+label)` so edge FKs survive reseeds
  (the current `{card-slug}#{index}` id reorders on reparse).
- **Reseed atomicity** — ports + edges reload in one transaction / version-swap.
- **Bounding** — the spike shows the full 5.5M is fine (1.3 GB, ms queries); revisit
  top-N-feeders-per-port only if reseed weight hurts.
- **Loop-tier** stays in `combo-instances` (§2.6); the edge graph never re-derives it.
- **Cleanup**: drop `atlas.port_edges_spike` (throwaway) once the design is accepted.
