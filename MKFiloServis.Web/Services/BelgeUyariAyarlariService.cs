using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Data;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Belge süresi uyarı ayarlarını JSON dosyasında okuyup yazar.
/// Yaklaşan belge uyarılarını önizleme için sorgular.
/// </summary>
public class BelgeUyariAyarlariService
{
    private readonly IWebHostEnvironment _env;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<BelgeUyariAyarlariService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public BelgeUyariAyarlariService(
        IWebHostEnvironment env,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<BelgeUyariAyarlariService> logger)
    {
        _env = env;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    // ─── Ayarlar ───────────────────────────────────────────────────────────

    private string DosyaYolu()
    {
        var klasor = Path.Combine(_env.ContentRootPath, "Data", "HatirlatmaAyarlari");
        if (!Directory.Exists(klasor))
            Directory.CreateDirectory(klasor);
        return Path.Combine(klasor, "belge_uyari_ayarlar.json");
    }

    public async Task<BelgeUyariAyarlar> GetAyarlarAsync()
    {
        var yol = DosyaYolu();
        if (File.Exists(yol))
        {
            try
            {
                var json = await File.ReadAllTextAsync(yol);
                return JsonSerializer.Deserialize<BelgeUyariAyarlar>(json, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Belge uyarı ayarları okunamadı: {Dosya}", yol);
            }
        }
        return new BelgeUyariAyarlar();
    }

    public async Task SaveAyarlarAsync(BelgeUyariAyarlar ayarlar)
    {
        var yol = DosyaYolu();
        var json = JsonSerializer.Serialize(ayarlar, JsonOptions);
        await File.WriteAllTextAsync(yol, json);
        _logger.LogInformation("Belge uyarı ayarları kaydedildi");
    }

    public async Task GuncelleSonCalismaAsync(DateTime sonCalisma, int uyariSayisi)
    {
        var ayarlar = await GetAyarlarAsync();
        ayarlar.SonCalisma = sonCalisma;
        ayarlar.SonCalismaUyariSayisi = uyariSayisi;
        await SaveAyarlarAsync(ayarlar);
    }

    // ─── Önizleme (settings page için) ────────────────────────────────────

    public async Task<List<BelgeUyariItem>> GetYaklasanBelgelerAsync(int[] uyariGunleri)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bugun = DateTime.Today;
        var sonrakiAy = bugun.AddDays(uyariGunleri.Length > 0 ? uyariGunleri.Max() : 30);
        var uyarilar = new List<BelgeUyariItem>();

        // Araç: muayene
        var muayeneler = await context.Araclar
            .Where(a => !a.IsDeleted && a.MuayeneBitisTarihi.HasValue
                     && a.MuayeneBitisTarihi.Value >= bugun
                     && a.MuayeneBitisTarihi.Value <= sonrakiAy)
            .Select(a => new { Plaka = a.AktifPlaka ?? a.SaseNo, Tarih = a.MuayeneBitisTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(muayeneler.Select(m => new BelgeUyariItem
        {
            EntityTipi = "Araç", EntityAdi = m.Plaka,
            BelgeTipi = "Muayene", BitisTarihi = m.Tarih,
            KalanGun = (m.Tarih - bugun).Days
        }));

        // Araç: kasko
        var kaskolar = await context.Araclar
            .Where(a => !a.IsDeleted && a.KaskoBitisTarihi.HasValue
                     && a.KaskoBitisTarihi.Value >= bugun
                     && a.KaskoBitisTarihi.Value <= sonrakiAy)
            .Select(a => new { Plaka = a.AktifPlaka ?? a.SaseNo, Tarih = a.KaskoBitisTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(kaskolar.Select(k => new BelgeUyariItem
        {
            EntityTipi = "Araç", EntityAdi = k.Plaka,
            BelgeTipi = "Kasko", BitisTarihi = k.Tarih,
            KalanGun = (k.Tarih - bugun).Days
        }));

        // Araç: trafik sigortası
        var sigortalar = await context.Araclar
            .Where(a => !a.IsDeleted && a.TrafikSigortaBitisTarihi.HasValue
                     && a.TrafikSigortaBitisTarihi.Value >= bugun
                     && a.TrafikSigortaBitisTarihi.Value <= sonrakiAy)
            .Select(a => new { Plaka = a.AktifPlaka ?? a.SaseNo, Tarih = a.TrafikSigortaBitisTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(sigortalar.Select(s => new BelgeUyariItem
        {
            EntityTipi = "Araç", EntityAdi = s.Plaka,
            BelgeTipi = "Trafik Sigortası", BitisTarihi = s.Tarih,
            KalanGun = (s.Tarih - bugun).Days
        }));

        // Personel: ehliyet
        var ehliyetler = await context.Soforler
            .Where(p => p.Aktif && !p.IsDeleted && p.EhliyetGecerlilikTarihi.HasValue
                     && p.EhliyetGecerlilikTarihi.Value >= bugun
                     && p.EhliyetGecerlilikTarihi.Value <= sonrakiAy)
            .Select(p => new { AdSoyad = p.TamAd, Tarih = p.EhliyetGecerlilikTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(ehliyetler.Select(e => new BelgeUyariItem
        {
            EntityTipi = "Personel", EntityAdi = e.AdSoyad,
            BelgeTipi = "Ehliyet", BitisTarihi = e.Tarih,
            KalanGun = (e.Tarih - bugun).Days
        }));

        // Personel: SRC
        var srcler = await context.Soforler
            .Where(p => p.Aktif && !p.IsDeleted && p.SrcBelgesiGecerlilikTarihi.HasValue
                     && p.SrcBelgesiGecerlilikTarihi.Value >= bugun
                     && p.SrcBelgesiGecerlilikTarihi.Value <= sonrakiAy)
            .Select(p => new { AdSoyad = p.TamAd, Tarih = p.SrcBelgesiGecerlilikTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(srcler.Select(s => new BelgeUyariItem
        {
            EntityTipi = "Personel", EntityAdi = s.AdSoyad,
            BelgeTipi = "SRC Belgesi", BitisTarihi = s.Tarih,
            KalanGun = (s.Tarih - bugun).Days
        }));

        // Personel: psikoteknik
        var psikolar = await context.Soforler
            .Where(p => p.Aktif && !p.IsDeleted && p.PsikoteknikGecerlilikTarihi.HasValue
                     && p.PsikoteknikGecerlilikTarihi.Value >= bugun
                     && p.PsikoteknikGecerlilikTarihi.Value <= sonrakiAy)
            .Select(p => new { AdSoyad = p.TamAd, Tarih = p.PsikoteknikGecerlilikTarihi!.Value })
            .ToListAsync();
        uyarilar.AddRange(psikolar.Select(p => new BelgeUyariItem
        {
            EntityTipi = "Personel", EntityAdi = p.AdSoyad,
            BelgeTipi = "Psikoteknik", BitisTarihi = p.Tarih,
            KalanGun = (p.Tarih - bugun).Days
        }));

        return uyarilar.OrderBy(u => u.KalanGun).ToList();
    }
}


