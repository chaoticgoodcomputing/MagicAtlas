# MagicAtlas — end-to-end system topology

The full pipeline from raw sources to the user, with the accretion loop that feeds the interaction
layer. Settled 2026-07-16 alongside [ADR 0003](../../libs/mast-interaction/docs/adr/0003-taxonomy-redesign.md)
(the taxonomy redesign), which defines the port/rollup vocabulary used below.

```
0. SOURCES         Scryfall bulk (oracle text, card data) · Commander Spellbook (combo corpus)
                         │
1. MAST PARSE      oracle text → AST                          libs/magic-ast
                   anchored by hand-parsed gold fixtures (~1,583), extended to matching cards
                         │
2. PORT PROJECTION AST → per-card ports; the union of        PortWalk (libs/mast-interaction;
                   witnessed ports (+ the scaffold) is the    libs/mast-query is the matching
                   port-topology artifact                     substrate the projection reuses)
                         │
3. INTERACTIONS    ports → port-to-port edges → cycles        PortGraphEngine (libs/mast-interaction)
                   (candidate combos), tiered by the
                   operator + §8 accounting
                         ▲
                         └── GOLDS → ROLLUP ← the accretion loop (ADR 0003 §8): hand-derived
                             interaction golds generate the rollup artifacts (port-topology,
                             port-interactions, + .cited verbose twins); the rollup feeds the
                             generalized rules INTO layer 3 for novel pairs. A feedback input
                             beside the pipeline, not a sequential stage. The .cited artifacts
                             also ship downstream (diagnostics).
                         │
4. FLOWTHRU        corpus + ports + interactions →            tests/magic-ast-tests flows
                   D1–D4 dumps (_08_Reporting/*.json)         (CardAtlas, PortGraphAtlas, …)
                         │
   SEED            dumps → Postgres                           apps/atlas-api AtlasSeeder
                   (file-drop of dumps today; the seeder      (skips when tables are non-empty;
                   runs on API startup)                       reseed = truncate + restart)
                         │
5. API             Postgres → GraphQL                         apps/atlas-api
                   http://localhost:55250/trax/graphql        (Trax + HotChocolate; non-default
                                                              ports: PG 55432 / GQL 55250)
                         │
6. WEBSITE         GraphQL → concept explorer → user          apps/atlas-web (Vite :55173)
                   card explorer / metro / station /
                   deck lens / synergy web
```

## Annotations

1. **Layer 2's code home.** The AST→ports projection is `PortWalk` in `libs/mast-interaction` — not
   `libs/mast-query`, which is the shape-matching *substrate* the projection reuses. Conceptually the
   label is exact, and ADR 0003 makes it literal: a port **is** a query against the AST (one clause can
   satisfy several port queries — the many-to-one is the design).
2. **The rollup is a feedback input, not a stage.** The accretion loop (per-gold interaction fixtures →
   generated rollup → generalized rules consumed by layer 3) sits *beside* the pipeline. Its lean
   artifacts serve the engine and (ultimately) the explorer columns; the `.cited` twins serve
   diagnostics (atlas-diag) and doc generation. It is the one arrow that points backward.
3. **Layer 4 is two half-steps.** Flowthru ends at the **dumps**; the dumps→Postgres write is the
   **API's seeder**. Flowthru also consumes more than ports/interactions — the CSB combo corpus and
   card metadata feed D1–D4 directly. (Reseed recipe: bust the code-blind Flowthru cache for the
   affected step, re-run the flow, copy the dump into `dumps/`, truncate the table, restart the API.)
4. **Layer 0 is real.** Scryfall + Commander Spellbook are pipeline stages with their own caching
   semantics (the CSB dump loads as an HTTP catalog item with a weekly conditional-GET window); corpus
   refresh is a maintenance activity, not an assumption.

## The shared epistemic architecture

The two intelligence layers are built the same way:

| | Layer 1 (parse) | Layers 2–3 (interactions) |
|---|---|---|
| Anchored by | hand-parsed AST gold fixtures | hand-derived interaction golds (ADR 0003 §8) |
| Extended by | the parser generalizing to matching cards | the rollup's rules generalizing to novel pairs |
| Gated by | gold-fixture suite + parse judges | per-gold judges + rollup-vs-scaffold topology sweeps |

Anchor by hand, generalize by structure, gate by judges. Everything downstream of layer 3 (Flowthru,
seed, API, website) is faithful plumbing — it must never *add* interaction knowledge, only transport,
aggregate, and render it.

## Diagnostic taps

- **atlas-diag** (`tools/atlas-diag`) reads the layer-4 dumps and diffs them against the live layer-5
  API — the bisection tool for "is this a data bug or a plumbing bug."
- The frontend (layer 6) is the **taxonomy oracle**: it renders every emit↔consume claim user-visibly,
  which is what exposed the ADR-0003 redesign in the first place.
