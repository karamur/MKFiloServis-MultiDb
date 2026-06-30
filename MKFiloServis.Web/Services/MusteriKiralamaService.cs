using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

// M��teri kiralama i�lemleri i�in servis interface'i
// CRUD operasyonlar� + �zel i� mant��� metodlar�
public interface IMusteriKiralamaService
{
    // T�m kiralamalar� getir
    Task<List<MusteriKiralama>> GetAllAsync();

    // ID'ye g�re kiralama getir
    Task<MusteriKiralama?> GetByIdAsync(int id);

    // Aktif kiralamalar� getir
    Task<List<MusteriKiralama>> GetAktifKiralamalarAsync();

    // M��teriye g�re kiralamalar� getir
    Task<List<MusteriKiralama>> GetByMusteriIdAsync(int musteriId);

    // Araca g�re kiralamalar� getir
    Task<List<MusteriKiralama>> GetByAracIdAsync(int aracId);

    // Yeni kiralama olu�tur (tarih �ak��mas� kontrol� ile)
    Task<MusteriKiralama> CreateAsync(MusteriKiralama kiralama);

    // Kiralama g�ncelle
    Task<MusteriKiralama> UpdateAsync(MusteriKiralama kiralama);

    // Kiralama iptal et
    Task<bool> IptalEtAsync(int id, string? iptalNedeni = null);

    // Ara� teslim al (kiralama ba�lat)
    Task<MusteriKiralama> TeslimAlAsync(int kiralamaId, int baslangicKm, int personelId);

    // Ara� teslim et (kiralama bitir)
    Task<MusteriKiralama> TeslimEtAsync(int kiralamaId, int bitisKm, int personelId);

    // Belirli tarih aral���nda ara� m�sait mi kontrol et
    Task<bool> AracMusaitMiAsync(int aracId, DateTime baslangic, DateTime bitis, int? haricKiralamaId = null);

    // Toplam tutar� hesapla
    decimal ToplamTutarHesapla(DateTime baslangic, DateTime bitis, decimal gunlukFiyat);
}

