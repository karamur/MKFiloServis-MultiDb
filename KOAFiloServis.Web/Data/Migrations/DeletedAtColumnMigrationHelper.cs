using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// BaseEntity.DeletedAt kolonunu, henüz sahip olmayan tablolara ekler (Kural 16).
/// Startup sırasında idempotent olarak çalışır.
/// </summary>
public static class DeletedAtColumnMigrationHelper
{
    public static async Task EnsureDeletedAtColumnAsync(ApplicationDbContext context)
    {
        var sql = @"
DO $$ DECLARE t record;
BEGIN
    FOR t IN
        SELECT table_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND column_name = 'IsDeleted'
          AND table_name NOT IN (
              SELECT table_name
              FROM information_schema.columns
              WHERE table_schema = 'public' AND column_name = 'DeletedAt'
          )
    LOOP
        EXECUTE 'ALTER TABLE ""' || t.table_name || '"" ADD COLUMN ""DeletedAt"" TIMESTAMP NULL';
        RAISE NOTICE 'DeletedAt eklendi: %', t.table_name;
    END LOOP;
END; $$;";

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    /// <summary>
    /// Eski DB'den aktarılan tablolarda eksik olabilecek kritik kolonları ekler.
    /// Her kolon için IF NOT EXISTS kontrolü yapar (idempotent).
    /// </summary>
    public static async Task EnsureMissingColumnsAsync(ApplicationDbContext context)
    {
        var fixes = new[]
        {
            // Firmalar
            @"ALTER TABLE ""Firmalar"" ADD COLUMN IF NOT EXISTS ""OrganizasyonId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""Firmalar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""Firmalar"" ADD COLUMN IF NOT EXISTS ""CariId"" INTEGER",
            @"ALTER TABLE ""Firmalar"" ADD COLUMN IF NOT EXISTS ""FirmaKodu"" VARCHAR(50)",
            // Araclar
            @"ALTER TABLE ""Araclar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""Araclar"" ADD COLUMN IF NOT EXISTS ""OrganizasyonId"" INTEGER",
            @"ALTER TABLE ""Araclar"" ADD COLUMN IF NOT EXISTS ""Aktif"" BOOLEAN DEFAULT true",
            // Guzergahlar
            @"ALTER TABLE ""Guzergahlar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // BankaHesaplari — dashboard finans verileri
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""IBAN"" VARCHAR(50)",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""AcilisBakiye"" DECIMAL(18,2) DEFAULT 0",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""Bakiye"" DECIMAL(18,2) DEFAULT 0",
            // Personel — dashboard sofor + belge uyarilari
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""KaynakFirmaId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""Aktif"" BOOLEAN DEFAULT true",
            @"ALTER TABLE ""PersonelMaaslari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""PersonelIzinleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Stok
            @"ALTER TABLE ""StokKartlari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Muhasebe
            @"ALTER TABLE ""MuhasebeFisleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""MuhasebeHesaplari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Cariler
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Hakedisler
            @"ALTER TABLE ""Hakedisler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Operasyon
            @"ALTER TABLE ""OperasyonKayitlari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // AracMasraflari
            @"ALTER TABLE ""AracMasraflari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
        };

        // Eksik tabloları oluştur (kaynak DB'den kopyala)
        var createTables = new[]
        {
            // PersonelAracAtamalari — sofor dashboard
            @"CREATE TABLE IF NOT EXISTS ""PersonelAracAtamalari"" (
                ""Id"" INTEGER NOT NULL, ""PersonelId"" INTEGER, ""AracId"" INTEGER, ""Plaka"" VARCHAR(20),
                ""AtamaTarihi"" TIMESTAMP, ""BitisTarihi"" TIMESTAMP, ""Aktif"" BOOLEAN DEFAULT true,
                ""IsDeleted"" BOOLEAN DEFAULT false, ""CreatedAt"" TIMESTAMP DEFAULT NOW(), ""UpdatedAt"" TIMESTAMP,
                PRIMARY KEY (""Id""))",
            // ServisCalismalari — dashboard servis + grafik
            @"CREATE TABLE IF NOT EXISTS ""ServisCalismalari"" (
                ""Id"" INTEGER NOT NULL, ""AracId"" INTEGER, ""SoforId"" INTEGER, ""GuzergahId"" INTEGER,
                ""BaslangicTarihi"" TIMESTAMP, ""BitisTarihi"" TIMESTAMP, ""CalismaTipi"" INTEGER,
                ""GunlukUcret"" DECIMAL(18,2), ""ToplamTutar"" DECIMAL(18,2),
                ""IsDeleted"" BOOLEAN DEFAULT false, ""CreatedAt"" TIMESTAMP DEFAULT NOW(), ""UpdatedAt"" TIMESTAMP,
                PRIMARY KEY (""Id""))",
        };

        foreach (var sql in createTables)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* tablo zaten var — sessiz geç */ }
        }

        foreach (var sql in fixes)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* kolon zaten var veya tablo yok — sessiz geç */ }
        }

        foreach (var sql in fixes)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* kolon zaten var veya tablo yok — sessiz geç */ }
        }
    }
}
