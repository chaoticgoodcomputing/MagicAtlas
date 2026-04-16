using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAtlas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MagicAtlas.Api.Seed;

/// <summary>
/// Seeds the <c>atlas</c> schema from Scryfall sources:
/// cards (local bulk file), rulings (bulk download), sets, and symbology.
/// </summary>
/// <remarks>
/// <para>Each table seeds independently and is idempotent-by-emptiness — if any rows exist in a given
/// table, that table is skipped. To re-seed, truncate the table (or drop the schema) and restart.</para>
/// <para>Cards read from a local bulk file (~150 MB) because the download is slow and large. Rulings,
/// sets, and symbology are fetched on-demand from Scryfall's HTTPS API (tens of MB combined).</para>
/// </remarks>
public sealed class AtlasSeeder
{
    private readonly IDbContextFactory<AtlasDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AtlasSeeder> _logger;
    private readonly string? _cardsBulkPath;

    private readonly string? _atlasPointsPath;

    public AtlasSeeder(
        IDbContextFactory<AtlasDbContext> dbFactory,
        IHttpClientFactory httpFactory,
        ILogger<AtlasSeeder> logger,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _httpFactory = httpFactory;
        _logger = logger;
        _cardsBulkPath = Resolve(configuration["Atlas:ScryfallBulkPath"], env.ContentRootPath);
        _atlasPointsPath = Resolve(configuration["Atlas:AtlasPointsPath"], env.ContentRootPath);
    }

