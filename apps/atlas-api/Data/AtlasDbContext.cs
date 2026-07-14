using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MagicAtlas.Api.Data;

public class AtlasDbContext : DbContext
{
    public AtlasDbContext(DbContextOptions<AtlasDbContext> options) : base(options) { }

    public DbSet<CardRow> Cards => Set<CardRow>();
    public DbSet<RulingRow> Rulings => Set<RulingRow>();
    public DbSet<SetRow> Sets => Set<SetRow>();
    public DbSet<CardSymbolRow> CardSymbols => Set<CardSymbolRow>();
    public DbSet<AtlasPointRow> AtlasPoints => Set<AtlasPointRow>();

    // ── CardAtlas analytics read models (D1–D4 + combo anchors + family lattice) ──
    public DbSet<PortRow> Ports => Set<PortRow>();
    public DbSet<ResourceFamilyRow> ResourceFamilies => Set<ResourceFamilyRow>();
    public DbSet<ResourceEdgeRow> ResourceEdges => Set<ResourceEdgeRow>();
    public DbSet<ComboRow> Combos => Set<ComboRow>();
    public DbSet<ArchetypeRow> Archetypes => Set<ArchetypeRow>();
    public DbSet<ComboAnchorRow> ComboAnchors => Set<ComboAnchorRow>();
    public DbSet<FamilyLatticeRow> FamilyLattices => Set<FamilyLatticeRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("atlas");

        var card = modelBuilder.Entity<CardRow>();
        card.HasIndex(c => c.Name);
        card.HasIndex(c => c.OracleId);
        card.HasIndex(c => c.Set);
        card.HasIndex(c => c.Rarity);
        card.HasIndex(c => c.Cmc);
        card.HasIndex(c => c.EdhrecRank);

        var ruling = modelBuilder.Entity<RulingRow>();
        ruling.HasIndex(r => r.OracleId);
        ruling.Property(r => r.Id).ValueGeneratedOnAdd();

        var set = modelBuilder.Entity<SetRow>();
        set.HasIndex(s => s.Code).IsUnique();
        set.HasIndex(s => s.ReleasedAt);
        set.HasIndex(s => s.SetType);

        var atlasPoint = modelBuilder.Entity<AtlasPointRow>();
        atlasPoint.Property(a => a.Id).ValueGeneratedOnAdd();
        atlasPoint.HasIndex(a => a.CardId);
        atlasPoint.HasIndex(a => a.TextType);

        // ── CardAtlas analytics read models — natural (non-generated) string keys ──
        var port = modelBuilder.Entity<PortRow>();
        port.HasKey(p => p.PortId);
        port.Property(p => p.PortId).ValueGeneratedNever();
        port.HasIndex(p => p.CardId);
        port.HasIndex(p => p.Card);
        port.HasIndex(p => p.Family);
        port.HasIndex(p => p.Side);

        var family = modelBuilder.Entity<ResourceFamilyRow>();
        family.HasKey(f => f.Family);
        family.Property(f => f.Family).ValueGeneratedNever();

        var edge = modelBuilder.Entity<ResourceEdgeRow>();
        edge.HasKey(e => e.Id);
        edge.Property(e => e.Id).ValueGeneratedNever();
        edge.HasIndex(e => e.FromFamily);
        edge.HasIndex(e => e.ToFamily);

        var combo = modelBuilder.Entity<ComboRow>();
        combo.HasKey(c => c.ComboId);
        combo.Property(c => c.ComboId).ValueGeneratedNever();
        combo.HasIndex(c => c.FamilySignature);
        combo.HasIndex(c => c.Tier);
        combo.HasIndex(c => c.Popularity);

        var archetype = modelBuilder.Entity<ArchetypeRow>();
        archetype.HasKey(a => a.Signature);
        archetype.Property(a => a.Signature).ValueGeneratedNever();
        archetype.HasIndex(a => a.RealizingCombos);

        var anchor = modelBuilder.Entity<ComboAnchorRow>();
        anchor.HasKey(a => a.Id);
        anchor.Property(a => a.Id).ValueGeneratedNever();
        anchor.HasIndex(a => a.CardId);
        anchor.HasIndex(a => a.PopularityMass);
        // CoStars is a list of a custom type; Trax owns the Npgsql data source (no EnableDynamicJson),
        // so serialize it to a JSON string ourselves — Npgsql writes string -> jsonb natively.
        anchor.Property(a => a.CoStars).HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ComboCoStarJson>>(v, (JsonSerializerOptions?)null) ?? new List<ComboCoStarJson>(),
            new ValueComparer<List<ComboCoStarJson>>(
                (l, r) => JsonSerializer.Serialize(l, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(r, (JsonSerializerOptions?)null),
                v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => JsonSerializer.Deserialize<List<ComboCoStarJson>>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<ComboCoStarJson>()));

        var lattice = modelBuilder.Entity<FamilyLatticeRow>();
        lattice.HasKey(l => l.Id);
        lattice.Property(l => l.Id).ValueGeneratedNever();
        lattice.HasIndex(l => l.Family);
    }
}
