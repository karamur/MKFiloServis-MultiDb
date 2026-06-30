using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Migration helper - SQL komutları güvenli, kullanıcı girdisi yok

namespace MKFiloServis.Web.Data.Migrations;

/// <summary>
/// SMS entity'leri için migration helper - runtime'da tablo ve kolonları oluşturur
/// </summary>
public static class SmsMigrationHelper
{
    public static async Task EnsureSmsTablesAsync(ApplicationDbContext context, ILogger logger)
    {
        var isSqlite = context.Database.ProviderName?.Contains("Sqlite") == true;

        // SmsAyar tablosu
        await EnsureSmsAyarTableAsync(context, logger, isSqlite);
        
        // SmsLog tablosu
        await EnsureSmsLogTableAsync(context, logger, isSqlite);
        
        // SmsSablon tablosu
        await EnsureSmsSablonTableAsync(context, logger, isSqlite);
    }

    private static async Task EnsureSmsAyarTableAsync(ApplicationDbContext context, ILogger logger, bool isSqlite)
    {
        const string tableName = "SmsAyarlari";
        
        try
        {
            var tableExists = await TableExistsAsync(context, tableName, isSqlite);
            
            if (!tableExists)
            {
                var sql = isSqlite
                    ? @"CREATE TABLE SmsAyarlari (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirmaId INTEGER NULL,
                        Provider INTEGER NOT NULL DEFAULT 0,
                        KullaniciAdi TEXT NULL,
                        ApiKey TEXT NULL,
                        GondericiNumara TEXT NULL,
                        ApiUrl TEXT NULL,
                        Aktif INTEGER NOT NULL DEFAULT 0,
                        Bakiye REAL NULL,
                        SonBakiyeSorguTarihi TEXT NULL,
                        ToplamGonderilenSms INTEGER NOT NULL DEFAULT 0,
                        ToplamBasarisizSms INTEGER NOT NULL DEFAULT 0,
                        SonGonderimTarihi TEXT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL,
                        IsDeleted INTEGER NOT NULL DEFAULT 0
                    )"
                    : @"CREATE TABLE IF NOT EXISTS ""SmsAyarlari"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""FirmaId"" INTEGER NULL,
                        ""Provider"" INTEGER NOT NULL DEFAULT 0,
                        ""KullaniciAdi"" VARCHAR(100) NULL,
                        ""ApiKey"" VARCHAR(200) NULL,
                        ""GondericiNumara"" VARCHAR(50) NULL,
                        ""ApiUrl"" VARCHAR(200) NULL,
                        ""Aktif"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""Bakiye"" DECIMAL(18,2) NULL,
                        ""SonBakiyeSorguTarihi"" TIMESTAMP NULL,
                        ""ToplamGonderilenSms"" INTEGER NOT NULL DEFAULT 0,
                        ""ToplamBasarisizSms"" INTEGER NOT NULL DEFAULT 0,
                        ""SonGonderimTarihi"" TIMESTAMP NULL,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP NULL,
                        ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
                    )";

                await context.Database.ExecuteSqlRawAsync(sql);
                logger.LogInformation("SMS: {Table} tablosu oluşturuldu", tableName);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMS: {Table} tablosu kontrolü/oluşturulması sırasında hata", tableName);
        }
    }

    private static async Task EnsureSmsLogTableAsync(ApplicationDbContext context, ILogger logger, bool isSqlite)
    {
        const string tableName = "SmsLoglari";
        
        try
        {
            var tableExists = await TableExistsAsync(context, tableName, isSqlite);
            
            if (!tableExists)
            {
                var sql = isSqlite
                    ? @"CREATE TABLE SmsLoglari (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        SmsAyarId INTEGER NULL,
                        Telefon TEXT NOT NULL,
                        Mesaj TEXT NOT NULL,
                        Durum INTEGER NOT NULL DEFAULT 0,
                        ProviderMesajId TEXT NULL,
                        HataMesaji TEXT NULL,
                        GonderimTarihi TEXT NULL,
                        IletimTarihi TEXT NULL,
                        IliskiliTablo TEXT NULL,
                        IliskiliKayitId INTEGER NULL,
                        Tip INTEGER NOT NULL DEFAULT 0,
                        GonderenKullaniciId INTEGER NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL,
                        IsDeleted INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY (SmsAyarId) REFERENCES SmsAyarlari(Id),
                        FOREIGN KEY (GonderenKullaniciId) REFERENCES Kullanicilar(Id)
                    )"
                    : @"CREATE TABLE IF NOT EXISTS ""SmsLoglari"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""SmsAyarId"" INTEGER NULL REFERENCES ""SmsAyarlari""(""Id""),
                        ""Telefon"" VARCHAR(20) NOT NULL,
                        ""Mesaj"" VARCHAR(500) NOT NULL,
                        ""Durum"" INTEGER NOT NULL DEFAULT 0,
                        ""ProviderMesajId"" VARCHAR(100) NULL,
                        ""HataMesaji"" VARCHAR(500) NULL,
                        ""GonderimTarihi"" TIMESTAMP NULL,
                        ""IletimTarihi"" TIMESTAMP NULL,
                        ""IliskiliTablo"" VARCHAR(50) NULL,
                        ""IliskiliKayitId"" INTEGER NULL,
                        ""Tip"" INTEGER NOT NULL DEFAULT 0,
                        ""GonderenKullaniciId"" INTEGER NULL REFERENCES ""Kullanicilar""(""Id""),
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP NULL,
                        ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
                    )";

                await context.Database.ExecuteSqlRawAsync(sql);
                logger.LogInformation("SMS: {Table} tablosu oluşturuldu", tableName);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMS: {Table} tablosu kontrolü/oluşturulması sırasında hata", tableName);
        }
    }

    private static async Task EnsureSmsSablonTableAsync(ApplicationDbContext context, ILogger logger, bool isSqlite)
    {
        const string tableName = "SmsSablonlari";
        
        try
        {
            var tableExists = await TableExistsAsync(context, tableName, isSqlite);
            
            if (!tableExists)
            {
                var sql = isSqlite
                    ? @"CREATE TABLE SmsSablonlari (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirmaId INTEGER NULL,
                        Adi TEXT NOT NULL,
                        Aciklama TEXT NULL,
                        Sablon TEXT NOT NULL,
                        Tip INTEGER NOT NULL DEFAULT 0,
                        Aktif INTEGER NOT NULL DEFAULT 1,
                        Varsayilan INTEGER NOT NULL DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NULL,
                        IsDeleted INTEGER NOT NULL DEFAULT 0
                    )"
                    : @"CREATE TABLE IF NOT EXISTS ""SmsSablonlari"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""FirmaId"" INTEGER NULL,
                        ""Adi"" VARCHAR(100) NOT NULL,
                        ""Aciklama"" VARCHAR(200) NULL,
                        ""Sablon"" VARCHAR(500) NOT NULL,
                        ""Tip"" INTEGER NOT NULL DEFAULT 0,
                        ""Aktif"" BOOLEAN NOT NULL DEFAULT TRUE,
                        ""Varsayilan"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP NULL,
                        ""IsDeleted"" BOOLEAN NOT NULL DEFAULT FALSE
                    )";

                await context.Database.ExecuteSqlRawAsync(sql);
                logger.LogInformation("SMS: {Table} tablosu oluşturuldu", tableName);

                // Varsayılan şablonları ekle
                await SeedDefaultSablonlarAsync(context, logger, isSqlite);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMS: {Table} tablosu kontrolü/oluşturulması sırasında hata", tableName);
        }
    }

    private static async Task SeedDefaultSablonlarAsync(ApplicationDbContext context, ILogger logger, bool isSqlite)
    {
        try
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            var sablonlar = new[]
            {
                ("Vade Hatırlatma", "Vadesi yaklaşan fatura bildirimi", "{MusteriAdi}, {FaturaNo} no'lu faturanızın vadesi {VadeTarihi} tarihinde dolacaktır. Tutar: {Tutar} TL", 1, 1), // VadeHatirlatma
                ("Ödeme Bildirimi", "Ödeme alındı bildirimi", "Sayin {MusteriAdi}, {Tutar} TL tutarindaki odemeniz alinmistir. Tesekkur ederiz.", 2, 1), // OdemeBildirimi
                ("Fatura Bildirimi", "Yeni fatura bildirimi", "{MusteriAdi}, {FaturaNo} no'lu faturaniz olusturulmustur. Tutar: {Tutar} TL, Vade: {VadeTarihi}", 3, 1), // FaturaBildirimi
                ("Genel Bildirim", "Genel amaçlı bildirim şablonu", "{MusteriAdi}, {Mesaj}", 0, 1) // Bildirim
            };

            foreach (var (adi, aciklama, sablon, tip, varsayilan) in sablonlar)
            {
                var sql = isSqlite
                    ? $"INSERT INTO SmsSablonlari (Adi, Aciklama, Sablon, Tip, Aktif, Varsayilan, CreatedAt, IsDeleted) VALUES ('{adi}', '{aciklama}', '{sablon}', {tip}, 1, {varsayilan}, '{now}', 0)"
                    : $"INSERT INTO \"SmsSablonlari\" (\"Adi\", \"Aciklama\", \"Sablon\", \"Tip\", \"Aktif\", \"Varsayilan\", \"CreatedAt\", \"IsDeleted\") VALUES ('{adi}', '{aciklama}', '{sablon}', {tip}, TRUE, {(varsayilan == 1 ? "TRUE" : "FALSE")}, '{now}', FALSE)";

                await context.Database.ExecuteSqlRawAsync(sql);
            }

            logger.LogInformation("SMS: Varsayılan şablonlar oluşturuldu");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMS: Varsayılan şablonlar oluşturulurken hata");
        }
    }

    private static async Task<bool> TableExistsAsync(ApplicationDbContext context, string tableName, bool isSqlite)
    {
        try
        {
            var sql = isSqlite
                ? $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'"
                : $"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{tableName}'";

            var result = await context.Database.ExecuteSqlRawAsync(sql);
            
            // Alternatif kontrol - tablo varsa sorgu çalışır
            try
            {
                await context.Database.ExecuteSqlRawAsync($"SELECT 1 FROM {(isSqlite ? tableName : $"\"{tableName}\"")} LIMIT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
}



