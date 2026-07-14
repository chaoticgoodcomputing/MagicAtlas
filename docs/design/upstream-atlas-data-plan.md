# Upstream data plan — powering the Atlas Explorer

Status: **proposed** · Date: 2026-07-14 · Owner: web/API

The Atlas Explorer frontend (`apps/atlas-web`, the concept surfaces: Metro map,
Station focus, Card Explorer, Deck Lens, Synergy web) is built and running on an
in-file sample corpus. This document is the plan to feed those surfaces from the
real pipeline. It supersedes the design-handoff note by grounding every "ask" in
the actual code.

The frontend was written against a single data-access seam —
`apps/atlas-web/src/data/atlas.ts`. Every hook there carries a `TODO(api:…)`
naming the GraphQL field it will bind to. When the endpoints below exist,
flipping the site onto live data is a change **in that one file**, not the views.

---

## 0. Where things actually stand

**The API serves five Scryfall-derived read models** (`apps/atlas-api`, Trax +
HotChocolate + Postgres): `cardRows`, `setRows`, `rulingRows`, `atlasPointRows`,
`cardSymbolRows`. All are flat `*Row` entities in `AtlasDbContext`, seeded from
the Scryfall bulk by `AtlasSeeder`. None of the port/family/combo analytics are
queryable.

