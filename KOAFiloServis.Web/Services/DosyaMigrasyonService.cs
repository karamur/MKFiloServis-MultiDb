using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using KOAFiloServis.Web.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// wwwroot/uploads (plain) → şifreli storage migration servisi.
/// Hem EbysEvrakDosya hem de SoforEvrak / AracDosya gibi tüm plain dosyaları kapsar.
/// </summary>
public class DosyaMigrasyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IWebHostEnvironment _env;
    private readonly IFileProtector _fileProtector;
    private readonly ILogger<DosyaMigrasyonService> _logger;

    // wwwroot altındaki eski upload klasörü (yeni kurulumda boş olacak)
    private string WwwrootUploads => Path.Combine(_env.WebRootPath, "uploads");

    public DosyaMigrasyonService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISecureFileService secureFileService,
        IWebHostEnvironment env,
        IFileProtector fileProtector,
        ILogger<DosyaMigrasyonService> logger)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _env = env;
        _fileProtector = fileProtector;
        _logger = logger;
    }

    /// <summary>Taşınmayı bekleyen dosya sayısını döndürür (önizleme).</summary>
    public async Task<DosyaMigrasyonOzet> OnizlemeAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);

        // EBYS
        var ebysCount = await ctx.EbysEvrakDosyalar
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && d.DosyaYolu.StartsWith("/uploads/"))
            .CountAsync(ct);

        var versiyonCount = await ctx.EbysEvrakDosyaVersiyonlar
            .Where(v => v.DosyaYolu != null && v.DosyaYolu.StartsWith("/uploads/"))
            .CountAsync(ct);

        // Araç Evrak
        var aracEvrakCount = await ctx.AracEvrakDosyalari
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && d.DosyaYolu.StartsWith("/uploads/"))
            .CountAsync(ct);

        var aracVersiyonCount = await ctx.AracEvrakDosyaVersiyonlar
            .Where(v => v.DosyaYolu != null && v.DosyaYolu.StartsWith("/uploads/"))
            .CountAsync(ct);

        return new DosyaMigrasyonOzet
        {
            EbysAnaCount = ebysCount,
            EbysVersiyonCount = versiyonCount,
            AracEvrakCount = aracEvrakCount,
            AracVersiyonCount = aracVersiyonCount,
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
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);

        // 1) EbysEvrakDosya
        var ebysDosyalar = await ctx.EbysEvrakDosyalar
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && d.DosyaYolu.StartsWith("/uploads/"))
            .ToListAsync(ct);

        foreach (var dosya in ebysDosyalar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu!, dosya.DosyaAdi,
                $"ebys/{dosya.EvrakId}", newPath => dosya.DosyaYolu = newPath);
            yield return adim;
            if (adim.Durum == MigrasyonDurum.Basarili)
                await ctx.SaveChangesAsync(ct); // Her dosyadan sonra kaydet
        }

        // 2) EbysEvrakDosyaVersiyon
        var versiyonlar = await ctx.EbysEvrakDosyaVersiyonlar
            .Where(v => v.DosyaYolu != null && v.DosyaYolu.StartsWith("/uploads/"))
            .ToListAsync(ct);

        foreach (var v in versiyonlar)
        {
            if (ct.IsCancellationRequested) yield break;
            var dosyaAdi = Path.GetFileName(v.DosyaYolu)!;
            var adim = await MigreDosyaAsync(v.DosyaYolu!, dosyaAdi,
                "ebys/versiyonlar", newPath => v.DosyaYolu = newPath);
            yield return adim;
            if (adim.Durum == MigrasyonDurum.Basarili)
                await ctx.SaveChangesAsync(ct);
        }

        // 3) AracEvrakDosyalari
        var aracDosyalar = await ctx.AracEvrakDosyalari
            .Where(d => !d.IsDeleted && d.DosyaYolu != null && d.DosyaYolu.StartsWith("/uploads/"))
            .ToListAsync(ct);

        foreach (var dosya in aracDosyalar)
        {
            if (ct.IsCancellationRequested) yield break;
            var adim = await MigreDosyaAsync(dosya.DosyaYolu, dosya.DosyaAdi,
                $"araclar/{dosya.AracEvrakId}/evrak", newPath => dosya.DosyaYolu = newPath);
            yield return adim;
            if (adim.Durum == MigrasyonDurum.Basarili)
                await ctx.SaveChangesAsync(ct);
        }

        // 4) AracEvrakDosyaVersiyonlar
        var aracVersiyonlar = await ctx.AracEvrakDosyaVersiyonlar
            .Where(v => v.DosyaYolu != null && v.DosyaYolu.StartsWith("/uploads/"))
            .ToListAsync(ct);

        foreach (var v in aracVersiyonlar)
        {
            if (ct.IsCancellationRequested) yield break;
            var dosyaAdi = Path.GetFileName(v.DosyaYolu)!;
            var adim = await MigreDosyaAsync(v.DosyaYolu!, dosyaAdi,
                "araclar/versiyonlar", newPath => v.DosyaYolu = newPath);
            yield return adim;
            if (adim.Durum == MigrasyonDurum.Basarili)
                await ctx.SaveChangesAsync(ct);
        }
    }

    private async Task<DosyaMigrasyonAdim> MigreDosyaAsync(
        string eskiRelativePath,
        string dosyaAdi,
        string hedefKlasor,
        Action<string> pathGuncelle)
    {
        var adim = new DosyaMigrasyonAdim { EskiYol = eskiRelativePath, DosyaAdi = dosyaAdi };
        try
        {
            // Eski path hem /uploads/... hem de wwwroot altında mutlak olabilir
            var fullPath = ResolveOldPath(eskiRelativePath);
            if (!File.Exists(fullPath))
            {
                adim.Durum = MigrasyonDurum.Atildi;
                adim.Mesaj = "Dosya disk üzerinde bulunamadı";
                return adim;
            }

            var icerik = await File.ReadAllBytesAsync(fullPath);

            // Zaten şifreli mi? (KOA1 magic)
            if (icerik.Length >= 4 &&
                icerik[0] == 'K' && icerik[1] == 'O' && icerik[2] == 'A' && icerik[3] == '1')
            {
                adim.Durum = MigrasyonDurum.Atildi;
                adim.Mesaj = "Zaten şifreli (KOA1)";
                return adim;
            }

            var yeniYol = await _secureFileService.SaveEncryptedAsync(hedefKlasor, dosyaAdi, icerik);
            pathGuncelle(yeniYol);

            // Eski plain dosyayı sil
            File.Delete(fullPath);

            adim.Durum = MigrasyonDurum.Basarili;
            adim.YeniYol = yeniYol;
            adim.Mesaj = $"{icerik.Length / 1024.0:0.#} KB şifrelendi";
            _logger.LogInformation("Migre edildi: {Eski} → {Yeni}", eskiRelativePath, yeniYol);
        }
        catch (Exception ex)
        {
            adim.Durum = MigrasyonDurum.Hata;
            adim.Mesaj = ex.Message;
            _logger.LogError(ex, "Migration hatası: {Yol}", eskiRelativePath);
        }
        return adim;
    }

    private string ResolveOldPath(string relativePath)
    {
        // /uploads/ebys/... → wwwroot/uploads/ebys/...
        var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_env.WebRootPath, normalized);
    }
}

public class DosyaMigrasyonOzet
{
    public int EbysAnaCount { get; set; }
    public int EbysVersiyonCount { get; set; }
    public int AracEvrakCount { get; set; }
    public int AracVersiyonCount { get; set; }
    public int Toplam => EbysAnaCount + EbysVersiyonCount + AracEvrakCount + AracVersiyonCount;
}

public class DosyaMigrasyonAdim
{
    public string DosyaAdi { get; set; } = "";
    public string EskiYol { get; set; } = "";
    public string? YeniYol { get; set; }
    public MigrasyonDurum Durum { get; set; }
    public string Mesaj { get; set; } = "";
}

public enum MigrasyonDurum { Basarili, Atildi, Hata }
