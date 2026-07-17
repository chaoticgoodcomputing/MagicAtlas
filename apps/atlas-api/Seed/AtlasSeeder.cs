using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAtlas.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace MagicAtlas.Api.Seed;

/// <summary>
/// Seeds the <c>atlas</c> schema from Scryfall sources:
/// cards (oracle-cards bulk), rulings (bulk download), sets, and symbology.
/// </summary>
/// <remarks>
/// <para>Each table seeds independently and is idempotent-by-emptiness — if any rows exist in a given
/// table, that table is skipped. To re-seed, truncate the table (or drop the schema) and restart.</para>
/// <para>Cards prefer a local bulk file when <c>Atlas:ScryfallBulkPath</c> is configured and the file
/// exists (a fast path for machines that already have it); otherwise they stream from Scryfall's
/// oracle-cards bulk over HTTPS — the same two-stage (metadata → <c>download_uri</c> → stream)
/// download the rulings seed uses. Rulings, sets, and symbology always fetch on-demand from Scryfall's
/// HTTPS API. No manual download is required — a fresh clone self-seeds on first run.</para>
/// </remarks>
public sealed class AtlasSeeder
{
    private readonly IDbContextFactory<AtlasDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AtlasSeeder> _logger;
    private readonly string? _cardsBulkPath;

    private readonly string? _atlasPointsPath;

    private readonly string? _cardPortsPath;
    private readonly string? _cardEdgesPath;
    private readonly string? _resourceGraphPath;
    private readonly string? _comboInstancesPath;
    private readonly string? _archetypeCatalogPath;
    private readonly string? _comboAnchorReportPath;

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
        _cardPortsPath = Resolve(configuration["Atlas:CardPortsPath"], env.ContentRootPath);
        _cardEdgesPath = Resolve(configuration["Atlas:CardEdgesPath"], env.ContentRootPath);
        _resourceGraphPath = Resolve(configuration["Atlas:ResourceGraphPath"], env.ContentRootPath);
        _comboInstancesPath = Resolve(configuration["Atlas:ComboInstancesPath"], env.ContentRootPath);
        _archetypeCatalogPath = Resolve(configuration["Atlas:ArchetypeCatalogPath"], env.ContentRootPath);
        _comboAnchorReportPath = Resolve(configuration["Atlas:ComboAnchorReportPath"], env.ContentRootPath);
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
        await SeedPortsAsync(ct);
        await SeedPortEdgesAsync(ct);
        await SeedResourceFamiliesAsync(ct);
        await SeedResourceEdgesAsync(ct);
        await SeedCombosAsync(ct);
        await SeedArchetypesAsync(ct);
        await SeedComboAnchorsAsync(ct);
        await SeedFamilyLatticeAsync(ct);
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

        // Prefer a local bulk file when one is configured and present (fast path for machines that
        // already have it). Otherwise stream the oracle-cards bulk straight from Scryfall over HTTPS —
        // the same two-stage (metadata → download_uri → stream) fetch the rulings seed uses. No manual
        // curl, no browser: a fresh clone self-seeds.
        await using var stream = await OpenCardsBulkStreamAsync(ct);

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

