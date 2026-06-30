using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// EbysEvrakKategoriler_Fix mega-migration'dan kalan eksik SirketId kolonlarını
/// ve eksik tabloları PostgreSQL'e ekler. Bu helper, bozuk migration zinciri
/// yüzünden hiç uygulanmamış schema değişikliklerini düzeltir.
/// </summary>
public static class SirketSchemaFixMigrationHelper
{
    public static async Task ApplySirketSchemaFixAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                await ApplyPostgreSqlFixAsync(context);
            }
            else if (context.Database.IsSqlite())
            {
                await ApplySqliteFixAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SirketSchemaFix migration hatası: {ex.Message}");
        }
    }

    private static async Task ApplyPostgreSqlFixAsync(ApplicationDbContext context)
    {
        // ADIM 1: Sirketler tablosunu oluştur (SirketId FK'ların hedefi)
        await CreateSirketlerTableAsync(context);

        // ADIM 2: SirketId kolonlarını 8 tabloya ekle
        await AddSirketIdColumnsAsync(context);

        // ADIM 3: Diğer eksik tabloları oluştur
        await CreateMissingTablesAsync(context);

        // ADIM 4: Diğer eksik kolonları ekle
        await AddMissingColumnsAsync(context);

        Console.WriteLine("SirketSchemaFix: Tüm eksik şema değişiklikleri tamamlandı.");
    }

    private static async Task CreateSirketlerTableAsync(ApplicationDbContext context)
    {
        try
        {
            var sql = @"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Sirketler') THEN
        CREATE TABLE ""Sirketler"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""SirketKodu"" VARCHAR(20) NOT NULL,
            ""Unvan"" VARCHAR(250) NOT NULL,
            ""KisaAd"" VARCHAR(100),
            ""VergiDairesi"" VARCHAR(100),
            ""VergiNo"" VARCHAR(11),
            ""Adres"" VARCHAR(500),
            ""Il"" VARCHAR(50),
            ""Ilce"" VARCHAR(50),
            ""PostaKodu"" VARCHAR(10),
            ""Telefon"" VARCHAR(20),
            ""Email"" VARCHAR(100),
            ""WebSitesi"" VARCHAR(200),
            ""LogoUrl"" VARCHAR(500),
            ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
            ""ParaBirimi"" VARCHAR(5) NOT NULL DEFAULT 'TRY',
            ""AyarlarJson"" TEXT,
            ""LisansBitisTarihi"" TIMESTAMP WITHOUT TIME ZONE,
            ""MaxKullaniciSayisi"" INTEGER NOT NULL DEFAULT 10,
            ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
            ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
        );
        CREATE UNIQUE INDEX ""IX_Sirketler_SirketKodu"" ON ""Sirketler"" (""SirketKodu"");
        RAISE NOTICE 'Sirketler tablosu oluşturuldu.';
    END IF;
END $$;";
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine("SirketSchemaFix: Sirketler tablosu kontrol edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SirketSchemaFix: Sirketler tablosu hatası: {ex.Message}");
        }
    }

    private static async Task AddSirketIdColumnsAsync(ApplicationDbContext context)
    {
        var sql = @"
DO $$
BEGIN
    -- Faturalar.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Faturalar' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Faturalar"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Faturalar.SirketId eklendi.';
    END IF;

    -- Cariler.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Cariler' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Cariler"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Cariler.SirketId eklendi.';
    END IF;

    -- Araclar.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Araclar' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Araclar"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Araclar.SirketId eklendi.';
    END IF;

    -- Guzergahlar.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Guzergahlar' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Guzergahlar"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Guzergahlar.SirketId eklendi.';
    END IF;

    -- BankaHesaplari.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'BankaHesaplari' AND column_name = 'SirketId') THEN
        ALTER TABLE ""BankaHesaplari"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'BankaHesaplari.SirketId eklendi.';
    END IF;

    -- BankaKasaHareketleri.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'BankaKasaHareketleri' AND column_name = 'SirketId') THEN
        ALTER TABLE ""BankaKasaHareketleri"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'BankaKasaHareketleri.SirketId eklendi.';
    END IF;

    -- Personeller.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Personeller.SirketId eklendi.';
    END IF;

    -- Kullanicilar.SirketId
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Kullanicilar' AND column_name = 'SirketId') THEN
        ALTER TABLE ""Kullanicilar"" ADD COLUMN ""SirketId"" INTEGER NULL;
        RAISE NOTICE 'Kullanicilar.SirketId eklendi.';
    END IF;

    -- Personeller.MuhasebeHesapId (mega-migration'dan)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'MuhasebeHesapId') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""MuhasebeHesapId"" INTEGER NULL;
        RAISE NOTICE 'Personeller.MuhasebeHesapId eklendi.';
    END IF;

    -- Personeller.SgkCalismaTuru (SGK calisma turu - default 1=TamZamanli)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'SgkCalismaTuru') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""SgkCalismaTuru"" INTEGER NOT NULL DEFAULT 1;
        RAISE NOTICE 'Personeller.SgkCalismaTuru eklendi.';
    END IF;

    -- Personeller.FirmaId (personelin calistigi firma)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Personeller' AND column_name = 'FirmaId') THEN
        ALTER TABLE ""Personeller"" ADD COLUMN ""FirmaId"" INTEGER NULL;
        RAISE NOTICE 'Personeller.FirmaId eklendi.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE tablename = 'Personeller' AND indexname = 'IX_Personeller_FirmaId') THEN
        CREATE INDEX ""IX_Personeller_FirmaId"" ON ""Personeller"" (""FirmaId"");
        RAISE NOTICE 'IX_Personeller_FirmaId indexi eklendi.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Personeller_Firmalar_FirmaId') THEN
        ALTER TABLE ""Personeller"" ADD CONSTRAINT ""FK_Personeller_Firmalar_FirmaId"" FOREIGN KEY (""FirmaId"") REFERENCES ""Firmalar""(""Id"") ON DELETE RESTRICT;
        RAISE NOTICE 'FK_Personeller_Firmalar_FirmaId eklendi.';
    END IF;
