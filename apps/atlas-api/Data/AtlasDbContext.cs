using Microsoft.EntityFrameworkCore;

namespace MagicAtlas.Api.Data;

public class AtlasDbContext : DbContext
{
    public AtlasDbContext(DbContextOptions<AtlasDbContext> options) : base(options) { }

    public DbSet<CardRow> Cards => Set<CardRow>();
    public DbSet<RulingRow> Rulings => Set<RulingRow>();
    public DbSet<SetRow> Sets => Set<SetRow>();
    public DbSet<CardSymbolRow> CardSymbols => Set<CardSymbolRow>();
    public DbSet<AtlasPointRow> AtlasPoints => Set<AtlasPointRow>();

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
    }
}
