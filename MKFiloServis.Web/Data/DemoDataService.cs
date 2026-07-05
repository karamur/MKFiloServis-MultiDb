using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MKFiloServis.Web.Data;

/// <summary>
/// Demo veriler ve veritabanı sıfırlama servisi
/// Admin panelinden tetiklenen operasyonlar:
/// - Tüm tabloları truncate etme
/// - Demo veri ekleme
/// - Demo verileri kaldırma
/// </summary>
public class DemoDataService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<DemoDataService> _logger;
    private readonly TestDataSeeder _testDataSeeder;

    public DemoDataService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<DemoDataService> logger,
        TestDataSeeder testDataSeeder)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _testDataSeeder = testDataSeeder;
    }

    /// <summary>
    /// Tüm tabloları CASCADE ile temizler
    /// </summary>
    public async Task<DemoDataResult> TruncateAllAsync()
    {
        var result = new DemoDataResult();
        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            // Foreign key constraints geçici olarak devre dışı bırak (PostgreSQL için)
            await ExecuteNonQueryAsync(db, "SET session_replication_role = 'replica'");

            // Tüm DbSet'lerin adlarını DatabaseFacade üzerinden al
            var tableNames = GetAllTableNames();

            foreach (var tableName in tableNames)
            {
                try
                {
                    // TRUNCATE CASCADE kullan
                    await ExecuteNonQueryAsync(db, $"TRUNCATE TABLE \"{tableName}\" CASCADE");
                    result.TruncatedTables.Add(tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tablo truncate başarısız: {TableName}", tableName);
                    // Bir tablo hatalı olsa da devam et
                }
            }

            // Foreign key constraints geri aç
            await ExecuteNonQueryAsync(db, "SET session_replication_role = 'origin'");

            result.Basarili = true;
            result.Mesaj = $"{result.TruncatedTables.Count} tablo başarıyla temizlendi";
            _logger.LogInformation("Database truncate tamamlandı: {Count} tablo temizlendi", result.TruncatedTables.Count);
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesaj = $"Database truncate başarısız: {ex.Message}";
            _logger.LogError(ex, "Database truncate hatası");
        }

        return result;
    }

    /// <summary>
    /// Demo verilerini ekler
    /// </summary>
    public async Task<DemoDataResult> SeedDemoDataAsync()
    {
        var result = new DemoDataResult();

        try
        {
            // Mevcut TestDataSeeder'ı kullan
            // Çünkü zaten tüm demo veri üretim mantığını içeriyor
            var seedResult = await _testDataSeeder.SeedAllAsync(silinenleriTemizle: false);

            result.Basarili = seedResult.Basarili;
            result.Mesaj = string.Join("; ", seedResult.Mesajlar);
            result.SeedCount = seedResult.ToplamKayit;

            if (result.Basarili)
            {
                _logger.LogInformation("Demo veriler eklendi: {Count} kayıt", result.SeedCount);
            }
            else
            {
                _logger.LogError("Demo veri ekleme başarısız: {Message}", result.Mesaj);
            }
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesaj = $"Demo veri ekleme başarısız: {ex.Message}";
            _logger.LogError(ex, "Demo veri ekleme hatası");
        }

        return result;
    }

    /// <summary>
    /// Tüm demo verileri kaldırır ([TEST] marker'ı olanları)
    /// </summary>
    public async Task<DemoDataResult> RemoveDemoDataAsync()
    {
        var result = new DemoDataResult();
        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            // TestDataSeeder'ın TemizleAsync metodunu kullan
            await _testDataSeeder.TemizleAsync();

            result.Basarili = true;
            result.Mesaj = "Tüm demo veriler kaldırıldı";
            _logger.LogInformation("Demo veriler kaldırıldı");
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesaj = $"Demo veri kaldırma başarısız: {ex.Message}";
            _logger.LogError(ex, "Demo veri kaldırma hatası");
        }

        return result;
    }

    /// <summary>
    /// Belirli bir firmanın TÜM verilerini siler (Cariler, Araçlar, Soförler, Faturalar, vb)
    /// Firma tenantı kontrol ederek ilişkili tüm verileri siler
    /// </summary>
    public async Task<DemoDataResult> ClearFirmaDataAsync(int firmaId)
    {
        var result = new DemoDataResult();
        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            _logger.LogInformation("Firma verisi silme başlıyor. FirmaId={FirmaId}", firmaId);

            // Firma var mı kontrol et
            var firma = await db.Firmalar.FirstOrDefaultAsync(f => f.Id == firmaId && !f.IsDeleted);
            if (firma == null)
            {
                result.Basarili = false;
                result.Mesaj = "Firma bulunamadı";
                return result;
            }

            // Foreign key constraints geçici olarak devre dışı bırak
            await ExecuteNonQueryAsync(db, "SET session_replication_role = 'replica'");

            try
            {
                // Firma bazlı verileri sil - sıra önemli (child'tan parent'a)
                var deletedCount = 0;

                // 1. İşlem tablolarından başla
                deletedCount += await DeleteByFirmaAsync(db, "ServisCalismalari", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "Faturalar", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "MuhasebeFisleri", firmaId);

                // 2. Personel ve Araç verisi
                deletedCount += await DeleteByFirmaAsync(db, "PersonelMaaslari", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "PersonelPuantajlar", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "AracMasraflari", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "Araclar", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "Soforler", firmaId);

                // 3. Cari ve ilişkili verisi
                deletedCount += await DeleteByFirmaAsync(db, "Cariler", firmaId);

                // 4. Gözergah ve sefer verisi
                deletedCount += await DeleteByFirmaAsync(db, "Guzergahlar", firmaId);

                // 5. Muhasebe verisi
                deletedCount += await DeleteByFirmaAsync(db, "MuhasebeHesaplari", firmaId);
                deletedCount += await DeleteByFirmaAsync(db, "BudgetOdemeler", firmaId);

                // 6. Banka verisi
                deletedCount += await DeleteByFirmaAsync(db, "BankaHesaplari", firmaId);

                // Foreign key constraints geri aç
                await ExecuteNonQueryAsync(db, "SET session_replication_role = 'origin'");

                result.Basarili = true;
                result.Mesaj = $"Firma verisi başarılı silindi. {deletedCount} kayıt silindi.";
                result.TruncatedTables.Add($"Toplam {deletedCount} kayıt silindi");

                _logger.LogInformation("Firma verisi silme tamamlandı. FirmaId={FirmaId} SilinenkayitSayisi={Count}", 
                    firmaId, deletedCount);
            }
            catch
            {
                // Foreign key constraints geri aç
                try { await ExecuteNonQueryAsync(db, "SET session_replication_role = 'origin'"); }
                catch { }

                throw;
            }
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesaj = $"Firma verisi silme başarısız: {ex.Message}";
            _logger.LogError(ex, "Firma verisi silme hatası. FirmaId={FirmaId}", firmaId);
        }

        return result;
    }

    /// <summary>
    /// Belirtilen tablodan firmaId'ye ait kayıtları sil
    /// </summary>
    private async Task<int> DeleteByFirmaAsync(ApplicationDbContext db, string tableName, int firmaId)
    {
        try
        {
            var delimitedTable = $"\"{tableName}\"";
            var sql = $"DELETE FROM {delimitedTable} WHERE \"FirmaId\" = {firmaId}";

            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            if (db.Database.GetDbConnection().State != ConnectionState.Open)
            {
                await db.Database.GetDbConnection().OpenAsync();
            }

            var affectedRows = await command.ExecuteNonQueryAsync();
            if (affectedRows > 0)
            {
                _logger.LogDebug("Tablo silinmesi: {TableName} - {Count} satır silindi", tableName, affectedRows);
            }
            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tablo silme başarısız: {TableName} FirmaId={FirmaId}", tableName, firmaId);
            // Bir tablo hatalı olsa da devam et
            return 0;
        }
    }

    /// <summary>
    /// Veritabanı sıfırla ve demo veri ekle (tek operasyon)
    /// </summary>
    public async Task<DemoDataResult> ResetAndSeedAsync()
    {
        var result = new DemoDataResult();

        try
        {
            _logger.LogInformation("Database sıfırlama ve demo veri ekleme başlıyor");

            // Adım 1: Temizle
            var truncateResult = await TruncateAllAsync();
            result.Mesajlar.Add($"Truncate: {truncateResult.Mesaj}");

            if (!truncateResult.Basarili)
            {
                result.Basarili = false;
                result.Mesaj = "Truncate başarısız";
                return result;
            }

            // Adım 2: Demo veri ekle
            var seedResult = await SeedDemoDataAsync();
            result.Mesajlar.Add($"Seed: {seedResult.Mesaj}");
            result.SeedCount = seedResult.SeedCount;

            result.Basarili = seedResult.Basarili;
            result.Mesaj = result.Basarili
                ? $"Database sıfırlandı ve {result.SeedCount} demo kayıt eklendi"
                : "Demo veri ekleme başarısız";

            _logger.LogInformation("ResetAndSeed tamamlandı: {Message}", result.Mesaj);
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesaj = $"ResetAndSeed başarısız: {ex.Message}";
            _logger.LogError(ex, "ResetAndSeed hatası");
        }

        return result;
    }

    /// <summary>
    /// SQL komutu çalıştır
    /// </summary>
    private async Task ExecuteNonQueryAsync(ApplicationDbContext db, string sql)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        if (db.Database.GetDbConnection().State != ConnectionState.Open)
        {
            await db.Database.GetDbConnection().OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Tüm tablo adlarını ApplicationDbContext'ten al
    /// </summary>
    private static List<string> GetAllTableNames()
    {
        return new List<string>
        {
            // Organizasyon ve Firma
            "Roller",
            "RolYetkileri",
            "Kullanicilar",
            "Lisanslar",
            "Organizasyonlar",
            "Subeler",
            "Firmalar",

            // CRM (ön temate kaldırılıyor - harici referanslar olabilir)
            "KullaniciCariler",
            "CariHatirlatmalar",
            "CariIletisimNotlar",
            "DashboardWidgetlar",
            "Hatirlaticilar",
            "SmsLoglari",
            "SmsSablonlari",
            "SmsAyarlari",
            "WhatsAppAyarlari",
            "EmailAyarlari",
            "Mesajlar",
            "EpostaBildirimLoglari",
            "Bildirimler",

            // Webhook
            "WebhookLoglar",
            "WebhookEndpointler",

            // WhatsApp İletişim
            "WhatsAppMesajlar",
            "WhatsAppSablonlar",
            "WhatsAppGrupUyeler",
            "WhatsAppGruplar",
            "WhatsAppKisiler",

            // Stok/Envanter
            "ServisParcalar",
            "ServisKayitlari",
            "AracIslemler",
            "StokHareketler",
            "StokKategoriler",
            "StokKartlari",

            // Muhasebe (önce Fatura kalemleri)
            "MuhasebeFisKalemleri",
            "MuhasebeFisleri",
            "MuhasebeAyarlari",
            "MuhasebeDonemleri",
            "MuhasebeHesaplari",
            "KostMerkezleri",
            "KdvHesapEslestirmeleri",
            "MuhasebeProjeler",

            // Bütçe (Budget)
            "BudgetHedefler",
            "TekrarlayanOdemeler",
            "BudgetMasrafKalemleri",
            "BudgetOdemeler",

            // Personel
            "PersonelAracAtamalari",
            "MaasOdemeSnapshotlar",
            "PersonelIzinHaklari",
            "PersonelIzinleri",
            "PersonelMaaslari",

            // Checklist
            "ChecklistKalemleri",
            "AylikChecklistler",

            // Banka/Kasa
            "OdemeEslestirmeleri",
            "FirmalarArasiTransferler",
            "BankaKasaHareketleri",
            "BankaKolonMappingler",
            "FinansHareketler",
            "BankaHesaplari",

            // Fatura (önemli - çocuk tablolar önce)
            "FaturaKalemleri",
            "Faturalar",

            // Piyasa Araştırma
            "PiyasaKaynaklar",
            "AracMarkaModeller",
            "PiyasaArastirmaIlanlar",
            "PiyasaArastirmalar",

            // Filo Operasyon
            "AracOperasyonDurumlari",
            "PlakaDonusumler",
            "AracAlimSatimlar",

            // Puantaj
            "FiloGunlukPuantajlar",
            "FiloGuzergahEslestirmeleri",
            "FirmaGuzergahEslestirmeleri",
            "FirmaAracSoforEslestirmeleri",
            "GunlukPuantajlar",
            "PersonelPuantajlar",

            // Hakediş
            "AracMaliyetSnapshotlari",
            "HakedisDetaylari",
            "Hakedisler",

            // Aylık Ödeme
            "AylikOdemeGerceklesenler",
            "AylikOdemePlanlari",

            // Kurum
            "Kurumlar",

            // Satış
            "AracMarkalari",
            "AracModelleri",
            "AracSatislari",
            "PiyasaIlanlari",
            "AracIlanlari",
            "SatisPersonelleri",

            // Kira (Kiralama)
            "MusteriKiralamalar",
            "ServisCalismaKiralamalar",
            "KiralamaAraclar",
            "KiralikPlakaTakipFaturalar",
            "KiralikPlakaTakipler",

            // Filo Servis (çocuk tablolar önce)
            "AracEvrakDosyalari",
            "EvrakDosyalari",
            "AracEvraklari",
            "AracMasraflari",
            "MasrafKalemleri",
            "GuzergahSeferleri",
            "ServisCalismalari",
            "AracPlakalar",
            "Guzergahlar",
            "Araclar",
            "Soforler",

            // Cari (son - pek çok referans var)
            "CariSeferUcretleri",
            "Cariler",

            // Kapasite
            "Kapasiteler",

            // Sistem
            "AktiviteLoglar",
            "TestSessionLogs"
        };
    }
}

/// <summary>
/// Demo veri operasyonu sonucu
/// </summary>
public class DemoDataResult
{
    public bool Basarili { get; set; }
    public string Mesaj { get; set; } = "";
    public List<string> Mesajlar { get; set; } = new();
    public List<string> TruncatedTables { get; set; } = new();
    public int SeedCount { get; set; }
}
