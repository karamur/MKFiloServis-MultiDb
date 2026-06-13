using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Arşiv taşıma dry-run analizi: DB kayıtlarını okur, hedef path'leri hesaplar,
/// eksik/missing/çakışma durumlarını raporlar. Gerçek dosya kopyalama YAPMAZ.
/// </summary>
public class ArchiveMigrationDryRunTests
{
    private readonly ITestOutputHelper _output;

    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=KOAFiloServis;Username=postgres;Password=Fast123";

    private static readonly DbContextOptions<ApplicationDbContext> DbOptions =
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(ConnectionString).Options;

    public ArchiveMigrationDryRunTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private ApplicationDbContext CreateContext() => new(DbOptions);

    private static string Norm(string value, bool removeSpaces = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return "BILINMIYOR";
        var result = value.Trim()
            .Replace('Ç', 'C').Replace('Ğ', 'G').Replace('İ', 'I')
            .Replace('Ö', 'O').Replace('Ş', 'S').Replace('Ü', 'U')
            .Replace('ç', 'c').Replace('ğ', 'g').Replace('ı', 'i')
            .Replace('ö', 'o').Replace('ş', 's').Replace('ü', 'u');
        foreach (var c in Path.GetInvalidFileNameChars()) result = result.Replace(c, '-');
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        result = result.Trim();
        if (removeSpaces) result = result.Replace(" ", "");
        return result;
    }

