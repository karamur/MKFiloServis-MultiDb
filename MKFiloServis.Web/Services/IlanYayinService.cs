using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public interface IIlanYayinService
{
    // Platform İşlemleri
    Task<List<IlanPlatformu>> GetPlatformlarAsync();
    Task<IlanPlatformu?> GetPlatformByIdAsync(int id);
    Task<IlanPlatformu> CreatePlatformAsync(IlanPlatformu platform);
    Task<IlanPlatformu> UpdatePlatformAsync(IlanPlatformu platform);
    Task DeletePlatformAsync(int id);

    // Araç İlan Yayın İşlemleri
    Task<List<AracIlanYayin>> GetYayinlarAsync(int? aracId = null, int? platformId = null, IlanYayinDurum? durum = null);
    Task<AracIlanYayin?> GetYayinByIdAsync(int id);
    Task<AracIlanYayin> CreateYayinAsync(AracIlanYayin yayin);
    Task<AracIlanYayin> UpdateYayinAsync(AracIlanYayin yayin);
    Task DeleteYayinAsync(int id);
    Task<bool> YayinAktifEtAsync(int id);
    Task<bool> YayinDurdurAsync(int id);
    Task<bool> YayinSatildiIsaretle(int id);

    // Araç İlan İçerik İşlemleri
    Task<AracIlanIcerik?> GetIcerikByAracAsync(int aracId, int? platformId = null);
    Task<AracIlanIcerik> CreateOrUpdateIcerikAsync(AracIlanIcerik icerik);

    // Kullanıcı Tercihleri İşlemleri
    Task<KullaniciTercihi?> GetKullaniciTercihiAsync(int kullaniciId);
    Task<KullaniciTercihi> CreateOrUpdateTercihAsync(KullaniciTercihi tercih);
    Task<string?> GetVarsayilanAnasayfaAsync(int kullaniciId);
    Task SetVarsayilanAnasayfaAsync(int kullaniciId, string sayfa);

    // Son İşlemler
    Task<List<KullaniciSonIslem>> GetSonIslemlerAsync(int kullaniciId, int adet = 10);
    Task KaydedSonIslemAsync(int kullaniciId, string sayfaYolu, string? sayfaBasligi = null, string? icon = null);

    // İstatistikler
    Task<YayinIstatistikleri> GetYayinIstatistikleriAsync(int? aracId = null);
}

public class YayinIstatistikleri
{
    public int ToplamPlatform { get; set; }
    public int AktifPlatform { get; set; }
    public int ToplamYayin { get; set; }
    public int AktifYayin { get; set; }
    public int TaslakYayin { get; set; }
    public int DurdurulanYayin { get; set; }
    public int SatilanYayin { get; set; }
    public int ToplamGoruntulenme { get; set; }
    public int ToplamTiklama { get; set; }
    public int ToplamFavorileme { get; set; }
    public int ToplamMesaj { get; set; }
    public List<PlatformIstatistik> PlatformBazliIstatistikler { get; set; } = new();
}

public class PlatformIstatistik
{
    public int PlatformId { get; set; }
    public string PlatformAdi { get; set; } = string.Empty;
    public int YayinSayisi { get; set; }
    public int GoruntulenmeToplamı { get; set; }
    public int TiklamaToplamı { get; set; }
}

