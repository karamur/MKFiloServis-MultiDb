using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Cari hatırlatma servisi implementasyonu
/// </summary>
public class CariHatirlatmaService : ICariHatirlatmaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IFirmaService _firmaService;
    private readonly IEmailService _emailService;
    private readonly ICRMService _crmService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CariHatirlatmaService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CariHatirlatmaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IFirmaService firmaService,
        IEmailService emailService,
        ICRMService crmService,
        IWebHostEnvironment env,
        ILogger<CariHatirlatmaService> logger)
    {
        _contextFactory = contextFactory;
        _firmaService = firmaService;
        _emailService = emailService;
        _crmService = crmService;
        _env = env;
        _logger = logger;
    }

    #region Ayarlar

    private string GetAyarlarDosyaYolu(int? firmaId)
    {
        var klasor = Path.Combine(_env.ContentRootPath, "Data", "HatirlatmaAyarlari");
        if (!Directory.Exists(klasor))
            Directory.CreateDirectory(klasor);
        
        var dosyaAdi = firmaId.HasValue ? $"cari_hatirlatma_{firmaId}.json" : "cari_hatirlatma_default.json";
        return Path.Combine(klasor, dosyaAdi);
    }

    public async Task<CariHatirlatmaSettings> GetAyarlarAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        
        var dosyaYolu = GetAyarlarDosyaYolu(hedefFirmaId);
        
        if (File.Exists(dosyaYolu))
        {
            try
            {
                var json = await File.ReadAllTextAsync(dosyaYolu);
                return JsonSerializer.Deserialize<CariHatirlatmaSettings>(json, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatma ayarları okunamadı: {Dosya}", dosyaYolu);
            }
        }
        
        return new CariHatirlatmaSettings();
    }

    public async Task SaveAyarlarAsync(CariHatirlatmaSettings ayarlar, int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        
        var dosyaYolu = GetAyarlarDosyaYolu(hedefFirmaId);
        var json = JsonSerializer.Serialize(ayarlar, JsonOptions);
        await File.WriteAllTextAsync(dosyaYolu, json);
        
        _logger.LogInformation("Cari hatırlatma ayarları kaydedildi: Firma={FirmaId}", hedefFirmaId);
    }

    #endregion

    #region Manuel Kontrol

    public async Task<CariHatirlatmaRapor> HatirlatmaKontroluYapAsync(int? firmaId = null, bool emailGonder = true, bool bildirimOlustur = true)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        var rapor = new CariHatirlatmaRapor { RaporTarihi = DateTime.Now };
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // Vade yaklaşan faturalar
        if (ayarlar.VadeYaklasanHatirlatma)
        {
            var vadeYaklasan = await VadeYaklasanFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
            rapor.Detaylar.AddRange(vadeYaklasan);
            rapor.VadeYaklasanFaturaSayisi = vadeYaklasan.Count;
        }
        
        // Vade geçmiş faturalar
        if (ayarlar.VadeGecmisHatirlatma)
        {
            var vadeGecmis = await VadeGecmisFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
            rapor.Detaylar.AddRange(vadeGecmis);
            rapor.VadeGecmisFaturaSayisi = vadeGecmis.Count;
            rapor.ToplamVadeGecmisTutar = vadeGecmis.Sum(v => v.Tutar ?? 0);
        }
        
        // Borç eşik aşımı
        if (ayarlar.BorcEsikHatirlatma)
        {
            var borcEsik = await BorcEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
            rapor.Detaylar.AddRange(borcEsik);
            rapor.BorcEsikAsilanCariSayisi = borcEsik.Count;
        }
        
        // Alacak eşik aşımı
        if (ayarlar.AlacakEsikHatirlatma)
        {
            var alacakEsik = await AlacakEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
            rapor.Detaylar.AddRange(alacakEsik);
            rapor.AlacakEsikAsilanCariSayisi = alacakEsik.Count;
        }
        
        // Hareketsiz cariler
        if (ayarlar.HareketsizCariHatirlatma)
        {
            var hareketsiz = await HareketsizCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
            rapor.Detaylar.AddRange(hareketsiz);
            rapor.HareketsizCariSayisi = hareketsiz.Count;
        }
        
        rapor.ToplamUyariSayisi = rapor.Detaylar.Count;
        
        // Hatırlatma kayıtları oluştur
        foreach (var detay in rapor.Detaylar)
        {
            var hatirlatma = new CariHatirlatma
            {
                CariId = detay.CariId,
                FaturaId = detay.FaturaId,
                Tip = detay.Tip,
                Baslik = detay.Aciklama,
                Aciklama = $"{detay.CariUnvan} - {detay.Aciklama}",
                Tutar = detay.Tutar,
                VadeTarihi = detay.VadeTarihi,
                VadeGecenGun = detay.VadeGecenGun,
                FirmaId = hedefFirmaId
            };
            
            // Sistem bildirimi oluştur
            if (bildirimOlustur && ayarlar.SistemBildirimiOlustur)
            {
                var bildirimId = await BildirimOlusturAsync(context, detay, hedefFirmaId);
                if (bildirimId.HasValue)
                {
                    hatirlatma.BildirimOlusturuldu = true;
                    hatirlatma.BildirimId = bildirimId;
                }
            }
            
            context.Set<CariHatirlatma>().Add(hatirlatma);
        }
        
        // E-posta gönder
        if (emailGonder && ayarlar.EmailGonder && rapor.Detaylar.Any())
        {
            var alicilar = await GetEmailAlicilariAsync(context, hedefFirmaId, ayarlar);
            if (alicilar.Any())
            {
                await TopluHatirlatmaEmailiGonderAsync(rapor, alicilar);
            }
        }
        
        // Ayarları güncelle
        ayarlar.SonKontrolTarihi = DateTime.Now;
        ayarlar.SonKontrolUyariSayisi = rapor.ToplamUyariSayisi;
        await SaveAyarlarAsync(ayarlar, hedefFirmaId);
        
        await context.SaveChangesAsync();
        
        _logger.LogInformation("Cari hatırlatma kontrolü tamamlandı: {UyariSayisi} uyarı", rapor.ToplamUyariSayisi);
        
        return rapor;
    }

    #endregion

    #region Vade Kontrolleri

    public async Task<List<CariHatirlatmaDetay>> VadeYaklasanFaturalariGetirAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await VadeYaklasanFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
    }

    private async Task<List<CariHatirlatmaDetay>> VadeYaklasanFaturalariGetirInternalAsync(
        ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        var bugun = DateTime.Today;
        var maxGun = ayarlar.VadeYaklasanGunleri.Max();
        var sonTarih = bugun.AddDays(maxGun);
        
        var faturalar = await context.Faturalar
            .Include(f => f.Cari)
            .Where(f => !f.IsDeleted &&
                       f.VadeTarihi.HasValue &&
                       f.VadeTarihi.Value > bugun &&
                       f.VadeTarihi.Value <= sonTarih &&
                       f.KalanTutar > 0 &&
                       (firmaId == null || f.FirmaId == firmaId))
            .ToListAsync();
        
        var detaylar = new List<CariHatirlatmaDetay>();
        
        foreach (var fatura in faturalar)
        {
            var kalanGun = (fatura.VadeTarihi!.Value - bugun).Days;
            
            if (ayarlar.VadeYaklasanGunleri.Contains(kalanGun))
            {
                detaylar.Add(new CariHatirlatmaDetay
                {
                    CariId = fatura.CariId,
                    CariKodu = fatura.Cari.CariKodu,
                    CariUnvan = fatura.Cari.Unvan,
                    Email = fatura.Cari.Email,
                    Telefon = fatura.Cari.Telefon,
                    Tip = CariHatirlatmaTipi.VadeYaklasan,
                    Aciklama = $"Fatura vadesi {kalanGun} gün sonra",
                    Tutar = fatura.KalanTutar,
                    VadeTarihi = fatura.VadeTarihi,
                    VadeGecenGun = -kalanGun,
                    FaturaNo = fatura.FaturaNo,
                    FaturaId = fatura.Id,
                    Oncelik = ayarlar.VadeYaklasanOncelik
                });
            }
        }
        
        return detaylar;
    }

    public async Task<List<CariHatirlatmaDetay>> VadeGecmisFaturalariGetirAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await VadeGecmisFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
    }

    private async Task<List<CariHatirlatmaDetay>> VadeGecmisFaturalariGetirInternalAsync(
        ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        var bugun = DateTime.Today;
        
        var faturalar = await context.Faturalar
            .Include(f => f.Cari)
            .Where(f => !f.IsDeleted &&
                       f.VadeTarihi.HasValue &&
                       f.VadeTarihi.Value < bugun &&
                       f.KalanTutar >= ayarlar.VadeGecmisMinTutar &&
                       (firmaId == null || f.FirmaId == firmaId))
            .ToListAsync();
        
        var detaylar = new List<CariHatirlatmaDetay>();
        
        foreach (var fatura in faturalar)
        {
            var gecenGun = (bugun - fatura.VadeTarihi!.Value).Days;
            
            if (ayarlar.VadeGecmisGunleri.Contains(gecenGun) || gecenGun > ayarlar.VadeGecmisGunleri.Max())
            {
                var oncelik = gecenGun switch
                {
                    >= 30 => BildirimOncelik.Kritik,
                    >= 15 => BildirimOncelik.Yuksek,
                    >= 7 => BildirimOncelik.Normal,
                    _ => ayarlar.VadeGecmisOncelik
                };
                
                detaylar.Add(new CariHatirlatmaDetay
                {
                    CariId = fatura.CariId,
                    CariKodu = fatura.Cari.CariKodu,
                    CariUnvan = fatura.Cari.Unvan,
                    Email = fatura.Cari.Email,
                    Telefon = fatura.Cari.Telefon,
                    Tip = CariHatirlatmaTipi.VadeGecmis,
                    Aciklama = $"Fatura vadesi {gecenGun} gün geçti",
                    Tutar = fatura.KalanTutar,
                    VadeTarihi = fatura.VadeTarihi,
                    VadeGecenGun = gecenGun,
                    FaturaNo = fatura.FaturaNo,
                    FaturaId = fatura.Id,
                    Oncelik = oncelik
                });
            }
        }
        
        return detaylar.OrderByDescending(d => d.VadeGecenGun).ToList();
    }

    #endregion

    #region Borç/Alacak Kontrolleri

    public async Task<List<CariHatirlatmaDetay>> BorcEsikAsilanCarileriGetirAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await BorcEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
    }

    private async Task<List<CariHatirlatmaDetay>> BorcEsikAsilanCarileriGetirInternalAsync(
        ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        // Cari bazında borç toplamları (Gelen fatura - Yapılan ödeme)
        var cariler = await context.Cariler
            .Where(c => !c.IsDeleted && c.Aktif && (firmaId == null || c.FirmaId == firmaId))
            .Select(c => new
            {
                c.Id,
                c.CariKodu,
                c.Unvan,
                c.Email,
                c.Telefon,
                // Gelen faturalardan kalan borç
                Borc = context.Faturalar
                    .Where(f => f.CariId == c.Id && !f.IsDeleted && f.FaturaYonu == FaturaYonu.Gelen)
                    .Sum(f => (decimal?)f.KalanTutar) ?? 0
            })
            .Where(x => x.Borc >= ayarlar.BorcEsikTutar)
            .ToListAsync();
        
        return cariler.Select(c => new CariHatirlatmaDetay
        {
            CariId = c.Id,
            CariKodu = c.CariKodu,
            CariUnvan = c.Unvan,
            Email = c.Email,
            Telefon = c.Telefon,
            Tip = CariHatirlatmaTipi.BorcEsikAsildi,
            Aciklama = $"Borç tutarı eşik değeri aştı: {c.Borc:N2} ₺",
            Tutar = c.Borc,
            Oncelik = ayarlar.BorcEsikOncelik
        }).ToList();
    }

    public async Task<List<CariHatirlatmaDetay>> AlacakEsikAsilanCarileriGetirAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await AlacakEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
    }

    private async Task<List<CariHatirlatmaDetay>> AlacakEsikAsilanCarileriGetirInternalAsync(
        ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        // Cari bazında alacak toplamları (Kesilen fatura - Alınan ödeme)
        var cariler = await context.Cariler
            .Where(c => !c.IsDeleted && c.Aktif && (firmaId == null || c.FirmaId == firmaId))
            .Select(c => new
            {
                c.Id,
                c.CariKodu,
                c.Unvan,
                c.Email,
                c.Telefon,
                // Kesilen faturalardan kalan alacak
                Alacak = context.Faturalar
                    .Where(f => f.CariId == c.Id && !f.IsDeleted && f.FaturaYonu == FaturaYonu.Giden)
                    .Sum(f => (decimal?)f.KalanTutar) ?? 0
            })
            .Where(x => x.Alacak >= ayarlar.AlacakEsikTutar)
            .ToListAsync();
        
        return cariler.Select(c => new CariHatirlatmaDetay
        {
            CariId = c.Id,
            CariKodu = c.CariKodu,
            CariUnvan = c.Unvan,
            Email = c.Email,
            Telefon = c.Telefon,
            Tip = CariHatirlatmaTipi.AlacakEsikAsildi,
            Aciklama = $"Alacak tutarı eşik değeri aştı: {c.Alacak:N2} ₺",
            Tutar = c.Alacak,
            Oncelik = BildirimOncelik.Yuksek
        }).ToList();
    }

    #endregion

    #region Hareketsiz Cari

    public async Task<List<CariHatirlatmaDetay>> HareketsizCarileriGetirAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await HareketsizCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
    }

    private async Task<List<CariHatirlatmaDetay>> HareketsizCarileriGetirInternalAsync(
        ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        var esikTarih = DateTime.Now.AddDays(-ayarlar.HareketsizCariGunSayisi);
        
        // Son hareket tarihi esik tarihinden önce olan cariler
        var cariler = await context.Cariler
            .Where(c => !c.IsDeleted && c.Aktif && 
                       c.CariTipi != CariTipi.Personel &&
                       (firmaId == null || c.FirmaId == firmaId))
            .Select(c => new
            {
                c.Id,
                c.CariKodu,
                c.Unvan,
                c.Email,
                c.Telefon,
                SonFaturaTarihi = context.Faturalar
                    .Where(f => f.CariId == c.Id && !f.IsDeleted)
                    .OrderByDescending(f => f.FaturaTarihi)
                    .Select(f => (DateTime?)f.FaturaTarihi)
                    .FirstOrDefault(),
                SonHareketTarihi = context.BankaKasaHareketleri
                    .Where(h => h.CariId == c.Id && !h.IsDeleted)
                    .OrderByDescending(h => h.IslemTarihi)
                    .Select(h => (DateTime?)h.IslemTarihi)
                    .FirstOrDefault()
            })
            .ToListAsync();
        
        return cariler
            .Where(c => 
            {
                var sonHareket = c.SonFaturaTarihi > c.SonHareketTarihi ? c.SonFaturaTarihi : c.SonHareketTarihi;
                return sonHareket.HasValue && sonHareket.Value < esikTarih;
            })
            .Select(c => 
            {
                var sonHareket = c.SonFaturaTarihi > c.SonHareketTarihi ? c.SonFaturaTarihi : c.SonHareketTarihi;
                var gecenGun = (DateTime.Now - sonHareket!.Value).Days;
                
                return new CariHatirlatmaDetay
                {
                    CariId = c.Id,
                    CariKodu = c.CariKodu,
                    CariUnvan = c.Unvan,
                    Email = c.Email,
                    Telefon = c.Telefon,
                    Tip = CariHatirlatmaTipi.HareketsizCari,
                    Aciklama = $"{gecenGun} gündür hareket yok (Son: {sonHareket:dd.MM.yyyy})",
                    VadeGecenGun = gecenGun,
                    Oncelik = BildirimOncelik.Dusuk
                };
            })
            .OrderByDescending(d => d.VadeGecenGun)
            .ToList();
    }

    #endregion

    #region Hatırlatma Geçmişi

    public async Task<List<CariHatirlatma>> GetHatirlatmaGecmisiAsync(int? cariId = null, int? firmaId = null, int sonKacGun = 30)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var baslangic = DateTime.Now.AddDays(-sonKacGun);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var query = context.Set<CariHatirlatma>()
            .Include(h => h.Cari)
            .Include(h => h.Fatura)
            .Where(h => !h.IsDeleted && h.CreatedAt >= baslangic);
        
        if (cariId.HasValue)
            query = query.Where(h => h.CariId == cariId);
        
        if (hedefFirmaId.HasValue)
            query = query.Where(h => h.FirmaId == hedefFirmaId);
        
        return await query
            .OrderByDescending(h => h.CreatedAt)
            .Take(500)
            .ToListAsync();
    }

    #endregion

    #region Tek Cari Hatırlatma

    public async Task<bool> TekCariHatirlatmaGonderAsync(int cariId, string mesaj, bool email = true, bool bildirim = true)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var cari = await context.Cariler.FindAsync(cariId);
        if (cari == null) return false;
        
        // Sistem bildirimi oluştur
        if (bildirim)
        {
            var adminler = await context.Kullanicilar
                .Where(k => k.Aktif && (k.Rol.RolAdi == "Admin" || k.Rol.RolAdi == "Yonetici"))
                .ToListAsync();
            
            foreach (var admin in adminler)
            {
                await _crmService.CreateBildirimAsync(new Bildirim
                {
                    KullaniciId = admin.Id,
                    Baslik = $"Cari Hatırlatma: {cari.Unvan}",
                    Icerik = mesaj,
                    Tip = BildirimTipi.Hatirlatici,
                    Oncelik = BildirimOncelik.Normal,
                    IliskiliTablo = "Cari",
                    IliskiliKayitId = cariId,
                    Link = $"/cariler/detay/{cariId}"
                });
            }
        }
        
        // E-posta gönder
        if (email && !string.IsNullOrEmpty(cari.Email))
        {
            var subject = "Hatırlatma";
            var body = $@"
                <h2>Sayın {cari.Unvan},</h2>
                <p>{mesaj}</p>
                <br/>
                <p>Saygılarımızla,</p>
            ";
            
            await _emailService.SendEmailAsync(cari.Email, subject, body);
        }
        
        return true;
    }

    #endregion

    #region E-posta İşlemleri

    private async Task<List<string>> GetEmailAlicilariAsync(ApplicationDbContext context, int? firmaId, CariHatirlatmaSettings ayarlar)
    {
        var alicilar = new List<string>();
        
        if (ayarlar.AdminlereGonder)
        {
            var adminler = await context.Kullanicilar
                .Where(k => k.Aktif && !string.IsNullOrEmpty(k.Email) &&
                           (k.Rol.RolAdi == "Admin" || k.Rol.RolAdi == "Yonetici"))
                .Select(k => k.Email!)
                .ToListAsync();
            
            alicilar.AddRange(adminler);
        }
        
        if (!string.IsNullOrEmpty(ayarlar.EkEmailAdresleri))
        {
            var ekler = ayarlar.EkEmailAdresleri
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => e.Contains('@'));
            
            alicilar.AddRange(ekler);
        }
        
        return alicilar.Distinct().ToList();
    }

    public async Task<bool> VadeHatirlatmaEmailiGonderAsync(CariHatirlatmaDetay detay)
    {
        if (string.IsNullOrEmpty(detay.Email)) return false;
        
        var subject = detay.Tip switch
        {
            CariHatirlatmaTipi.VadeYaklasan => $"Fatura Vade Hatırlatması - {detay.FaturaNo}",
            CariHatirlatmaTipi.VadeGecmis => $"Vadesi Geçmiş Fatura - {detay.FaturaNo}",
            _ => "Ödeme Hatırlatması"
        };
        
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px;'>
                <h2 style='color: #333;'>Sayın {detay.CariUnvan},</h2>
                <p>Aşağıdaki faturanız için hatırlatma yapmak istiyoruz:</p>
                <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                    <tr style='background: #f5f5f5;'>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Fatura No</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{detay.FaturaNo}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Vade Tarihi</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd;'>{detay.VadeTarihi:dd.MM.yyyy}</td>
                    </tr>
                    <tr style='background: #f5f5f5;'>
                        <td style='padding: 10px; border: 1px solid #ddd;'><strong>Kalan Tutar</strong></td>
                        <td style='padding: 10px; border: 1px solid #ddd; color: #d9534f; font-weight: bold;'>{detay.Tutar:N2} ₺</td>
                    </tr>
                </table>
                <p>{detay.Aciklama}</p>
                <p>Ödemenizi en kısa sürede gerçekleştirmenizi rica ederiz.</p>
                <br/>
                <p>Saygılarımızla,</p>
            </div>
        ";
        
        return await _emailService.SendEmailAsync(detay.Email, subject, body);
    }

    public async Task<bool> TopluHatirlatmaEmailiGonderAsync(CariHatirlatmaRapor rapor, List<string> alicilar)
    {
        if (!alicilar.Any()) return false;
        
        var subject = $"Cari Hatırlatma Raporu - {rapor.RaporTarihi:dd.MM.yyyy} ({rapor.ToplamUyariSayisi} Uyarı)";
        
        var vadeGecmisHtml = "";
        var vadeYaklasanHtml = "";
        var digerHtml = "";
        
        var vadeGecmisler = rapor.Detaylar.Where(d => d.Tip == CariHatirlatmaTipi.VadeGecmis).Take(20).ToList();
        if (vadeGecmisler.Any())
        {
            vadeGecmisHtml = $@"
                <h3 style='color: #d9534f;'>🔴 Vadesi Geçmiş Faturalar ({rapor.VadeGecmisFaturaSayisi})</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                    <tr style='background: #d9534f; color: white;'>
                        <th style='padding: 8px; text-align: left;'>Cari</th>
                        <th style='padding: 8px;'>Fatura No</th>
                        <th style='padding: 8px;'>Vade</th>
                        <th style='padding: 8px;'>Geçen Gün</th>
                        <th style='padding: 8px; text-align: right;'>Tutar</th>
                    </tr>
                    {string.Join("", vadeGecmisler.Select(d => $@"
                    <tr style='border-bottom: 1px solid #ddd;'>
                        <td style='padding: 8px;'>{d.CariUnvan}</td>
                        <td style='padding: 8px; text-align: center;'>{d.FaturaNo}</td>
                        <td style='padding: 8px; text-align: center;'>{d.VadeTarihi:dd.MM.yyyy}</td>
                        <td style='padding: 8px; text-align: center; color: #d9534f; font-weight: bold;'>{d.VadeGecenGun} gün</td>
                        <td style='padding: 8px; text-align: right;'>{d.Tutar:N2} ₺</td>
                    </tr>"))}
                </table>
            ";
        }
        
        var vadeYaklasanlar = rapor.Detaylar.Where(d => d.Tip == CariHatirlatmaTipi.VadeYaklasan).Take(20).ToList();
        if (vadeYaklasanlar.Any())
        {
            vadeYaklasanHtml = $@"
                <h3 style='color: #f0ad4e;'>🟡 Vadesi Yaklaşan Faturalar ({rapor.VadeYaklasanFaturaSayisi})</h3>
                <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                    <tr style='background: #f0ad4e; color: white;'>
                        <th style='padding: 8px; text-align: left;'>Cari</th>
                        <th style='padding: 8px;'>Fatura No</th>
                        <th style='padding: 8px;'>Vade</th>
                        <th style='padding: 8px;'>Kalan Gün</th>
                        <th style='padding: 8px; text-align: right;'>Tutar</th>
                    </tr>
                    {string.Join("", vadeYaklasanlar.Select(d => $@"
                    <tr style='border-bottom: 1px solid #ddd;'>
                        <td style='padding: 8px;'>{d.CariUnvan}</td>
                        <td style='padding: 8px; text-align: center;'>{d.FaturaNo}</td>
                        <td style='padding: 8px; text-align: center;'>{d.VadeTarihi:dd.MM.yyyy}</td>
                        <td style='padding: 8px; text-align: center;'>{Math.Abs(d.VadeGecenGun ?? 0)} gün</td>
                        <td style='padding: 8px; text-align: right;'>{d.Tutar:N2} ₺</td>
                    </tr>"))}
                </table>
            ";
        }
        
        var digerler = rapor.Detaylar
            .Where(d => d.Tip != CariHatirlatmaTipi.VadeGecmis && d.Tip != CariHatirlatmaTipi.VadeYaklasan)
            .Take(10)
            .ToList();
        if (digerler.Any())
        {
            digerHtml = $@"
                <h3 style='color: #5bc0de;'>ℹ️ Diğer Uyarılar</h3>
                <ul>
                    {string.Join("", digerler.Select(d => $"<li><strong>{d.CariUnvan}</strong>: {d.Aciklama}</li>"))}
                </ul>
            ";
        }
        
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 800px;'>
                <h2>📋 Günlük Cari Hatırlatma Raporu</h2>
                <p style='color: #666;'>Tarih: {rapor.RaporTarihi:dd.MM.yyyy HH:mm}</p>
                
                <div style='background: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h4 style='margin: 0 0 10px 0;'>Özet</h4>
                    <table>
                        <tr><td>Vadesi Geçmiş Fatura:</td><td><strong>{rapor.VadeGecmisFaturaSayisi}</strong> ({rapor.ToplamVadeGecmisTutar:N2} ₺)</td></tr>
                        <tr><td>Vadesi Yaklaşan Fatura:</td><td><strong>{rapor.VadeYaklasanFaturaSayisi}</strong></td></tr>
                        <tr><td>Borç Eşik Aşımı:</td><td><strong>{rapor.BorcEsikAsilanCariSayisi}</strong> cari</td></tr>
                        <tr><td>Alacak Eşik Aşımı:</td><td><strong>{rapor.AlacakEsikAsilanCariSayisi}</strong> cari</td></tr>
                    </table>
                </div>
                
                {vadeGecmisHtml}
                {vadeYaklasanHtml}
                {digerHtml}
                
                <p style='color: #999; font-size: 12px; margin-top: 30px;'>
                    Bu e-posta CRM Filo Servis sistemi tarafından otomatik olarak gönderilmiştir.
                </p>
            </div>
        ";
        
        return await _emailService.SendEmailAsync(alicilar, subject, body);
    }

    #endregion

    #region Bildirim Oluşturma

    private async Task<int?> BildirimOlusturAsync(ApplicationDbContext context, CariHatirlatmaDetay detay, int? firmaId)
    {
        // Admin kullanıcıları al
        var adminler = await context.Kullanicilar
            .Where(k => k.Aktif && (k.Rol.RolAdi == "Admin" || k.Rol.RolAdi == "Yonetici"))
            .Take(5)
            .ToListAsync();
        
        if (!adminler.Any()) return null;
        
        var baslik = detay.Tip switch
        {
            CariHatirlatmaTipi.VadeYaklasan => $"⏰ Vade Yaklaşıyor: {detay.CariUnvan}",
            CariHatirlatmaTipi.VadeGecmis => $"🔴 Vade Geçti: {detay.CariUnvan}",
            CariHatirlatmaTipi.BorcEsikAsildi => $"⚠️ Borç Eşik Aşıldı: {detay.CariUnvan}",
            CariHatirlatmaTipi.AlacakEsikAsildi => $"💰 Alacak Eşik Aşıldı: {detay.CariUnvan}",
            CariHatirlatmaTipi.HareketsizCari => $"😴 Hareketsiz Cari: {detay.CariUnvan}",
            _ => $"Cari Hatırlatma: {detay.CariUnvan}"
        };
        
        var icerik = detay.FaturaNo != null 
            ? $"{detay.Aciklama} - Fatura: {detay.FaturaNo} - Tutar: {detay.Tutar:N2} ₺"
            : detay.Aciklama;
        
        var link = detay.FaturaId.HasValue 
            ? $"/faturalar/detay/{detay.FaturaId}"
            : $"/cariler/detay/{detay.CariId}";
        
        int? ilkBildirimId = null;
        
        foreach (var admin in adminler)
        {
            var bildirim = new Bildirim
            {
                KullaniciId = admin.Id,
                Baslik = baslik,
                Icerik = icerik,
                Tip = BildirimTipi.OdemeBildirimi,
                Oncelik = detay.Oncelik,
                IliskiliTablo = detay.FaturaId.HasValue ? "Fatura" : "Cari",
                IliskiliKayitId = detay.FaturaId ?? detay.CariId,
                Link = link
            };
            
            context.Bildirimler.Add(bildirim);
            await context.SaveChangesAsync();
            
            ilkBildirimId ??= bildirim.Id;
        }
        
        return ilkBildirimId;
    }

    #endregion

    #region Özet İstatistikler

    public async Task<CariHatirlatmaOzet> GetHatirlatmaOzetiAsync(int? firmaId = null)
    {
        var aktifFirma = _firmaService.GetAktifFirma();
        var hedefFirmaId = firmaId ?? aktifFirma?.FirmaId;
        var ayarlar = await GetAyarlarAsync(hedefFirmaId);
        
        await using var context = await _contextFactory.CreateDbContextAsync();
        
        var bugun = DateTime.Today;
        var haftaBasi = bugun.AddDays(-(int)bugun.DayOfWeek);
        
        var ozet = new CariHatirlatmaOzet
        {
            SonKontrolTarihi = ayarlar.SonKontrolTarihi
        };
        
        // Vade yaklaşan
        var vadeYaklasan = await VadeYaklasanFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
        ozet.VadeYaklasanFaturaSayisi = vadeYaklasan.Count;
        ozet.VadeYaklasanTutar = vadeYaklasan.Sum(v => v.Tutar ?? 0);
        
        // Vade geçmiş
        var vadeGecmis = await VadeGecmisFaturalariGetirInternalAsync(context, hedefFirmaId, ayarlar);
        ozet.VadeGecmisFaturaSayisi = vadeGecmis.Count;
        ozet.VadeGecmisTutar = vadeGecmis.Sum(v => v.Tutar ?? 0);
        
        // Eşik aşımları
        var borcEsik = await BorcEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
        ozet.BorcEsikAsilanCariSayisi = borcEsik.Count;
        
        var alacakEsik = await AlacakEsikAsilanCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
        ozet.AlacakEsikAsilanCariSayisi = alacakEsik.Count;
        
        // Hareketsiz cariler
        if (ayarlar.HareketsizCariHatirlatma)
        {
            var hareketsiz = await HareketsizCarileriGetirInternalAsync(context, hedefFirmaId, ayarlar);
            ozet.HareketsizCariSayisi = hareketsiz.Count;
        }
        
        // Gönderilen hatırlatmalar
        ozet.BugunGonderilenHatirlatmaSayisi = await context.Set<CariHatirlatma>()
            .CountAsync(h => !h.IsDeleted && h.CreatedAt.Date == bugun && (hedefFirmaId == null || h.FirmaId == hedefFirmaId));
        
        ozet.BuHaftaGonderilenHatirlatmaSayisi = await context.Set<CariHatirlatma>()
            .CountAsync(h => !h.IsDeleted && h.CreatedAt >= haftaBasi && (hedefFirmaId == null || h.FirmaId == hedefFirmaId));
        
        return ozet;
    }

    #endregion
}


