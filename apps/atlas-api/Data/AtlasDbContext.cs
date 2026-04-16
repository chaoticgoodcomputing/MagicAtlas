using Microsoft.EntityFrameworkCore;

namespace MagicAtlas.Api.Data;

public class AtlasDbContext : DbContext
{
    public AtlasDbContext(DbContextOptions<AtlasDbContext> options) : base(options) { }

    public DbSet<CardRow> Cards => Set<CardRow>();

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
    }
}
