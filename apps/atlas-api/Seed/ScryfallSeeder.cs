using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAtlas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MagicAtlas.Api.Seed;

/// <summary>
/// Reads Scryfall's <c>oracle-cards</c> bulk export and seeds the <c>atlas.cards</c> table on first run.
/// </summary>
/// <remarks>
/// The seeder is idempotent-by-emptiness: if any rows already exist, it does nothing. To re-seed,
/// truncate the table (or drop the schema) and restart. The file is streamed rather than loaded into
/// memory; bulk exports can exceed 400 MB.
/// </remarks>
public sealed class ScryfallSeeder
{
    private readonly IDbContextFactory<AtlasDbContext> _dbFactory;
    private readonly ILogger<ScryfallSeeder> _logger;
    private readonly string? _bulkPath;

    public ScryfallSeeder(
        IDbContextFactory<AtlasDbContext> dbFactory,
        ILogger<ScryfallSeeder> logger,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        var configured = configuration["Atlas:ScryfallBulkPath"];
        _bulkPath = string.IsNullOrWhiteSpace(configured)
            ? null
            : Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(configured, env.ContentRootPath);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Cards.AnyAsync(ct))
        {
            _logger.LogInformation("Atlas cards already seeded ({Count} rows) — skipping.", await db.Cards.CountAsync(ct));
            return;
        }

        if (string.IsNullOrWhiteSpace(_bulkPath) || !File.Exists(_bulkPath))
        {
            _logger.LogWarning(
                "Scryfall bulk file not found at '{Path}'. API will serve an empty catalog. " +
                "Download oracle-cards.json from https://scryfall.com/docs/api/bulk-data and set Atlas:ScryfallBulkPath.",
                _bulkPath ?? "<unset>");
            return;
        }

        _logger.LogInformation("Seeding Atlas cards from {Path}...", _bulkPath);

        await using var stream = File.OpenRead(_bulkPath);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var batch = new List<CardRow>(capacity: 250);
        var total = 0;

        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawCard>(stream, options, ct))
        {
            if (raw is null) continue;
            if (raw.Lang != "en") continue;

            batch.Add(Map(raw));

            if (batch.Count >= 250)
            {
                await FlushAsync(db, batch, ct);
                total += batch.Count;
                batch.Clear();
                _logger.LogInformation("Seeded {Total} cards so far...", total);
            }
        }

        if (batch.Count > 0)
        {
            await FlushAsync(db, batch, ct);
            total += batch.Count;
        }

        _logger.LogInformation("Seed complete: {Total} cards.", total);
    }

    private static async Task FlushAsync(AtlasDbContext db, IEnumerable<CardRow> rows, CancellationToken ct)
    {
        db.Cards.AddRange(rows);
        await db.SaveChangesAsync(ct);
        // Detach so the change tracker doesn't grow unbounded.
        db.ChangeTracker.Clear();
    }

    private static CardRow Map(RawCard c) => new()
    {
        Id = c.Id,
        OracleId = c.OracleId,
        Name = c.Name ?? "",
        Lang = c.Lang ?? "en",
        ReleasedAt = DateTime.SpecifyKind(c.ReleasedAt ?? DateTime.MinValue, DateTimeKind.Utc),
        ScryfallUri = c.ScryfallUri ?? "",
        Layout = c.Layout ?? "normal",
        ManaCost = c.ManaCost,
        Cmc = c.Cmc ?? 0m,
        TypeLine = c.TypeLine,
        OracleText = c.OracleText,
        Power = c.Power,
        Toughness = c.Toughness,
        Loyalty = c.Loyalty,
        Rarity = c.Rarity ?? "common",
        Colors = c.Colors ?? new(),
        ColorIdentity = c.ColorIdentity ?? new(),
        Types = new(),      // Scryfall doesn't split type_line; left empty until pipeline reintroduced.
        Subtypes = new(),
        Keywords = c.Keywords ?? new(),
        Games = c.Games ?? new(),
        Reserved = c.Reserved,
        Foil = c.Foil,
        Nonfoil = c.Nonfoil,
        Set = c.Set ?? "",
        SetName = c.SetName ?? "",
        SetType = c.SetType ?? "",
        CollectorNumber = c.CollectorNumber ?? "",
        Artist = c.Artist,
        ImageUriNormal = c.ImageUris?.Normal,
        ImageUriLarge = c.ImageUris?.Large,
        ImageUriArtCrop = c.ImageUris?.ArtCrop,
        PriceUsd = ParseDecimal(c.Prices?.Usd),
        PriceUsdFoil = ParseDecimal(c.Prices?.UsdFoil),
        EdhrecRank = c.EdhrecRank,
    };

    private static decimal? ParseDecimal(string? s) =>
        decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : null;

    // ── Raw Scryfall shape ───────────────────────────────────────────────
    private sealed class RawCard
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("oracle_id")] public Guid? OracleId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("lang")] public string? Lang { get; set; }
        [JsonPropertyName("released_at")] public DateTime? ReleasedAt { get; set; }
        [JsonPropertyName("scryfall_uri")] public string? ScryfallUri { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
        [JsonPropertyName("mana_cost")] public string? ManaCost { get; set; }
        [JsonPropertyName("cmc")] public decimal? Cmc { get; set; }
        [JsonPropertyName("type_line")] public string? TypeLine { get; set; }
        [JsonPropertyName("oracle_text")] public string? OracleText { get; set; }
        [JsonPropertyName("power")] public string? Power { get; set; }
        [JsonPropertyName("toughness")] public string? Toughness { get; set; }
        [JsonPropertyName("loyalty")] public string? Loyalty { get; set; }
        [JsonPropertyName("rarity")] public string? Rarity { get; set; }
        [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
        [JsonPropertyName("color_identity")] public List<string>? ColorIdentity { get; set; }
        [JsonPropertyName("keywords")] public List<string>? Keywords { get; set; }
        [JsonPropertyName("games")] public List<string>? Games { get; set; }
        [JsonPropertyName("reserved")] public bool Reserved { get; set; }
        [JsonPropertyName("foil")] public bool Foil { get; set; }
        [JsonPropertyName("nonfoil")] public bool Nonfoil { get; set; }
        [JsonPropertyName("set")] public string? Set { get; set; }
        [JsonPropertyName("set_name")] public string? SetName { get; set; }
        [JsonPropertyName("set_type")] public string? SetType { get; set; }
        [JsonPropertyName("collector_number")] public string? CollectorNumber { get; set; }
        [JsonPropertyName("artist")] public string? Artist { get; set; }
        [JsonPropertyName("image_uris")] public RawImageUris? ImageUris { get; set; }
        [JsonPropertyName("prices")] public RawPrices? Prices { get; set; }
        [JsonPropertyName("edhrec_rank")] public int? EdhrecRank { get; set; }
    }

    private sealed class RawImageUris
    {
        [JsonPropertyName("normal")] public string? Normal { get; set; }
        [JsonPropertyName("large")] public string? Large { get; set; }
        [JsonPropertyName("art_crop")] public string? ArtCrop { get; set; }
    }

    private sealed class RawPrices
    {
        [JsonPropertyName("usd")] public string? Usd { get; set; }
        [JsonPropertyName("usd_foil")] public string? UsdFoil { get; set; }
    }
}
