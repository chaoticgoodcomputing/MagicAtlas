# Magic Atlas — API + Web

Two new apps sit beside the existing Flowthru pipeline:

- [`apps/atlas-api`](../atlas-api) — read-only GraphQL over the Scryfall oracle catalog, built on [Trax](https://github.com/chaoticgoodcomputing/trax) + HotChocolate, backed by Postgres.
- [`apps/atlas-web`](../atlas-web) — Vite + React + Apollo client that consumes the API.

## Quick start

```bash
# 1. Start Postgres
docker compose -f apps/atlas-api/docker-compose.yml up -d

# 2. Download the Scryfall oracle bulk file (~150 MB)
#    https://scryfall.com/docs/api/bulk-data — "Oracle Cards" → save to:
mkdir -p dumps
curl -L -o dumps/oracle-cards.json \
  "$(curl -s https://api.scryfall.com/bulk-data/oracle-cards | jq -r .download_uri)"

# 3. Run the API (will seed on first launch — takes a minute)
dotnet run --project apps/atlas-api

# 4. Run the web app
cd apps/atlas-web && pnpm install && pnpm dev
```

- GraphQL IDE: <http://localhost:5250/trax/graphql>
- Web UI: <http://localhost:5173>
- Health: <http://localhost:5250/trax/health>

## Example queries

```graphql
query {
  atlas {
    cards(
      first: 20
      where: { name: { contains: "Dragon" } }
      order: { edhrecRank: ASC }
    ) {
      totalCount
      nodes { id name manaCost typeLine rarity imageUriNormal priceUsd }
      pageInfo { hasNextPage endCursor }
    }
  }
}
```

## Architecture notes

- The API owns its own flattened `CardRow` entity ([`Data/CardRow.cs`](Data/CardRow.cs)) — list-like fields
  (colors, types, keywords) are stored as jsonb. Rarity/layout/set type are plain strings, not enums,
  so schema changes on the Scryfall side don't break queries.
- `[TraxQueryModel]` on the entity auto-generates filter/sort/pagination schema via HotChocolate.
- `ScryfallSeeder` streams the oracle bulk JSON — the file can exceed 400 MB, so it deserializes
  cards one at a time rather than loading the whole array.
- Seed is idempotent-by-emptiness: rows present → no-op. To re-seed, drop the `atlas` schema.

## Known gap: MagicAtlas pipeline integration

The atlas-api project is **not** currently wired to the MagicAtlas Flowthru pipeline. The pipeline
targets an older Flowthru API (`Flowthru.Data`, `[SerializedLabel]`, `IStructuredSerializable`, etc.)
that was removed in Flowthru 0.3+. Migrating ~64 files with ~1500 compile errors is deferred.

When the migration happens, the planned wiring is:

1. Add `Flowthru.Extensions.EFCore` to `MagicAtlas.csproj`.
2. Declare `PersistedCards` in `Catalog.Primary.cs` as `EFCoreItemFactory.Enumerable.EFCore<CardRow, AtlasDbContext>(...)`.
3. Add a `CardProjection` node to `CardProcessing` that maps `Card → CardRow` and writes to `PersistedCards`.
4. Delete `ScryfallSeeder.cs` — its job becomes "run the pipeline."

Until then, the seeder reads `oracle-cards.json` directly.
