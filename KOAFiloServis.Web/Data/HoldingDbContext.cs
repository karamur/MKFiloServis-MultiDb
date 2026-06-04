using KOAFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Data;

/// <summary>
/// [OBSOLETE] Nihai mimari (2026) ile kullanımdan kaldırılmıştır.
/// HoldingVeri ve HoldingRapor entity'leri artık <see cref="ApplicationDbContext"/> içinde yönetilir.
/// </summary>
[Obsolete("Nihai mimari: HoldingDbContext kaldırıldı. ApplicationDbContext kullanın.")]
public class HoldingDbContext : DbContext
{
    public HoldingDbContext(DbContextOptions<HoldingDbContext> options) : base(options) { }

    public DbSet<HoldingVeri> HoldingVeriler { get; set; }
    public DbSet<HoldingRapor> HoldingRaporlar { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HoldingVeri>(entity =>
        {
            entity.HasIndex(e => new { e.FirmaId, e.Yil, e.Ay, e.Kategori }).IsUnique();
            entity.Property(e => e.FirmaKodu).HasMaxLength(50);
            entity.Property(e => e.FirmaAdi).HasMaxLength(250);
            entity.Property(e => e.Kategori).HasMaxLength(50);
            entity.Property(e => e.JsonDetay).HasColumnType("text");
        });

        modelBuilder.Entity<HoldingRapor>(entity =>
        {
            entity.Property(e => e.Ad).HasMaxLength(250);
            entity.Property(e => e.Tip).HasMaxLength(50);
            entity.Property(e => e.JsonFiltreler).HasColumnType("text");
            entity.Property(e => e.JsonSonuc).HasColumnType("text");
            entity.Property(e => e.OlusturanKullanici).HasMaxLength(100);
        });
    }
}
