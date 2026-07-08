using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Mevcut evrak kayıtlarını yeni arşiv yapısına taşıyan backfill servisi.
///
/// İşlem sırası (her evrak için):
/// 1. Eski dosyayı decrypt et
/// 2. Yeni şifreli arşive KOA1 formatında yaz
/// 3. Şifreli arşivi tekrar decrypt edip doğrula
/// 4. DB DosyaYolu'nu güncelle
///
/// Eski dosyalar silinmez.
/// </summary>
public sealed class EvrakArsivBackfillService : IEvrakArsivBackfillService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IEvrakArsivService _evrakArsivService;
    private readonly ILogger<EvrakArsivBackfillService> _logger;

    public EvrakArsivBackfillService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        IEvrakArsivService evrakArsivService,
        ILogger<EvrakArsivBackfillService> logger)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _evrakArsivService = evrakArsivService;
        _logger = logger;
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
                .ThenInclude(a => a!.Firma)
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
                string evrakNiteligi = dosya.AracEvrak?.EvrakAdi
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

                var uzanti = FileNameHelper.NormalizeExtension(dosya.DosyaTipi ?? ".pdf");
                var firmaAdi = arac.Firma?.FirmaAdi ?? "FIRMA";
                var hedefKlasor = AppStoragePaths.BuildAracArsivKlasoru(plaka, firmaAdi);
                var hedefDosyaBase = AppStoragePaths.NormalizeFolderName(evrakNiteligi ?? "EVRAK")
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty);

                satir.YeniSifreliPath = $"Arsiv/Sifreli/Araclar/{hedefKlasor}/{hedefDosyaBase}{uzanti}.enc";

                if (!dryRun)
                {
                    var yeniSifreliRelativePath = await _evrakArsivService.ArsivleAracEvrakAsync(
                        plaka,
                        firmaAdi,
                        evrakNiteligi ?? "EVRAK",
                        plainBytes,
                        uzanti,
                        cancellationToken);

                    satir.YeniSifreliPath = yeniSifreliRelativePath;

                    if (updateDatabase)
                    {
                        await context.AracEvrakDosyalari
                            .IgnoreQueryFilters()
                            .Where(x => x.Id == dosya.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(x => x.DosyaYolu, yeniSifreliRelativePath)
                                .SetProperty(x => x.DosyaAdi, Path.GetFileName(yeniSifreliRelativePath))
                                .SetProperty(x => x.DosyaTipi, uzanti.TrimStart('.'))
                                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
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
                .ThenInclude(s => s.Firma)
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
                string evrakNiteligi = evrak.EvrakTanim?.EvrakAdi ?? "EVRAK";

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

                var uzanti = FileNameHelper.NormalizeExtension(evrak.DosyaTipi ?? ".pdf");
                var firmaAdi = personel?.Firma?.FirmaAdi ?? "FIRMA";
                var hedefKlasor = AppStoragePaths.BuildPersonelArsivKlasoru(personel?.Ad ?? "PERSONEL", personel?.Soyad ?? evrak.SoforId.ToString(), firmaAdi);
                var hedefDosyaBase = AppStoragePaths.NormalizeFolderName(evrakNiteligi ?? "EVRAK")
                    .Replace(" ", string.Empty)
                    .Replace("-", string.Empty);

                satir.YeniSifreliPath = $"Arsiv/Sifreli/Personeller/{hedefKlasor}/{hedefDosyaBase}{uzanti}.enc";

                if (!dryRun)
                {
                    var yeniSifreliRelativePath = await _evrakArsivService.ArsivlePersonelEvrakAsync(
                        adSoyad,
                        firmaAdi,
                        evrakNiteligi ?? "EVRAK",
                        plainBytes,
                        uzanti,
                        cancellationToken);

                    satir.YeniSifreliPath = yeniSifreliRelativePath;

                    if (updateDatabase)
                    {
                        await context.PersonelOzlukEvraklar
                            .IgnoreQueryFilters()
                            .Where(x => x.Id == evrak.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(x => x.DosyaYolu, yeniSifreliRelativePath)
                                .SetProperty(x => x.DosyaAdi, Path.GetFileName(yeniSifreliRelativePath))
                                .SetProperty(x => x.DosyaTipi, uzanti.TrimStart('.'))
                                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                                cancellationToken);
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



