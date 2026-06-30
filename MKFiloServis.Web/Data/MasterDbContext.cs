using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data;

/// <summary>
/// [OBSOLETE] Nihai mimari (2026) ile kullanımdan kaldırılmıştır.
/// Tüm entity'ler artık tek <see cref="ApplicationDbContext"/> içinde yönetilir.
/// Bu sınıf yalnızca geriye dönük referanslar için korunmaktadır.
/// </summary>
[Obsolete("Nihai mimari: MasterDbContext yerine ApplicationDbContext kullanın (tek PostgreSQL).")]
public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<Firma> Firmalar { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<Lisans> Lisanslar { get; set; }
    public DbSet<Rol> Roller { get; set; }
    public DbSet<RolYetki> RolYetkileri { get; set; }
    public DbSet<AppAyarlari> AppAyarlari { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Firma>(entity =>
        {
            entity.HasIndex(e => e.FirmaKodu).IsUnique();
            entity.Property(e => e.FirmaKodu).HasMaxLength(50);
            entity.Property(e => e.FirmaAdi).HasMaxLength(250);
            entity.Property(e => e.VergiNo).HasMaxLength(11);
            entity.Property(e => e.DatabaseName).HasMaxLength(100);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasIndex(e => e.KullaniciAdi).IsUnique();
            entity.Property(e => e.KullaniciAdi).HasMaxLength(50);
            entity.Property(e => e.AdSoyad).HasMaxLength(100);
            entity.Property(e => e.IkiFaktorSecretKey).HasMaxLength(200);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.Kullanicilar)
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Restrict);
            // MasterDbContext'te olmayan navigation property'leri ignore et
            entity.Ignore(e => e.Bildirimler);
            entity.Ignore(e => e.GonderilenMesajlar);
            entity.Ignore(e => e.AlinanMesajlar);
            entity.Ignore(e => e.Hatirlaticilar);
            entity.Ignore(e => e.BagliCariler);
            entity.Ignore(e => e.DashboardWidgetlari);
            entity.Ignore(e => e.Sofor);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasIndex(e => e.RolAdi).IsUnique();
            entity.Property(e => e.RolAdi).HasMaxLength(50);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<RolYetki>(entity =>
        {
            entity.HasIndex(e => new { e.RolId, e.YetkiKodu }).IsUnique();
            entity.Property(e => e.YetkiKodu).HasMaxLength(100);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.Yetkiler)
                .HasForeignKey(e => e.RolId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<Lisans>(entity =>
        {
            entity.HasIndex(e => e.LisansAnahtari).IsUnique();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
        }
    }
}


