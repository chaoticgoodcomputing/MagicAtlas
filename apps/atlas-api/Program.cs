// ─────────────────────────────────────────────────────────────────────────────
// Magic Atlas — GraphQL API
//
// A read-only GraphQL representation of the Scryfall MTG oracle catalog,
// powered by Trax + HotChocolate. Seeds from oracle-cards.json on first run.
//
// Prerequisites:
//   1. Start Postgres:  docker compose up -d   (from apps/atlas-api/)
//   2. Download bulk:   https://scryfall.com/docs/api/bulk-data  (oracle-cards)
//   3. Set path:        Atlas:ScryfallBulkPath in appsettings.Development.json
//   4. Run:             dotnet run --project apps/atlas-api
//
// Try it:
//   http://localhost:5250/trax/graphql  — Banana Cake Pop IDE
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
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
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

// ── Seeder ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ScryfallSeeder>();

// ── GraphQL with model query discovery on AtlasDbContext ──────────────
builder.Services.AddTraxGraphQL(graphql =>
    graphql
        .MaxExecutionDepth(8)
        .ConfigureCost(opts => opts.MaxFieldCost = 50000)
        .AddDbContext<AtlasDbContext>()
        .AllowIntrospection(_ => true)
);

builder.Services.AddHealthChecks().AddTraxHealthCheck();

var app = builder.Build();

// ── Ensure the atlas schema + cards table exist, then seed on first run ──
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AtlasDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();

    // Create schema + tables on first run only. GenerateCreateScript has no
    // "IF NOT EXISTS" option, so we skip entirely when the target table already exists.
    db.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS atlas");
    var tableExists = (long?)await db.Database
        .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_schema = 'atlas' AND table_name = 'cards'")
        .SingleAsync() > 0;
    if (!tableExists)
    {
        db.Database.ExecuteSqlRaw(db.Database.GenerateCreateScript());
    }

    var seeder = scope.ServiceProvider.GetRequiredService<ScryfallSeeder>();
    await seeder.SeedAsync();
}

app.UseCors();
app.UseTraxGraphQL();
app.MapHealthChecks("/trax/health");

app.Run();

namespace MagicAtlas.Api
{
    public partial class Program;
}