    private static string? Resolve(string? configured, string contentRoot) =>
        string.IsNullOrWhiteSpace(configured)
            ? null
            : Path.IsPathRooted(configured)
                ? configured
                : Path.GetFullPath(configured, contentRoot);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedCardsAsync(ct);
        await SeedRulingsAsync(ct);
        await SeedSetsAsync(ct);
        await SeedSymbolsAsync(ct);
        await SeedAtlasPointsAsync(ct);
    }

    // ── Cards ────────────────────────────────────────────────────────────

    private async Task SeedCardsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Cards.AnyAsync(ct))
        {
            _logger.LogInformation("Cards already seeded ({Count} rows) — skipping.", await db.Cards.CountAsync(ct));
            return;
        }

        if (string.IsNullOrWhiteSpace(_cardsBulkPath) || !File.Exists(_cardsBulkPath))
        {
            _logger.LogWarning(
                "Scryfall cards bulk file not found at '{Path}'. Card catalog will be empty. " +
                "Download oracle-cards.json from https://scryfall.com/docs/api/bulk-data.",
                _cardsBulkPath ?? "<unset>");
            return;
        }

        _logger.LogInformation("Seeding cards from {Path}...", _cardsBulkPath);

        await using var stream = File.OpenRead(_cardsBulkPath);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var batch = new List<CardRow>(capacity: 250);
        var total = 0;

        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawCard>(stream, options, ct))
        {
            if (raw is null) continue;
            if (raw.Lang != "en") continue;

            batch.Add(MapCard(raw));

            if (batch.Count >= 250)
            {
                await FlushAsync(db, batch, ct);
                total += batch.Count;
                batch.Clear();
                _logger.LogInformation("  ...seeded {Total} cards", total);
            }
        }

        if (batch.Count > 0)
        {
            await FlushAsync(db, batch, ct);
            total += batch.Count;
        }

        _logger.LogInformation("Cards: {Total} rows.", total);
    }

    // ── Rulings ──────────────────────────────────────────────────────────

    private async Task SeedRulingsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Rulings.AnyAsync(ct))
        {
            _logger.LogInformation("Rulings already seeded ({Count} rows) — skipping.", await db.Rulings.CountAsync(ct));
            return;
        }

        var http = _httpFactory.CreateClient("scryfall");

        _logger.LogInformation("Resolving Scryfall rulings bulk URL...");
        var bulkInfo = await http.GetFromJsonAsync<BulkDataInfo>(
            "https://api.scryfall.com/bulk-data/rulings", ct)
            ?? throw new InvalidOperationException("Failed to resolve rulings bulk-data info.");

        _logger.LogInformation("Downloading rulings from {Uri} ({SizeMb} MB)...",
            bulkInfo.DownloadUri, bulkInfo.Size / (1024 * 1024));

        await using var stream = await http.GetStreamAsync(bulkInfo.DownloadUri, ct);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var batch = new List<RulingRow>(capacity: 500);
        var total = 0;

        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawRuling>(stream, options, ct))
        {
            if (raw is null) continue;

            batch.Add(new RulingRow
            {
                OracleId = raw.OracleId,
                Source = raw.Source ?? "",
                PublishedAt = DateTime.SpecifyKind(raw.PublishedAt ?? DateTime.MinValue, DateTimeKind.Utc),
                Comment = raw.Comment ?? "",
            });

            if (batch.Count >= 500)
            {
                await FlushAsync(db, batch, ct);
                total += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await FlushAsync(db, batch, ct);
            total += batch.Count;
        }

        _logger.LogInformation("Rulings: {Total} rows.", total);
    }

    // ── Sets ─────────────────────────────────────────────────────────────

    private async Task SeedSetsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Sets.AnyAsync(ct))
        {
            _logger.LogInformation("Sets already seeded ({Count} rows) — skipping.", await db.Sets.CountAsync(ct));
            return;
        }

        _logger.LogInformation("Fetching sets from Scryfall...");
        var http = _httpFactory.CreateClient("scryfall");

        var rows = new List<SetRow>();
        string? next = "https://api.scryfall.com/sets";
        while (!string.IsNullOrEmpty(next))
        {
            var page = await http.GetFromJsonAsync<ScryfallPage<RawSet>>(next, ct)
                ?? throw new InvalidOperationException("Empty sets page.");

            foreach (var raw in page.Data ?? Enumerable.Empty<RawSet>())
            {
                rows.Add(new SetRow
                {
                    Id = raw.Id,
                    Code = raw.Code ?? "",
                    MtgoCode = raw.MtgoCode,
                    ArenaCode = raw.ArenaCode,
                    TcgplayerId = raw.TcgplayerId,
                    Name = raw.Name ?? "",
                    SetType = raw.SetType ?? "",
                    ReleasedAt = raw.ReleasedAt.HasValue
                        ? DateTime.SpecifyKind(raw.ReleasedAt.Value, DateTimeKind.Utc)
                        : null,
                    BlockCode = raw.BlockCode,
                    Block = raw.Block,
                    ParentSetCode = raw.ParentSetCode,
                    CardCount = raw.CardCount,
                    PrintedSize = raw.PrintedSize,
                    Digital = raw.Digital,
                    FoilOnly = raw.FoilOnly,
                    NonfoilOnly = raw.NonfoilOnly,
                    ScryfallUri = raw.ScryfallUri ?? "",
                    IconSvgUri = raw.IconSvgUri ?? "",
                });
            }
            next = page.HasMore ? page.NextPage : null;
        }

        db.Sets.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Sets: {Total} rows.", rows.Count);
    }

    // ── Card symbols ─────────────────────────────────────────────────────

    private async Task SeedSymbolsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.CardSymbols.AnyAsync(ct))
        {
            _logger.LogInformation("Card symbols already seeded ({Count} rows) — skipping.", await db.CardSymbols.CountAsync(ct));
            return;
        }

        _logger.LogInformation("Fetching symbology from Scryfall...");
        var http = _httpFactory.CreateClient("scryfall");

        var page = await http.GetFromJsonAsync<ScryfallPage<RawSymbol>>(
            "https://api.scryfall.com/symbology", ct)
            ?? throw new InvalidOperationException("Empty symbology response.");

        var rows = (page.Data ?? Enumerable.Empty<RawSymbol>())
            .Select(raw => new CardSymbolRow
            {
                Symbol = raw.Symbol ?? "",
                SvgUri = raw.SvgUri ?? "",
                English = raw.English ?? "",
                Transposable = raw.Transposable,
                RepresentsMana = raw.RepresentsMana,
                AppearsInManaCosts = raw.AppearsInManaCosts,
                ManaValue = raw.ManaValue,
                Hybrid = raw.Hybrid,
                Phyrexian = raw.Phyrexian,
                Colors = raw.Colors ?? new(),
            })
            .Where(r => r.Symbol.Length > 0)
            .ToList();

        db.CardSymbols.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Card symbols: {Total} rows.", rows.Count);
    }

    // ── Atlas points (UMAP output from the MagicAtlas pipeline) ──────────

    private async Task SeedAtlasPointsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.AtlasPoints.AnyAsync(ct))
        {
            _logger.LogInformation("Atlas points already seeded ({Count} rows) — skipping.", await db.AtlasPoints.CountAsync(ct));
            return;
        }

        if (string.IsNullOrWhiteSpace(_atlasPointsPath) || !File.Exists(_atlasPointsPath))
        {
            _logger.LogInformation(
                "Atlas points file not found at '{Path}' — skipping. Run `dotnet run --project apps/atlas -- run OracleEmbedding` to generate.",
                _atlasPointsPath ?? "<unset>");
            return;
        }

        _logger.LogInformation("Seeding atlas points from {Path}...", _atlasPointsPath);

        await using var stream = File.OpenRead(_atlasPointsPath);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var batch = new List<AtlasPointRow>(capacity: 1000);
        var total = 0;

        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawAtlasPoint>(stream, options, ct))
        {
            if (raw is null) continue;

            batch.Add(new AtlasPointRow
            {
                CardId = raw.CardId,
                X = raw.X,
                Y = raw.Y,
                TextType = raw.TextType ?? "passive",
            });

            if (batch.Count >= 1000)
            {
                await FlushAsync(db, batch, ct);
                total += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await FlushAsync(db, batch, ct);
            total += batch.Count;
        }

        _logger.LogInformation("Atlas points: {Total} rows.", total);
    }

    private sealed class RawAtlasPoint
    {
        [JsonPropertyName("card_id")] public Guid CardId { get; set; }
        [JsonPropertyName("x")] public double X { get; set; }
        [JsonPropertyName("y")] public double Y { get; set; }
        [JsonPropertyName("text_type")] public string? TextType { get; set; }
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static async Task FlushAsync<T>(AtlasDbContext db, IEnumerable<T> rows, CancellationToken ct)
        where T : class
    {
        db.AddRange(rows);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    private static CardRow MapCard(RawCard c) => new()
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
        Types = new(),
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

    // ── Raw Scryfall DTOs ────────────────────────────────────────────────

    private sealed class ScryfallPage<T>
    {
        [JsonPropertyName("data")] public List<T>? Data { get; set; }
        [JsonPropertyName("has_more")] public bool HasMore { get; set; }
        [JsonPropertyName("next_page")] public string? NextPage { get; set; }
    }

    private sealed class BulkDataInfo
    {
        [JsonPropertyName("download_uri")] public string DownloadUri { get; set; } = "";
        [JsonPropertyName("size")] public long Size { get; set; }
    }

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

    private sealed class RawRuling
    {
        [JsonPropertyName("oracle_id")] public Guid OracleId { get; set; }
        [JsonPropertyName("source")] public string? Source { get; set; }
        [JsonPropertyName("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
    }

    private sealed class RawSet
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("mtgo_code")] public string? MtgoCode { get; set; }
        [JsonPropertyName("arena_code")] public string? ArenaCode { get; set; }
        [JsonPropertyName("tcgplayer_id")] public int? TcgplayerId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("set_type")] public string? SetType { get; set; }
        [JsonPropertyName("released_at")] public DateTime? ReleasedAt { get; set; }
        [JsonPropertyName("block_code")] public string? BlockCode { get; set; }
        [JsonPropertyName("block")] public string? Block { get; set; }
        [JsonPropertyName("parent_set_code")] public string? ParentSetCode { get; set; }
        [JsonPropertyName("card_count")] public int CardCount { get; set; }
        [JsonPropertyName("printed_size")] public int? PrintedSize { get; set; }
        [JsonPropertyName("digital")] public bool Digital { get; set; }
        [JsonPropertyName("foil_only")] public bool FoilOnly { get; set; }
        [JsonPropertyName("nonfoil_only")] public bool NonfoilOnly { get; set; }
        [JsonPropertyName("scryfall_uri")] public string? ScryfallUri { get; set; }
        [JsonPropertyName("icon_svg_uri")] public string? IconSvgUri { get; set; }
    }

    private sealed class RawSymbol
    {
        [JsonPropertyName("symbol")] public string? Symbol { get; set; }
        [JsonPropertyName("svg_uri")] public string? SvgUri { get; set; }
        [JsonPropertyName("english")] public string? English { get; set; }
        [JsonPropertyName("transposable")] public bool Transposable { get; set; }
        [JsonPropertyName("represents_mana")] public bool RepresentsMana { get; set; }
        [JsonPropertyName("appears_in_mana_costs")] public bool AppearsInManaCosts { get; set; }
        [JsonPropertyName("mana_value")] public decimal? ManaValue { get; set; }
        [JsonPropertyName("hybrid")] public bool Hybrid { get; set; }
        [JsonPropertyName("phyrexian")] public bool Phyrexian { get; set; }
        [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
    }
}
