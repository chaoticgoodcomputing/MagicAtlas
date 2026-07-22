using HotChocolate;
using HotChocolate.Types;
using MagicAtlas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MagicAtlas.Api.Resolvers;

/// <summary>
/// The P2 <b>deck resolver</b> surface (plan §3): two <i>computed</i> GraphQL fields the frontend's
/// <c>useCardNeighbours</c> / <c>useDeckAnalysis</c> hooks bind to. These are not table reads — they
/// join the seeded analytics tables (<see cref="PortRow"/>, <see cref="ComboRow"/>,
/// <see cref="CardRow"/>, <see cref="FamilyLatticeRow"/>) server-side so the client never ships 95k
/// combos to the browser.
///
/// Registered as a HotChocolate type extension on the Trax-generated <c>AtlasDiscoverQueries</c> object
/// (the GraphQL type behind <c>discover.atlas</c>; its name is
/// <c>PascalCase("atlas") + "DiscoverQueries"</c> — see Trax's <c>QueryModelTypeModule</c>). Wired in
/// <c>Program.cs</c> via <c>graphql.AddTypeExtension&lt;AtlasDeckResolver&gt;()</c>. The DTO shapes below
/// deliberately mirror the frontend mock types in <c>apps/atlas-web/src/data/mock.ts</c>
/// (<c>Candidate</c>, <c>CoverRow</c>/<c>CoverSide</c>, <c>Ring</c>, <c>NearMiss</c>/<c>NearMissCand</c>)
/// so a future frontend flip is a thin adapter, not a reshape.
/// </summary>
[ExtendObjectType("AtlasDiscoverQueries")]
public sealed class AtlasDeckResolver
{
    // Combo certainty ordering (Green > Amber > Red) for the rings/near-miss ranking below.
    private const int UnknownTierRank = 99;

    private static int TierRank(string? tier) => tier switch
    {
        "Green" => 0,
        "Amber" => 1,
        "Red" => 2,
        _ => UnknownTierRank,
    };

    // ADR 0004 #43: ports no longer carry a single tier — rank a card's ports by the split-out PROVENANCE
    // (parsed beats inferred beats declared). Conditionality is orthogonal and is surfaced, not ranked on.
    private static int ProvenanceRank(string? provenance) => provenance switch
    {
        "" or null => 0, // parsed (a real port)
        "Inferred" => 1,
        "Declared" => 2,
        _ => 3,
    };

    private static string BestTier(string? a, string? b) => TierRank(a) <= TierRank(b) ? (a ?? "") : (b ?? "");