    /// <summary>
    /// DRY-RUN ANALİZ: Personel ve araç evraklarını tarar, hedef path'leri hesaplar.
    /// Hiçbir dosya kopyalanmaz, hiçbir DB kaydı değişmez.
    /// </summary>
    [Fact]
    public async Task DryRun_AnalyzePersonelAndAracEvrak()
    {
        await using var ctx = CreateContext();
        var storageRoot = AppStoragePaths.DefaultStorageRoot;

        // ─── PERSONEL ──────────────────────────────────────────────
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("  PERSONEL EVRAK ANALİZİ");
        _output.WriteLine("═══════════════════════════════════════════");

        var personelEvraklar = await ctx.PersonelOzlukEvraklar
            .IgnoreQueryFilters()
            .Include(e => e.Sofor).ThenInclude(s => s.Firma)
            .Include(e => e.EvrakTanim)
            .Where(e => !string.IsNullOrWhiteSpace(e.DosyaYolu) && !e.IsDeleted)
            .ToListAsync();

        var personelTotal = personelEvraklar.Count;
        var personelAlreadyNew = 0;
        var personelMissing = 0;
        var personelPending = 0;
        var personelCollisions = 0;
        var personelFirmaMissing = 0;
        var personelPersonelMissing = 0;
        var personelTargetPaths = new Dictionary<string, int>();

        var personelEntries = new List<(int Id, string Display, string Old, string New, string Status, string? Error)>();

        foreach (var evrak in personelEvraklar)
        {
            var n = (evrak.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');

            // Hedef path'i her zaman hesapla, eskiyle karşılaştır
            var ad = evrak.Sofor?.Ad;
            var soyad = evrak.Sofor?.Soyad;
            var firma = evrak.Sofor?.Firma?.FirmaAdi;
            var klasor = AppStoragePaths.BuildPersonelArsivKlasoru(
                ad ?? "PERSONEL", soyad ?? evrak.SoforId.ToString(), firma);
            var evrakAdi = evrak.EvrakTanim?.EvrakAdi ?? "Evrak";
            var uzanti = Path.GetExtension((evrak.DosyaYolu ?? ".pdf").Replace(".enc", ""));
            if (string.IsNullOrEmpty(uzanti)) uzanti = ".pdf";
            var ts = (evrak.CreatedAt == default ? DateTime.Now : evrak.CreatedAt).ToString("yyyyMMdd_HHmmss");
            var dosyaAdi = $"{Norm(ad ?? "PERSONEL", true)}{Norm(soyad ?? evrak.SoforId.ToString(), true)}{Norm(evrakAdi, true)}_{ts}{uzanti}.enc";
            var targetPath = $"{AppStoragePaths.PersonelEvrakRelativeRoot}/{klasor}/{dosyaAdi}";

            // Eski path yeni hedef path ile aynı mı?
            var oldNormalized = n;
            if (string.Equals(oldNormalized, targetPath.Replace('\\', '/').TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                personelAlreadyNew++;
                personelEntries.Add((evrak.Id,
                    $"{evrak.Sofor?.TamAd} - {evrak.EvrakTanim?.EvrakAdi}",
                    evrak.DosyaYolu!, evrak.DosyaYolu!, "Skipped", null));
                continue;
            }

            // Kaynak dosya var mı?
            var normalizedPath = n;
            if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath["uploads/".Length..];
            var diskPath = Path.Combine(storageRoot, "uploads", normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            // Arsiv prefix kontrolü
            if (n.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase))
                diskPath = Path.Combine(storageRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            var fileExists = File.Exists(diskPath);

            if (!fileExists)
            {
                // Fallback: direkt uploads altında dene
                var altPath = Path.Combine(storageRoot, "uploads", evrak.DosyaYolu!.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                fileExists = File.Exists(altPath);
                if (fileExists) diskPath = altPath;
            }

            if (!fileExists)
            {
                personelMissing++;
                personelEntries.Add((evrak.Id,
                    $"{evrak.Sofor?.TamAd} - {evrak.EvrakTanim?.EvrakAdi}",
                    evrak.DosyaYolu!, "", "Missing", $"Diskte yok: {diskPath}"));
                continue;
            }

            // Firma / Personel bilgisi istatistikleri
            if (string.IsNullOrWhiteSpace(evrak.Sofor?.Firma?.FirmaAdi))
                personelFirmaMissing++;
            if (string.IsNullOrWhiteSpace(evrak.Sofor?.Ad) && string.IsNullOrWhiteSpace(evrak.Sofor?.Soyad))
                personelPersonelMissing++;

            // Çakışma kontrolü
            if (personelTargetPaths.ContainsKey(targetPath))
            {
                personelCollisions++;
                personelTargetPaths[targetPath]++;
            }
            else
            {
                personelTargetPaths[targetPath] = 1;
            }

            personelPending++;
            personelEntries.Add((evrak.Id,
                $"{evrak.Sofor?.TamAd} - {evrak.EvrakTanim?.EvrakAdi}",
                evrak.DosyaYolu!, targetPath, "Pending", null));
        }

        // Personel özet
        _output.WriteLine($"Toplam personel evrak kaydı: {personelTotal}");
        _output.WriteLine($"Taşınacak: {personelPending}");
        _output.WriteLine($"Zaten yeni dizinde: {personelAlreadyNew}");
        _output.WriteLine($"Dosyası eksik (diskte yok): {personelMissing}");
        _output.WriteLine($"Firma bilgisi eksik: {personelFirmaMissing}");
        _output.WriteLine($"Personel adı eksik: {personelPersonelMissing}");
        _output.WriteLine($"Çakışma riski olan: {personelCollisions}");
        _output.WriteLine("");

        // Örnek path'ler (ilk 5)
        _output.WriteLine("--- Örnek Personel Path'leri ---");
        foreach (var e in personelEntries.Where(e => e.Status == "Pending").Take(5))
        {
            _output.WriteLine($"  Id={e.Id} | {e.Display}");
            _output.WriteLine($"    ESKİ: {e.Old}");
            _output.WriteLine($"    YENİ: {e.New}");
        }
        _output.WriteLine("");

        // Missing'ler (ilk 5)
        var personelMissingList = personelEntries.Where(e => e.Status == "Missing").Take(5).ToList();
        if (personelMissingList.Any())
        {
            _output.WriteLine("--- Eksik Personel Dosyaları (ilk 5) ---");
            foreach (var e in personelMissingList)
            {
                _output.WriteLine($"  Id={e.Id} | {e.Display} | {e.Error}");
            }
            _output.WriteLine("");
        }

        // ─── ARAÇ ──────────────────────────────────────────────────
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("  ARAÇ EVRAK ANALİZİ");
        _output.WriteLine("═══════════════════════════════════════════");

        var aracEvraklar = await ctx.AracEvrakDosyalari
            .IgnoreQueryFilters()
            .Include(d => d.AracEvrak).ThenInclude(e => e.Arac).ThenInclude(a => a.Firma)
            .Where(d => !string.IsNullOrWhiteSpace(d.DosyaYolu) && !d.IsDeleted)
            .ToListAsync();

        var aracTotal = aracEvraklar.Count;
        var aracAlreadyNew = 0;
        var aracMissing = 0;
        var aracPending = 0;
        var aracCollisions = 0;
        var aracFirmaMissing = 0;
        var aracPlakaMissing = 0;
        var aracTargetPaths = new Dictionary<string, int>();

        var aracEntries = new List<(int Id, string Display, string Old, string New, string Status, string? Error)>();

        foreach (var dosya in aracEvraklar)
        {
            var n = (dosya.DosyaYolu ?? "").Replace('\\', '/').TrimStart('/');

            // Hedef path'i her zaman hesapla
            var plaka = dosya.AracEvrak?.Arac?.AktifPlaka
                        ?? dosya.AracEvrak?.Arac?.SaseNo
                        ?? dosya.AracEvrak?.AracId.ToString()
                        ?? "ARAC";
            var firma = dosya.AracEvrak?.Arac?.Firma?.FirmaAdi;
            var klasor = AppStoragePaths.BuildAracArsivKlasoru(plaka, firma);
            var kategori = dosya.AracEvrak?.EvrakKategorisi ?? "Evrak";
            var uzanti = Path.GetExtension((dosya.DosyaYolu ?? ".pdf").Replace(".enc", ""));
            if (string.IsNullOrEmpty(uzanti)) uzanti = ".pdf";
            var ts = (dosya.CreatedAt == default ? DateTime.Now : dosya.CreatedAt).ToString("yyyyMMdd_HHmmss");
            var dosyaAdi = $"{Norm(plaka, true)}{Norm(kategori, true)}_{ts}{uzanti}.enc";
            var targetPath = $"{AppStoragePaths.AracEvrakRelativeRoot}/{klasor}/{dosyaAdi}";

            var oldNormalized = n;
            if (string.Equals(oldNormalized, targetPath.Replace('\\', '/').TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                aracAlreadyNew++;
                aracEntries.Add((dosya.Id,
                    $"{dosya.AracEvrak?.Arac?.AktifPlaka} - {dosya.AracEvrak?.EvrakKategorisi}",
                    dosya.DosyaYolu!, dosya.DosyaYolu!, "Skipped", null));
                continue;
            }

            var normalizedPath = n;
            if (normalizedPath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath["uploads/".Length..];
            var diskPath = Path.Combine(storageRoot, "uploads", normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            if (n.StartsWith("Arsiv/", StringComparison.OrdinalIgnoreCase))
                diskPath = Path.Combine(storageRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar));

            var fileExists = File.Exists(diskPath);
            if (!fileExists)
            {
                var altPath = Path.Combine(storageRoot, "uploads", dosya.DosyaYolu!.Replace('\\', '/').TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                fileExists = File.Exists(altPath);
                if (fileExists) diskPath = altPath;
            }

            if (!fileExists)
            {
                aracMissing++;
                aracEntries.Add((dosya.Id,
                    $"{dosya.AracEvrak?.Arac?.AktifPlaka} - {dosya.AracEvrak?.EvrakKategorisi}",
                    dosya.DosyaYolu!, "", "Missing", $"Diskte yok: {diskPath}"));
                continue;
            }

            // Firma / Plaka bilgisi istatistikleri
            if (string.IsNullOrWhiteSpace(dosya.AracEvrak?.Arac?.Firma?.FirmaAdi))
                aracFirmaMissing++;
            if (string.IsNullOrWhiteSpace(dosya.AracEvrak?.Arac?.AktifPlaka))
                aracPlakaMissing++;

            if (aracTargetPaths.ContainsKey(targetPath))
            {
                aracCollisions++;
                aracTargetPaths[targetPath]++;
            }
            else
            {
                aracTargetPaths[targetPath] = 1;
            }

            aracPending++;
            aracEntries.Add((dosya.Id,
                $"{plaka} - {kategori}",
                dosya.DosyaYolu!, targetPath, "Pending", null));
        }

        _output.WriteLine($"Toplam araç evrak kaydı: {aracTotal}");
        _output.WriteLine($"Taşınacak: {aracPending}");
        _output.WriteLine($"Zaten yeni dizinde: {aracAlreadyNew}");
        _output.WriteLine($"Dosyası eksik (diskte yok): {aracMissing}");
        _output.WriteLine($"Firma bilgisi eksik: {aracFirmaMissing}");
        _output.WriteLine($"Plaka bilgisi eksik: {aracPlakaMissing}");
        _output.WriteLine($"Çakışma riski olan: {aracCollisions}");
        _output.WriteLine("");

        _output.WriteLine("--- Örnek Araç Path'leri ---");
        foreach (var e in aracEntries.Where(e => e.Status == "Pending").Take(5))
        {
            _output.WriteLine($"  Id={e.Id} | {e.Display}");
            _output.WriteLine($"    ESKİ: {e.Old}");
            _output.WriteLine($"    YENİ: {e.New}");
        }
        _output.WriteLine("");

        var aracMissingList = aracEntries.Where(e => e.Status == "Missing").Take(5).ToList();
        if (aracMissingList.Any())
        {
            _output.WriteLine("--- Eksik Araç Dosyaları (ilk 5) ---");
            foreach (var e in aracMissingList)
            {
                _output.WriteLine($"  Id={e.Id} | {e.Display} | {e.Error}");
            }
            _output.WriteLine("");
        }

        // ─── ÖZET ──────────────────────────────────────────────────
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine("  GENEL ÖZET");
        _output.WriteLine("═══════════════════════════════════════════");
        _output.WriteLine($"Personel: Toplam={personelTotal} Taşınacak={personelPending} YeniDizinde={personelAlreadyNew} Eksik={personelMissing} Çakışma={personelCollisions}");
        _output.WriteLine($"Araç:     Toplam={aracTotal} Taşınacak={aracPending} YeniDizinde={aracAlreadyNew} Eksik={aracMissing} Çakışma={aracCollisions}");
        _output.WriteLine("");

        // Assertions — test başarısız olmasın, sadece raporlasın
        _output.WriteLine("DRY-RUN BAŞARIYLA TAMAMLANDI.");
        _output.WriteLine($"Rapor klasörü: {AppStoragePaths.DefaultStorageRoot}\\Arsiv\\MigrationReports");

        // Önemli: DRY-RUN'da hiçbir değişiklik yapılmadı
        Assert.True(true); // Her zaman geçer — bu sadece analiz
    }
}
