using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Kayıp/kırık EvrakDosya kayıtlarını tespit ve temizlik servisi.
/// Varsayılan mod: DryRun (salt rapor). CleanupAsync ile uygulama.
/// </summary>
public class EvrakDosyaMaintenanceService : IEvrakDosyaMaintenanceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly FileService _fileService;
    private readonly ISecureFileService _secureFileService;
    private readonly ILogger<EvrakDosyaMaintenanceService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EvrakDosyaMaintenanceService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        FileService fileService,
        ISecureFileService secureFileService,
        ILogger<EvrakDosyaMaintenanceService> logger,
        IWebHostEnvironment environment)
    {
        _dbFactory = dbFactory;
        _fileService = fileService;
        _secureFileService = secureFileService;
        _logger = logger;
        _environment = environment;
    }

    public async Task<EvrakDosyaMaintenanceReport> AnalyzeAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("EvrakDosya bakim RAPORU baslatiliyor (DryRun)...");
        return await RunAsync(dryRun: true, ct);
    }

    public async Task<EvrakDosyaMaintenanceReport> CleanupAsync(
        EvrakDosyaMaintenanceReport? previewReport = null, CancellationToken ct = default)
    {
        if (previewReport is { KayipSayisi: 0 })
        {
            _logger.LogInformation("Temizlik atlandi: kayip kayit yok.");
            return previewReport;
        }

        _logger.LogWarning("EvrakDosya bakim TEMIZLIK modu baslatiliyor. Kayip sayisi: {Count}",
            previewReport?.KayipSayisi ?? 0);
        return await RunAsync(dryRun: false, ct);
    }

    private async Task<EvrakDosyaMaintenanceReport> RunAsync(bool dryRun, CancellationToken ct)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Tüm aktif EvrakDosya kayıtlarını çek
            var tumEvrakDosyalar = await db.EvrakDosyalari
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.PersonelId)
                .ThenBy(x => x.EvrakTipi)
                .ToListAsync(ct);

            var tumOzlukTanimlar = await db.OzlukEvrakTanimlari
                .Where(t => !t.IsDeleted && t.Aktif)
                .ToListAsync(ct);

            var databasePaths = await DatabaseFilePathInventory.ReadAllAsync(db, ct);
            var storageRoot = AppStoragePaths.GetStorageRoot(_environment.ContentRootPath);
            var yetimSifreliDosyalar = SecureFileOrphanScanner.FindOrphans(storageRoot, databasePaths).ToList();
            var sifresizDosyaAdaylari = SecureFileOrphanScanner.FindLegacyPlaintextCandidates(storageRoot, databasePaths)
                .Concat(SecureFileOrphanScanner.FindLegacyUploadCandidates(
                    _fileService.UploadRootPath, databasePaths, fileServiceRoot: true))
                .Concat(SecureFileOrphanScanner.FindLegacyUploadCandidates(
                    Path.Combine(_environment.WebRootPath, "uploads"), databasePaths, fileServiceRoot: false))
                .OrderBy(candidate => candidate.StoragePath, OperatingSystem.IsWindows()
                    ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList();

            var kayiplar = new List<EvrakDosyaKayipOgesi>();
            var saglamlar = new List<EvrakDosyaKayipOgesi>();

            foreach (var evrakDosya in tumEvrakDosyalar)
            {
                ct.ThrowIfCancellationRequested();

                var sifreli = evrakDosya.DosyaYolu.EndsWith(".enc", StringComparison.OrdinalIgnoreCase)
                    && (evrakDosya.DosyaYolu.Contains('/') || evrakDosya.DosyaYolu.Contains('\\'));
                var tamYol = sifreli ? evrakDosya.DosyaYolu : _fileService.GetFullPath(evrakDosya.DosyaYolu);
                var dosyaVar = sifreli
                    ? await _secureFileService.ExistsAsync(evrakDosya.DosyaYolu, ct)
                    : File.Exists(tamYol);
                var canonical = EvrakTipiCanonicalMapper.GetCanonicalName(evrakDosya.EvrakTipi);

                // Personel adını bul
                string? personelAdi = null;
                if (evrakDosya.PersonelId.HasValue)
                {
                    var sofor = await db.Soforler
                        .Where(s => s.Id == evrakDosya.PersonelId.Value)
                        .Select(s => s.TamAd)
                        .FirstOrDefaultAsync(ct);
                    personelAdi = sofor;
                }

                // Eşleşen PersonelOzlukEvrak kaydını bul
                int? eslesenOzlukEvrakId = null;
                string? ozlukEvrakAdi = null;
                bool ozlukEvrakTamamlandi = false;

                if (canonical != null && evrakDosya.PersonelId.HasValue)
                {
                    var eslesenTanim = tumOzlukTanimlar.FirstOrDefault(t =>
                        EvrakTipiCanonicalMapper.BelongsToCategory(t.EvrakAdi, canonical));

                    if (eslesenTanim != null)
                    {
                        var ozlukEvrak = await db.PersonelOzlukEvraklar
                            .Where(e => e.SoforId == evrakDosya.PersonelId.Value
                                        && e.EvrakTanimId == eslesenTanim.Id
                                        && !e.IsDeleted)
                            .FirstOrDefaultAsync(ct);

                        if (ozlukEvrak != null)
                        {
                            eslesenOzlukEvrakId = ozlukEvrak.Id;
                            ozlukEvrakAdi = eslesenTanim.EvrakAdi;
                            ozlukEvrakTamamlandi = ozlukEvrak.Tamamlandi;
                        }
                    }
                }

                var oge = new EvrakDosyaKayipOgesi
                {
                    EvrakDosyaId = evrakDosya.Id,
                    EvrakTipi = evrakDosya.EvrakTipi,
                    DosyaAdi = evrakDosya.DosyaAdi,
                    DosyaYolu = evrakDosya.DosyaYolu,
                    PersonelId = evrakDosya.PersonelId,
                    PersonelAdi = personelAdi,
                    CanonicalTip = canonical,
                    TamYol = tamYol,
                    DosyaVar = dosyaVar,
                    EslesenOzlukEvrakId = eslesenOzlukEvrakId,
                    OzlukEvrakAdi = ozlukEvrakAdi,
                    OzlukEvrakTamamlandi = ozlukEvrakTamamlandi
                };

                if (dosyaVar)
                    saglamlar.Add(oge);
                else
                    kayiplar.Add(oge);
            }

            var report = new EvrakDosyaMaintenanceReport
            {
                ToplamEvrakDosya = tumEvrakDosyalar.Count,
                KayipSayisi = kayiplar.Count,
                SaglamSayisi = saglamlar.Count,
                Kayiplar = kayiplar,
                Saglamlar = saglamlar,
                YetimSifreliDosyalar = yetimSifreliDosyalar,
                YetimSifreliDosyaSayisi = yetimSifreliDosyalar.Count,
                SifresizDosyaAdaylari = sifresizDosyaAdaylari,
                SifresizDosyaAdaySayisi = sifresizDosyaAdaylari.Count,
                DryRun = dryRun
            };

            // ── Temizlik (sadece dryRun=false ise) ──
            if (!dryRun && kayiplar.Count > 0)
            {
                _logger.LogWarning("TEMIZLIK BASLIYOR: {Count} kayıp EvrakDosya soft-delete edilecek.", kayiplar.Count);

                int temizlenenEvrakDosya = 0;
                int temizlenenOzlukEvrak = 0;

                foreach (var kayip in kayiplar)
                {
                    ct.ThrowIfCancellationRequested();

                    // 1. EvrakDosya soft-delete
                    var evrakDosyaEntity = await db.EvrakDosyalari
                        .FirstOrDefaultAsync(x => x.Id == kayip.EvrakDosyaId && !x.IsDeleted, ct);

                    if (evrakDosyaEntity != null)
                    {
                        evrakDosyaEntity.IsDeleted = true;
                        temizlenenEvrakDosya++;
                        _logger.LogInformation("  EvrakDosya soft-delete: Id={Id} Tip={Tip} Dosya={Dosya}",
                            kayip.EvrakDosyaId, kayip.EvrakTipi, kayip.DosyaAdi);
                    }

                    // 2. PersonelOzlukEvrak temizliği
                    if (kayip.EslesenOzlukEvrakId.HasValue)
                    {
                        var ozlukEvrak = await db.PersonelOzlukEvraklar
                            .FirstOrDefaultAsync(x => x.Id == kayip.EslesenOzlukEvrakId.Value && !x.IsDeleted, ct);

                        if (ozlukEvrak != null)
                        {
                            ozlukEvrak.Tamamlandi = false;
                            ozlukEvrak.DosyaYolu = null;
                            ozlukEvrak.DosyaAdi = null;
                            ozlukEvrak.DosyaTipi = null;
                            ozlukEvrak.DosyaBoyutu = null;
                            ozlukEvrak.UpdatedAt = DateTime.UtcNow;
                            temizlenenOzlukEvrak++;
                            _logger.LogInformation("  PersonelOzlukEvrak temizlendi: Id={Id} Tanim={Tanim}",
                                ozlukEvrak.Id, kayip.OzlukEvrakAdi);
                        }
                    }
                }

                await db.SaveChangesAsync(ct);

                report = report with
                {
                    TemizlenenEvrakDosya = temizlenenEvrakDosya,
                    TemizlenenOzlukEvrak = temizlenenOzlukEvrak,
                    DryRun = false
                };

                _logger.LogWarning("TEMIZLIK TAMAMLANDI: {EvrakDosya} EvrakDosya + {OzlukEvrak} PersonelOzlukEvrak temizlendi.",
                    temizlenenEvrakDosya, temizlenenOzlukEvrak);
            }

            return report;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("EvrakDosya bakim iptal edildi.");
            return new EvrakDosyaMaintenanceReport { HataMesaji = "İptal edildi." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EvrakDosya bakim hatasi");
            return new EvrakDosyaMaintenanceReport { HataMesaji = ex.Message };
        }
    }

}