    /// <summary>
    /// Ranked cards that have a port on <paramref name="family"/> for the given <paramref name="side"/>.
    /// <list type="bullet">
    /// <item><c>side="emit"</c> → cards whose emit-port family is <b>subsumed by</b> <paramref name="family"/>
    /// (the family, as a supergroup, covers the card's emit port — mirrors mock <c>emittersOf</c>).</item>
    /// <item><c>side="consume"</c> → cards whose consume-port <b>subsumes</b> <paramref name="family"/>
    /// (the card's consume port is a supergroup of the family — mirrors mock <c>consumersOf</c>).</item>
    /// </list>
    /// <c>via</c> flags a supergroup/subgroup (transitive) match where the port family differs from the
    /// requested family. Ranked by tier, then card popularity (<see cref="CardRow.EdhrecRank"/> asc).
    /// </summary>
    public async Task<IReadOnlyList<CandidateResult>> Candidates(
        string family,
        string side,
        int limit,
        [Service] IDbContextFactory<AtlasDbContext> dbFactory)
    {
        var normSide = (side ?? "").Trim().ToLowerInvariant();
        if (normSide is not ("emit" or "consume"))
            throw new GraphQLException($"side must be 'emit' or 'consume', got '{side}'.");
        if (limit <= 0) limit = 12;

        await using var db = await dbFactory.CreateDbContextAsync();

        var (subsOf, supersOf) = await LoadLatticeAsync(db);

        // Acceptable port families for the requested (family, side).
        //  emit:    families the requested family subsumes   → {family} ∪ subsOf[family]
        //  consume: families that subsume the requested one  → {family} ∪ supersOf[family]
        var acceptable = new HashSet<string> { family };
        if (normSide == "emit" && subsOf.TryGetValue(family, out var subs)) acceptable.UnionWith(subs);
        if (normSide == "consume" && supersOf.TryGetValue(family, out var sups)) acceptable.UnionWith(sups);

        var ports = await db.Ports
            .Where(p => p.Side == normSide && acceptable.Contains(p.Family))
            .Select(p => new { p.Card, p.Family, p.Conditionality, p.Provenance })
            .ToListAsync();

        if (ports.Count == 0) return Array.Empty<CandidateResult>();

        // Card ranking signal (edhrecRank: lower = more popular).
        var names = ports.Select(p => p.Card).Distinct().ToList();
        var rankByName = await db.Cards
            .Where(c => names.Contains(c.Name))
            .Select(c => new { c.Name, c.EdhrecRank })
            .ToListAsync();
        var rankLookup = rankByName
            .GroupBy(c => c.Name)
            .ToDictionary(g => g.Key, g => g.Min(x => x.EdhrecRank));

        // One row per card: keep the best (lowest provenance rank — parsed beats backfilled) port; `via` if
        // the matched port family differs from the requested family.
        var byCard = ports
            .GroupBy(p => p.Card)
            .Select(g =>
            {
                var best = g.OrderBy(p => ProvenanceRank(p.Provenance)).First();
                return new CandidateResult(
                    Card: g.Key,
                    Port: best.Family,
                    Conditionality: best.Conditionality ?? "",
                    Provenance: best.Provenance ?? "",
                    Confidence: null,
                    Via: best.Family != family);
            });

        int RankOf(string card) =>
            rankLookup.TryGetValue(card, out var r) && r.HasValue ? r.Value : int.MaxValue;

        return byCard
            .OrderBy(c => ProvenanceRank(c.Provenance))
            .ThenBy(c => RankOf(c.Card))
            .ThenBy(c => c.Card, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Deck-scoped analysis from a list of card names: per-family port <c>coverage</c> (emit/consume,
    /// with super/subgroup rollups), complete tiered <c>rings</c> whose card set is a subset of the deck,
    /// and <c>nearMiss</c> combos that are exactly one card short. Powers the Deck Lens.
    /// </summary>
    public async Task<DeckAnalysis> AnalyzeDeck(
        IReadOnlyList<string> cards,
        [Service] IDbContextFactory<AtlasDbContext> dbFactory)
    {
        var deckSet = new HashSet<string>(cards ?? Array.Empty<string>(), StringComparer.Ordinal);

        await using var db = await dbFactory.CreateDbContextAsync();

        var (subsOf, supersOf) = await LoadLatticeAsync(db);

        var coverage = await BuildCoverageAsync(db, deckSet, subsOf, supersOf);
        var (rings, nearMiss) = await BuildRingsAndNearMissAsync(db, deckSet);

        return new DeckAnalysis(coverage, rings, nearMiss);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>Load the family lattice as two directed maps: supergroup→subgroups and subgroup→supergroups.</summary>
    private static async Task<(Dictionary<string, HashSet<string>> SubsOf, Dictionary<string, HashSet<string>> SupersOf)>
        LoadLatticeAsync(AtlasDbContext db)
    {
        var edges = await db.FamilyLattices
            .Select(l => new { l.Family, l.SubFamily })
            .ToListAsync();

        var subsOf = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var supersOf = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            (subsOf.TryGetValue(e.Family, out var s) ? s : subsOf[e.Family] = new(StringComparer.Ordinal)).Add(e.SubFamily);
            (supersOf.TryGetValue(e.SubFamily, out var p) ? p : supersOf[e.SubFamily] = new(StringComparer.Ordinal)).Add(e.Family);
        }
        return (subsOf, supersOf);
    }

    private static async Task<IReadOnlyList<DeckCoverageRow>> BuildCoverageAsync(
        AtlasDbContext db,
        HashSet<string> deckSet,
        Dictionary<string, HashSet<string>> subsOf,
        Dictionary<string, HashSet<string>> supersOf)
    {
        if (deckSet.Count == 0) return Array.Empty<DeckCoverageRow>();

        var deckPorts = await db.Ports
            .Where(p => deckSet.Contains(p.Card))
            .Select(p => new { p.Family, p.Side })
            .ToListAsync();

        // direct[(family, side)] = count of deck ports with that exact family+side.
        var direct = deckPorts
            .GroupBy(p => (p.Family, p.Side))
            .ToDictionary(g => g.Key, g => g.Count());

        int Direct(string fam, string side) => direct.TryGetValue((fam, side), out var n) ? n : 0;

        // Families to report: every family that appears directly, plus any supergroup that
        // rolls up a subfamily present in the deck.
        var reportFamilies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in deckPorts)
        {
            reportFamilies.Add(p.Family);
            if (supersOf.TryGetValue(p.Family, out var sups)) reportFamilies.UnionWith(sups);
        }

        DeckCoverSide Side(string fam, string side)
        {
            var own = Direct(fam, side);
            var subs = subsOf.TryGetValue(fam, out var sf)
                ? sf.Select(s => new DeckSubCount(s, Direct(s, side))).Where(x => x.Count > 0).ToList()
                : new List<DeckSubCount>();
            return new DeckCoverSide(own, subs);
        }

        return reportFamilies
            .Select(fam =>
            {
                // `note` mirrors the mock's presentation hint: the supergroup this family rolls up into.
                var note = supersOf.TryGetValue(fam, out var sups) ? sups.OrderBy(x => x).FirstOrDefault() : null;
                return new DeckCoverageRow(fam, Side(fam, "emit"), Side(fam, "consume"), note);
            })
            .OrderByDescending(r => r.Emit.Own + r.Consume.Own
                + r.Emit.Subs.Sum(s => s.Count) + r.Consume.Subs.Sum(s => s.Count))
            .ThenBy(r => r.Family, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<(IReadOnlyList<DeckRing> Rings, IReadOnlyList<DeckNearMiss> NearMiss)>
        BuildRingsAndNearMissAsync(AtlasDbContext db, HashSet<string> deckSet)
    {
        if (deckSet.Count == 0)
            return (Array.Empty<DeckRing>(), Array.Empty<DeckNearMiss>());

        // A ring needs all cards in the deck (missing 0); a near-miss is missing exactly 1.
        // Prefilter by card count so we never materialize combos that can't qualify.
        var relevant = await db.Combos
            .Where(c => c.CardCount >= 1 && c.CardCount <= deckSet.Count + 1)
            .Select(c => new { c.Cards, c.FamilyRing, c.Tier, c.Results, c.Popularity, c.CardCount })
            .ToListAsync();

        var rings = new List<DeckRing>();
        // missing-card → (aggregated popularity, best tier, best combo for ring/results)
        var nearByCard = new Dictionary<string, NearAccum>(StringComparer.Ordinal);

        foreach (var c in relevant)
        {
            var parts = c.Cards.Split(" + ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) continue;

            var missing = parts.Where(p => !deckSet.Contains(p)).ToList();

            if (missing.Count == 0)
            {
                rings.Add(new DeckRing(c.Cards, c.FamilyRing, c.Tier, c.Popularity, Confidence: null));
            }
            else if (missing.Count == 1 && parts.Length >= 2)
            {
                var card = missing[0];
                if (!nearByCard.TryGetValue(card, out var acc))
                {
                    acc = new NearAccum();
                    nearByCard[card] = acc;
                }
                acc.Combos++;
                acc.TotalPopularity += c.Popularity;
                acc.BestTier = BestTier(acc.BestTier, c.Tier);
                if (c.Popularity >= acc.TopPopularity)
                {
                    acc.TopPopularity = c.Popularity;
                    acc.TopRing = c.FamilyRing;
                    acc.TopResults = c.Results;
                }
            }
        }

        rings = rings
            .OrderBy(r => TierRank(r.Tier))
            .ThenByDescending(r => r.Pop)
            .Take(100)
            .ToList();

        // Price join for the missing cards (best-effort — mirrors NearMissCand.price).
        var missingNames = nearByCard.Keys.ToList();
        var priceByName = missingNames.Count == 0
            ? new Dictionary<string, decimal?>()
            : (await db.Cards
                    .Where(c => missingNames.Contains(c.Name))
                    .Select(c => new { c.Name, c.PriceUsd })
                    .ToListAsync())
                .GroupBy(c => c.Name)
                .ToDictionary(g => g.Key, g => g.Min(x => x.PriceUsd));

        string Price(string name) =>
            priceByName.TryGetValue(name, out var p) && p.HasValue ? $"${p.Value:0.00}" : "—";

        var nearMiss = nearByCard
            .Select(kv =>
            {
                var (card, acc) = (kv.Key, kv.Value);
                var evidence = $"completes {acc.Combos} combo(s)"
                    + (string.IsNullOrEmpty(acc.TopResults) ? "" : $"; e.g. {acc.TopResults}");
                var cand = new DeckNearMissCand(card, evidence, Price(card), acc.TotalPopularity);
                return new DeckNearMiss(
                    Missing: card,
                    Ring: acc.TopRing,
                    ResultTier: acc.BestTier,
                    Cands: new List<DeckNearMissCand> { cand });
            })
            .OrderByDescending(n => n.Cands[0].Score)
            .ThenBy(n => TierRank(n.ResultTier))
            .Take(20)
            .ToList();

        return (rings, nearMiss);
    }

    private sealed class NearAccum
    {
        public int Combos;
        public int TotalPopularity;
        public int TopPopularity = -1;
        public string BestTier = "";
        public string TopRing = "";
        public string TopResults = "";
    }
}

// ── DTOs (mirror apps/atlas-web/src/data/mock.ts) ────────────────────────────

/// <summary>A ranked candidate card (mock <c>Candidate extends PoolCard</c>). ADR 0004 #43: the port tier
/// is split into <see cref="Conditionality"/> (is it conditional, and how) + <see cref="Provenance"/>
/// (parsed / Inferred / Declared).</summary>
public sealed record CandidateResult(
    string Card,
    string Port,
    string Conditionality,
    string Provenance,
    double? Confidence,
    bool Via);

/// <summary>Deck analysis payload (mock <c>useDeckAnalysis</c> result).</summary>
public sealed record DeckAnalysis(
    IReadOnlyList<DeckCoverageRow> Coverage,
    IReadOnlyList<DeckRing> Rings,
    IReadOnlyList<DeckNearMiss> NearMiss);

/// <summary>One coverage row (mock <c>CoverRow = [family, emit, consume]</c>).</summary>
public sealed record DeckCoverageRow(
    string Family,
    DeckCoverSide Emit,
    DeckCoverSide Consume,
    string? Note);

/// <summary>Emit/consume side of a coverage row (mock <c>CoverSide</c>).</summary>
public sealed record DeckCoverSide(
    int Own,
    IReadOnlyList<DeckSubCount> Subs);

/// <summary>A subgroup rollup count (mock <c>CoverSide.subs: [fam, count][]</c>).</summary>
public sealed record DeckSubCount(string Family, int Count);

/// <summary>A complete ring present in the deck (mock <c>Ring</c>).</summary>
public sealed record DeckRing(
    string Cards,
    string Ring,
    string Tier,
    int Pop,
    double? Confidence);

/// <summary>A "one card away" closer (mock <c>NearMiss</c>).</summary>
public sealed record DeckNearMiss(
    string Missing,
    string Ring,
    string ResultTier,
    IReadOnlyList<DeckNearMissCand> Cands);

/// <summary>A candidate that closes a near-miss (mock <c>NearMissCand</c>).</summary>
public sealed record DeckNearMissCand(
    string Name,
    string Evidence,
    string Price,
    int Score);