    /// <summary>
    /// Opens the oracle-cards bulk as a stream: the local file at <c>Atlas:ScryfallBulkPath</c> when
    /// it exists, else Scryfall's HTTPS bulk download (resolve the rotating <c>download_uri</c> from
    /// the stable metadata endpoint, then stream it). Caller owns disposal.
    /// </summary>
    private async Task<Stream> OpenCardsBulkStreamAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_cardsBulkPath) && File.Exists(_cardsBulkPath))
        {
            _logger.LogInformation("Seeding cards from local bulk file {Path}...", _cardsBulkPath);
            return File.OpenRead(_cardsBulkPath);
        }

        var http = _httpFactory.CreateClient("scryfall");

        _logger.LogInformation("Resolving Scryfall oracle-cards bulk URL...");
        var bulkInfo = await http.GetFromJsonAsync<BulkDataInfo>(
            "https://api.scryfall.com/bulk-data/oracle-cards", ct)
            ?? throw new InvalidOperationException("Failed to resolve oracle-cards bulk-data info.");

        _logger.LogInformation("Downloading oracle-cards from {Uri} ({SizeMb} MB)...",
            bulkInfo.DownloadUri, bulkInfo.Size / (1024 * 1024));

        return await http.GetStreamAsync(bulkInfo.DownloadUri, ct);
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

    // ── CardAtlas analytics datasets (D1–D4 + anchors + lattice) ─────────
    //
    // These datasets are pipeline output (the CardAtlasFlow in the test/atlas-flows project), NOT a
    // Scryfall HTTP fetch, so every seeder is file-drop + skip-if-missing. Their JSON keys are camelCase
    // (the reporting records use [SerializedLabel("camelCase")]) — the Web JSON defaults match camelCase
    // case-insensitively, and each Raw DTO also pins [JsonPropertyName("camelCase")] explicitly.
    // Rows that reference cards by name resolve name → CardRow.Id via a once-built map, like the cards
    // seeder, so ports/anchors carry a joinable CardId Guid.

    private async Task SeedPortsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Ports.AnyAsync(ct))
        {
            _logger.LogInformation("Ports already seeded ({Count} rows) — skipping.", await db.Ports.CountAsync(ct));
            return;
        }

        if (!FileReady(_cardPortsPath, "Card ports")) return;

        _logger.LogInformation("Seeding ports from {Path}...", _cardPortsPath);

        var nameToId = await BuildCardNameMapAsync(db, ct);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var perCardIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var batch = new List<PortRow>(capacity: 1000);
        var total = 0;

        await using var stream = File.OpenRead(_cardPortsPath!);
        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawPort>(stream, options, ct))
        {
            if (raw is null) continue;

            var card = raw.Card ?? "";
            var index = perCardIndex.TryGetValue(card, out var i) ? i : 0;
            perCardIndex[card] = index + 1;

            batch.Add(new PortRow
            {
                PortId = $"{Slug(card)}#{index}",
                CardId = nameToId.TryGetValue(card, out var id) ? id : Guid.Empty,
                Card = card,
                Label = raw.Label ?? "",
                Family = raw.Family ?? "",
                Side = raw.Side ?? "",
                Tier = raw.Tier,
                Confidence = raw.Confidence,
                OracleLineIndex = raw.OracleLineIndex ?? 0,
                Spans = raw.Spans,
                Stem = raw.Stem,
                Manner = raw.Manner,
                IsSelf = raw.IsSelf,
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

        _logger.LogInformation("Ports: {Total} rows.", total);
    }

    /// <summary>
    /// Seeds <c>atlas.port_edges</c> — the denormalized port→port interaction graph — from the engine's
    /// materialized union (<c>card-edges.json</c>). Runs AFTER ports+cards are seeded (it denormalizes off
    /// them). Streams the (large) edge dump and bulk-loads via Npgsql binary COPY, computing each row's
    /// denormalized columns in-stream from small in-memory maps (target-card attrs; the target card's
    /// emit-families = the <c>target_reaches</c> second-degree tags). Drops its secondary indexes before
    /// the COPY and rebuilds them after — fastest load, and the seeder owns these indexes (Trax/EF create
    /// only the table + PK). Idempotent-by-emptiness like the other seeds.
    /// </summary>
    private async Task SeedPortEdgesAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.PortEdges.AnyAsync(ct))
        {
            _logger.LogInformation("Port edges already seeded ({Count} rows) — skipping.", await db.PortEdges.CountAsync(ct));
            return;
        }

        if (!FileReady(_cardEdgesPath, "Card edges")) return;

        _logger.LogInformation("Seeding port edges from {Path}...", _cardEdgesPath);

        // Denormalization sources (small, in-memory): each target card's emit-side families (the
        // second-degree reachability), and its cmc/edhrec/colors.
        var reachesByCard = (await db.Ports
                .Where(p => p.Side == "emit")
                .Select(p => new { p.Card, p.Family })
                .ToListAsync(ct))
            .GroupBy(p => p.Card, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Family).Distinct().ToArray(), StringComparer.Ordinal);

        // Card names are NOT unique (token cards like "Soldier", split/DFC printings), so build the map
        // last-wins rather than ToDictionary (which throws on a dup key). cmc/colors/edhrec are stable
        // across printings of the same name, so any printing's attrs are fine.
        var attrsByCard = new Dictionary<string, (double Cmc, int? Edhrec, string[] Colors)>(StringComparer.Ordinal);
        foreach (var c in await db.Cards
                     .Select(c => new { c.Name, c.Cmc, c.EdhrecRank, c.Colors })
                     .ToListAsync(ct))
            attrsByCard[c.Name] = ((double)c.Cmc, c.EdhrecRank, (c.Colors ?? new()).ToArray());

        // (card, label) -> engine family, so each edge endpoint carries its family denormalized (the
        // frontend renders/colors the hop without re-deriving families client-side).
        var famByPort = new Dictionary<(string Card, string Label), string>();
        foreach (var p in await db.Ports.Select(p => new { p.Card, p.Label, p.Family }).ToListAsync(ct))
            famByPort[(p.Card, p.Label)] = p.Family;

        // Combo-popularity back-annotation: an edge's relevance = the max popularity of any combo whose
        // card set contains BOTH its endpoints (a co-occurrence proxy — the exact cycle-membership would
        // need the reconstructed edge list, but "both cards in a popular combo" is the right relevance
        // signal and sinks the ~inert edges to 0). Combo.Cards is "A + B [+ C…]".
        var pairPop = new Dictionary<(string From, string To), int>();
        foreach (var combo in await db.Combos.Select(c => new { c.Cards, c.Popularity }).ToListAsync(ct))
        {
            var cards = combo.Cards.Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < cards.Length; i++)
                for (var j = 0; j < cards.Length; j++)
                {
                    if (i == j) continue;
                    var key = (cards[i], cards[j]);
                    if (!pairPop.TryGetValue(key, out var cur) || combo.Popularity > cur)
                        pairPop[key] = combo.Popularity;
                }
        }

        var indexNames = new[]
        {
            "ix_port_edges_from", "ix_port_edges_to",
            "ix_port_edges_reaches", "ix_port_edges_source_reaches",
            "ix_port_edges_edhrec", "ix_port_edges_cmc",
            "ix_port_edges_from_edhrec", "ix_port_edges_from_cmc",
            "ix_port_edges_popularity",
        };
        foreach (var ix in indexNames)
            db.Database.ExecuteSqlRaw($"DROP INDEX IF EXISTS atlas.{ix}");

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var empty = Array.Empty<string>();
        long total = 0;

        await using (var writer = await conn.BeginBinaryImportAsync(
            "COPY atlas.port_edges (from_card, from_label, from_family, to_card, to_label, to_family, "
                + "relation, tier, from_cmc, from_edhrec, from_colors, source_reaches, "
                + "to_cmc, to_edhrec, to_colors, target_reaches, popularity) FROM STDIN (FORMAT BINARY)",
            ct))
        {
            await using var stream = File.OpenRead(_cardEdgesPath!);
            await foreach (var e in JsonSerializer.DeserializeAsyncEnumerable<RawEdge>(stream, options, ct))
            {
                if (e is null) continue;
                var fromCard = e.FromCard ?? "";
                var toCard = e.ToCard ?? "";

                // The broad pairwise graph is over REAL cards only. Skip copy-graft synthetic identities
                // ("X copy of Y" — ~97% of the engine's raw union) and any other non-card node: copy
                // interactions are set-contextual and belong in the combo view (design §4), not this
                // pairwise edge table. Requiring both endpoints be real cards also guarantees every seeded
                // row carries its denormalized attrs on both sides.
                if (!attrsByCard.TryGetValue(fromCard, out var fromAttr)
                    || !attrsByCard.TryGetValue(toCard, out var toAttr))
                    continue;
                var toReaches = reachesByCard.TryGetValue(toCard, out var tr) ? tr : empty;
                var fromReaches = reachesByCard.TryGetValue(fromCard, out var fr) ? fr : empty;

                var fromLabel = e.FromLabel ?? "";
                var toLabel = e.ToLabel ?? "";

                await writer.StartRowAsync(ct);
                await writer.WriteAsync(fromCard, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(fromLabel, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(famByPort.GetValueOrDefault((fromCard, fromLabel), ""), NpgsqlDbType.Text, ct);
                await writer.WriteAsync(toCard, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(toLabel, NpgsqlDbType.Text, ct);
                await writer.WriteAsync(famByPort.GetValueOrDefault((toCard, toLabel), ""), NpgsqlDbType.Text, ct);
                await writer.WriteAsync(e.Family ?? "", NpgsqlDbType.Text, ct);
                if (e.Tier is null) await writer.WriteNullAsync(ct);
                else await writer.WriteAsync(e.Tier, NpgsqlDbType.Text, ct);
                // source (feeder) side
                await writer.WriteAsync(fromAttr.Cmc, NpgsqlDbType.Double, ct);
                if (fromAttr.Edhrec is int fedh) await writer.WriteAsync(fedh, NpgsqlDbType.Integer, ct);
                else await writer.WriteNullAsync(ct);
                await writer.WriteAsync(fromAttr.Colors, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(fromReaches, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                // target (consumer) side
                await writer.WriteAsync(toAttr.Cmc, NpgsqlDbType.Double, ct);
                if (toAttr.Edhrec is int tedh) await writer.WriteAsync(tedh, NpgsqlDbType.Integer, ct);
                else await writer.WriteNullAsync(ct);
                await writer.WriteAsync(toAttr.Colors, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(toReaches, NpgsqlDbType.Array | NpgsqlDbType.Text, ct);
                await writer.WriteAsync(
                    pairPop.TryGetValue((fromCard, toCard), out var pop) ? pop : 0, NpgsqlDbType.Integer, ct);
                total++;
            }
            await writer.CompleteAsync(ct);
        }

        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_from ON atlas.port_edges (from_card, from_label)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_to ON atlas.port_edges (to_card, to_label)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_reaches ON atlas.port_edges USING GIN (target_reaches)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_source_reaches ON atlas.port_edges USING GIN (source_reaches)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_edhrec ON atlas.port_edges (to_edhrec)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_cmc ON atlas.port_edges (to_cmc)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_from_edhrec ON atlas.port_edges (from_edhrec)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_from_cmc ON atlas.port_edges (from_cmc)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS ix_port_edges_popularity ON atlas.port_edges (popularity DESC)");

        _logger.LogInformation("Port edges: {Total} rows.", total);
    }

    private async Task SeedResourceFamiliesAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.ResourceFamilies.AnyAsync(ct))
        {
            _logger.LogInformation("Resource families already seeded ({Count} rows) — skipping.", await db.ResourceFamilies.CountAsync(ct));
            return;
        }

        if (!FileReady(_resourceGraphPath, "Resource graph")) return;

        _logger.LogInformation("Seeding resource families from {Path}...", _resourceGraphPath);

        var graph = await ReadObjectAsync<RawResourceGraph>(_resourceGraphPath!, ct);
        var rows = (graph?.Stations ?? new List<RawResourceStation>())
            .Where(s => !string.IsNullOrWhiteSpace(s.Family))
            .Select(s => new ResourceFamilyRow
            {
                Family = s.Family!,
                Cards = s.Cards,
                Labels = s.Labels,
            })
            .ToList();

        db.ResourceFamilies.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Resource families: {Total} rows.", rows.Count);
    }

    private async Task SeedResourceEdgesAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.ResourceEdges.AnyAsync(ct))
        {
            _logger.LogInformation("Resource edges already seeded ({Count} rows) — skipping.", await db.ResourceEdges.CountAsync(ct));
            return;
        }

        if (!FileReady(_resourceGraphPath, "Resource graph")) return;

        _logger.LogInformation("Seeding resource edges from {Path}...", _resourceGraphPath);

        var graph = await ReadObjectAsync<RawResourceGraph>(_resourceGraphPath!, ct);
        var rows = (graph?.Lines ?? new List<RawResourceLine>())
            .Where(l => !string.IsNullOrWhiteSpace(l.From) && !string.IsNullOrWhiteSpace(l.To))
            .Select(l => new ResourceEdgeRow
            {
                Id = $"{l.From}>{l.To}",
                FromFamily = l.From!,
                ToFamily = l.To!,
                RealizingCombos = l.RealizingCombos,
                BestTier = l.BestTier ?? "",
                Engine = l.Engine,
                Origin = l.Origin,
            })
            .ToList();

        db.ResourceEdges.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Resource edges: {Total} rows.", rows.Count);
    }

    private async Task SeedCombosAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Combos.AnyAsync(ct))
        {
            _logger.LogInformation("Combos already seeded ({Count} rows) — skipping.", await db.Combos.CountAsync(ct));
            return;
        }

        if (!FileReady(_comboInstancesPath, "Combo instances")) return;

        _logger.LogInformation("Seeding combos from {Path}...", _comboInstancesPath);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var batch = new List<ComboRow>(capacity: 1000);
        // combo-instances.json lists a combo once per realizing family ring, so the same comboId
        // recurs; dedupe on the primary key or EF's change tracker rejects the second instance.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;

        await using var stream = File.OpenRead(_comboInstancesPath!);
        await foreach (var raw in JsonSerializer.DeserializeAsyncEnumerable<RawCombo>(stream, options, ct))
        {
            if (raw is null || string.IsNullOrWhiteSpace(raw.ComboId)) continue;
            if (!seen.Add(raw.ComboId!)) continue;

            batch.Add(new ComboRow
            {
                ComboId = raw.ComboId!,
                Cards = raw.Cards ?? "",
                CardCount = raw.CardCount,
                FamilySignature = raw.FamilySignature ?? "",
                FamilyRing = raw.FamilyRing ?? "",
                Tier = raw.Tier ?? "",
                Firable = raw.Firable,
                Results = raw.Results ?? "",
                Popularity = raw.Popularity,
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

        _logger.LogInformation("Combos: {Total} rows.", total);
    }

    private async Task SeedArchetypesAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.Archetypes.AnyAsync(ct))
        {
            _logger.LogInformation("Archetypes already seeded ({Count} rows) — skipping.", await db.Archetypes.CountAsync(ct));
            return;
        }

        if (!FileReady(_archetypeCatalogPath, "Archetype catalog")) return;

        _logger.LogInformation("Seeding archetypes from {Path}...", _archetypeCatalogPath);

        var catalog = await ReadObjectAsync<RawArchetypeCatalog>(_archetypeCatalogPath!, ct);
        var rows = (catalog?.Entries ?? new List<RawArchetypeEntry>())
            .Where(e => !string.IsNullOrWhiteSpace(e.Families))
            // Signature = sorted Families join — the stable, order-independent archetype key.
            .Select(e => new ArchetypeRow
            {
                Signature = SortedSignature(e.Families!),
                Families = e.Families!,
                FamilyCount = e.FamilyCount,
                RealizingCombos = e.RealizingCombos,
                BestTier = e.BestTier ?? "",
                GreenFraction = e.GreenFraction,
                ExampleCards = e.ExampleCards ?? "",
                Results = e.Results ?? "",
            })
            // Guard against duplicate signatures (defensive; the catalog is already distinct by families).
            .GroupBy(r => r.Signature)
            .Select(g => g.First())
            .ToList();

        db.Archetypes.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Archetypes: {Total} rows.", rows.Count);
    }

    private async Task SeedComboAnchorsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.ComboAnchors.AnyAsync(ct))
        {
            _logger.LogInformation("Combo anchors already seeded ({Count} rows) — skipping.", await db.ComboAnchors.CountAsync(ct));
            return;
        }

        if (!FileReady(_comboAnchorReportPath, "Combo anchor report")) return;

        _logger.LogInformation("Seeding combo anchors from {Path}...", _comboAnchorReportPath);

        var nameToId = await BuildCardNameMapAsync(db, ct);
        var report = await ReadObjectAsync<RawComboAnchorReport>(_comboAnchorReportPath!, ct);

        var rows = (report?.TopAnchors ?? new List<RawComboAnchor>())
            .Where(a => !string.IsNullOrWhiteSpace(a.Card))
            .GroupBy(a => a.Card!)
            .Select(g => g.First())
            .Select(a => new ComboAnchorRow
            {
                Id = a.Card!,
                CardId = nameToId.TryGetValue(a.Card!, out var id) ? id : Guid.Empty,
                Card = a.Card!,
                TypeLine = a.TypeLine ?? "",
                BlockReason = a.BlockReason ?? "",
                BlockedComboCount = a.BlockedComboCount,
                SoleBlockerCount = a.SoleBlockerCount,
                PopularityMass = a.PopularityMass,
                MaxComboPopularity = a.MaxComboPopularity,
                TopPayoffs = a.TopPayoffs ?? new(),
                CoStars = (a.CoStars ?? new List<RawComboCoStar>())
                    .Select(c => new ComboCoStarJson
                    {
                        Card = c.Card ?? "",
                        SharedCombos = c.SharedCombos,
                        SharedPopularity = c.SharedPopularity,
                        AlsoUnparsed = c.AlsoUnparsed,
                    })
                    .ToList(),
            })
            .ToList();

        db.ComboAnchors.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Combo anchors: {Total} rows.", rows.Count);
    }

    /// <summary>
    /// The family super/subgroup lattice — the containment set the client currently hardcodes
    /// (<c>GROUPS = { death:["sacrifice"], card:["mill"] }</c>), now served as data.
    /// <para>
    /// <b>Deferred — the lattice is NOT derivable from the family grammar today (ledger-W2 Track B finding).</b>
    /// <c>FamilyGrammar</c>/<c>FamilyEdge</c> (<c>libs/mast-interaction/Interaction.cs</c>) encode
    /// <em>directional flow/modifier edges</em> — <c>from → to</c> on a <c>ResourceKind</c>, tagged
    /// <c>EdgeFamily.Flow</c> ("A emits R, B consumes R") or <c>EdgeFamily.Modifier</c> ("A rewrites B's
    /// emission"). Those are resource HANDOFFS between labels, not super/sub CONTAINMENTS ("every
    /// sacrifice counts-as a death"). Likewise <c>ResourceFamilies</c>
    /// (<c>libs/atlas-flows/Flows/Shared/ResourceFamilies.cs</c>) is a <em>flat</em> label→family map with
    /// no super/sub axis, and neither <c>"card"</c> nor <c>"mill"</c> is even a family in its canonical set —
    /// so the two hardcoded containments cannot be projected from it either. No file in the grammar or the
    /// taxonomy authors a sub⊇super / counts-as relation, so promoting one would be fabrication.
    /// </para>
    /// <para>
    /// <b>To make it real:</b> author sub→super containments declaratively (either a new
    /// <c>counts-as</c>/<c>subFamily→superFamily</c> facet on <c>ResourceFamilies</c>, or a sibling JSON
    /// grammar next to the family-edge JSON — e.g. rows <c>{ subFamily, superFamily }</c>), emit them from
    /// the CardAtlas / FamilyRollup path as <c>_08_Reporting/family-lattice.json</c>
    /// (<c>FamilyLatticeRow</c>-shaped: <c>id "family&gt;subFamily"</c>, <c>family</c>, <c>subFamily</c>),
    /// then read that here via config key <c>Atlas:FamilyLatticePath</c> with the hardcode below as the
    /// absent-file fallback. Kept hardcoded until the containment taxonomy is authored upstream.
    /// </para>
    /// </summary>
    private async Task SeedFamilyLatticeAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (await db.FamilyLattices.AnyAsync(ct))
        {
            _logger.LogInformation("Family lattice already seeded ({Count} rows) — skipping.", await db.FamilyLattices.CountAsync(ct));
            return;
        }

        var containments = new (string Family, string SubFamily)[]
        {
            ("death", "sacrifice"),
            ("card", "mill"),
        };

        var rows = containments
            .Select(c => new FamilyLatticeRow
            {
                Id = $"{c.Family}>{c.SubFamily}",
                Family = c.Family,
                SubFamily = c.SubFamily,
            })
            .ToList();

        db.FamilyLattices.AddRange(rows);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Family lattice: {Total} rows.", rows.Count);
    }

    // ── CardAtlas seeding helpers ────────────────────────────────────────

    /// <summary>True if the dataset file is present; logs a skip note and returns false otherwise.</summary>
    private bool FileReady(string? path, string label)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) return true;

        _logger.LogInformation(
            "{Label} file not found at '{Path}' — skipping. Run the CardAtlas pipeline to generate it.",
            label, path ?? "<unset>");
        return false;
    }

    /// <summary>Builds a name → CardRow.Id map once, so name-keyed datasets can carry a joinable Guid.</summary>
    private static async Task<Dictionary<string, Guid>> BuildCardNameMapAsync(AtlasDbContext db, CancellationToken ct)
    {
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var pairs = await db.Cards
            .AsNoTracking()
            .Select(c => new { c.Name, c.Id })
            .ToListAsync(ct);

        foreach (var p in pairs)
            map.TryAdd(p.Name, p.Id);

        return map;
    }

    private static async Task<T?> ReadObjectAsync<T>(string path, CancellationToken ct)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, options, ct);
    }

    /// <summary>A URL/id-safe slug for a card name: lowercase alphanumerics, other runs collapsed to '-'.</summary>
    private static string Slug(string s)
    {
        var chars = new char[s.Length];
        var n = 0;
        var lastDash = false;
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
            {
                chars[n++] = char.ToLowerInvariant(ch);
                lastDash = false;
            }
            else if (!lastDash && n > 0)
            {
                chars[n++] = '-';
                lastDash = true;
            }
        }
        var slug = new string(chars, 0, n).TrimEnd('-');
        return slug.Length == 0 ? "card" : slug;
    }

    /// <summary>Sorted, comma-joined normalization of a family-signature string (order-independent key).</summary>
    private static string SortedSignature(string families) =>
        string.Join(", ", families
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(f => f, StringComparer.Ordinal));

    // ── Raw CardAtlas DTOs (camelCase dumps) ─────────────────────────────

    private sealed class RawPort
    {
        [JsonPropertyName("card")] public string? Card { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("family")] public string? Family { get; set; }
        [JsonPropertyName("side")] public string? Side { get; set; }
        // §4 provenance / backfill fields — absent in the current dump, populated once those passes land.
        [JsonPropertyName("tier")] public string? Tier { get; set; }
        [JsonPropertyName("confidence")] public double? Confidence { get; set; }
        [JsonPropertyName("oracleLineIndex")] public int? OracleLineIndex { get; set; }
        [JsonPropertyName("spans")] public int[][]? Spans { get; set; }
        // ADR-0003 structured facets (Stage 4).
        [JsonPropertyName("stem")] public string? Stem { get; set; }
        [JsonPropertyName("manner")] public string? Manner { get; set; }
        [JsonPropertyName("isSelf")] public bool IsSelf { get; set; }
    }

    /// <summary>One row of <c>card-edges.json</c> (the engine's materialized port→port union).</summary>
    private sealed class RawEdge
    {
        [JsonPropertyName("fromCard")] public string? FromCard { get; set; }
        [JsonPropertyName("fromLabel")] public string? FromLabel { get; set; }
        [JsonPropertyName("toCard")] public string? ToCard { get; set; }
        [JsonPropertyName("toLabel")] public string? ToLabel { get; set; }
        [JsonPropertyName("family")] public string? Family { get; set; }
        [JsonPropertyName("tier")] public string? Tier { get; set; }
    }

    private sealed class RawResourceGraph
    {
        [JsonPropertyName("stations")] public List<RawResourceStation>? Stations { get; set; }
        [JsonPropertyName("lines")] public List<RawResourceLine>? Lines { get; set; }
    }

    private sealed class RawResourceStation
    {
        [JsonPropertyName("family")] public string? Family { get; set; }
        [JsonPropertyName("cards")] public int Cards { get; set; }
        [JsonPropertyName("labels")] public int Labels { get; set; }
    }

    private sealed class RawResourceLine
    {
        [JsonPropertyName("from")] public string? From { get; set; }
        [JsonPropertyName("to")] public string? To { get; set; }
        [JsonPropertyName("realizingCombos")] public int RealizingCombos { get; set; }
        [JsonPropertyName("bestTier")] public string? BestTier { get; set; }
        [JsonPropertyName("engine")] public bool Engine { get; set; }
        [JsonPropertyName("origin")] public string? Origin { get; set; }
    }

    private sealed class RawCombo
    {
        [JsonPropertyName("comboId")] public string? ComboId { get; set; }
        [JsonPropertyName("cards")] public string? Cards { get; set; }
        [JsonPropertyName("cardCount")] public int CardCount { get; set; }
        [JsonPropertyName("familySignature")] public string? FamilySignature { get; set; }
        [JsonPropertyName("familyRing")] public string? FamilyRing { get; set; }
        [JsonPropertyName("tier")] public string? Tier { get; set; }
        [JsonPropertyName("firable")] public bool Firable { get; set; }
        [JsonPropertyName("results")] public string? Results { get; set; }
        [JsonPropertyName("popularity")] public int Popularity { get; set; }
    }

    private sealed class RawArchetypeCatalog
    {
        [JsonPropertyName("entries")] public List<RawArchetypeEntry>? Entries { get; set; }
    }

    private sealed class RawArchetypeEntry
    {
        [JsonPropertyName("families")] public string? Families { get; set; }
        [JsonPropertyName("familyCount")] public int FamilyCount { get; set; }
        [JsonPropertyName("realizingCombos")] public int RealizingCombos { get; set; }
        [JsonPropertyName("bestTier")] public string? BestTier { get; set; }
        [JsonPropertyName("greenFraction")] public double GreenFraction { get; set; }
        [JsonPropertyName("exampleCards")] public string? ExampleCards { get; set; }
        [JsonPropertyName("results")] public string? Results { get; set; }
    }

    private sealed class RawComboAnchorReport
    {
        [JsonPropertyName("topAnchors")] public List<RawComboAnchor>? TopAnchors { get; set; }
    }

    private sealed class RawComboAnchor
    {
        [JsonPropertyName("card")] public string? Card { get; set; }
        [JsonPropertyName("typeLine")] public string? TypeLine { get; set; }
        [JsonPropertyName("blockReason")] public string? BlockReason { get; set; }
        [JsonPropertyName("blockedComboCount")] public int BlockedComboCount { get; set; }
        [JsonPropertyName("soleBlockerCount")] public int SoleBlockerCount { get; set; }
        [JsonPropertyName("popularityMass")] public long PopularityMass { get; set; }
        [JsonPropertyName("maxComboPopularity")] public int MaxComboPopularity { get; set; }
        [JsonPropertyName("topPayoffs")] public List<string>? TopPayoffs { get; set; }
        [JsonPropertyName("coStars")] public List<RawComboCoStar>? CoStars { get; set; }
    }

    private sealed class RawComboCoStar
    {
        [JsonPropertyName("card")] public string? Card { get; set; }
        [JsonPropertyName("sharedCombos")] public int SharedCombos { get; set; }
        [JsonPropertyName("sharedPopularity")] public long SharedPopularity { get; set; }
        [JsonPropertyName("alsoUnparsed")] public bool AlsoUnparsed { get; set; }
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
