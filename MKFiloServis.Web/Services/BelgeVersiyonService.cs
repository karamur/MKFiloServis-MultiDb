using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// EBYS Belge Versiyon Yönetim Servisi Implementasyonu
/// </summary>
public class BelgeVersiyonService : IBelgeVersiyonService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ISecureFileService _secureFileService;

    public BelgeVersiyonService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IWebHostEnvironment environment,
        ISecureFileService secureFileService)
    {
        _contextFactory = contextFactory;
        _environment = environment;
        _secureFileService = secureFileService;
    }

    #region EBYS Evrak Dosya Versiyonları

    public async Task<List<EbysEvrakDosyaVersiyon>> GetEbysEvrakDosyaVersiyonlariAsync(int evrakDosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EbysEvrakDosyaVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .Where(v => v.EvrakDosyaId == evrakDosyaId && !v.IsDeleted)
            .OrderByDescending(v => v.VersiyonNo)
            .ToListAsync();
    }

    public async Task<EbysEvrakDosyaVersiyon?> GetEbysEvrakDosyaVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EbysEvrakDosyaVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);
    }

    public async Task ArsivleEbysEvrakDosyaAsync(int evrakDosyaId, string? degisiklikNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.EbysEvrakDosyalar
            .FirstOrDefaultAsync(d => d.Id == evrakDosyaId && !d.IsDeleted);

        if (dosya == null)
            throw new ArgumentException("Evrak dosyası bulunamadı.", nameof(evrakDosyaId));

        if (string.IsNullOrWhiteSpace(dosya.DosyaYolu))
            throw new InvalidOperationException("EBYS dosya yolu boş; sürüm arşivlenemedi.");

        // Sürüm geçmişi ana dosyanın yaşam döngüsünden bağımsız olmalı.
        var archivedPath = await _secureFileService.CopyEncryptedAsync(
            dosya.DosyaYolu,
            $"ebys/versions/{evrakDosyaId}",
            $"v{dosya.VersiyonNo}_{dosya.DosyaAdi}");

        var versiyon = new EbysEvrakDosyaVersiyon
        {
            EvrakDosyaId = evrakDosyaId,
            VersiyonNo = dosya.VersiyonNo,
            DosyaAdi = dosya.DosyaAdi,
            DosyaYolu = archivedPath,
            DosyaTipi = dosya.DosyaTipi,
            DosyaBoyutu = dosya.DosyaBoyutu,
            Aciklama = dosya.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? dosya.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.UtcNow
        };

        context.EbysEvrakDosyaVersiyonlar.Add(versiyon);

        // Ana dosyanın versiyon numarasını artır
        dosya.VersiyonNo++;
        dosya.SonDegisiklikNotu = degisiklikNotu;
        dosya.UpdatedAt = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception saveException)
        {
            try
            {
                await using var verifyContext = await _contextFactory.CreateDbContextAsync();
                var isReferenced = await verifyContext.EbysEvrakDosyaVersiyonlar.AsNoTracking()
                    .AnyAsync(x => x.DosyaYolu == archivedPath);
                if (!isReferenced)
                    await _secureFileService.DeleteAsync(archivedPath);
            }
            catch (Exception compensationException)
            {
                throw new AggregateException(
                    "EBYS sürüm kaydı başarısız veya belirsiz; arşiv dosyasına DB başvurusu doğrulanamadı ve dosya güvenlik için korunuyor.",
                    saveException, compensationException);
            }
            throw;
        }
    }

    public async Task<byte[]?> GetEbysEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetEbysEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        var storedContent = await _secureFileService.ReadDecryptedAsync(versiyon.DosyaYolu);
        if (storedContent != null) return storedContent;

        // Eski sürümlerin webroot içindeki açık dosya yolunu geriye dönük oku.
        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        var fizikselYol = StorageFilePath.Resolve(webRoot, versiyon.DosyaYolu.TrimStart('/', '\\'));
        if (!StorageFilePath.IsWithinRoot(webRoot, fizikselYol, allowRoot: false))
            throw new InvalidOperationException("Eski EBYS sürüm yolu webroot dışına çıkıyor.");
        return File.Exists(fizikselYol) ? await File.ReadAllBytesAsync(fizikselYol) : null;
    }

    public async Task SilEbysEvrakVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await context.EbysEvrakDosyaVersiyonlar
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);

        if (versiyon != null)
        {
            versiyon.IsDeleted = true;
            versiyon.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            // Geri yükleme ile fiziksel silme ayrı işlemler olduğundan, dosya
            // temizliği kalıcı/serileştirilmiş kuyruk kurulana kadar ertelenir.
        }
    }

    #endregion

    #region Araç Evrak Dosya Versiyonları

    public async Task<List<AracEvrakDosyaVersiyon>> GetAracEvrakDosyaVersiyonlariAsync(int aracEvrakDosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracEvrakDosyaVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .Where(v => v.AracEvrakDosyaId == aracEvrakDosyaId && !v.IsDeleted)
            .OrderByDescending(v => v.VersiyonNo)
            .ToListAsync();
    }

    public async Task<AracEvrakDosyaVersiyon?> GetAracEvrakDosyaVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracEvrakDosyaVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);
    }

    public async Task ArsivleAracEvrakDosyaAsync(int aracEvrakDosyaId, string? degisiklikNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var dosya = await context.AracEvrakDosyalari
            .FirstOrDefaultAsync(d => d.Id == aracEvrakDosyaId && !d.IsDeleted);

        if (dosya == null)
            throw new ArgumentException("Araç evrak dosyası bulunamadı.", nameof(aracEvrakDosyaId));

        if (string.IsNullOrWhiteSpace(dosya.DosyaYolu))
            throw new InvalidOperationException("Araç evrak dosya yolu boş; sürüm arşivlenemedi.");
        var archivedPath = await CopyVersionFileAsync(
            dosya.DosyaYolu, $"arac-evrak/versions/{aracEvrakDosyaId}", $"v{dosya.VersiyonNo}_{dosya.DosyaAdi}");

        var versiyon = new AracEvrakDosyaVersiyon
        {
            AracEvrakDosyaId = aracEvrakDosyaId,
            VersiyonNo = dosya.VersiyonNo,
            DosyaAdi = dosya.DosyaAdi,
            DosyaYolu = archivedPath,
            DosyaTipi = dosya.DosyaTipi,
            DosyaBoyutu = dosya.DosyaBoyutu,
            Aciklama = dosya.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? dosya.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.UtcNow
        };

        context.AracEvrakDosyaVersiyonlar.Add(versiyon);

        dosya.VersiyonNo++;
        dosya.SonDegisiklikNotu = degisiklikNotu;
        dosya.UpdatedAt = DateTime.UtcNow;

        try { await context.SaveChangesAsync(); }
        catch (Exception saveException)
        {
            await CompensateVersionCopyAsync(archivedPath,
                verify => verify.AracEvrakDosyaVersiyonlar.AsNoTracking().AnyAsync(x => x.DosyaYolu == archivedPath),
                saveException);
            throw;
        }
    }

    public async Task<byte[]?> GetAracEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetAracEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        return await ReadVersionFileAsync(versiyon.DosyaYolu);
    }

    public async Task SilAracEvrakVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await context.AracEvrakDosyaVersiyonlar
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);

        if (versiyon != null)
        {
            versiyon.IsDeleted = true;
            versiyon.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            // Geri yükleme ile fiziksel silme ayrı işlemler olduğundan, dosya
            // temizliği kalıcı/serileştirilmiş kuyruk kurulana kadar ertelenir.
        }
    }

    #endregion

    #region Personel Özlük Evrak Versiyonları

    public async Task<List<PersonelOzlukEvrakVersiyon>> GetPersonelOzlukEvrakVersiyonlariAsync(int personelOzlukEvrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelOzlukEvrakVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .Where(v => v.PersonelOzlukEvrakId == personelOzlukEvrakId && !v.IsDeleted)
            .OrderByDescending(v => v.VersiyonNo)
            .ToListAsync();
    }

    public async Task<PersonelOzlukEvrakVersiyon?> GetPersonelOzlukEvrakVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelOzlukEvrakVersiyonlar
            .Include(v => v.OlusturanKullanici)
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);
    }

    public async Task ArsivlePersonelOzlukEvrakAsync(int personelOzlukEvrakId, string? degisiklikNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrak = await context.PersonelOzlukEvraklar
            .FirstOrDefaultAsync(e => e.Id == personelOzlukEvrakId && !e.IsDeleted);

        if (evrak == null)
            throw new ArgumentException("Personel özlük evrak bulunamadı.", nameof(personelOzlukEvrakId));

        if (string.IsNullOrWhiteSpace(evrak.DosyaYolu))
            throw new InvalidOperationException("Personel özlük evrak dosya yolu boş; sürüm arşivlenemedi.");
        var archivedPath = await CopyVersionFileAsync(
            evrak.DosyaYolu, $"personel-ozluk/versions/{personelOzlukEvrakId}", $"v{evrak.VersiyonNo}_{evrak.DosyaAdi}");

        var versiyon = new PersonelOzlukEvrakVersiyon
        {
            PersonelOzlukEvrakId = personelOzlukEvrakId,
            VersiyonNo = evrak.VersiyonNo,
            DosyaYolu = archivedPath,
            DosyaAdi = evrak.DosyaAdi,
            DosyaTipi = evrak.DosyaTipi,
            DosyaBoyutu = evrak.DosyaBoyutu,
            Aciklama = evrak.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? evrak.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.UtcNow
        };

        context.PersonelOzlukEvrakVersiyonlar.Add(versiyon);

        evrak.VersiyonNo++;
        evrak.SonDegisiklikNotu = degisiklikNotu;
        evrak.UpdatedAt = DateTime.UtcNow;

        try { await context.SaveChangesAsync(); }
        catch (Exception saveException)
        {
            await CompensateVersionCopyAsync(archivedPath,
                verify => verify.PersonelOzlukEvrakVersiyonlar.AsNoTracking().AnyAsync(x => x.DosyaYolu == archivedPath),
                saveException);
            throw;
        }
    }

    public async Task<byte[]?> GetPersonelOzlukEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetPersonelOzlukEvrakVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        return await ReadVersionFileAsync(versiyon.DosyaYolu);
    }

    public async Task SilPersonelOzlukEvrakVersiyonAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await context.PersonelOzlukEvrakVersiyonlar
            .FirstOrDefaultAsync(v => v.Id == versiyonId && !v.IsDeleted);

        if (versiyon != null)
        {
            versiyon.IsDeleted = true;
            versiyon.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            // Geri yükleme ile fiziksel silme ayrı işlemler olduğundan, dosya
            // temizliği kalıcı/serileştirilmiş kuyruk kurulana kadar ertelenir.
        }
    }

    #endregion

    #region Versiyon Karşılaştırma

    public async Task<BelgeVersiyonKarsilastirma?> KarsilastirEbysVersiyonlarAsync(int versiyon1Id, int versiyon2Id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var v1 = await GetEbysEvrakDosyaVersiyonAsync(versiyon1Id);
        var v2 = await GetEbysEvrakDosyaVersiyonAsync(versiyon2Id);

        if (v1 == null || v2 == null)
            return null;

        return new BelgeVersiyonKarsilastirma
        {
            EskiVersiyonNo = Math.Min(v1.VersiyonNo, v2.VersiyonNo),
            YeniVersiyonNo = Math.Max(v1.VersiyonNo, v2.VersiyonNo),
            EskiDosyaAdi = v1.VersiyonNo < v2.VersiyonNo ? v1.DosyaAdi : v2.DosyaAdi,
            YeniDosyaAdi = v1.VersiyonNo < v2.VersiyonNo ? v2.DosyaAdi : v1.DosyaAdi,
            EskiTarih = v1.VersiyonNo < v2.VersiyonNo ? v1.OlusturmaTarihi : v2.OlusturmaTarihi,
            YeniTarih = v1.VersiyonNo < v2.VersiyonNo ? v2.OlusturmaTarihi : v1.OlusturmaTarihi,
            BoyutFarki = Math.Abs(v2.DosyaBoyutu - v1.DosyaBoyutu),
            DegisiklikNotu = v1.VersiyonNo < v2.VersiyonNo ? v2.DegisiklikNotu : v1.DegisiklikNotu
        };
    }

    public async Task<BelgeVersiyonKarsilastirma?> KarsilastirAracVersiyonlarAsync(int versiyon1Id, int versiyon2Id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var v1 = await GetAracEvrakDosyaVersiyonAsync(versiyon1Id);
        var v2 = await GetAracEvrakDosyaVersiyonAsync(versiyon2Id);

        if (v1 == null || v2 == null)
            return null;

        return new BelgeVersiyonKarsilastirma
        {
            EskiVersiyonNo = Math.Min(v1.VersiyonNo, v2.VersiyonNo),
            YeniVersiyonNo = Math.Max(v1.VersiyonNo, v2.VersiyonNo),
            EskiDosyaAdi = v1.VersiyonNo < v2.VersiyonNo ? v1.DosyaAdi : v2.DosyaAdi,
            YeniDosyaAdi = v1.VersiyonNo < v2.VersiyonNo ? v2.DosyaAdi : v1.DosyaAdi,
            EskiTarih = v1.VersiyonNo < v2.VersiyonNo ? v1.OlusturmaTarihi : v2.OlusturmaTarihi,
            YeniTarih = v1.VersiyonNo < v2.VersiyonNo ? v2.OlusturmaTarihi : v1.OlusturmaTarihi,
            BoyutFarki = Math.Abs(v2.DosyaBoyutu - v1.DosyaBoyutu),
            DegisiklikNotu = v1.VersiyonNo < v2.VersiyonNo ? v2.DegisiklikNotu : v1.DegisiklikNotu
        };
    }

    #endregion

    #region Geri Yükleme

    public async Task GeriYukleEbysVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetEbysEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null)
            throw new ArgumentException("Versiyon bulunamadı.", nameof(versiyonId));

        var dosya = await context.EbysEvrakDosyalar
            .FirstOrDefaultAsync(d => d.Id == versiyon.EvrakDosyaId && !d.IsDeleted);

        if (dosya == null)
            throw new InvalidOperationException("Ana dosya bulunamadı.");

        // Mevcut versiyonu arşivle
        await ArsivleEbysEvrakDosyaAsync(dosya.Id, $"Geri yükleme öncesi arşiv (v{versiyon.VersiyonNo}'a geri yüklendi)", kullaniciId);

        // Eski versiyonu geri yükle
        dosya.DosyaAdi = versiyon.DosyaAdi;
        dosya.DosyaYolu = versiyon.DosyaYolu;
        dosya.DosyaTipi = versiyon.DosyaTipi;
        dosya.DosyaBoyutu = versiyon.DosyaBoyutu;
        dosya.Aciklama = versiyon.Aciklama;
        dosya.SonDegisiklikNotu = geriYuklemeNotu ?? $"v{versiyon.VersiyonNo}'dan geri yüklendi";
        dosya.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task GeriYukleAracVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetAracEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null)
            throw new ArgumentException("Versiyon bulunamadı.", nameof(versiyonId));

        var dosya = await context.AracEvrakDosyalari
            .FirstOrDefaultAsync(d => d.Id == versiyon.AracEvrakDosyaId && !d.IsDeleted);

        if (dosya == null)
            throw new InvalidOperationException("Ana dosya bulunamadı.");

        // Mevcut versiyonu arşivle
        await ArsivleAracEvrakDosyaAsync(dosya.Id, $"Geri yükleme öncesi arşiv (v{versiyon.VersiyonNo}'a geri yüklendi)", kullaniciId);

        // Eski versiyonu geri yükle
        dosya.DosyaAdi = versiyon.DosyaAdi;
        dosya.DosyaYolu = versiyon.DosyaYolu;
        dosya.DosyaTipi = versiyon.DosyaTipi;
        dosya.DosyaBoyutu = versiyon.DosyaBoyutu;
        dosya.Aciklama = versiyon.Aciklama;
        dosya.SonDegisiklikNotu = geriYuklemeNotu ?? $"v{versiyon.VersiyonNo}'dan geri yüklendi";
        dosya.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task GeriYuklePersonelOzlukVersiyonAsync(int versiyonId, string? geriYuklemeNotu = null, int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetPersonelOzlukEvrakVersiyonAsync(versiyonId);
        if (versiyon == null)
            throw new ArgumentException("Versiyon bulunamadı.", nameof(versiyonId));

        var evrak = await context.PersonelOzlukEvraklar
            .FirstOrDefaultAsync(e => e.Id == versiyon.PersonelOzlukEvrakId && !e.IsDeleted);

        if (evrak == null)
            throw new InvalidOperationException("Ana evrak bulunamadı.");

        // Mevcut versiyonu arşivle
        await ArsivlePersonelOzlukEvrakAsync(evrak.Id, $"Geri yükleme öncesi arşiv (v{versiyon.VersiyonNo}'a geri yüklendi)", kullaniciId);

        // Eski versiyonu geri yükle
        evrak.DosyaYolu = versiyon.DosyaYolu;
        evrak.DosyaAdi = versiyon.DosyaAdi;
        evrak.DosyaTipi = versiyon.DosyaTipi;
        evrak.DosyaBoyutu = versiyon.DosyaBoyutu;
        evrak.Aciklama = versiyon.Aciklama;
        evrak.SonDegisiklikNotu = geriYuklemeNotu ?? $"v{versiyon.VersiyonNo}'dan geri yüklendi";
        evrak.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private async Task<string> CopyVersionFileAsync(string sourcePath, string targetDirectory, string fileName)
    {
        if (await _secureFileService.ExistsAsync(sourcePath))
            return await _secureFileService.CopyEncryptedAsync(sourcePath, targetDirectory, fileName);

        var legacyPath = ResolveLegacyWebRootPath(sourcePath);
        if (!File.Exists(legacyPath))
            throw new FileNotFoundException("Arşivlenecek belge dosyası bulunamadı.", sourcePath);

        var legacyBytes = await File.ReadAllBytesAsync(legacyPath);
        return await _secureFileService.SaveEncryptedAsync(targetDirectory, fileName, legacyBytes);
    }

    private async Task<byte[]?> ReadVersionFileAsync(string storedPath)
    {
        var protectedContent = await _secureFileService.ReadDecryptedAsync(storedPath);
        if (protectedContent != null) return protectedContent;

        var legacyPath = ResolveLegacyWebRootPath(storedPath);
        return File.Exists(legacyPath) ? await File.ReadAllBytesAsync(legacyPath) : null;
    }

    private string ResolveLegacyWebRootPath(string relativePath)
    {
        return StorageFilePath.Resolve(_environment.WebRootPath, relativePath.TrimStart('/', '\\'));
    }

    private async Task CompensateVersionCopyAsync(
        string copiedPath,
        Func<ApplicationDbContext, Task<bool>> isReferenced,
        Exception saveException)
    {
        try
        {
            await using var verifyContext = await _contextFactory.CreateDbContextAsync();
            if (!await isReferenced(verifyContext))
                await _secureFileService.DeleteAsync(copiedPath);
        }
        catch (Exception compensationException)
        {
            throw new AggregateException(
                "Sürüm kaydı başarısız veya belirsiz; arşiv dosyasına DB başvurusu doğrulanamadı ve dosya güvenlik için korunuyor.",
                saveException, compensationException);
        }
    }

    #endregion
}