public class IlanYayinService : IIlanYayinService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public IlanYayinService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    #region Platform İşlemleri

    public async Task<List<IlanPlatformu>> GetPlatformlarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IlanPlatformlari
            .OrderBy(p => p.SiraNo)
            .ThenBy(p => p.PlatformAdi)
            .ToListAsync();
    }

    public async Task<IlanPlatformu?> GetPlatformByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.IlanPlatformlari
            .Include(p => p.Yayinlar)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IlanPlatformu> CreatePlatformAsync(IlanPlatformu platform)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        platform.CreatedAt = DateTime.UtcNow;
        context.IlanPlatformlari.Add(platform);
        await context.SaveChangesAsync();
        return platform;
    }

    public async Task<IlanPlatformu> UpdatePlatformAsync(IlanPlatformu platform)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        platform.UpdatedAt = DateTime.UtcNow;
        context.IlanPlatformlari.Update(platform);
        await context.SaveChangesAsync();
        return platform;
    }

    public async Task DeletePlatformAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var platform = await context.IlanPlatformlari.FindAsync(id);
        if (platform != null)
        {
            platform.IsDeleted = true;
            platform.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Araç İlan Yayın İşlemleri

    public async Task<List<AracIlanYayin>> GetYayinlarAsync(int? aracId = null, int? platformId = null, IlanYayinDurum? durum = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracIlanYayinlar
            .Include(y => y.Arac)
            .Include(y => y.Platform)
            .Include(y => y.YayinlayanKullanici)
            .AsQueryable();

        if (aracId.HasValue)
            query = query.Where(y => y.AracId == aracId.Value);

        if (platformId.HasValue)
            query = query.Where(y => y.PlatformId == platformId.Value);

        if (durum.HasValue)
            query = query.Where(y => y.Durum == durum.Value);

        return await query
            .OrderByDescending(y => y.CreatedAt)
            .ToListAsync();
    }

    public async Task<AracIlanYayin?> GetYayinByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.AracIlanYayinlar
            .Include(y => y.Arac)
            .Include(y => y.Platform)
            .Include(y => y.YayinlayanKullanici)
            .FirstOrDefaultAsync(y => y.Id == id);
    }

    public async Task<AracIlanYayin> CreateYayinAsync(AracIlanYayin yayin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        yayin.CreatedAt = DateTime.UtcNow;
        context.AracIlanYayinlar.Add(yayin);
        await context.SaveChangesAsync();
        return yayin;
    }

    public async Task<AracIlanYayin> UpdateYayinAsync(AracIlanYayin yayin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        yayin.UpdatedAt = DateTime.UtcNow;
        yayin.SonGuncelleme = DateTime.UtcNow;
        context.AracIlanYayinlar.Update(yayin);
        await context.SaveChangesAsync();
        return yayin;
    }

    public async Task DeleteYayinAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yayin = await context.AracIlanYayinlar.FindAsync(id);
        if (yayin != null)
        {
            yayin.IsDeleted = true;
            yayin.Durum = IlanYayinDurum.Silindi;
            yayin.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> YayinAktifEtAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yayin = await context.AracIlanYayinlar.FindAsync(id);
        if (yayin == null) return false;

        yayin.Durum = IlanYayinDurum.Aktif;
        yayin.YayinBaslangic = DateTime.UtcNow;
        yayin.SonGuncelleme = DateTime.UtcNow;
        yayin.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> YayinDurdurAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yayin = await context.AracIlanYayinlar.FindAsync(id);
        if (yayin == null) return false;

        yayin.Durum = IlanYayinDurum.Durduruldu;
        yayin.YayinBitis = DateTime.UtcNow;
        yayin.SonGuncelleme = DateTime.UtcNow;
        yayin.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> YayinSatildiIsaretle(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var yayin = await context.AracIlanYayinlar.FindAsync(id);
        if (yayin == null) return false;

        yayin.Durum = IlanYayinDurum.Satildi;
        yayin.YayinBitis = DateTime.UtcNow;
        yayin.SonGuncelleme = DateTime.UtcNow;
        yayin.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Araç İlan İçerik İşlemleri

    public async Task<AracIlanIcerik?> GetIcerikByAracAsync(int aracId, int? platformId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AracIlanIcerikleri
            .Include(i => i.Arac)
            .Where(i => i.AracId == aracId);

        if (platformId.HasValue)
            query = query.Where(i => i.PlatformId == platformId.Value);
        else
            query = query.Where(i => i.PlatformId == null); // Genel içerik

        return await query.FirstOrDefaultAsync();
    }

    public async Task<AracIlanIcerik> CreateOrUpdateIcerikAsync(AracIlanIcerik icerik)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.AracIlanIcerikleri
            .FirstOrDefaultAsync(i => i.AracId == icerik.AracId && i.PlatformId == icerik.PlatformId);

        if (existing != null)
        {
            existing.IlanBasligi = icerik.IlanBasligi;
            existing.IlanAciklamasi = icerik.IlanAciklamasi;
            existing.OzellikListesi = icerik.OzellikListesi;
            existing.FotografListesi = icerik.FotografListesi;
            existing.VitrinFotografi = icerik.VitrinFotografi;
            existing.MetaBaslik = icerik.MetaBaslik;
            existing.MetaAciklama = icerik.MetaAciklama;
            existing.AnahtarKelimeler = icerik.AnahtarKelimeler;
            existing.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return existing;
        }
        else
        {
            icerik.CreatedAt = DateTime.UtcNow;
            context.AracIlanIcerikleri.Add(icerik);
            await context.SaveChangesAsync();
            return icerik;
        }
    }

    #endregion

    #region Kullanıcı Tercihleri İşlemleri

    public async Task<KullaniciTercihi?> GetKullaniciTercihiAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KullaniciTercihleri
            .Include(t => t.Kullanici)
            .FirstOrDefaultAsync(t => t.KullaniciId == kullaniciId);
    }

    public async Task<KullaniciTercihi> CreateOrUpdateTercihAsync(KullaniciTercihi tercih)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.KullaniciTercihleri
            .FirstOrDefaultAsync(t => t.KullaniciId == tercih.KullaniciId);

        if (existing != null)
        {
            existing.VarsayilanAnasayfa = tercih.VarsayilanAnasayfa;
            existing.AnasayfaWidgetGoster = tercih.AnasayfaWidgetGoster;
            existing.AnasayfaWidgetSirasi = tercih.AnasayfaWidgetSirasi;
            existing.Tema = tercih.Tema;
            existing.SidebarDurum = tercih.SidebarDurum;
            existing.EmailBildirimAktif = tercih.EmailBildirimAktif;
            existing.TarayiciBildirimAktif = tercih.TarayiciBildirimAktif;
            existing.SesBildirimAktif = tercih.SesBildirimAktif;
            existing.VarsayilanSayfaBoyutu = tercih.VarsayilanSayfaBoyutu;
            existing.VarsayilanSiralama = tercih.VarsayilanSiralama;
            existing.DigerTercihler = tercih.DigerTercihler;
            existing.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return existing;
        }
        else
        {
            tercih.CreatedAt = DateTime.UtcNow;
            context.KullaniciTercihleri.Add(tercih);
            await context.SaveChangesAsync();
            return tercih;
        }
    }

    public async Task<string?> GetVarsayilanAnasayfaAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tercih = await context.KullaniciTercihleri
            .Where(t => t.KullaniciId == kullaniciId)
            .Select(t => t.VarsayilanAnasayfa)
            .FirstOrDefaultAsync();

        return tercih;
    }

    public async Task SetVarsayilanAnasayfaAsync(int kullaniciId, string sayfa)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tercih = await context.KullaniciTercihleri
            .FirstOrDefaultAsync(t => t.KullaniciId == kullaniciId);

        if (tercih != null)
        {
            tercih.VarsayilanAnasayfa = sayfa;
            tercih.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            tercih = new KullaniciTercihi
            {
                KullaniciId = kullaniciId,
                VarsayilanAnasayfa = sayfa,
                CreatedAt = DateTime.UtcNow
            };
            context.KullaniciTercihleri.Add(tercih);
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region Son İşlemler

    public async Task<List<KullaniciSonIslem>> GetSonIslemlerAsync(int kullaniciId, int adet = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.KullaniciSonIslemler
            .Where(s => s.KullaniciId == kullaniciId)
            .OrderByDescending(s => s.ErisimZamani)
            .Take(adet)
            .ToListAsync();
    }

    public async Task KaydedSonIslemAsync(int kullaniciId, string sayfaYolu, string? sayfaBasligi = null, string? icon = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.KullaniciSonIslemler
            .FirstOrDefaultAsync(s => s.KullaniciId == kullaniciId && s.SayfaYolu == sayfaYolu);

        if (existing != null)
        {
            existing.ErisimZamani = DateTime.UtcNow;
            existing.ErisimSayisi++;
            existing.SayfaBasligi = sayfaBasligi ?? existing.SayfaBasligi;
            existing.Icon = icon ?? existing.Icon;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var yeniIslem = new KullaniciSonIslem
            {
                KullaniciId = kullaniciId,
                SayfaYolu = sayfaYolu,
                SayfaBasligi = sayfaBasligi,
                Icon = icon,
                ErisimZamani = DateTime.UtcNow,
                ErisimSayisi = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.KullaniciSonIslemler.Add(yeniIslem);

            // Eski kayıtları temizle (en fazla 50 kayıt tut)
            var eskiler = await context.KullaniciSonIslemler
                .Where(s => s.KullaniciId == kullaniciId)
                .OrderByDescending(s => s.ErisimZamani)
                .Skip(50)
                .ToListAsync();

            foreach (var eski in eskiler)
            {
                eski.IsDeleted = true;
            }
        }

        await context.SaveChangesAsync();
    }

    #endregion

    #region İstatistikler

    public async Task<YayinIstatistikleri> GetYayinIstatistikleriAsync(int? aracId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var platformlar = await context.IlanPlatformlari.ToListAsync();

        var yayinQuery = context.AracIlanYayinlar.AsQueryable();
        if (aracId.HasValue)
            yayinQuery = yayinQuery.Where(y => y.AracId == aracId.Value);

        var yayinlar = await yayinQuery.ToListAsync();

        var istatistik = new YayinIstatistikleri
        {
            ToplamPlatform = platformlar.Count,
            AktifPlatform = platformlar.Count(p => p.Aktif),
            ToplamYayin = yayinlar.Count,
            AktifYayin = yayinlar.Count(y => y.Durum == IlanYayinDurum.Aktif),
            TaslakYayin = yayinlar.Count(y => y.Durum == IlanYayinDurum.Taslak),
            DurdurulanYayin = yayinlar.Count(y => y.Durum == IlanYayinDurum.Durduruldu),
            SatilanYayin = yayinlar.Count(y => y.Durum == IlanYayinDurum.Satildi),
            ToplamGoruntulenme = yayinlar.Sum(y => y.GoruntulenmeSayisi),
            ToplamTiklama = yayinlar.Sum(y => y.TiklamaSayisi),
            ToplamFavorileme = yayinlar.Sum(y => y.FavorilenmeSayisi),
            ToplamMesaj = yayinlar.Sum(y => y.MesajSayisi)
        };

        foreach (var platform in platformlar.Where(p => p.Aktif))
        {
            var platformYayinlar = yayinlar.Where(y => y.PlatformId == platform.Id).ToList();
            istatistik.PlatformBazliIstatistikler.Add(new PlatformIstatistik
            {
                PlatformId = platform.Id,
                PlatformAdi = platform.PlatformAdi,
                YayinSayisi = platformYayinlar.Count,
                GoruntulenmeToplamı = platformYayinlar.Sum(y => y.GoruntulenmeSayisi),
                TiklamaToplamı = platformYayinlar.Sum(y => y.TiklamaSayisi)
            });
        }

        return istatistik;
    }

    #endregion
}