**The analytics datasets already exist — but only as pipeline output files, and
only inside the test project.** The Card/Combo/Family/Archetype datasets (the
brief's D1–D4) are produced by the `CardAtlasFlow` in **`tests/magic-ast-tests`**,
written to `tests/magic-ast-tests/Data/_08_Reporting/*.json`:

| File | Produced by | Row type (`…/Data/_08_Reporting/Schemas/`) |
|---|---|---|
| `card-ports.json` | `CardAtlasFlow` → `CardPortsStep` | `CardPortRow` (`CardAtlas.cs`) |
| `card-meta.json` | `CardPortsStep` | `CardMetaRow` |
| `combo-instances.json` | `CardAtlasFlow` → `ReconstructCombosStep` | `ComboInstanceRow` |
| `resource-graph.json` | `CardAtlasFlow` → `FamilyRollupStep` | `ResourceGraph{Stations,Lines}` |
| `archetype-catalog.json` | `FamilyRollupStep` | `ArchetypeCatalog{Entries}` |
| `combo-anchor-report.json` | `InteractionTriage` → `RankComboAnchorsStep` | `ComboAnchorReport{TopAnchors}` |

So the true critical path is three hops, none of which exist yet:

```
   MAST + mast-interaction            atlas-flows (promoted            atlas-api
   (ports, combos, families)   ──►    from the test project)    ──►   Postgres  ──►  GraphQL  ──►  atlas-web
        already computed              §2 pipeline wiring               §1 query models       data/atlas.ts seam
```

**Prerequisite (P0): promote the CardAtlas reporting flow out of the test
project.** `CardAtlasFlow`, its steps, and the `_08_Reporting/Schemas/*`
records live in `tests/magic-ast-tests` — a test assembly, not something
production can run. Before any wiring, move the flow + schemas into a shippable
project (either `libs/atlas-flows`, which is the natural home and already has the
`_01_Raw … _08_Reporting` layering and Flowthru 0.28.0 wired, or a new
`apps/atlas-pipeline` runner). The `ResourceFamilies` helper
(`tests/magic-ast-tests/Flows/Shared/ResourceFamilies.cs` — the canonical
family taxonomy) moves with it.

---

## 1. API query models (the headline gap)

Mirror the existing `AtlasPointRow` pattern — the closest template, since it is
already a one-card-to-many-rows fan-out fed from a JSON dump. For each dataset,
add a `*Row` entity with `[TraxQueryModel(Namespace = GraphQLNamespaces.Atlas)]`
+ `[Table("…", Schema = "atlas")]`, a `DbSet` on `AtlasDbContext`, and index
config in `OnModelCreating`. The startup path in `Program.cs`
(`CreateMissingObjectsAsync`) already creates any table that doesn't yet exist,
so adding an entity is no-migration friction.

New GraphQL fields (under `discover.atlas.*`), each with the standard
filter/sort/cursor pagination Trax generates:

| Field | Entity | Source dataset | Keyed by |
|---|---|---|---|
| `portRows` | `PortRow` | `card-ports.json` (+ MAST spans, §4) | `portId` |
| `resourceFamilyRows` | `ResourceFamilyRow` | `resource-graph.json` stations | `family` |
| `resourceEdgeRows` | `ResourceEdgeRow` | `resource-graph.json` lines | `(from,to)` |
| `comboRows` | `ComboRow` | `combo-instances.json` | `comboId` |
| `archetypeRows` | `ArchetypeRow` | `archetype-catalog.json` entries | `signature` |
| `comboAnchorRows` | `ComboAnchorRow` | `combo-anchor-report.json` anchors | `cardId` |
| `familyLatticeRows` | `FamilyLatticeRow` | authored (`FamilyGrammar`) | `(family,subFamily)` |

### 1.1 Entity shapes

Mirror the source records field-for-field, adding the two things the frontend
needs uniformly: a **stable id** and a **tier**.

- **`PortRow`** ← `CardPortRow` (`Card`, `Label`, `Family`, `Side`) plus:
  `PortId` (stable, `"{card-slug}#{index}"`), `CardId` (Guid, joined — see 1.2),
  `Tier`, and the oracle provenance from §4: `OracleLineIndex`, `Spans`
  (`int[][]`, jsonb — `[[start,end), …]`). For triggered/activated abilities,
  keep the consume↔emit pairing so one clause focuses one side of the Explorer:
  either a `PairId` shared by the consume and emit rows, or a nested
  `consume`/`emit` shape as the handoff sketched.
- **`ResourceFamilyRow`** ← `ResourceStation` (`Family`, `Cards`, `Labels`).
  Add `Labels`→viz metadata as needed; hue/coords stay client-side (they are a
  presentation concern, already in `mock.ts`/`FAM`).
- **`ResourceEdgeRow`** ← `ResourceLine` (`From`, `To`, `RealizingCombos`,
  `BestTier`, `Engine`) plus **`Origin: "rules" | "card"`** (the metro's
  plain-rail-vs-card-◆ distinction — a new field; `FamilyGrammar`/`FamilyEdge`
  knows whether an edge is grammar-derived vs. realized by a specific card, so
  it can be emitted at rollup time). Directionality + tier already present.
- **`ComboRow`** ← `ComboInstanceRow` (`ComboId`, `Cards`, `CardCount`,
  `FamilySignature`, `FamilyRing`, `Tier`, `Firable`, `Results`, `Popularity`).
- **`ArchetypeRow`** ← `ArchetypeEntry` (`Families`, `FamilyCount`,
  `RealizingCombos`, `BestTier`, `GreenFraction`, `ExampleCards`, `Results`).
  `Signature` = sorted `Families` join.
- **`ComboAnchorRow`** ← `ComboAnchor` (`Card`, `TypeLine`, `BlockReason`,
  `BlockedComboCount`, `SoleBlockerCount`, `PopularityMass`,
  `MaxComboPopularity`, `TopPayoffs`, `CoStars[]`). `CoStars` as jsonb.
- **`FamilyLatticeRow`** — the super/subgroup DAG the client currently hardcodes
  as `GROUPS = { death:["sacrifice"], card:["mill"] }`. Ship it as data: rows of
  `(family, subFamily)` plus, per port label, its "counts-as" closure so the
  client doesn't recompute the transitive set. Source: the family grammar
  (`libs/mast-interaction/FamilyGrammar.cs`) knows the containment.

### 1.2 Two caveats that will bite (from the code)

1. **camelCase vs snake_case DTOs.** The reporting records use
   `[SerializedLabel("camelCase")]`, so their JSON keys are camelCase — unlike
   the snake_case Scryfall DTOs the current seeder maps. The seeder DTOs for
   these datasets (`RawPort`, `RawCombo`, …) must use `[JsonPropertyName("…")]`
   with camelCase names (or a `JsonNamingPolicy.CamelCase` options object).
2. **Guid vs name join.** Combo/port rows key cards by **name** (`string Card`)
   and Scryfall oracle-id as a **string**, whereas `CardRow`/`AtlasPointRow` key
   by `Guid`. To give ports/anchors a `CardId: Guid` for deep-links and joins
   back to `atlas.cards`, the seeder must resolve name/oracle-id → `CardRow.Id`
   at load time (build a name→Guid map once, like `SeedAtlasPointsAsync` does).

### 1.3 Tiers everywhere (cross-cutting)

Every port, edge, ring, and inferred row carries `tier ∈ {Green, Amber,
Inferred, Declared}`; **Inferred** additionally carries `confidence` (0–1) +
`evidence`. The reconstruction engine
(`libs/mast-interaction/PortGraphEngine.cs`, `PortCycle.Firable` / tiering)
already distinguishes Green/Amber; **Inferred and Declared are the statistical
backfill tiers** and must be produced by the backfill pass (see the existing
"statistical backfill direction" design). The API must not invent tiers — it
projects what the pipeline emits, uniformly, on every endpoint.

---

## 2. Pipeline → Postgres wiring

`atlas-api` is not wired to Flowthru at all (README "Known gap"); the old
pipeline targeted a removed Flowthru API. Flowthru is now 0.28.0 and there is
**no EFCore integration package** referenced. Two options:

**Option A — file-drop + seed (recommended first).** Have the promoted
`CardAtlasFlow` write its `_08_Reporting/*.json` to a known `dumps/` directory,
and extend `AtlasSeeder` with `SeedPortsAsync` / `SeedCombosAsync` / … following
`SeedAtlasPointsAsync` exactly: read a path from config
(`Atlas:CardPortsPath`, etc.), skip-if-missing with a log, stream `RawX` DTOs,
batch-flush, idempotent-by-emptiness (`if (await db.Ports.AnyAsync()) return;`).
Lowest friction, mirrors what already works for atlas points. Re-seed = truncate
the table. This unblocks the whole frontend without new infrastructure.

**Option B — EFCore-backed catalog (proper, later).** Implement a
`CatalogAbstract` subclass whose `IItem<T>` writes to `AtlasDbContext` instead of
JSON (the `Catalog.cs` remarks already flag this as the intended future shape).
Then the pipeline writes rows directly, no file hop. More work; do it once the
schemas are stable.

Either way, the pipeline run itself becomes the "seed": `AtlasSeeder`'s
Scryfall-card seeding stays, and the analytics tables are populated from pipeline
output rather than a second HTTP fetch.

---

## 3. Deck resolver endpoint

The Deck Lens and the Explorer candidate columns need a **deck-scoped** query and
a **candidate** query — the client must not join 95k combos.

- **`analyzeDeck(cards: [String!]!)`** — a HotChocolate mutation/resolver (not a
  Trax query model; it's a compute, not a table). Returns:
  `coverage` (per-family emit/consume counts with super/subgroup rollups — the
  hollow double-count segments), `rings` (complete tiered rings present in the
  deck), and `nearMiss` (ranked "one card away" closers, scored by
  shared-popularity / ubiquity / price). The `ComboAnchor`/`CoStar` data
  (`combo-anchor-report.json`) already supports the near-miss ranking.
- **`candidates(family: String!, side: Side!, limit: Int)`** — powers the
  Explorer's left/right columns (emitters of what a line consumes; consumers of
  what it emits, including supergroup matches). Server-side ranked.

These map to the frontend hooks `useDeckAnalysis` and `useCardNeighbours`.

---

## 4. MAST — oracle-text source spans per port

The Explorer highlights the exact oracle substring that produced each port, and
the synergy nodes print that clause inline. Today those spans are hand-authored
(`ORACLE` in `mock.ts`). To make them real:

**The plumbing already exists but is dropped on success.**
`libs/magic-ast/AST/TextSpan.cs` defines `readonly record struct TextSpan(int
Start, int Length)` with `FromBounds`. `ClauseSplitter` already computes a
per-clause `TextSpan SourceSpan` (`Parsing/ClauseSplitter.cs`), and the
tokenizer (Superpower) tracks positions. But that offset is retained **only on
unparsed/residual nodes** (`UnparsedAbility.SourceSpan`, `UnparsedEffect`,
`Diagnostic.Location`) — once a clause parses into a real
`TriggeredAbility`/`ActivatedAbility`/`StaticAbility`, the span is discarded.

**Concrete change, in order:**

1. **Retain the span on parsed nodes.** Add `TextSpan? SourceSpan` to the base
   `Ability` record (`libs/magic-ast/AST/Abilities/Ability.cs`) — and, for
   clause-level provenance, to the base `Effect` type
   (`AST/Effects/Core/…`). Populate it from `Clause.SourceSpan` in the ability
   parsers (`StaticAbilityParser`, `TriggeredAbilityParser`, … already receive
   the `Clause`). Low risk: additive, nullable, serializes via existing
   `TextSpan` JSON.
2. **Carry the span through port projection.** Port labels are minted in
   `libs/mast-interaction` (`PortWalk.Project` in `PortGraph.cs`, labels in
   `PortLabel.cs`). Thread the originating ability/effect `SourceSpan` onto
   `PortNode` (add `TextSpan? SourceSpan` + `int OracleLineIndex`).
3. **Emit spans in the dataset.** `CardPortsStep` (which already calls
   `PortWalk` and `ResourceFamilies.Of(label)`) writes `CardPortRow.Spans` +
   `OracleLineIndex` alongside `Label`/`Family`/`Side`.
4. **Project to the API.** `PortRow.Spans` (jsonb `int[][]`) as in §1.1.

Allow multiple spans per port (split clauses) — `Spans` is a list of
`[start,end)` pairs.

---

## 5. Cross-cutting

- **Stable ids for deep links.** `CardRow` has a `Guid Id` (good). Combos,
  archetypes, and ports each need a stable id (`comboId` exists; `portId` and
  archetype `signature` to be minted deterministically) so every surface is
  shareable. The frontend already deep-links views via URL hash; per-entity ids
  extend that to per-combo/per-card.
- **Images.** The mock loads art via Scryfall `cards/named?...&format=image`.
  Production should use `CardRow.ImageUriNormal/Large` (already stored) to avoid
  Scryfall rate limits and name-collision ambiguity. The name→Guid map from §1.2
  gives the join.
- **Family hue + metro coordinates stay client-side.** They are presentation
  (in `mock.ts`/`FAM`), not analytics — the API ships family *identity* and
  *stats*, the client owns the palette and layout.

---

## 6. Sequencing

1. **P0** — promote `CardAtlasFlow` + `_08_Reporting` schemas + `ResourceFamilies`
   out of `tests/magic-ast-tests` into a shippable project.
2. **P1** — `PortRow`, `ResourceFamilyRow`, `ResourceEdgeRow`, `ComboRow`,
   `ArchetypeRow`, `ComboAnchorRow`, `FamilyLatticeRow` entities + `AtlasSeeder`
   Option-A loaders. Flip `useFamilyGraph`, `useStation`, `useArchetypes`,
   `useCardNeighbours` (read side) onto GraphQL.
3. **P2** — `analyzeDeck` + `candidates` resolvers. Flip `useDeckAnalysis`.
4. **P3** — MAST `SourceSpan` on parsed nodes → `PortNode` → `CardPortRow` →
   `PortRow.Spans`. Flip `useOracle` off the hand-authored `ORACLE` map.
5. **P4** — Option-B EFCore catalog; add Inferred/Declared backfill tiers across
   all endpoints.

Each phase leaves the site fully working (mock where unbound), because the seam
in `data/atlas.ts` isolates the swap.
