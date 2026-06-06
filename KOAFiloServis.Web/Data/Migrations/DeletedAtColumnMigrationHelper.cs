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
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""HesapKodu"" VARCHAR(50)",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""HesapAdi"" VARCHAR(200)",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""IBAN"" VARCHAR(50)",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""AcilisBakiye"" DECIMAL(18,2) DEFAULT 0",
            @"ALTER TABLE ""BankaHesaplari"" ADD COLUMN IF NOT EXISTS ""Bakiye"" DECIMAL(18,2) DEFAULT 0",
            // BankaKasaHareketleri — dashboard finans hareket listesi
            @"ALTER TABLE ""BankaKasaHareketleri"" ADD COLUMN IF NOT EXISTS ""AracId"" INTEGER",
            // Personel — dashboard sofor + belge uyarilari
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""KaynakFirmaId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""KaynakKayitId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""TasimaTedarikciId"" INTEGER",
            @"ALTER TABLE ""Personeller"" ADD COLUMN IF NOT EXISTS ""MuhasebeHesapId"" INTEGER",
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
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""KaynakFirmaId"" INTEGER",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""KaynakKayitId"" INTEGER",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""MuhasebeHesapId"" INTEGER",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""PersonelAvansHesapId"" INTEGER",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""Borc"" DECIMAL(18,2) DEFAULT 0",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""Alacak"" DECIMAL(18,2) DEFAULT 0",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""SoforId"" INTEGER",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""SozlesmeBaslangicTarihi"" TIMESTAMP",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""SozlesmeBitisTarihi"" TIMESTAMP",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""SozlesmeNo"" VARCHAR(100)",
            @"ALTER TABLE ""Cariler"" ADD COLUMN IF NOT EXISTS ""YetkiliKisi"" VARCHAR(200)",
            // Faturalar
            @"ALTER TABLE ""Faturalar"" ADD COLUMN IF NOT EXISTS ""HakedisId"" INTEGER",
            // Hakedisler
            @"ALTER TABLE ""Hakedisler"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // Operasyon
            @"ALTER TABLE ""OperasyonKayitlari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // AracMasraflari
            @"ALTER TABLE ""AracMasraflari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            // BankaKasaHareketleri + BudgetMasrafKalemleri — butce dashboard
            @"ALTER TABLE ""BankaKasaHareketleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""BudgetMasrafKalemleri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""BudgetMasrafKalemleri"" ADD COLUMN IF NOT EXISTS ""Icon"" VARCHAR(100)",
            @"ALTER TABLE ""BudgetMasrafKalemleri"" ADD COLUMN IF NOT EXISTS ""Renk"" VARCHAR(20)",
            @"ALTER TABLE ""BudgetMasrafKalemleri"" ADD COLUMN IF NOT EXISTS ""SiraNo"" INTEGER DEFAULT 0",
            @"ALTER TABLE ""BudgetMasrafKalemleri"" ADD COLUMN IF NOT EXISTS ""Kategori"" VARCHAR(100)",
            // Kural 5: IFirmaTenant implementasyonu olan ancak FirmaId kolonu eksik kritik tablolar
            @"ALTER TABLE ""AktiviteLoglar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER",
            @"ALTER TABLE ""Kullanicilar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""Roller"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""RolYetkileri"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""Lisanslar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""AppAyarlari"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
            @"ALTER TABLE ""Organizasyonlar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
        };

        // Eksik tabloları oluştur (EF entity modeli ile uyumlu)
        var createTables = new[]
        {
            // PersonelAracAtamalari — sofor dashboard (Sofor.cs: PersonelAracAtama entity)
            @"CREATE TABLE IF NOT EXISTS ""PersonelAracAtamalari"" (
                ""Id"" INTEGER NOT NULL, ""SoforId"" INTEGER NOT NULL, ""AracId"" INTEGER NOT NULL,
                ""BaslangicTarihi"" TIMESTAMP NOT NULL DEFAULT NOW(), ""BitisTarihi"" TIMESTAMP,
                ""Aktif"" BOOLEAN DEFAULT true, ""Notlar"" TEXT,
                ""IsDeleted"" BOOLEAN DEFAULT false, ""CreatedAt"" TIMESTAMP DEFAULT NOW(), ""UpdatedAt"" TIMESTAMP,
                ""DeletedAt"" TIMESTAMP, ""FirmaId"" INTEGER,
                PRIMARY KEY (""Id""))",
            // ServisCalismalari — dashboard servis + grafik (ServisCalisma.cs entity)
            @"CREATE TABLE IF NOT EXISTS ""ServisCalismalari"" (
                ""Id"" INTEGER NOT NULL, ""CalismaTarihi"" TIMESTAMP NOT NULL,
                ""ServisTuru"" INTEGER NOT NULL, ""Fiyat"" DECIMAL(18,2),
                ""KmBaslangic"" INTEGER, ""KmBitis"" INTEGER,
                ""BaslangicSaati"" INTERVAL, ""BitisSaati"" INTERVAL,
                ""ArizaOlduMu"" BOOLEAN DEFAULT false, ""ArizaAciklamasi"" TEXT,
                ""Durum"" INTEGER NOT NULL DEFAULT 0, ""Notlar"" TEXT,
                ""AracId"" INTEGER NOT NULL, ""SoforId"" INTEGER NOT NULL, ""GuzergahId"" INTEGER NOT NULL,
                ""IsDeleted"" BOOLEAN DEFAULT false, ""CreatedAt"" TIMESTAMP DEFAULT NOW(), ""UpdatedAt"" TIMESTAMP,
                ""DeletedAt"" TIMESTAMP, ""FirmaId"" INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (""Id""))",
        };

        // LastikStoklar — EF entity (Lastik.cs: LastikStok) ile uyumlu eksik kolonlar
        var lastikFixes = new[]
        {
            @"ALTER TABLE ""LastikStoklar"" ADD COLUMN IF NOT EXISTS ""Aktif"" BOOLEAN DEFAULT true",
            @"ALTER TABLE ""LastikStoklar"" ADD COLUMN IF NOT EXISTS ""AracId"" INTEGER",
            @"ALTER TABLE ""LastikStoklar"" ADD COLUMN IF NOT EXISTS ""YedekMi"" BOOLEAN DEFAULT false",
            @"ALTER TABLE ""LastikStoklar"" ADD COLUMN IF NOT EXISTS ""KaynakAracId"" INTEGER",
            @"ALTER TABLE ""LastikStoklar"" ADD COLUMN IF NOT EXISTS ""FirmaId"" INTEGER DEFAULT 1",
        };

        // 1) Önce eksik tabloları oluştur (ALTER TABLE altta çalışsın diye)
        foreach (var sql in createTables)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* tablo zaten var — sessiz geç */ }
        }

        // 2) Eksik kolonları ekle (mevcut tablolara)
        foreach (var sql in fixes)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* kolon zaten var veya tablo yok — sessiz geç */ }
        }

        // 3) LastikStoklar özel kolonları
        foreach (var sql in lastikFixes)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); }
            catch { /* kolon zaten var veya tablo yok — sessiz geç */ }
        }
    }
}
