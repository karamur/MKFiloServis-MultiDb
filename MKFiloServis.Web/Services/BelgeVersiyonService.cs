using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
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

    public BelgeVersiyonService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment environment)
    {
        _contextFactory = contextFactory;
        _environment = environment;
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

        // Mevcut dosyayı versiyon tablosuna arşivle
        var versiyon = new EbysEvrakDosyaVersiyon
        {
            EvrakDosyaId = evrakDosyaId,
            VersiyonNo = dosya.VersiyonNo,
            DosyaAdi = dosya.DosyaAdi,
            DosyaYolu = dosya.DosyaYolu,
            DosyaTipi = dosya.DosyaTipi,
            DosyaBoyutu = dosya.DosyaBoyutu,
            Aciklama = dosya.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? dosya.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.Now
        };

        context.EbysEvrakDosyaVersiyonlar.Add(versiyon);

        // Ana dosyanın versiyon numarasını artır
        dosya.VersiyonNo++;
        dosya.SonDegisiklikNotu = degisiklikNotu;
        dosya.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<byte[]?> GetEbysEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetEbysEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        var fizikselYol = Path.Combine(_environment.WebRootPath, versiyon.DosyaYolu.TrimStart('/'));
        if (File.Exists(fizikselYol))
        {
            return await File.ReadAllBytesAsync(fizikselYol);
        }
        return null;
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

        var versiyon = new AracEvrakDosyaVersiyon
        {
            AracEvrakDosyaId = aracEvrakDosyaId,
            VersiyonNo = dosya.VersiyonNo,
            DosyaAdi = dosya.DosyaAdi,
            DosyaYolu = dosya.DosyaYolu,
            DosyaTipi = dosya.DosyaTipi,
            DosyaBoyutu = dosya.DosyaBoyutu,
            Aciklama = dosya.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? dosya.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.Now
        };

        context.AracEvrakDosyaVersiyonlar.Add(versiyon);

        dosya.VersiyonNo++;
        dosya.SonDegisiklikNotu = degisiklikNotu;
        dosya.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<byte[]?> GetAracEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetAracEvrakDosyaVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        var fizikselYol = Path.Combine(_environment.WebRootPath, versiyon.DosyaYolu.TrimStart('/'));
        if (File.Exists(fizikselYol))
        {
            return await File.ReadAllBytesAsync(fizikselYol);
        }
        return null;
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

        var versiyon = new PersonelOzlukEvrakVersiyon
        {
            PersonelOzlukEvrakId = personelOzlukEvrakId,
            VersiyonNo = evrak.VersiyonNo,
            DosyaYolu = evrak.DosyaYolu,
            DosyaAdi = evrak.DosyaAdi,
            DosyaTipi = evrak.DosyaTipi,
            DosyaBoyutu = evrak.DosyaBoyutu,
            Aciklama = evrak.Aciklama,
            DegisiklikNotu = degisiklikNotu ?? evrak.SonDegisiklikNotu,
            OlusturanKullaniciId = kullaniciId,
            OlusturmaTarihi = DateTime.Now
        };

        context.PersonelOzlukEvrakVersiyonlar.Add(versiyon);

        evrak.VersiyonNo++;
        evrak.SonDegisiklikNotu = degisiklikNotu;
        evrak.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task<byte[]?> GetPersonelOzlukEvrakVersiyonIcerikAsync(int versiyonId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var versiyon = await GetPersonelOzlukEvrakVersiyonAsync(versiyonId);
        if (versiyon == null || string.IsNullOrEmpty(versiyon.DosyaYolu))
            return null;

        var fizikselYol = Path.Combine(_environment.WebRootPath, versiyon.DosyaYolu.TrimStart('/'));
        if (File.Exists(fizikselYol))
        {
            return await File.ReadAllBytesAsync(fizikselYol);
        }
        return null;
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

    #endregion
}



