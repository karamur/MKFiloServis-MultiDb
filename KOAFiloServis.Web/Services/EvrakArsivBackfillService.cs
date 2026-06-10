using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// Mevcut evrak kayıtlarını yeni arşiv yapısına taşıyan backfill servisi.
///
/// İşlem sırası (her evrak için):
/// 1. Eski dosyayı decrypt et
/// 2. Yeni şifresiz arşive yaz
/// 3. Yeni şifreli arşive KOA1 formatında yaz
/// 4. Şifreli arşivi tekrar decrypt edip doğrula
/// 5. DB DosyaYolu'nu güncelle
///
/// Eski dosyalar silinmez.
/// </summary>
public sealed class EvrakArsivBackfillService : IEvrakArsivBackfillService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IFileProtector _fileProtector;
    private readonly IEvrakArsivService _evrakArsivService;
    private readonly ILogger<EvrakArsivBackfillService> _logger;
    private readonly string _storageRoot;

    public EvrakArsivBackfillService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        IFileProtector fileProtector,
        IEvrakArsivService evrakArsivService,
        IWebHostEnvironment environment,
        ILogger<EvrakArsivBackfillService> logger)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _fileProtector = fileProtector;
        _evrakArsivService = evrakArsivService;
        _logger = logger;
        _storageRoot = AppStoragePaths.GetStorageRoot(environment.ContentRootPath);
    }

    public async Task<EvrakArsivBackfillRaporu> DryRunAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteInternalAsync(dryRun: true, updateDatabase: false, overwriteExisting: false, cancellationToken);
    }

    public async Task<EvrakArsivBackfillRaporu> ExecuteAsync(
        bool updateDatabase,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInternalAsync(dryRun: false, updateDatabase, overwriteExisting, cancellationToken);
    }

    private async Task<EvrakArsivBackfillRaporu> ExecuteInternalAsync(
        bool dryRun,
        bool updateDatabase,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        var rapor = new EvrakArsivBackfillRaporu
        {
            Baslangic = DateTime.Now,
            DryRun = dryRun
        };

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // ── Araç Evrakları ────────────────────────────────────────────
        var aracDosyalar = await context.AracEvrakDosyalari
            .IgnoreQueryFilters() // Admin backfill: tenant filtresini aş
            .Include(d => d.AracEvrak)
                .ThenInclude(e => e!.Arac)
            .Where(d => !d.IsDeleted
                && !string.IsNullOrEmpty(d.DosyaYolu)
                && !d.DosyaYolu.StartsWith("Arsiv/")) // Zaten arşivlenmiş olanları atla
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);

        rapor.AracToplam = aracDosyalar.Count;
        _logger.LogInformation("Backfill: {Count} araç evrak dosyası bulundu", aracDosyalar.Count);

        foreach (var dosya in aracDosyalar)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var satir = new EvrakArsivBackfillSatir
            {
                EvrakTipi = "Arac",
                EvrakId = dosya.Id,
                EskiDosyaYolu = dosya.DosyaYolu
            };

            try
            {
                var arac = dosya.AracEvrak?.Arac;
                if (arac == null)
                {
                    satir.Hata = "Araç bilgisi bulunamadı";
                    rapor.AracBasarisiz++;
                    rapor.Satirlar.Add(satir);
                    continue;
                }

                var plaka = arac.AktifPlaka ?? arac.SaseNo ?? $"ARAC-{arac.Id}";
                var sasiNo = arac.SaseNo ?? "SASIYOK";
                var evrakNiteligi = dosya.AracEvrak?.EvrakAdi
                    ?? dosya.AracEvrak?.EvrakKategorisi
                    ?? "EVRAK";

                satir.Sahip = $"{plaka} ({sasiNo})";
                satir.EvrakNiteligi = evrakNiteligi;

                // Eski dosyayı decrypt et
                var plainBytes = await _secureFileService.ReadDecryptedAsync(dosya.DosyaYolu, cancellationToken);
                if (plainBytes == null || plainBytes.Length == 0)
                {
                    satir.Hata = "Eski dosya decrypt edilemedi veya boş";
                    rapor.AracBasarisiz++;
                    rapor.Satirlar.Add(satir);
                    continue;
                }

                // Arşiv zamanı: CreatedAt > dosya LastWriteTime > Now
                var arsivZamani = dosya.CreatedAt != default ? dosya.CreatedAt : DateTime.Now;
                var tarihSaat = arsivZamani.ToString("yyyyMMdd-HHmmss");

                var uzanti = FileNameHelper.NormalizeExtension(dosya.DosyaTipi ?? ".pdf");

                // Yeni pathleri hesapla
                var plakaNorm = FileNameHelper.NormalizeFileName(plaka, $"ARAC-{arac.Id}");
                var sasiNorm = FileNameHelper.NormalizeFileName(sasiNo, "SASIYOK");
                var evrakNorm = FileNameHelper.NormalizeFileName(evrakNiteligi, "EVRAK");
                var baseName = $"{plakaNorm}-{sasiNorm}-{evrakNorm}-{tarihSaat}";

                var yeniSifreliRelativePath = $"Arsiv/Sifreli/Araclar/{baseName}/{baseName}{uzanti}.enc";
                var sifreliFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifreli", "Araclar", baseName);
                var sifresizFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifresiz", "Araclar", baseName);
                var sifreliFullPath = Path.Combine(sifreliFullDir, $"{baseName}{uzanti}.enc");
                var sifresizFullPath = Path.Combine(sifresizFullDir, $"{baseName}{uzanti}");

                satir.YeniSifreliPath = yeniSifreliRelativePath;
                satir.YeniSifresizPath = sifresizFullPath;

                if (!dryRun)
                {
                    // Hedef varsa ve overwrite false ise suffix ekle
                    if (!overwriteExisting && File.Exists(sifreliFullPath))
                    {
                        var v = 2;
                        while (File.Exists(sifreliFullPath))
                        {
                            baseName = $"{plakaNorm}-{sasiNorm}-{evrakNorm}-{tarihSaat}-V{v}";
                            yeniSifreliRelativePath = $"Arsiv/Sifreli/Araclar/{baseName}/{baseName}{uzanti}.enc";
                            sifreliFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifreli", "Araclar", baseName);
                            sifresizFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifresiz", "Araclar", baseName);
                            sifreliFullPath = Path.Combine(sifreliFullDir, $"{baseName}{uzanti}.enc");
                            sifresizFullPath = Path.Combine(sifresizFullDir, $"{baseName}{uzanti}");
                            v++;
                        }
                    }

                    // 1) Şifresiz arşiv
                    Directory.CreateDirectory(sifresizFullDir);
                    await File.WriteAllBytesAsync(sifresizFullPath, plainBytes, cancellationToken);
                    if (!File.Exists(sifresizFullPath) || new FileInfo(sifresizFullPath).Length <= 0)
                        throw new InvalidOperationException("Şifresiz arşiv dosyası yazılamadı veya boş.");

                    // 2) Şifreli arşiv (KOA1)
                    Directory.CreateDirectory(sifreliFullDir);
                    var encrypted = _fileProtector.Protect(plainBytes); // KOA1 format
                    await File.WriteAllBytesAsync(sifreliFullPath, encrypted, cancellationToken);

                    // 3) Doğrula: şifreli dosyayı decrypt et
                    var verifyBytes = _fileProtector.Unprotect(await File.ReadAllBytesAsync(sifreliFullPath, cancellationToken));
                    if (!verifyBytes.SequenceEqual(plainBytes))
                        throw new InvalidOperationException("Şifreli arşiv doğrulaması başarısız: decrypt sonucu orijinal ile aynı değil.");

                    // 4) DB güncelleme
                    if (updateDatabase)
                    {
                        await context.AracEvrakDosyalari
                            .IgnoreQueryFilters()
                            .Where(x => x.Id == dosya.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(x => x.DosyaYolu, yeniSifreliRelativePath)
                                .SetProperty(x => x.DosyaAdi, $"{baseName}{uzanti}.enc")
                                .SetProperty(x => x.DosyaTipi, uzanti.TrimStart('.'))
                                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
                    }
                }
                else
                {
                    // Dry-run: sadece eski dosya var mı kontrol et
                    if (string.IsNullOrEmpty(dosya.DosyaYolu))
                    {
                        satir.Hata = "Eski DosyaYolu boş";
                        rapor.AracBasarisiz++;
                        rapor.Satirlar.Add(satir);
                        continue;
                    }
                }

                satir.Basarili = true;
                rapor.AracBasarili++;
            }
            catch (Exception ex)
            {
                satir.Hata = ex.Message;
                rapor.AracBasarisiz++;
                _logger.LogError(ex, "Backfill arac evrak başarısız. Id={Id}, Yol={Yol}", dosya.Id, dosya.DosyaYolu);
            }

            rapor.Satirlar.Add(satir);
        }

        // ── Personel Evrakları ─────────────────────────────────────────
        var personelEvraklar = await context.PersonelOzlukEvraklar
            .IgnoreQueryFilters() // Admin backfill: tenant filtresini aş
            .Include(e => e.Sofor)
            .Include(e => e.EvrakTanim)
            .Where(e => !e.IsDeleted
                && !string.IsNullOrEmpty(e.DosyaYolu)
                && !e.DosyaYolu.StartsWith("Arsiv/")) // Zaten arşivlenmiş olanları atla
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

        rapor.PersonelToplam = personelEvraklar.Count;
        _logger.LogInformation("Backfill: {Count} personel evrak kaydı bulundu", personelEvraklar.Count);

        foreach (var evrak in personelEvraklar)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var satir = new EvrakArsivBackfillSatir
            {
                EvrakTipi = "Personel",
                EvrakId = evrak.Id,
                EskiDosyaYolu = evrak.DosyaYolu ?? ""
            };

            try
            {
                var personel = evrak.Sofor;
                var adSoyad = personel?.TamAd ?? $"PERSONEL-{evrak.SoforId}";
                var evrakNiteligi = evrak.EvrakTanim?.EvrakAdi ?? "EVRAK";

                satir.Sahip = adSoyad;
                satir.EvrakNiteligi = evrakNiteligi;

                if (string.IsNullOrEmpty(evrak.DosyaYolu))
                {
                    satir.Hata = "DosyaYolu boş";
                    rapor.PersonelBasarisiz++;
                    rapor.Satirlar.Add(satir);
                    continue;
                }

                // Eski dosyayı decrypt et
                var plainBytes = await _secureFileService.ReadDecryptedAsync(evrak.DosyaYolu, cancellationToken);
                if (plainBytes == null || plainBytes.Length == 0)
                {
                    satir.Hata = "Eski dosya decrypt edilemedi veya boş";
                    rapor.PersonelBasarisiz++;
                    rapor.Satirlar.Add(satir);
                    continue;
                }

                // Arşiv zamanı
                var arsivZamani = evrak.CreatedAt != default ? evrak.CreatedAt : DateTime.Now;
                var tarihSaat = arsivZamani.ToString("yyyyMMdd-HHmmss");

                var uzanti = FileNameHelper.NormalizeExtension(evrak.DosyaTipi ?? ".pdf");

                // Yeni pathleri hesapla
                var adSoyadNorm = FileNameHelper.NormalizeFileName(adSoyad, $"PERSONEL-{evrak.SoforId}");
                var evrakNorm = FileNameHelper.NormalizeFileName(evrakNiteligi, "EVRAK");
                var baseName = $"{adSoyadNorm}-{evrakNorm}-{tarihSaat}";

                var yeniSifreliRelativePath = $"Arsiv/Sifreli/Personeller/{baseName}/{baseName}{uzanti}.enc";
                var sifreliFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifreli", "Personeller", baseName);
                var sifresizFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifresiz", "Personeller", baseName);
                var sifreliFullPath = Path.Combine(sifreliFullDir, $"{baseName}{uzanti}.enc");
                var sifresizFullPath = Path.Combine(sifresizFullDir, $"{baseName}{uzanti}");

                satir.YeniSifreliPath = yeniSifreliRelativePath;
                satir.YeniSifresizPath = sifresizFullPath;

                if (!dryRun)
                {
                    if (!overwriteExisting && File.Exists(sifreliFullPath))
                    {
                        var v = 2;
                        while (File.Exists(sifreliFullPath))
                        {
                            baseName = $"{adSoyadNorm}-{evrakNorm}-{tarihSaat}-V{v}";
                            yeniSifreliRelativePath = $"Arsiv/Sifreli/Personeller/{baseName}/{baseName}{uzanti}.enc";
                            sifreliFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifreli", "Personeller", baseName);
                            sifresizFullDir = Path.Combine(_storageRoot, "Arsiv", "Sifresiz", "Personeller", baseName);
                            sifreliFullPath = Path.Combine(sifreliFullDir, $"{baseName}{uzanti}.enc");
                            sifresizFullPath = Path.Combine(sifresizFullDir, $"{baseName}{uzanti}");
                            v++;
                        }
                    }

                    // 1) Şifresiz
                    Directory.CreateDirectory(sifresizFullDir);
                    await File.WriteAllBytesAsync(sifresizFullPath, plainBytes, cancellationToken);
                    if (!File.Exists(sifresizFullPath) || new FileInfo(sifresizFullPath).Length <= 0)
                        throw new InvalidOperationException("Şifresiz arşiv dosyası yazılamadı veya boş.");

                    // 2) Şifreli (KOA1)
                    Directory.CreateDirectory(sifreliFullDir);
                    var encrypted = _fileProtector.Protect(plainBytes);
                    await File.WriteAllBytesAsync(sifreliFullPath, encrypted, cancellationToken);

                    // 3) Doğrula
                    var verifyBytes = _fileProtector.Unprotect(await File.ReadAllBytesAsync(sifreliFullPath, cancellationToken));
                    if (!verifyBytes.SequenceEqual(plainBytes))
                        throw new InvalidOperationException("Şifreli arşiv doğrulaması başarısız.");

                    // 4) DB güncelleme
                    if (updateDatabase)
                    {
                        await context.PersonelOzlukEvraklar
                            .IgnoreQueryFilters()
                            .Where(x => x.Id == evrak.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(x => x.DosyaYolu, yeniSifreliRelativePath)
                                .SetProperty(x => x.DosyaAdi, $"{baseName}{uzanti}.enc")
                                .SetProperty(x => x.DosyaTipi, uzanti.TrimStart('.'))
                                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(evrak.DosyaYolu))
                    {
                        satir.Hata = "Eski DosyaYolu boş";
                        rapor.PersonelBasarisiz++;
                        rapor.Satirlar.Add(satir);
                        continue;
                    }
                }

                satir.Basarili = true;
                rapor.PersonelBasarili++;
            }
            catch (Exception ex)
            {
                satir.Hata = ex.Message;
                rapor.PersonelBasarisiz++;
                _logger.LogError(ex, "Backfill personel evrak başarısız. Id={Id}, Yol={Yol}", evrak.Id, evrak.DosyaYolu);
            }

            rapor.Satirlar.Add(satir);
        }

        rapor.Bitis = DateTime.Now;
        _logger.LogInformation(
            "Backfill tamamlandı. DryRun={DryRun}. Arac: {AracOk}/{AracTotal}, Personel: {PersonelOk}/{PersonelTotal}",
            dryRun, rapor.AracBasarili, rapor.AracToplam, rapor.PersonelBasarili, rapor.PersonelToplam);

        return rapor;
    }
}