END $$;";

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine("SirketSchemaFix: SirketId kolonları kontrol edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SirketSchemaFix: SirketId kolonları hatası: {ex.Message}");
        }
    }

    private static async Task CreateMissingTablesAsync(ApplicationDbContext context)
    {
        // BildirimAyarlari
        await CreateTableIfNotExistsAsync(context, "BildirimAyarlari", @"
            CREATE TABLE ""BildirimAyarlari"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""KullaniciId"" INTEGER NOT NULL,
                ""FaturaVadeUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""EhliyetBitisUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SrcBelgesiUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""PsikoteknikUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SaglikRaporuUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""TrafikSigortaUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""KaskoUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""MuayeneUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""DestekTalebiUyarisi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SistemBildirimleri"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""EpostaAlsin"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""EpostaAdresi"" TEXT,
                ""SmsAlsin"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SmsTelefon"" VARCHAR(20),
                ""SmsVadeHatirlatma"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SmsBelgeHatirlatma"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""VadeUyariGunSayisi"" INTEGER NOT NULL DEFAULT 7,
                ""BelgeUyariGunSayisi"" INTEGER NOT NULL DEFAULT 30,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // EpostaBildirimLoglari
        await CreateTableIfNotExistsAsync(context, "EpostaBildirimLoglari", @"
            CREATE TABLE ""EpostaBildirimLoglari"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""KullaniciId"" INTEGER NOT NULL,
                ""EpostaAdresi"" VARCHAR(200) NOT NULL,
                ""UyariSayisi"" INTEGER NOT NULL DEFAULT 0,
                ""GonderimTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""Basarili"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""HataMesaji"" VARCHAR(500),
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // WebhookEndpointler
        await CreateTableIfNotExistsAsync(context, "WebhookEndpointler", @"
            CREATE TABLE ""WebhookEndpointler"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Ad"" VARCHAR(100) NOT NULL,
                ""Aciklama"" VARCHAR(500),
                ""Url"" VARCHAR(500) NOT NULL,
                ""Secret"" VARCHAR(100),
                ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""MaxRetry"" INTEGER NOT NULL DEFAULT 3,
                ""RetryDelaySaniye"" INTEGER NOT NULL DEFAULT 30,
                ""OlayFiltresi"" TEXT,
                ""HttpMethod"" TEXT NOT NULL DEFAULT 'POST',
                ""Headers"" TEXT,
                ""ToplamGonderim"" INTEGER NOT NULL DEFAULT 0,
                ""BasariliGonderim"" INTEGER NOT NULL DEFAULT 0,
                ""BasarisizGonderim"" INTEGER NOT NULL DEFAULT 0,
                ""SonGonderimTarihi"" TIMESTAMP WITHOUT TIME ZONE,
                ""SonBasariliTarih"" TIMESTAMP WITHOUT TIME ZONE,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // WebhookLoglar
        await CreateTableIfNotExistsAsync(context, "WebhookLoglar", @"
            CREATE TABLE ""WebhookLoglar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""WebhookEndpointId"" INTEGER NOT NULL,
                ""OlayTipi"" VARCHAR(100) NOT NULL,
                ""Payload"" TEXT,
                ""Durum"" INTEGER NOT NULL DEFAULT 0,
                ""HttpStatusCode"" INTEGER NOT NULL DEFAULT 0,
                ""ResponseBody"" TEXT,
                ""GonderimTarihi"" TIMESTAMP WITHOUT TIME ZONE,
                ""YanitTarihi"" TIMESTAMP WITHOUT TIME ZONE,
                ""SureMilisaniye"" INTEGER NOT NULL DEFAULT 0,
                ""RetryCount"" INTEGER NOT NULL DEFAULT 0,
                ""HataMesaji"" TEXT,
                ""IliskiliTablo"" TEXT,
                ""IliskiliKayitId"" INTEGER,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracBolgeler
        await CreateTableIfNotExistsAsync(context, "AracBolgeler", @"
            CREATE TABLE ""AracBolgeler"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""BolgeAdi"" TEXT NOT NULL,
                ""Tip"" INTEGER NOT NULL DEFAULT 0,
                ""MerkezLatitude"" DOUBLE PRECISION,
                ""MerkezLongitude"" DOUBLE PRECISION,
                ""YaricapMetre"" DOUBLE PRECISION,
                ""PoligonKoordinatlari"" TEXT,
                ""Renk"" TEXT,
                ""GirisBildirimi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""CikisBildirimi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""Notlar"" TEXT,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracBolgeAtamalar
        await CreateTableIfNotExistsAsync(context, "AracBolgeAtamalar", @"
            CREATE TABLE ""AracBolgeAtamalar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""AracBolgeId"" INTEGER NOT NULL,
                ""AracId"" INTEGER NOT NULL,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracTakipCihazlar
        await CreateTableIfNotExistsAsync(context, "AracTakipCihazlar", @"
            CREATE TABLE ""AracTakipCihazlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""AracId"" INTEGER NOT NULL,
                ""CihazId"" TEXT NOT NULL,
                ""CihazMarka"" TEXT,
                ""CihazModel"" TEXT,
                ""SimKartNo"" TEXT,
                ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
                ""KurulumTarihi"" TIMESTAMP WITHOUT TIME ZONE,
                ""SonIletisimZamani"" TIMESTAMP WITHOUT TIME ZONE,
                ""BataryaSeviyesi"" INTEGER,
                ""SinyalGucu"" INTEGER,
                ""Notlar"" TEXT,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracKonumlar
        await CreateTableIfNotExistsAsync(context, "AracKonumlar", @"
            CREATE TABLE ""AracKonumlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""AracTakipCihazId"" INTEGER NOT NULL,
                ""Latitude"" DOUBLE PRECISION NOT NULL,
                ""Longitude"" DOUBLE PRECISION NOT NULL,
                ""Hiz"" DOUBLE PRECISION,
                ""Yon"" DOUBLE PRECISION,
                ""Rakım"" DOUBLE PRECISION,
                ""Hassasiyet"" DOUBLE PRECISION,
                ""KayitZamani"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""KontakDurumu"" BOOLEAN,
                ""MotorDurumu"" BOOLEAN,
                ""YakitSeviyesi"" INTEGER,
                ""Kilometre"" INTEGER,
                ""Sicaklik"" DOUBLE PRECISION,
                ""OlayTipi"" INTEGER NOT NULL DEFAULT 0,
                ""Adres"" TEXT,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracTakipAlarmlar
        await CreateTableIfNotExistsAsync(context, "AracTakipAlarmlar", @"
            CREATE TABLE ""AracTakipAlarmlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""AracTakipCihazId"" INTEGER NOT NULL,
                ""AlarmTipi"" INTEGER NOT NULL DEFAULT 0,
                ""AlarmZamani"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""Latitude"" DOUBLE PRECISION,
                ""Longitude"" DOUBLE PRECISION,
                ""Mesaj"" TEXT,
                ""Deger"" DOUBLE PRECISION,
                ""Okundu"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Islendi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""Notlar"" TEXT,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // EbysAramaGecmisleri
        await CreateTableIfNotExistsAsync(context, "EbysAramaGecmisleri", @"
            CREATE TABLE ""EbysAramaGecmisleri"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""KullaniciId"" INTEGER NOT NULL,
                ""AramaMetni"" VARCHAR(500) NOT NULL,
                ""FiltreJson"" VARCHAR(2000),
                ""SonucSayisi"" INTEGER NOT NULL DEFAULT 0,
                ""AramaTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // EbysBelgeEmbeddingler
        await CreateTableIfNotExistsAsync(context, "EbysBelgeEmbeddingler", @"
            CREATE TABLE ""EbysBelgeEmbeddingler"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Kaynak"" INTEGER NOT NULL DEFAULT 0,
                ""KaynakId"" INTEGER NOT NULL,
                ""DosyaId"" INTEGER,
                ""Metin"" VARCHAR(8000) NOT NULL,
                ""MetinOzet"" VARCHAR(500),
                ""EmbeddingJson"" TEXT NOT NULL,
                ""EmbeddingBoyutu"" INTEGER NOT NULL DEFAULT 0,
                ""ModelAdi"" VARCHAR(100),
                ""OlusturmaTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""GuncellemeTarihi"" TIMESTAMP WITHOUT TIME ZONE,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // EbysEvrakDosyaVersiyonlar
        await CreateTableIfNotExistsAsync(context, "EbysEvrakDosyaVersiyonlar", @"
            CREATE TABLE ""EbysEvrakDosyaVersiyonlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""EvrakDosyaId"" INTEGER NOT NULL,
                ""VersiyonNo"" INTEGER NOT NULL DEFAULT 1,
                ""DosyaAdi"" TEXT NOT NULL,
                ""DosyaYolu"" TEXT NOT NULL,
                ""DosyaTipi"" TEXT,
                ""DosyaBoyutu"" BIGINT NOT NULL DEFAULT 0,
                ""Aciklama"" TEXT,
                ""DegisiklikNotu"" TEXT,
                ""OlusturanKullaniciId"" INTEGER,
                ""OlusturmaTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // EbysKayitliAramalar
        await CreateTableIfNotExistsAsync(context, "EbysKayitliAramalar", @"
            CREATE TABLE ""EbysKayitliAramalar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""KullaniciId"" INTEGER NOT NULL,
                ""AramaAdi"" VARCHAR(100) NOT NULL,
                ""Aciklama"" VARCHAR(250),
                ""FiltreJson"" VARCHAR(2000) NOT NULL,
                ""BildirimAktif"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""SiraNo"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // PersonelOzlukEvrakVersiyonlar
        await CreateTableIfNotExistsAsync(context, "PersonelOzlukEvrakVersiyonlar", @"
            CREATE TABLE ""PersonelOzlukEvrakVersiyonlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""PersonelOzlukEvrakId"" INTEGER NOT NULL,
                ""VersiyonNo"" INTEGER NOT NULL DEFAULT 1,
                ""DosyaYolu"" TEXT,
                ""DosyaAdi"" TEXT,
                ""DosyaTipi"" TEXT,
                ""DosyaBoyutu"" BIGINT,
                ""Aciklama"" TEXT,
                ""DegisiklikNotu"" TEXT,
                ""OlusturanKullaniciId"" INTEGER,
                ""OlusturmaTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // AracEvrakDosyaVersiyonlar
        await CreateTableIfNotExistsAsync(context, "AracEvrakDosyaVersiyonlar", @"
            CREATE TABLE ""AracEvrakDosyaVersiyonlar"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""AracEvrakDosyaId"" INTEGER NOT NULL,
                ""VersiyonNo"" INTEGER NOT NULL DEFAULT 1,
                ""DosyaAdi"" TEXT NOT NULL,
                ""DosyaYolu"" TEXT NOT NULL,
                ""DosyaTipi"" TEXT,
                ""DosyaBoyutu"" BIGINT NOT NULL DEFAULT 0,
                ""Aciklama"" TEXT,
                ""DegisiklikNotu"" TEXT,
                ""OlusturanKullaniciId"" INTEGER,
                ""OlusturmaTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");

        // SirketTransferLoglari
        await CreateTableIfNotExistsAsync(context, "SirketTransferLoglari", @"
            CREATE TABLE ""SirketTransferLoglari"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""EntityTuru"" VARCHAR(50) NOT NULL,
                ""EntityId"" INTEGER NOT NULL,
                ""EntityAciklama"" VARCHAR(500),
                ""KaynakSirketId"" INTEGER NOT NULL,
                ""HedefSirketId"" INTEGER NOT NULL,
                ""KullaniciId"" INTEGER NOT NULL,
                ""TransferTarihi"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""Durum"" INTEGER NOT NULL DEFAULT 0,
                ""HataMesaji"" VARCHAR(2000),
                ""IliskiliVerilerTransferEdildi"" BOOLEAN NOT NULL DEFAULT FALSE,
                ""IliskiliEntitySayisi"" INTEGER NOT NULL DEFAULT 0,
                ""Notlar"" VARCHAR(1000),
                ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
                ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE,
                ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
            )");
    }

    private static async Task AddMissingColumnsAsync(ApplicationDbContext context)
    {
        var sql = @"
DO $$
BEGIN
    -- PersonelOzlukEvraklar eksik kolonlar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PersonelOzlukEvraklar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelOzlukEvraklar' AND column_name = 'DosyaAdi') THEN
            ALTER TABLE ""PersonelOzlukEvraklar"" ADD COLUMN ""DosyaAdi"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelOzlukEvraklar' AND column_name = 'DosyaBoyutu') THEN
            ALTER TABLE ""PersonelOzlukEvraklar"" ADD COLUMN ""DosyaBoyutu"" BIGINT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelOzlukEvraklar' AND column_name = 'DosyaTipi') THEN
            ALTER TABLE ""PersonelOzlukEvraklar"" ADD COLUMN ""DosyaTipi"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelOzlukEvraklar' AND column_name = 'SonDegisiklikNotu') THEN
            ALTER TABLE ""PersonelOzlukEvraklar"" ADD COLUMN ""SonDegisiklikNotu"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelOzlukEvraklar' AND column_name = 'VersiyonNo') THEN
            ALTER TABLE ""PersonelOzlukEvraklar"" ADD COLUMN ""VersiyonNo"" INTEGER NOT NULL DEFAULT 0;
        END IF;
    END IF;

    -- EbysEvrakDosyalar eksik kolonlar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'EbysEvrakDosyalar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EbysEvrakDosyalar' AND column_name = 'SonDegisiklikNotu') THEN
            ALTER TABLE ""EbysEvrakDosyalar"" ADD COLUMN ""SonDegisiklikNotu"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EbysEvrakDosyalar' AND column_name = 'VersiyonNo') THEN
            ALTER TABLE ""EbysEvrakDosyalar"" ADD COLUMN ""VersiyonNo"" INTEGER NOT NULL DEFAULT 0;
        END IF;
    END IF;

    -- AracEvrakDosyalari eksik kolonlar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AracEvrakDosyalari') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AracEvrakDosyalari' AND column_name = 'SonDegisiklikNotu') THEN
            ALTER TABLE ""AracEvrakDosyalari"" ADD COLUMN ""SonDegisiklikNotu"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'AracEvrakDosyalari' AND column_name = 'VersiyonNo') THEN
            ALTER TABLE ""AracEvrakDosyalari"" ADD COLUMN ""VersiyonNo"" INTEGER NOT NULL DEFAULT 0;
        END IF;
    END IF;

    -- PersonelPuantajlar eksik kolonlar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'PersonelPuantajlar') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayDurumu') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayDurumu"" INTEGER NOT NULL DEFAULT 0;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayNotu') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayNotu"" TEXT NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnayTarihi') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnayTarihi"" TIMESTAMP WITHOUT TIME ZONE NULL;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'PersonelPuantajlar' AND column_name = 'OnaylayanKullanici') THEN
            ALTER TABLE ""PersonelPuantajlar"" ADD COLUMN ""OnaylayanKullanici"" TEXT NULL;
        END IF;
    END IF;
END $$;";

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql);
            Console.WriteLine("SirketSchemaFix: Eksik kolonlar kontrol edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SirketSchemaFix: Eksik kolonlar hatası: {ex.Message}");
        }
    }

    private static async Task CreateTableIfNotExistsAsync(ApplicationDbContext context, string tableName, string createSql)
    {
        try
        {
            var checkSql = $@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}') THEN
        {createSql};
        RAISE NOTICE '{tableName} tablosu oluşturuldu.';
    END IF;
END $$;";
            await context.Database.ExecuteSqlRawAsync(checkSql);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SirketSchemaFix: {tableName} tablosu hatası: {ex.Message}");
        }
    }

    private static async Task ApplySqliteFixAsync(ApplicationDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Sirketler"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                    ""SirketKodu"" TEXT NOT NULL,
                    ""Unvan"" TEXT NOT NULL,
                    ""KisaAd"" TEXT NULL,
                    ""VergiDairesi"" TEXT NULL,
                    ""VergiNo"" TEXT NULL,
                    ""Adres"" TEXT NULL,
                    ""Il"" TEXT NULL,
                    ""Ilce"" TEXT NULL,
                    ""PostaKodu"" TEXT NULL,
                    ""Telefon"" TEXT NULL,
                    ""Email"" TEXT NULL,
                    ""WebSitesi"" TEXT NULL,
                    ""LogoUrl"" TEXT NULL,
                    ""Aktif"" INTEGER NOT NULL DEFAULT 1,
                    ""ParaBirimi"" TEXT NOT NULL DEFAULT 'TRY',
                    ""AyarlarJson"" TEXT NULL,
                    ""LisansBitisTarihi"" TEXT NULL,
                    ""MaxKullaniciSayisi"" INTEGER NOT NULL DEFAULT 10,
                    ""CreatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedAt"" TEXT NULL,
                    ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Sirketler_SirketKodu"" ON ""Sirketler"" (""SirketKodu"");
            ");
            Console.WriteLine("SQLite: Sirketler tablosu kontrol edildi.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite: Sirketler tablosu hatası: {ex.Message}");
        }

        // SQLite için SirketId kolonlarını ekle
        var tables = new[] { "Faturalar", "Cariler", "Araclar", "Guzergahlar", "BankaHesaplari", "BankaKasaHareketleri", "Personeller", "Kullanicilar" };

        foreach (var table in tables)
        {
            try
            {
                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT 1 FROM pragma_table_info('{table}') WHERE name = 'SirketId' LIMIT 1";
                var exists = await checkCmd.ExecuteScalarAsync() is not null;

                if (!exists)
                {
                    using var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = $@"ALTER TABLE ""{table}"" ADD COLUMN ""SirketId"" INTEGER NULL";
                    await alterCmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"SQLite: {table}.SirketId eklendi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQLite: {table}.SirketId hatası: {ex.Message}");
            }
        }
        // SQLite için Personeller.SgkCalismaTuru kolonu kontrolü
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM pragma_table_info('Personeller') WHERE name = 'SgkCalismaTuru' LIMIT 1";
            var exists = await checkCmd.ExecuteScalarAsync() is not null;

            if (!exists)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = @"ALTER TABLE ""Personeller"" ADD COLUMN ""SgkCalismaTuru"" INTEGER NOT NULL DEFAULT 1";
                await alterCmd.ExecuteNonQueryAsync();
                Console.WriteLine("SQLite: Personeller.SgkCalismaTuru eklendi.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite: Personeller.SgkCalismaTuru hatası: {ex.Message}");
        }

        // SQLite için Personeller.FirmaId kolonu kontrolü
        try
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM pragma_table_info('Personeller') WHERE name = 'FirmaId' LIMIT 1";
            var exists = await checkCmd.ExecuteScalarAsync() is not null;

            if (!exists)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = @"ALTER TABLE ""Personeller"" ADD COLUMN ""FirmaId"" INTEGER NULL";
                await alterCmd.ExecuteNonQueryAsync();
                Console.WriteLine("SQLite: Personeller.FirmaId eklendi.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SQLite: Personeller.FirmaId hatası: {ex.Message}");
        }

    }
}



