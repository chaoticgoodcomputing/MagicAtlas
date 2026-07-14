// ─────────────────────────────────────────────────────────────────────────────
// Magic Atlas — GraphQL API
//
// A read-only GraphQL representation of the Scryfall MTG oracle catalog,
// powered by Trax + HotChocolate. Self-seeds from Scryfall over HTTP on first run.
//
// Prerequisites:
//   1. Start Postgres:  docker compose up -d   (from apps/atlas-api/)
//   2. Run:             dotnet run --project apps/atlas-api
//
// Cards stream from Scryfall's oracle-cards bulk over HTTP on first start (no manual
// download). To use a pre-downloaded bulk file instead, point Atlas:ScryfallBulkPath
// at it in appsettings.Development.json — present-and-readable wins over the HTTP fetch.
//
// Try it:
//   http://localhost:55250/trax/graphql  — Banana Cake Pop IDE
//
//   query {
//     atlas {
//       cards(first: 20, where: { name: { contains: "Dragon" } },
//             order: { edhrecRank: ASC }) {
//         nodes { id name manaCost typeLine imageUriNormal priceUsd }
//         pageInfo { hasNextPage endCursor }
//         totalCount
//       }
//     }
//   }
// ─────────────────────────────────────────────────────────────────────────────

using MagicAtlas.Api.Data;
using MagicAtlas.Api.Resolvers;
using MagicAtlas.Api.Seed;
using Microsoft.EntityFrameworkCore;
using Trax.Api.Extensions;
using Trax.Api.GraphQL.Extensions;
using Trax.Effect.Data.Extensions;
using Trax.Effect.Data.Postgres.Extensions;
using Trax.Effect.Extensions;
using Trax.Effect.Provider.Json.Extensions;
using Trax.Mediator.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("TraxDatabase")
    ?? throw new InvalidOperationException("Connection string 'TraxDatabase' not found.");

builder.Services.AddLogging(logging => logging.AddConsole());

// ── CORS for the Vite dev server ──────────────────────────────────────
// Trax's AuthorizationRegistrationValidator requires ASP.NET Core's auth services
// even when no [TraxAuthorize] trains exist. No policies needed.
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins("http://localhost:55173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

// ── Trax: mediator + effects. No trains yet, so we pass the API assembly. ──
builder.Services.AddTrax(trax =>
    trax.AddEffects(effects =>
            effects
                .UsePostgres(connectionString)
                .AddJson()
        )
        .AddMediator(typeof(Program).Assembly)
);

// ── Application DbContext (separate schema from Trax's own tables) ────
builder.Services.AddDbContextFactory<AtlasDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Seeder + HTTP client for fetching Scryfall bulk/API data ─────────
builder.Services.AddHttpClient("scryfall", c =>
{
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MagicAtlas/0.1 (+https://github.com/local)");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddSingleton<AtlasSeeder>();

// ── GraphQL with model query discovery on AtlasDbContext ──────────────
builder.Services.AddTraxGraphQL(graphql =>
    graphql
        .MaxExecutionDepth(8)
        .ConfigureCost(opts =>
        {
            // atlasPointRows returns ~30K nodes in one go to power the WebGL scatter —
            // each with 4 scalar fields. Budget generously for that read path.
            opts.MaxFieldCost = 500_000;
            opts.MaxTypeCost = 500_000;
        })
        .ConfigureSchema(schema => schema.ModifyPagingOptions(opts =>
        {
            opts.MaxPageSize = 50_000;
            opts.DefaultPageSize = 50;
            opts.IncludeTotalCount = true;
        }))
        .AddDbContext<AtlasDbContext>()
        // ── P2 deck resolver (plan §3): computed candidates / analyzeDeck fields.
        // Extends the Trax-generated "AtlasDiscoverQueries" object (the type behind
        // discover.atlas) via a HotChocolate [ExtendObjectType] type extension.
        .AddTypeExtension<AtlasDeckResolver>()
        .AllowIntrospection(_ => true)
);

builder.Services.AddHealthChecks().AddTraxHealthCheck();

var app = builder.Build();

// ── Ensure the atlas schema + cards table exist, then seed on first run ──
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AtlasDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();

    // Create schema + only the tables that don't yet exist. GenerateCreateScript produces a
    // single blob for the whole model; we split it by statement and skip any CREATE TABLE / INDEX
    // whose target already lives in the DB. This keeps adding new entities no-friction — the next
    // startup picks them up and leaves everything else alone.
    db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS atlas");
    await CreateMissingObjectsAsync(db);

    var seeder = scope.ServiceProvider.GetRequiredService<AtlasSeeder>();
    await seeder.SeedAsync();
}

app.UseCors();
app.UseTraxGraphQL();
app.MapHealthChecks("/trax/health");

app.Run();

// ─────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────

static async Task CreateMissingObjectsAsync(AtlasDbContext db)
{
    var existingTables = (await db.Database
        .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'atlas'")
        .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

    var existingIndexes = (await db.Database
        .SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE schemaname = 'atlas'")
        .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var stmt in SplitSqlStatements(db.Database.GenerateCreateScript()))
    {
        if (ShouldSkip(stmt, existingTables, existingIndexes)) continue;
        db.Database.ExecuteSqlRaw(stmt);
    }
}

// EF's create-script embeds DO $EF$ ... END $EF$; blocks whose bodies contain
// semicolons. Naïve Split(';') mangles them, so we walk the script while
// tracking dollar-quoted regions and only treat top-level semicolons as boundaries.
static IEnumerable<string> SplitSqlStatements(string script)
{
    var sb = new System.Text.StringBuilder();
    string? openTag = null;
    int i = 0;

    while (i < script.Length)
    {
        if (openTag is null && script[i] == '$')
        {
            var close = script.IndexOf('$', i + 1);
            if (close > i)
            {
                openTag = script.Substring(i, close - i + 1);
                sb.Append(openTag);
                i = close + 1;
                continue;
            }
        }
        else if (openTag is not null
                 && i + openTag.Length <= script.Length
                 && script.AsSpan(i, openTag.Length).SequenceEqual(openTag))
        {
            sb.Append(openTag);
            i += openTag.Length;
            openTag = null;
            continue;
        }

        if (openTag is null && script[i] == ';')
        {
            var stmt = sb.ToString().Trim();
            if (stmt.Length > 0) yield return stmt;
            sb.Clear();
        }
        else
        {
            sb.Append(script[i]);
        }
        i++;
    }

    var tail = sb.ToString().Trim();
    if (tail.Length > 0) yield return tail;
}

static bool ShouldSkip(string stmt, HashSet<string> tables, HashSet<string> indexes)
{
    // CREATE TABLE atlas.<name>  → skip if <name> exists
    var tableMatch = System.Text.RegularExpressions.Regex.Match(
        stmt, @"CREATE TABLE atlas\.(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (tableMatch.Success && tables.Contains(tableMatch.Groups[1].Value)) return true;

    // CREATE INDEX "IX_..." or CREATE UNIQUE INDEX "IX_..."  → skip if that index exists
    var indexMatch = System.Text.RegularExpressions.Regex.Match(
        stmt, @"CREATE (?:UNIQUE )?INDEX ""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (indexMatch.Success && indexes.Contains(indexMatch.Groups[1].Value)) return true;

    // The CREATE SCHEMA DO-block is already handled by our explicit CREATE SCHEMA IF NOT EXISTS
    // above, so we can safely skip it here.
    if (stmt.Contains("CREATE SCHEMA atlas", StringComparison.OrdinalIgnoreCase)) return true;

    return false;
}

namespace MagicAtlas.Api
{
    public partial class Program;
}