// M��teri kiralama servisi implementasyonu
public class MusteriKiralamaService : IMusteriKiralamaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<MusteriKiralamaService> _logger;

    public MusteriKiralamaService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<MusteriKiralamaService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // T�m kiralamalar� getir, silinmemi� olanlar, tarihe g�re s�ral�
    public async Task<List<MusteriKiralama>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MusteriKiralamalar
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.BaslangicTarihi)
            .ToListAsync();
    }

    // ID'ye g�re kiralama getir
    public async Task<MusteriKiralama?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MusteriKiralamalar
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    // Sadece aktif durumda olan kiralamalar� getir
    public async Task<List<MusteriKiralama>> GetAktifKiralamalarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MusteriKiralamalar
            .Where(x => !x.IsDeleted && x.Durum == KiralamaDurumu.Aktif)
            .OrderBy(x => x.PlanlananBitisTarihi)
            .ToListAsync();
    }

    // M��teriye ait t�m kiralamalar� getir
    public async Task<List<MusteriKiralama>> GetByMusteriIdAsync(int musteriId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MusteriKiralamalar
            .Where(x => !x.IsDeleted && x.MusteriId == musteriId)
            .OrderByDescending(x => x.BaslangicTarihi)
            .ToListAsync();
    }

    // Araca ait t�m kiralamalar� getir
    public async Task<List<MusteriKiralama>> GetByAracIdAsync(int aracId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.MusteriKiralamalar
            .Where(x => !x.IsDeleted && x.AracId == aracId)
            .OrderByDescending(x => x.BaslangicTarihi)
            .ToListAsync();
    }

    // Yeni kiralama olu�tur, �nce ara� m�saitli�ini kontrol et
    public async Task<MusteriKiralama> CreateAsync(MusteriKiralama kiralama)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // Ara� m�sait mi kontrol et
        var musaitMi = await AracMusaitMiAsync(kiralama.AracId, kiralama.BaslangicTarihi, kiralama.PlanlananBitisTarihi);
        if (!musaitMi)
        {
            throw new InvalidOperationException("Ara� se�ilen tarihler aras�nda m�sait de�il!");
        }

        // Toplam tutar� hesapla
        kiralama.ToplamTutar = ToplamTutarHesapla(kiralama.BaslangicTarihi, kiralama.PlanlananBitisTarihi, kiralama.GunlukFiyat);

        // S�zle�me numaras� olu�tur
        kiralama.SozlesmeNo = $"KR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";

        kiralama.CreatedAt = DateTime.Now;
        context.MusteriKiralamalar.Add(kiralama);
        await context.SaveChangesAsync();

        _logger.LogInformation("Yeni kiralama olu�turuldu: {SozlesmeNo}", kiralama.SozlesmeNo);
        return kiralama;
    }

    // Kiralama g�ncelle
    public async Task<MusteriKiralama> UpdateAsync(MusteriKiralama kiralama)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await GetByIdAsync(kiralama.Id);
        if (existing == null)
        {
            throw new InvalidOperationException("Kiralama bulunamad�!");
        }

        // Tarih de�i�tiyse m�saitlik kontrol� yap
        if (existing.BaslangicTarihi != kiralama.BaslangicTarihi || 
            existing.PlanlananBitisTarihi != kiralama.PlanlananBitisTarihi ||
            existing.AracId != kiralama.AracId)
        {
            var musaitMi = await AracMusaitMiAsync(kiralama.AracId, kiralama.BaslangicTarihi, kiralama.PlanlananBitisTarihi, kiralama.Id);
            if (!musaitMi)
            {
                throw new InvalidOperationException("Ara� se�ilen tarihler aras�nda m�sait de�il!");
            }
        }

        // Toplam tutar� yeniden hesapla
        kiralama.ToplamTutar = ToplamTutarHesapla(kiralama.BaslangicTarihi, kiralama.PlanlananBitisTarihi, kiralama.GunlukFiyat);

        kiralama.UpdatedAt = DateTime.Now;
        context.MusteriKiralamalar.Update(kiralama);
        await context.SaveChangesAsync();

        return kiralama;
    }

    // Kiralama iptal et
    public async Task<bool> IptalEtAsync(int id, string? iptalNedeni = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kiralama = await GetByIdAsync(id);
        if (kiralama == null) return false;

        if (kiralama.Durum == KiralamaDurumu.Tamamlandi)
        {
            throw new InvalidOperationException("Tamamlanm�� kiralama iptal edilemez!");
        }

        kiralama.Durum = KiralamaDurumu.IptalEdildi;
        kiralama.Notlar = string.IsNullOrEmpty(kiralama.Notlar) 
            ? $"�ptal nedeni: {iptalNedeni}" 
            : $"{kiralama.Notlar}\n�ptal nedeni: {iptalNedeni}";
        kiralama.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        _logger.LogInformation("Kiralama iptal edildi: {Id}", id);
        return true;
    }

    // Ara� teslim al - kiralama ba�lat
    public async Task<MusteriKiralama> TeslimAlAsync(int kiralamaId, int baslangicKm, int personelId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kiralama = await GetByIdAsync(kiralamaId);
        if (kiralama == null)
        {
            throw new InvalidOperationException("Kiralama bulunamad�!");
        }

        if (kiralama.Durum != KiralamaDurumu.Rezervasyon)
        {
            throw new InvalidOperationException("Sadece rezervasyon durumundaki kiralama teslim al�nabilir!");
        }

        kiralama.Durum = KiralamaDurumu.Aktif;
        kiralama.BaslangicKm = baslangicKm;
        kiralama.TeslimEdenPersonelId = personelId;
        kiralama.BaslangicTarihi = DateTime.Now; // Ger�ek ba�lang�� zaman�
        kiralama.UpdatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        _logger.LogInformation("Ara� teslim al�nd�: Kiralama {Id}, KM: {Km}", kiralamaId, baslangicKm);
        return kiralama;
    }

    // Ara� teslim et - kiralama bitir
    public async Task<MusteriKiralama> TeslimEtAsync(int kiralamaId, int bitisKm, int personelId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kiralama = await GetByIdAsync(kiralamaId);
        if (kiralama == null)
        {
            throw new InvalidOperationException("Kiralama bulunamad�!");
        }

        if (kiralama.Durum != KiralamaDurumu.Aktif)
        {
            throw new InvalidOperationException("Sadece aktif kiralama teslim edilebilir!");
        }

        if (bitisKm < (kiralama.BaslangicKm ?? 0))
        {
            throw new InvalidOperationException("Biti� kilometresi ba�lang�� kilometresinden k���k olamaz!");
        }

        kiralama.Durum = KiralamaDurumu.Tamamlandi;
        kiralama.GercekBitisTarihi = DateTime.Now;
        kiralama.BitisKm = bitisKm;
        kiralama.TeslimAlanPersonelId = personelId;
        kiralama.UpdatedAt = DateTime.Now;

        // Ger�ek s�reye g�re tutar� yeniden hesapla
        kiralama.ToplamTutar = ToplamTutarHesapla(kiralama.BaslangicTarihi, kiralama.GercekBitisTarihi.Value, kiralama.GunlukFiyat);

        await context.SaveChangesAsync();
        _logger.LogInformation("Ara� teslim edildi: Kiralama {Id}, KM: {Km}, Tutar: {Tutar}", kiralamaId, bitisKm, kiralama.ToplamTutar);
        return kiralama;
    }

    // Ara� belirli tarihler aras�nda m�sait mi kontrol et
    public async Task<bool> AracMusaitMiAsync(int aracId, DateTime baslangic, DateTime bitis, int? haricKiralamaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.MusteriKiralamalar
            .Where(x => !x.IsDeleted 
                && x.AracId == aracId 
                && x.Durum != KiralamaDurumu.IptalEdildi
                && x.Durum != KiralamaDurumu.Tamamlandi);

        // G�ncelleme durumunda mevcut kayd� hari� tut
        if (haricKiralamaId.HasValue)
        {
            query = query.Where(x => x.Id != haricKiralamaId.Value);
        }

        // Tarih �ak��mas� kontrol�
        var cakisan = await query.AnyAsync(x =>
            (baslangic >= x.BaslangicTarihi && baslangic <= (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi)) ||
            (bitis >= x.BaslangicTarihi && bitis <= (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi)) ||
            (baslangic <= x.BaslangicTarihi && bitis >= (x.GercekBitisTarihi ?? x.PlanlananBitisTarihi)));

        return !cakisan;
    }

    // Toplam tutar� hesapla (g�n say�s� * g�nl�k fiyat)
    public decimal ToplamTutarHesapla(DateTime baslangic, DateTime bitis, decimal gunlukFiyat)
    {
        var gunSayisi = (int)Math.Ceiling((bitis - baslangic).TotalDays);
        if (gunSayisi < 1) gunSayisi = 1; // Minimum 1 g�n
        return gunSayisi * gunlukFiyat;
    }
}



