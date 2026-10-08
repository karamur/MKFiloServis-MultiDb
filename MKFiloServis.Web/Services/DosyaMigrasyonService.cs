using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using MKFiloServis.Web.Services.Interfaces;
using MKFiloServis.Web.Services.Security;
using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Aktif firma kapsamındaki EBYS/araç belgeleri ile ortak evrakın eski düz dosyalarını
/// şifreli depoya taşıyan migration servisi.
/// </summary>
public class DosyaMigrasyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly FileService _legacyCommonFileService;
    private readonly LegacyFileCleanupService _legacyCleanup;
    private readonly IAktifFirmaProvider _activeFirm;
    private readonly IWebHostEnvironment _env;
    private readonly IFileProtector _fileProtector;
    private readonly ILogger<DosyaMigrasyonService> _logger;

    // wwwroot altındaki eski upload klasörü (yeni kurulumda boş olacak)
    private string WwwrootUploads => Path.Combine(_env.WebRootPath, "uploads");
    private string SupportUploads => Path.Combine(_env.WebRootPath, "uploads", "destek");

    public DosyaMigrasyonService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        FileService legacyCommonFileService,
        LegacyFileCleanupService legacyCleanup,
        IAktifFirmaProvider activeFirm,
        IWebHostEnvironment env,
        IFileProtector fileProtector,
        ILogger<DosyaMigrasyonService> logger)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _legacyCommonFileService = legacyCommonFileService;
        _legacyCleanup = legacyCleanup;
        _activeFirm = activeFirm;
        _env = env;
        _fileProtector = fileProtector;
        _logger = logger;
    }

    /// <summary>Taşınmayı bekleyen dosya sayısını döndürür (önizleme).</summary>
    public async Task<DosyaMigrasyonOzet> OnizlemeAsync(CancellationToken ct = default)
    {
        var firmaId = RequireSingleActiveFirm();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);

        // EBYS
        var ebysPaths = await ctx.EbysEvrakDosyalar.IgnoreQueryFilters()
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(d => d.DosyaYolu)
            .ToListAsync(ct);
        var deletedEbysPaths = await ctx.EbysEvrakDosyalar.IgnoreQueryFilters()
            .Where(d => d.IsDeleted && d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(d => d.DosyaYolu)
            .ToListAsync(ct);

        var ebysVersiyonPaths = await ctx.EbysEvrakDosyaVersiyonlar.IgnoreQueryFilters()
            .Where(v => !v.IsDeleted && v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(v => v.DosyaYolu)
            .ToListAsync(ct);
        var deletedEbysVersiyonPaths = await ctx.EbysEvrakDosyaVersiyonlar.IgnoreQueryFilters()
            .Where(v => v.IsDeleted && v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(v => v.DosyaYolu)
            .ToListAsync(ct);

        var aracDosyaPaths = await FirmVehicleDocuments(ctx, firmaId)
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(d => d.DosyaYolu)
            .ToListAsync(ct);
        var deletedAracDosyaPaths = await FirmVehicleDocuments(ctx, firmaId)
            .Where(d => d.IsDeleted && d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(d => d.DosyaYolu)
            .ToListAsync(ct);

        var aracVersiyonPaths = await FirmVehicleVersions(ctx, firmaId)
            .Where(v => !v.IsDeleted && v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(v => v.DosyaYolu)
            .ToListAsync(ct);
        var deletedAracVersiyonPaths = await FirmVehicleVersions(ctx, firmaId)
            .Where(v => v.IsDeleted && v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .Select(v => v.DosyaYolu)
            .ToListAsync(ct);

        var personelPaths = await FirmPersonnelDocuments(ctx, firmaId)
            .Where(e => !e.IsDeleted && !e.Sofor.IsDeleted && e.DosyaYolu != null &&
                ((e.DosyaYolu.StartsWith("/uploads/") || e.DosyaYolu.StartsWith("uploads/") || e.DosyaYolu.StartsWith("\\uploads\\")) ||
                 (!e.DosyaYolu.Contains("/") && !e.DosyaYolu.Contains("\\") && !e.DosyaYolu.EndsWith(".enc"))))
            .Select(e => e.DosyaYolu).ToListAsync(ct);
        var deletedPersonelPaths = await FirmPersonnelDocuments(ctx, firmaId)
            .Where(e => (e.IsDeleted || e.Sofor.IsDeleted) && e.DosyaYolu != null &&
                ((e.DosyaYolu.StartsWith("/uploads/") || e.DosyaYolu.StartsWith("uploads/") || e.DosyaYolu.StartsWith("\\uploads\\")) ||
                 (!e.DosyaYolu.Contains("/") && !e.DosyaYolu.Contains("\\") && !e.DosyaYolu.EndsWith(".enc"))))
            .Select(e => e.DosyaYolu).ToListAsync(ct);
        var personelVersionPaths = await FirmPersonnelVersions(ctx, firmaId)
            .Where(v => !v.IsDeleted && !v.PersonelOzlukEvrak!.IsDeleted && !v.PersonelOzlukEvrak.Sofor.IsDeleted && v.DosyaYolu != null &&
                ((v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")) ||
                 (!v.DosyaYolu.Contains("/") && !v.DosyaYolu.Contains("\\") && !v.DosyaYolu.EndsWith(".enc"))))
            .Select(v => v.DosyaYolu).ToListAsync(ct);
        var deletedPersonelVersionPaths = await FirmPersonnelVersions(ctx, firmaId)
            .Where(v => (v.IsDeleted || v.PersonelOzlukEvrak!.IsDeleted || v.PersonelOzlukEvrak.Sofor.IsDeleted) && v.DosyaYolu != null &&
                ((v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")) ||
                 (!v.DosyaYolu.Contains("/") && !v.DosyaYolu.Contains("\\") && !v.DosyaYolu.EndsWith(".enc"))))
            .Select(v => v.DosyaYolu).ToListAsync(ct);

        var invoicePdfPaths = await ctx.Faturalar.IgnoreQueryFilters()
            .Where(f => f.FirmaId == firmaId && !f.IsDeleted && f.PdfDosyaYolu != null &&
                (f.PdfDosyaYolu.StartsWith("/uploads/") || f.PdfDosyaYolu.StartsWith("uploads/") || f.PdfDosyaYolu.StartsWith("\\uploads\\") ||
                 f.PdfDosyaYolu.StartsWith("/efatura/") || f.PdfDosyaYolu.StartsWith("efatura/") || f.PdfDosyaYolu.StartsWith("\\efatura\\") ||
                 f.PdfDosyaYolu.StartsWith("/belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("\\belgeler\\efatura\\")))
            .Select(f => f.PdfDosyaYolu).ToListAsync(ct);
        var deletedInvoicePdfPaths = await ctx.Faturalar.IgnoreQueryFilters()
            .Where(f => f.FirmaId == firmaId && f.IsDeleted && f.PdfDosyaYolu != null &&
                (f.PdfDosyaYolu.StartsWith("/uploads/") || f.PdfDosyaYolu.StartsWith("uploads/") || f.PdfDosyaYolu.StartsWith("\\uploads\\") ||
                 f.PdfDosyaYolu.StartsWith("/efatura/") || f.PdfDosyaYolu.StartsWith("efatura/") || f.PdfDosyaYolu.StartsWith("\\efatura\\") ||
                 f.PdfDosyaYolu.StartsWith("/belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("\\belgeler\\efatura\\")))
            .Select(f => f.PdfDosyaYolu).ToListAsync(ct);
        var invoiceXmlPaths = await ctx.Faturalar.IgnoreQueryFilters()
            .Where(f => f.FirmaId == firmaId && !f.IsDeleted && f.XmlDosyaYolu != null &&
                (f.XmlDosyaYolu.StartsWith("/uploads/") || f.XmlDosyaYolu.StartsWith("uploads/") || f.XmlDosyaYolu.StartsWith("\\uploads\\") ||
                 f.XmlDosyaYolu.StartsWith("/efatura/") || f.XmlDosyaYolu.StartsWith("efatura/") || f.XmlDosyaYolu.StartsWith("\\efatura\\") ||
                 f.XmlDosyaYolu.StartsWith("/belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("\\belgeler\\efatura\\")))
            .Select(f => f.XmlDosyaYolu).ToListAsync(ct);
        var deletedInvoiceXmlPaths = await ctx.Faturalar.IgnoreQueryFilters()
            .Where(f => f.FirmaId == firmaId && f.IsDeleted && f.XmlDosyaYolu != null &&
                (f.XmlDosyaYolu.StartsWith("/uploads/") || f.XmlDosyaYolu.StartsWith("uploads/") || f.XmlDosyaYolu.StartsWith("\\uploads\\") ||
                 f.XmlDosyaYolu.StartsWith("/efatura/") || f.XmlDosyaYolu.StartsWith("efatura/") || f.XmlDosyaYolu.StartsWith("\\efatura\\") ||
                 f.XmlDosyaYolu.StartsWith("/belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("\\belgeler\\efatura\\")))
            .Select(f => f.XmlDosyaYolu).ToListAsync(ct);

        var ortakEvrakYollari = await FirmCommonDocuments(ctx, firmaId).AsNoTracking()
            .Select(d => new { d.DosyaYolu, d.IsDeleted })
            .ToListAsync(ct);
        var tedarikciDosyalari = await FirmSupplierDocuments(ctx, firmaId)
            .Select(d => new { d.DosyaYolu, d.IsDeleted })
            .ToListAsync(ct);
        var legacyTedarikciDosyalari = tedarikciDosyalari
            .Where(d => IsLegacyCommonPath(d.DosyaYolu)).ToList();
        var destekEkleri = await FirmSupportAttachments(ctx, firmaId).ToListAsync(ct);
        var legacyDestekEkleri = destekEkleri.Where(d => IsLegacySupportPath(d.DosyaYolu)).ToList();

        return new DosyaMigrasyonOzet
        {
            EbysAnaCount = ebysPaths.Count,
            SilinmisEbysAnaCount = deletedEbysPaths.Count,
            EbysVersiyonCount = ebysVersiyonPaths.Count,
            SilinmisEbysVersiyonCount = deletedEbysVersiyonPaths.Count,
            AracEvrakCount = aracDosyaPaths.Count,
            SilinmisAracEvrakCount = deletedAracDosyaPaths.Count,
            AracVersiyonCount = aracVersiyonPaths.Count,
            SilinmisAracVersiyonCount = deletedAracVersiyonPaths.Count,
            PersonelEvrakCount = personelPaths.Count,
            SilinmisPersonelEvrakCount = deletedPersonelPaths.Count,
            PersonelVersiyonCount = personelVersionPaths.Count,
            SilinmisPersonelVersiyonCount = deletedPersonelVersionPaths.Count,
            FaturaPdfCount = invoicePdfPaths.Count,
            SilinmisFaturaPdfCount = deletedInvoicePdfPaths.Count,
            FaturaXmlCount = invoiceXmlPaths.Count,
            SilinmisFaturaXmlCount = deletedInvoiceXmlPaths.Count,
            OrtakEvrakCount = ortakEvrakYollari.Count(d => !d.IsDeleted && IsLegacyCommonPath(d.DosyaYolu)),
            SilinmisOrtakEvrakCount = ortakEvrakYollari.Count(d => d.IsDeleted && IsLegacyCommonPath(d.DosyaYolu)),
            TedarikciDosyaCount = legacyTedarikciDosyalari.Count(d => !d.IsDeleted),
            SilinmisTedarikciDosyaCount = legacyTedarikciDosyalari.Count(d => d.IsDeleted),
            DestekEkiCount = legacyDestekEkleri.Count(d => !d.IsDeleted),
            SilinmisDestekEkiCount = legacyDestekEkleri.Count(d => d.IsDeleted),
        };
    }

    /// <summary>
    /// Migration çalıştırır. Her dosyayı okur, şifreli storage'a yazar,
    /// DB path'ini günceller, eski plain dosyayı siler.
    /// Her dosyadan sonra SaveChanges yapılır — yarıda kesilse bile ilerleme kaybolmaz.
    /// </summary>
    public async IAsyncEnumerable<DosyaMigrasyonAdim> MigrateAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var firmaId = RequireSingleActiveFirm();
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);

        // 1) EbysEvrakDosya
        var ebysDosyalar = await ctx.EbysEvrakDosyalar.IgnoreQueryFilters()
            .Where(d => d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .ToListAsync(ct);

        foreach (var dosya in ebysDosyalar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu!, dosya.DosyaAdi,
                $"ebys/{dosya.EvrakId}", newPath => dosya.DosyaYolu = newPath, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 2) EbysEvrakDosyaVersiyon
        var versiyonlar = await ctx.EbysEvrakDosyaVersiyonlar.IgnoreQueryFilters()
            .Where(v => v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .ToListAsync(ct);

        foreach (var v in versiyonlar)
        {
            if (ct.IsCancellationRequested) yield break;
            var dosyaAdi = Path.GetFileName(v.DosyaYolu)!;
            var adim = await MigreDosyaAsync(v.DosyaYolu!, dosyaAdi,
                "ebys/versiyonlar", newPath => v.DosyaYolu = newPath, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 3) AracEvrakDosyalari
        var aracDosyalar = await FirmVehicleDocuments(ctx, firmaId)
            .Where(d => d.DosyaYolu != null && (d.DosyaYolu.StartsWith("/uploads/") || d.DosyaYolu.StartsWith("uploads/") || d.DosyaYolu.StartsWith("\\uploads\\")))
            .ToListAsync(ct);

        foreach (var dosya in aracDosyalar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu, dosya.DosyaAdi,
                $"araclar/{dosya.AracEvrakId}/evrak", newPath => dosya.DosyaYolu = newPath, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 4) AracEvrakDosyaVersiyonlar
        var aracVersiyonlar = await FirmVehicleVersions(ctx, firmaId)
            .Where(v => v.DosyaYolu != null && (v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")))
            .ToListAsync(ct);

        foreach (var v in aracVersiyonlar)
        {
            if (ct.IsCancellationRequested) yield break;
            var dosyaAdi = Path.GetFileName(v.DosyaYolu)!;
            var adim = await MigreDosyaAsync(v.DosyaYolu!, dosyaAdi,
                "araclar/versiyonlar", newPath => v.DosyaYolu = newPath, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 5) Seçili firmanın aktif/silinmiş personel özlük dosyaları ve sürümleri.
        var personelEvraklar = await FirmPersonnelDocuments(ctx, firmaId)
            .Where(e => e.DosyaYolu != null && ((e.DosyaYolu.StartsWith("/uploads/") || e.DosyaYolu.StartsWith("uploads/") || e.DosyaYolu.StartsWith("\\uploads\\")) ||
                (!e.DosyaYolu.Contains("/") && !e.DosyaYolu.Contains("\\") && !e.DosyaYolu.EndsWith(".enc")))).ToListAsync(ct);
        foreach (var evrak in personelEvraklar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(evrak.DosyaYolu!, evrak.DosyaAdi ?? Path.GetFileName(evrak.DosyaYolu!),
                $"personel/{evrak.SoforId}/ozluk", yeni => evrak.DosyaYolu = yeni, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        var personelVersiyonlar = await FirmPersonnelVersions(ctx, firmaId)
            .Where(v => v.DosyaYolu != null && ((v.DosyaYolu.StartsWith("/uploads/") || v.DosyaYolu.StartsWith("uploads/") || v.DosyaYolu.StartsWith("\\uploads\\")) ||
                (!v.DosyaYolu.Contains("/") && !v.DosyaYolu.Contains("\\") && !v.DosyaYolu.EndsWith(".enc")))).ToListAsync(ct);
        foreach (var versiyon in personelVersiyonlar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(versiyon.DosyaYolu!, versiyon.DosyaAdi ?? Path.GetFileName(versiyon.DosyaYolu!),
                $"personel/{versiyon.PersonelOzlukEvrak!.SoforId}/ozluk-versiyon", yeni => versiyon.DosyaYolu = yeni, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 6) Seçili firmaya ait faturaların eski PDF/XML yolları.
        var faturalar = await ctx.Faturalar.IgnoreQueryFilters()
            .Where(f => f.FirmaId == firmaId &&
                ((f.PdfDosyaYolu != null && (f.PdfDosyaYolu.StartsWith("/uploads/") || f.PdfDosyaYolu.StartsWith("uploads/") || f.PdfDosyaYolu.StartsWith("\\uploads\\") ||
                    f.PdfDosyaYolu.StartsWith("/efatura/") || f.PdfDosyaYolu.StartsWith("efatura/") || f.PdfDosyaYolu.StartsWith("\\efatura\\") ||
                    f.PdfDosyaYolu.StartsWith("/belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("belgeler/efatura/") || f.PdfDosyaYolu.StartsWith("\\belgeler\\efatura\\"))) ||
                 (f.XmlDosyaYolu != null && (f.XmlDosyaYolu.StartsWith("/uploads/") || f.XmlDosyaYolu.StartsWith("uploads/") || f.XmlDosyaYolu.StartsWith("\\uploads\\") ||
                    f.XmlDosyaYolu.StartsWith("/efatura/") || f.XmlDosyaYolu.StartsWith("efatura/") || f.XmlDosyaYolu.StartsWith("\\efatura\\") ||
                    f.XmlDosyaYolu.StartsWith("/belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("belgeler/efatura/") || f.XmlDosyaYolu.StartsWith("\\belgeler\\efatura\\")))))
            .ToListAsync(ct);
        foreach (var fatura in faturalar)
        {
            if (ct.IsCancellationRequested) yield break;
            if (IsLegacyWebPath(fatura.PdfDosyaYolu))
            {
                var adim = await MigreDosyaAsync(fatura.PdfDosyaYolu!, $"fatura-{fatura.Id}.pdf",
                    $"faturalar/{fatura.Id}/pdf", yeni => fatura.PdfDosyaYolu = yeni, ct,
                    storageUploadsRoot: IsLegacyWebPath(fatura.PdfDosyaYolu) && !IsLegacyInvoiceWebPath(fatura.PdfDosyaYolu),
                    invoiceWebRoot: IsLegacyInvoiceWebPath(fatura.PdfDosyaYolu));
                if (adim.Durum == MigrasyonDurum.Basarili)
                    await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
                yield return adim;
            }
            if (IsLegacyWebPath(fatura.XmlDosyaYolu))
            {
                var adim = await MigreDosyaAsync(fatura.XmlDosyaYolu!, $"fatura-{fatura.Id}.xml",
                    $"faturalar/{fatura.Id}/xml", yeni => fatura.XmlDosyaYolu = yeni, ct,
                    storageUploadsRoot: IsLegacyWebPath(fatura.XmlDosyaYolu) && !IsLegacyInvoiceWebPath(fatura.XmlDosyaYolu),
                    invoiceWebRoot: IsLegacyInvoiceWebPath(fatura.XmlDosyaYolu));
                if (adim.Durum == MigrasyonDurum.Basarili)
                    await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
                yield return adim;
            }
        }

        // 7) Geri alınabilir silinmiş kayıtlar dahil ortak evrakın eski düz dosyaları.
        var ortakEvraklar = await FirmCommonDocuments(ctx, firmaId)
            .ToListAsync(ct);

        foreach (var dosya in ortakEvraklar.Where(d => IsLegacyCommonPath(d.DosyaYolu)))
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu, dosya.DosyaAdi,
                "ortak-evrak", newPath => dosya.DosyaYolu = newPath, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 8) Cari firma bağı seçili tenant'ı kanıtlayan tedarikçi ekleri.
        var tedarikciDosyalari = await FirmSupplierDocuments(ctx, firmaId).ToListAsync(ct);
        foreach (var dosya in tedarikciDosyalari.Where(d => IsLegacyCommonPath(d.DosyaYolu)))
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu, dosya.DosyaAdi,
                $"tedarikci-evraklar/{dosya.TedarikciEvrakId}", yeni => dosya.DosyaYolu = yeni, ct);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }

        // 9) Destek eki yalnızca talebin Cari -> Firma bağı seçili firmayı doğruluyorsa taşınır.
        var destekEkleri = await FirmSupportAttachments(ctx, firmaId).ToListAsync(ct);
        foreach (var ek in destekEkleri.Where(d => IsLegacySupportPath(d.DosyaYolu)))
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(ek.DosyaYolu, ek.DosyaAdi,
                $"destek/{ek.DestekTalebiId ?? ek.YanitId ?? ek.Id}", yeni => ek.DosyaYolu = yeni, ct,
                supportUploadsRoot: true);
            if (adim.Durum == MigrasyonDurum.Basarili)
                await KaydiYazVeEskiDosyayiTemizleAsync(ctx, adim, ct);
            yield return adim;
        }
    }

    private async Task<DosyaMigrasyonAdim> MigreDosyaAsync(
        string eskiRelativePath,
        string dosyaAdi,
        string hedefKlasor,
        Action<string> pathGuncelle,
        CancellationToken ct,
        bool storageUploadsRoot = false,
        bool supportUploadsRoot = false,
        bool invoiceWebRoot = false)
    {
        var adim = new DosyaMigrasyonAdim { EskiYol = eskiRelativePath, DosyaAdi = dosyaAdi };
        try
        {
            // Eski path hem /uploads/... hem de wwwroot altında mutlak olabilir
            var fullPath = ResolveOldPath(eskiRelativePath, storageUploadsRoot, supportUploadsRoot, invoiceWebRoot);
            if (!File.Exists(fullPath))
            {
                adim.Durum = MigrasyonDurum.Atildi;
                adim.Mesaj = "Dosya disk üzerinde bulunamadı";
                return adim;
            }

            var icerik = await File.ReadAllBytesAsync(fullPath, ct);

            // Zaten şifreli mi? (KOA1 magic)
            if (icerik.Length >= 4 &&
                icerik[0] == 'K' && icerik[1] == 'O' && icerik[2] == 'A' && icerik[3] == '1')
            {
                adim.Durum = MigrasyonDurum.Atildi;
                adim.Mesaj = "Zaten şifreli (KOA1)";
                return adim;
            }

            var yeniYol = await _secureFileService.SaveEncryptedAsync(hedefKlasor, dosyaAdi, icerik, ct);
            try
            {
                var geriOkunan = await _secureFileService.ReadDecryptedAsync(yeniYol, ct);
                if (geriOkunan is null || !icerik.AsSpan().SequenceEqual(geriOkunan))
                    throw new InvalidOperationException("Yeni şifreli dosya içerik doğrulamasından geçmedi; eski dosya korundu.");
            }
            catch (Exception verifyException)
            {
                try
                {
                    await _secureFileService.DeleteAsync(yeniYol, CancellationToken.None);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("Şifreli kopya doğrulanamadı ve yeni dosya temizlenemedi.",
                        verifyException, cleanupException);
                }
                throw;
            }

            // Journal önce kalıcılaştırılır: DB commit sonrası süreç kesilirse worker
            // eski dosyayı yeni yolun DB durumuna göre güvenle yeniden değerlendirir.
            try
            {
                adim.CleanupKey = await _legacyCleanup.EnqueueMigrationAsync(
                    eskiRelativePath, yeniYol, ct, storageUploadsRoot, supportUploadsRoot, invoiceWebRoot);
            }
            catch (Exception enqueueException)
            {
                try
                {
                    await _secureFileService.DeleteAsync(yeniYol, CancellationToken.None);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("Geçiş temizleme isteği kaydedilemedi ve yeni kopya temizlenemedi.",
                        enqueueException, cleanupException);
                }
                throw;
            }
            pathGuncelle(yeniYol);

            adim.Durum = MigrasyonDurum.Basarili;
            adim.YeniYol = yeniYol;
            adim.Mesaj = $"{icerik.Length / 1024.0:0.#} KB şifrelendi ve geri okunarak doğrulandı";
            _logger.LogInformation("Migre edildi: {Eski} → {Yeni}", eskiRelativePath, yeniYol);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            adim.Durum = MigrasyonDurum.Hata;
            adim.Mesaj = ex.Message;
            _logger.LogError(ex, "Migration hatası: {Yol}", eskiRelativePath);
        }
        return adim;
    }

    private async Task KaydiYazVeEskiDosyayiTemizleAsync(
        ApplicationDbContext context,
        DosyaMigrasyonAdim adim,
        CancellationToken cancellationToken)
    {
        var yeniYol = adim.YeniYol
            ?? throw new InvalidOperationException("Başarılı dosya geçişinde yeni depolama yolu eksik.");
        var cleanupKey = adim.CleanupKey
            ?? throw new InvalidOperationException("Başarılı dosya geçişinde kalıcı temizleme isteği eksik.");

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception saveException)
        {
            try
            {
                await using var verify = await _contextFactory.CreateDbContextAsync(cancellationToken);
                if (!await DosyaYoluReferansliMiAsync(verify, yeniYol, cancellationToken))
                {
                    // SaveChanges did not publish this path. Retire the speculative
                    // copy first, then remove its old/new migration intent; otherwise
                    // the worker would retry forever waiting for a DB path that will
                    // never exist.
                    await _secureFileService.DeleteAsync(yeniYol, CancellationToken.None);
                    await _legacyCleanup.CompleteAsync(cleanupKey, CancellationToken.None);
                    throw new InvalidOperationException("Yeni dosya yolu DB'de doğrulanamadı; eski açık dosya korundu.");
                }
                _logger.LogWarning(saveException,
                    "Dosya yolu SaveChanges hatası sonrası DB'de doğrulandı; eski yol temizliğine devam ediliyor. Eski={Eski}, Yeni={Yeni}",
                    adim.EskiYol, yeniYol);
            }
            catch (Exception verifyException)
            {
                throw new AggregateException(
                    "Dosya yolu veritabanına kaydedilemedi veya doğrulanamadı; eski dosya güvenlik için korundu.",
                    saveException, verifyException);
            }
        }

        try
        {
            await _legacyCleanup.ProcessAsync(cleanupKey, CancellationToken.None);
            adim.Mesaj = "Şifreli kopya ve DB yolu doğrulandı; eski dosya geri alma için yerinde korundu.";
        }
        catch (Exception cleanupException)
        {
            adim.Durum = MigrasyonDurum.TemizlikBekliyor;
            adim.Mesaj = $"Yeni şifreli yol DB'ye kaydedildi; eski dosyanın saklama doğrulaması başarısız: {cleanupException.Message}";
            _logger.LogError(cleanupException,
                "Yeni şifreli dosya DB'de kayıtlı, ancak eski dosyanın saklama doğrulaması tamamlanamadı. Eski={Eski}, Yeni={Yeni}",
                adim.EskiYol, yeniYol);
        }
    }

    private static async Task<bool> DosyaYoluReferansliMiAsync(
        ApplicationDbContext context,
        string path,
        CancellationToken cancellationToken)
    {
        var paths = await DatabaseFilePathInventory.ReadAllAsync(context, cancellationToken);
        return paths.Any(candidate => string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase));
    }

    private string ResolveOldPath(string relativePath, bool storageUploadsRoot = false, bool supportUploadsRoot = false, bool invoiceWebRoot = false)
    {
        if (supportUploadsRoot)
        {
            var cleanupKey = _legacyCleanup.CreateKey(relativePath, supportUploadsRoot: true);
            var relative = cleanupKey["__legacy_cleanup_v1__/support-uploads/".Length..];
            return StorageFilePath.Resolve(SupportUploads, relative);
        }
        if (invoiceWebRoot)
        {
            var cleanupKey = _legacyCleanup.CreateKey(relativePath, invoiceWebRoot: true);
            var relative = cleanupKey["__legacy_cleanup_v1__/invoice-web/".Length..];
            return StorageFilePath.Resolve(_env.WebRootPath, relative);
        }
        if (IsLegacyCommonPath(relativePath))
            return _legacyCommonFileService.GetFullPath(relativePath);

        var normalized = relativePath.Trim().Replace('\\', '/');
        if (normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["/uploads/".Length..];
        else if (normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["uploads/".Length..];
        else
            throw new InvalidOperationException("Eski belge yolu uploads klasörü altında değil.");

        var root = storageUploadsRoot
            ? MKFiloServis.Web.Helpers.AppStoragePaths.GetUploadsRoot(_env.ContentRootPath)
            : WwwrootUploads;
        return StorageFilePath.Resolve(root, normalized);
    }

    private static bool IsLegacyWebPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = path.Trim().Replace('\\', '/');
        return normalized.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ||
               IsLegacyInvoiceWebPath(normalized);
    }

    private static bool IsLegacyInvoiceWebPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var normalized = path.Trim().Replace('\\', '/');
        return normalized.StartsWith("/efatura/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("efatura/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("/belgeler/efatura/", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("belgeler/efatura/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLegacyCommonPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        !Path.IsPathRooted(path) &&
        !path.Contains('/') && !path.Contains('\\') &&
        !path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase);

    private bool IsLegacySupportPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path)) return false;
        var root = Path.GetFullPath(SupportUploads).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private int RequireSingleActiveFirm()
    {
        if (_activeFirm.TumFirmalar || _activeFirm.AktifFirmaId is not > 0)
            throw new InvalidOperationException("Dosya geçişi için tek bir aktif firma seçilmelidir.");
        return _activeFirm.AktifFirmaId.Value;
    }

    private static IQueryable<EvrakDosya> FirmCommonDocuments(ApplicationDbContext ctx, int firmaId) =>
        ctx.EvrakDosyalari.IgnoreQueryFilters().Where(d =>
            (d.PersonelId.HasValue || d.AracId.HasValue) &&
            (!d.PersonelId.HasValue || ctx.Soforler.IgnoreQueryFilters()
                .Any(s => s.Id == d.PersonelId.Value && s.FirmaId == firmaId)) &&
            (!d.AracId.HasValue || ctx.Araclar.IgnoreQueryFilters()
                .Any(a => a.Id == d.AracId.Value && a.FirmaId == firmaId)));

    private static IQueryable<AracEvrakDosya> FirmVehicleDocuments(ApplicationDbContext ctx, int firmaId) =>
        ctx.AracEvrakDosyalari.IgnoreQueryFilters().Where(d =>
            (d.FirmaId == null || d.FirmaId == firmaId) &&
            ctx.AracEvraklari.IgnoreQueryFilters().Any(e =>
                e.Id == d.AracEvrakId && ctx.Araclar.IgnoreQueryFilters()
                    .Any(a => a.Id == e.AracId && a.FirmaId == firmaId)));

    private static IQueryable<AracEvrakDosyaVersiyon> FirmVehicleVersions(ApplicationDbContext ctx, int firmaId) =>
        ctx.AracEvrakDosyaVersiyonlar.IgnoreQueryFilters().Where(v =>
            FirmVehicleDocuments(ctx, firmaId).Any(d => d.Id == v.AracEvrakDosyaId));

    private static IQueryable<PersonelOzlukEvrak> FirmPersonnelDocuments(ApplicationDbContext ctx, int firmaId) =>
        ctx.PersonelOzlukEvraklar.IgnoreQueryFilters().Where(e =>
            ctx.Soforler.IgnoreQueryFilters().Any(s => s.Id == e.SoforId && s.FirmaId == firmaId));

    private static IQueryable<PersonelOzlukEvrakVersiyon> FirmPersonnelVersions(ApplicationDbContext ctx, int firmaId) =>
        ctx.PersonelOzlukEvrakVersiyonlar.IgnoreQueryFilters().Where(v =>
            FirmPersonnelDocuments(ctx, firmaId).Any(e => e.Id == v.PersonelOzlukEvrakId));

    private static IQueryable<TedarikciEvrakDosya> FirmSupplierDocuments(ApplicationDbContext ctx, int firmaId) =>
        ctx.TedarikciEvrakDosyalari.IgnoreQueryFilters().Where(d =>
            ctx.TedarikciEvraklari.IgnoreQueryFilters().Any(e => e.Id == d.TedarikciEvrakId &&
                ctx.TasimaTedarikciler.IgnoreQueryFilters().Any(t =>
                    t.Id == e.TasimaTedarikciId && t.CariId.HasValue &&
                    ctx.Cariler.IgnoreQueryFilters().Any(c => c.Id == t.CariId.Value && c.FirmaId == firmaId))));

    private static IQueryable<DestekTalebiEk> FirmSupportAttachments(ApplicationDbContext ctx, int firmaId) =>
        ctx.DestekTalebiEkleri.IgnoreQueryFilters().Where(a =>
            (a.DestekTalebiId.HasValue && ctx.DestekTalepleri.IgnoreQueryFilters().Any(t =>
                t.Id == a.DestekTalebiId.Value && t.CariId.HasValue &&
                ctx.Cariler.IgnoreQueryFilters().Any(c => c.Id == t.CariId.Value && c.FirmaId == firmaId))) ||
            (a.YanitId.HasValue && ctx.DestekTalebiYanitlari.IgnoreQueryFilters().Any(y =>
                y.Id == a.YanitId.Value && ctx.DestekTalepleri.IgnoreQueryFilters().Any(t =>
                    t.Id == y.DestekTalebiId && t.CariId.HasValue &&
                    ctx.Cariler.IgnoreQueryFilters().Any(c => c.Id == t.CariId.Value && c.FirmaId == firmaId)))));
}

public class DosyaMigrasyonOzet
{
    public int EbysAnaCount { get; set; }
    public int SilinmisEbysAnaCount { get; set; }
    public int EbysVersiyonCount { get; set; }
    public int SilinmisEbysVersiyonCount { get; set; }
    public int AracEvrakCount { get; set; }
    public int SilinmisAracEvrakCount { get; set; }
    public int AracVersiyonCount { get; set; }
    public int SilinmisAracVersiyonCount { get; set; }
    public int PersonelEvrakCount { get; set; }
    public int SilinmisPersonelEvrakCount { get; set; }
    public int PersonelVersiyonCount { get; set; }
    public int SilinmisPersonelVersiyonCount { get; set; }
    public int FaturaPdfCount { get; set; }
    public int SilinmisFaturaPdfCount { get; set; }
    public int FaturaXmlCount { get; set; }
    public int SilinmisFaturaXmlCount { get; set; }
    public int OrtakEvrakCount { get; set; }
    public int SilinmisOrtakEvrakCount { get; set; }
    public int TedarikciDosyaCount { get; set; }
    public int SilinmisTedarikciDosyaCount { get; set; }
    public int DestekEkiCount { get; set; }
    public int SilinmisDestekEkiCount { get; set; }
    public int Toplam => EbysAnaCount + SilinmisEbysAnaCount + EbysVersiyonCount + SilinmisEbysVersiyonCount
        + AracEvrakCount + SilinmisAracEvrakCount + AracVersiyonCount + SilinmisAracVersiyonCount
        + PersonelEvrakCount + SilinmisPersonelEvrakCount + PersonelVersiyonCount + SilinmisPersonelVersiyonCount
        + FaturaPdfCount + SilinmisFaturaPdfCount + FaturaXmlCount + SilinmisFaturaXmlCount
        + OrtakEvrakCount + SilinmisOrtakEvrakCount + TedarikciDosyaCount + SilinmisTedarikciDosyaCount
        + DestekEkiCount + SilinmisDestekEkiCount;
}

public class DosyaMigrasyonAdim
{
    public string DosyaAdi { get; set; } = "";
    public string EskiYol { get; set; } = "";
    public string? YeniYol { get; set; }
    public MigrasyonDurum Durum { get; set; }
    public string Mesaj { get; set; } = "";
    internal string? CleanupKey { get; set; }
}

public enum MigrasyonDurum { Basarili, Atildi, Hata, TemizlikBekliyor }


