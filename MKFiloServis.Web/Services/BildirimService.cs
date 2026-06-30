using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Bildirim servisi - vade/belge süresi uyarıları ve kullanıcı bildirimleri
/// </summary>
public class BildirimService : IBildirimService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<BildirimService> _logger;
    private readonly IEmailService _emailService;

    public BildirimService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<BildirimService> logger, IEmailService emailService)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _emailService = emailService;
    }

    #region Bildirim CRUD

    public async Task<List<Bildirim>> GetKullaniciBildirimlerAsync(int kullaniciId, bool sadeceOkunmamis = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Bildirimler
            .Where(b => !b.IsDeleted && b.KullaniciId == kullaniciId);

        if (sadeceOkunmamis)
        {
            query = query.Where(b => !b.Okundu);
        }

        return await query
            .OrderByDescending(b => b.CreatedAt)
            .Take(100) // Son 100 bildirim
            .ToListAsync();
    }

    public async Task<int> GetOkunmamisBildirimSayisiAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Bildirimler
            .CountAsync(b => !b.IsDeleted && b.KullaniciId == kullaniciId && !b.Okundu);
    }

    public async Task<Bildirim?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Bildirimler
            .Include(b => b.Kullanici)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
    }

    public async Task<Bildirim> CreateAsync(Bildirim bildirim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        bildirim.CreatedAt = DateTime.Now;
        context.Bildirimler.Add(bildirim);
        await context.SaveChangesAsync();
        return bildirim;
    }

    public async Task OkunduOlarakIsaretle(int bildirimId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bildirim = await context.Bildirimler.FindAsync(bildirimId);
        if (bildirim != null)
        {
            bildirim.Okundu = true;
            bildirim.OkunmaTarihi = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }

    public async Task TumunuOkunduYapAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bildirimler = await context.Bildirimler
            .Where(b => !b.IsDeleted && b.KullaniciId == kullaniciId && !b.Okundu)
            .ToListAsync();

        foreach (var bildirim in bildirimler)
        {
            bildirim.Okundu = true;
            bildirim.OkunmaTarihi = DateTime.Now;
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var bildirim = await context.Bildirimler.FindAsync(id);
        if (bildirim != null)
        {
            bildirim.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Bildirim Ayarları

    public async Task<BildirimAyar?> GetKullaniciAyarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.BildirimAyarlari
            .FirstOrDefaultAsync(a => !a.IsDeleted && a.KullaniciId == kullaniciId);
    }

    public async Task<BildirimAyar> SaveAyarAsync(BildirimAyar ayar)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (ayar.Id == 0)
        {
            ayar.CreatedAt = DateTime.Now;
            context.BildirimAyarlari.Add(ayar);
        }
        else
        {
            ayar.UpdatedAt = DateTime.Now;
            context.BildirimAyarlari.Update(ayar);
        }
        
        await context.SaveChangesAsync();
        return ayar;
    }

    #endregion

    #region Otomatik Bildirim Tarama

    public async Task<List<BildirimOzet>> TaraVeBildirimOlusturAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozetler = new List<BildirimOzet>();
        
        // Vade yaklaşan faturaları tara
        var faturaOzetleri = await VadeYaklasanFaturalariTaraAsync(7);
        ozetler.AddRange(faturaOzetleri);
        
        // Süresi dolan belgeleri tara
        var belgeOzetleri = await SuresiDolanBelgeleriTaraAsync(30);
        ozetler.AddRange(belgeOzetleri);
        
        // Tüm kullanıcılara bildirim oluştur
        var kullanicilar = await context.Kullanicilar
            .Where(k => !k.IsDeleted && k.Aktif)
            .ToListAsync();
        
        foreach (var ozet in ozetler)
        {
            foreach (var kullanici in kullanicilar)
            {
                // Kullanıcının bu tip bildirim alıp almayacağını kontrol et
                var ayar = await GetKullaniciAyarAsync(kullanici.Id);
                if (!ShouldReceiveNotification(ayar, ozet.Tip))
                    continue;
                
                // Aynı bildirim daha önce oluşturulmuş mu kontrol et
                var mevcutBildirim = await context.Bildirimler
                    .AnyAsync(b => !b.IsDeleted 
                        && b.KullaniciId == kullanici.Id 
                        && b.Tip == ozet.Tip
                        && b.IliskiliTablo == ozet.IliskiliTablo
                        && b.IliskiliKayitId == ozet.IliskiliKayitId
                        && b.CreatedAt > DateTime.Now.AddDays(-1)); // Son 1 günde aynı bildirim var mı?
                
                if (mevcutBildirim)
                    continue;
                
                // Yeni bildirim oluştur
                var bildirim = new Bildirim
                {
                    KullaniciId = kullanici.Id,
                    Baslik = ozet.Baslik,
                    Icerik = ozet.Aciklama,
                    Tip = ozet.Tip,
                    Oncelik = ozet.Oncelik,
                    IliskiliTablo = ozet.IliskiliTablo,
                    IliskiliKayitId = ozet.IliskiliKayitId,
                    Link = ozet.Link,
                    SonGosterimTarihi = ozet.BitisTarihi?.AddDays(7), // Bitiş tarihinden 7 gün sonraya kadar göster
                    CreatedAt = DateTime.Now
                };
                
                context.Bildirimler.Add(bildirim);
            }
        }
        
        await context.SaveChangesAsync();
        _logger.LogInformation("Bildirim taraması tamamlandı. {Count} yeni bildirim özeti oluşturuldu.", ozetler.Count);
        
        return ozetler;
    }

    public async Task<List<BildirimOzet>> VadeYaklasanFaturalariTaraAsync(int gunSayisi = 7)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozetler = new List<BildirimOzet>();
        var bugun = DateTime.Today;
        var bitisTarihi = bugun.AddDays(gunSayisi);
        
        // Vadesi yaklaşan veya geçmiş ödenmemiş faturalar
        var faturalar = await context.Faturalar
            .Include(f => f.Firma)
            .Where(f => !f.IsDeleted 
                && f.VadeTarihi != null 
                && f.VadeTarihi <= bitisTarihi
                && f.Durum != FaturaDurum.Odendi
                && f.KalanTutar > 0)
            .OrderBy(f => f.VadeTarihi)
            .ToListAsync();
        
        foreach (var fatura in faturalar)
        {
            var kalanGun = (fatura.VadeTarihi!.Value - bugun).Days;
            var oncelik = kalanGun switch
            {
                <= 0 => BildirimOncelik.Kritik,
                <= 3 => BildirimOncelik.Yuksek,
                <= 7 => BildirimOncelik.Normal,
                _ => BildirimOncelik.Dusuk
            };
            
            var durum = kalanGun <= 0 ? "VADESİ GEÇMİŞ" : $"{kalanGun} gün kaldı";
            
            ozetler.Add(new BildirimOzet
            {
                Tip = BildirimTipi.FaturaVade,
                Baslik = $"Fatura Vade Uyarısı: {fatura.FaturaNo}",
                Aciklama = $"{fatura.Firma?.FirmaAdi ?? "Bilinmiyor"} - {fatura.GenelToplam:N2} TL ({durum})",
                IliskiliTablo = "Fatura",
                IliskiliKayitId = fatura.Id,
                Link = $"/faturalar/{fatura.Id}",
                BitisTarihi = fatura.VadeTarihi,
                KalanGun = kalanGun,
                Oncelik = oncelik
            });
        }
        
        return ozetler;
    }

    public async Task<List<BildirimOzet>> SuresiDolanBelgeleriTaraAsync(int gunSayisi = 30)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozetler = new List<BildirimOzet>();
        var bugun = DateTime.Today;
        var bitisTarihi = bugun.AddDays(gunSayisi);
        
        // ARAÇ BELGELERİ
        var araclar = await context.Araclar
            .Where(a => !a.IsDeleted && a.Aktif)
            .ToListAsync();
        
        foreach (var arac in araclar)
        {
            var plakaGosterim = arac.AktifPlaka ?? arac.SaseNo;
            
            // Trafik Sigortası
            if (arac.TrafikSigortaBitisTarihi != null && arac.TrafikSigortaBitisTarihi <= bitisTarihi)
            {
                var kalanGun = (arac.TrafikSigortaBitisTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.TrafikSigorta, 
                    "Trafik Sigortası", plakaGosterim, "Arac", arac.Id,
                    $"/araclar/{arac.Id}", arac.TrafikSigortaBitisTarihi.Value, kalanGun));
            }
            
            // Kasko
            if (arac.KaskoBitisTarihi != null && arac.KaskoBitisTarihi <= bitisTarihi)
            {
                var kalanGun = (arac.KaskoBitisTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.Kasko, 
                    "Kasko", plakaGosterim, "Arac", arac.Id,
                    $"/araclar/{arac.Id}", arac.KaskoBitisTarihi.Value, kalanGun));
            }
            
            // Muayene
            if (arac.MuayeneBitisTarihi != null && arac.MuayeneBitisTarihi <= bitisTarihi)
            {
                var kalanGun = (arac.MuayeneBitisTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.Muayene, 
                    "Araç Muayenesi", plakaGosterim, "Arac", arac.Id,
                    $"/araclar/{arac.Id}", arac.MuayeneBitisTarihi.Value, kalanGun));
            }
        }
        
        // ŞOFÖR BELGELERİ
        var soforler = await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif && s.Gorev == PersonelGorev.Sofor)
            .ToListAsync();
        
        foreach (var sofor in soforler)
        {
            var soforAdi = $"{sofor.Ad} {sofor.Soyad}";
            
            // Ehliyet
            if (sofor.EhliyetGecerlilikTarihi != null && sofor.EhliyetGecerlilikTarihi <= bitisTarihi)
            {
                var kalanGun = (sofor.EhliyetGecerlilikTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.EhliyetBitis, 
                    "Ehliyet", soforAdi, "Sofor", sofor.Id,
                    $"/personel/{sofor.Id}", sofor.EhliyetGecerlilikTarihi.Value, kalanGun));
            }
            
            // SRC Belgesi
            if (sofor.SrcBelgesiGecerlilikTarihi != null && sofor.SrcBelgesiGecerlilikTarihi <= bitisTarihi)
            {
                var kalanGun = (sofor.SrcBelgesiGecerlilikTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.SrcBelgesi, 
                    "SRC Belgesi", soforAdi, "Sofor", sofor.Id,
                    $"/personel/{sofor.Id}", sofor.SrcBelgesiGecerlilikTarihi.Value, kalanGun));
            }
            
            // Psikoteknik
            if (sofor.PsikoteknikGecerlilikTarihi != null && sofor.PsikoteknikGecerlilikTarihi <= bitisTarihi)
            {
                var kalanGun = (sofor.PsikoteknikGecerlilikTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.Psikoteknik, 
                    "Psikoteknik Belgesi", soforAdi, "Sofor", sofor.Id,
                    $"/personel/{sofor.Id}", sofor.PsikoteknikGecerlilikTarihi.Value, kalanGun));
            }
            
            // Sağlık Raporu
            if (sofor.SaglikRaporuGecerlilikTarihi != null && sofor.SaglikRaporuGecerlilikTarihi <= bitisTarihi)
            {
                var kalanGun = (sofor.SaglikRaporuGecerlilikTarihi.Value - bugun).Days;
                ozetler.Add(CreateBelgeOzet(BildirimTipi.SaglikRaporu, 
                    "Sağlık Raporu", soforAdi, "Sofor", sofor.Id,
                    $"/personel/{sofor.Id}", sofor.SaglikRaporuGecerlilikTarihi.Value, kalanGun));
            }
        }
        
        return ozetler.OrderBy(o => o.KalanGun).ToList();
    }

    private BildirimOzet CreateBelgeOzet(BildirimTipi tip, string belgeTipi, string kayitAdi, 
        string iliskiliTablo, int iliskiliId, string link, DateTime bitisTarihi, int kalanGun)
    {
        var oncelik = kalanGun switch
        {
            <= 0 => BildirimOncelik.Kritik,
            <= 7 => BildirimOncelik.Yuksek,
            <= 15 => BildirimOncelik.Normal,
            _ => BildirimOncelik.Dusuk
        };
        
        var durum = kalanGun <= 0 ? "SÜRESİ DOLMUŞ" : $"{kalanGun} gün kaldı";
        
        return new BildirimOzet
        {
            Tip = tip,
            Baslik = $"{belgeTipi} Uyarısı: {kayitAdi}",
            Aciklama = $"{belgeTipi} bitiş tarihi: {bitisTarihi:dd.MM.yyyy} ({durum})",
            IliskiliTablo = iliskiliTablo,
            IliskiliKayitId = iliskiliId,
            Link = link,
            BitisTarihi = bitisTarihi,
            KalanGun = kalanGun,
            Oncelik = oncelik
        };
    }

    private bool ShouldReceiveNotification(BildirimAyar? ayar, BildirimTipi tip)
    {
        // Ayar yoksa varsayılan olarak tüm bildirimleri al
        if (ayar == null) return true;
        
        return tip switch
        {
            BildirimTipi.FaturaVade => ayar.FaturaVadeUyarisi,
            BildirimTipi.EhliyetBitis => ayar.EhliyetBitisUyarisi,
            BildirimTipi.SrcBelgesi => ayar.SrcBelgesiUyarisi,
            BildirimTipi.Psikoteknik => ayar.PsikoteknikUyarisi,
            BildirimTipi.SaglikRaporu => ayar.SaglikRaporuUyarisi,
            BildirimTipi.TrafikSigorta => ayar.TrafikSigortaUyarisi,
            BildirimTipi.Kasko => ayar.KaskoUyarisi,
            BildirimTipi.Muayene => ayar.MuayeneUyarisi,
            BildirimTipi.DestekTalebi => ayar.DestekTalebiUyarisi,
            BildirimTipi.Sistem => ayar.SistemBildirimleri,
            _ => true
        };
    }

    #endregion

    #region Dashboard

    public async Task<BildirimDashboardDto> GetDashboardOzetAsync(int? kullaniciId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Bildirimler.Where(b => !b.IsDeleted);
        
        if (kullaniciId.HasValue)
        {
            query = query.Where(b => b.KullaniciId == kullaniciId.Value);
        }
        
        var bildirimler = await query.ToListAsync();
        
        var dto = new BildirimDashboardDto
        {
            ToplamBildirim = bildirimler.Count,
            OkunmamisBildirim = bildirimler.Count(b => !b.Okundu),
            KritikBildirim = bildirimler.Count(b => b.Oncelik == BildirimOncelik.Kritik && !b.Okundu),
            SonBildirimler = bildirimler
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToList()
        };
        
        // Kategorilere göre sayılar
        dto.KategoriBazliSayilar = bildirimler
            .Where(b => !b.Okundu)
            .GroupBy(b => b.Tip)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Vade yaklaşan fatura sayısı
        dto.VadeYaklasanFatura = await context.Faturalar
            .CountAsync(f => !f.IsDeleted 
                && f.VadeTarihi != null 
                && f.VadeTarihi <= DateTime.Today.AddDays(7)
                && f.Durum != FaturaDurum.Odendi
                && f.KalanTutar > 0);
        
        // Süresi dolan belge sayısı
        var bugun = DateTime.Today;
        var bitisTarihi = bugun.AddDays(30);
        
        var aracBelgeSayisi = await context.Araclar
            .Where(a => !a.IsDeleted && a.Aktif)
            .CountAsync(a => 
                (a.TrafikSigortaBitisTarihi != null && a.TrafikSigortaBitisTarihi <= bitisTarihi) ||
                (a.KaskoBitisTarihi != null && a.KaskoBitisTarihi <= bitisTarihi) ||
                (a.MuayeneBitisTarihi != null && a.MuayeneBitisTarihi <= bitisTarihi));
        
        var soforBelgeSayisi = await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif && s.Gorev == PersonelGorev.Sofor)
            .CountAsync(s => 
                (s.EhliyetGecerlilikTarihi != null && s.EhliyetGecerlilikTarihi <= bitisTarihi) ||
                (s.SrcBelgesiGecerlilikTarihi != null && s.SrcBelgesiGecerlilikTarihi <= bitisTarihi) ||
                (s.PsikoteknikGecerlilikTarihi != null && s.PsikoteknikGecerlilikTarihi <= bitisTarihi) ||
                (s.SaglikRaporuGecerlilikTarihi != null && s.SaglikRaporuGecerlilikTarihi <= bitisTarihi));
        
        dto.SuresiDolanBelge = aracBelgeSayisi + soforBelgeSayisi;
        
        // Yaklaşan olaylar
        var faturaOzetleri = await VadeYaklasanFaturalariTaraAsync(7);
        var belgeOzetleri = await SuresiDolanBelgeleriTaraAsync(30);
        dto.YaklasanOlaylar = faturaOzetleri.Concat(belgeOzetleri)
            .OrderBy(o => o.KalanGun)
            .Take(20)
            .ToList();

        return dto;
    }

    #endregion

    #region E-posta Bildirimleri

    public async Task<int> EpostaBildirimGonderAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var gonderimSayisi = 0;

        try
        {
            // E-posta almak isteyen ve e-posta adresi olan kullanıcıları al
            var ayarlar = await context.BildirimAyarlari
                .Include(a => a.Kullanici)
                .Where(a => !a.IsDeleted && a.EpostaAlsin && !string.IsNullOrEmpty(a.EpostaAdresi))
                .ToListAsync();

            if (!ayarlar.Any())
            {
                _logger.LogInformation("E-posta bildirimi alacak kullanıcı bulunamadı.");
                return 0;
            }

            foreach (var ayar in ayarlar)
            {
                try
                {
                    // Kullanıcının ayarlarına göre uyarıları topla
                    var uyarilar = new List<BelgeUyariEmail>();

                    // Fatura vade uyarıları
                    if (ayar.FaturaVadeUyarisi)
                    {
                        var faturaOzetleri = await VadeYaklasanFaturalariTaraAsync(ayar.VadeUyariGunSayisi);
                        uyarilar.AddRange(faturaOzetleri.Select(o => new BelgeUyariEmail
                        {
                            SahipAdi = "Fatura: " + o.Baslik.Replace("Fatura Vade Uyarısı: ", ""),
                            BelgeAdi = "Ödenmemiş Fatura",
                            BitisTarihi = o.BitisTarihi ?? DateTime.Today,
                            GunKaldi = o.KalanGun
                        }));
                    }

                    // Belge süresi uyarıları
                    var belgeOzetleri = await SuresiDolanBelgeleriTaraAsync(ayar.BelgeUyariGunSayisi);
                    foreach (var ozet in belgeOzetleri)
                    {
                        var uyariAlsin = ozet.Tip switch
                        {
                            BildirimTipi.EhliyetBitis => ayar.EhliyetBitisUyarisi,
                            BildirimTipi.SrcBelgesi => ayar.SrcBelgesiUyarisi,
                            BildirimTipi.Psikoteknik => ayar.PsikoteknikUyarisi,
                            BildirimTipi.SaglikRaporu => ayar.SaglikRaporuUyarisi,
                            BildirimTipi.TrafikSigorta => ayar.TrafikSigortaUyarisi,
                            BildirimTipi.Kasko => ayar.KaskoUyarisi,
                            BildirimTipi.Muayene => ayar.MuayeneUyarisi,
                            _ => false
                        };

                        if (uyariAlsin)
                        {
                            uyarilar.Add(new BelgeUyariEmail
                            {
                                SahipAdi = ozet.Baslik.Replace(" Uyarısı: ", ": "),
                                BelgeAdi = GetBelgeTipiAdi(ozet.Tip),
                                BitisTarihi = ozet.BitisTarihi ?? DateTime.Today,
                                GunKaldi = ozet.KalanGun
                            });
                        }
                    }

                    // Uyarı varsa e-posta gönder
                    if (uyarilar.Any())
                    {
                        // Son 24 saatte aynı kullanıcıya e-posta gönderilmiş mi kontrol et
                        var sonGonderim = await context.Set<EpostaBildirimLog>()
                            .Where(l => l.KullaniciId == ayar.KullaniciId && l.GonderimTarihi > DateTime.Now.AddHours(-24))
                            .AnyAsync();

                        if (!sonGonderim)
                        {
                            var basarili = await _emailService.SendBelgeUyariEmailAsync(ayar.EpostaAdresi!, uyarilar);

                            if (basarili)
                            {
                                // Log kaydı oluştur
                                context.Set<EpostaBildirimLog>().Add(new EpostaBildirimLog
                                {
                                    KullaniciId = ayar.KullaniciId,
                                    EpostaAdresi = ayar.EpostaAdresi!,
                                    UyariSayisi = uyarilar.Count,
                                    GonderimTarihi = DateTime.Now,
                                    Basarili = true
                                });

                                gonderimSayisi++;
                                _logger.LogInformation("E-posta bildirimi gönderildi: {Email} ({UyariSayisi} uyarı)", 
                                    ayar.EpostaAdresi, uyarilar.Count);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Son 24 saatte {Email} adresine e-posta gönderilmiş, atlanıyor.", ayar.EpostaAdresi);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kullanıcı {KullaniciId} için e-posta gönderilirken hata oluştu.", ayar.KullaniciId);
                }
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-posta bildirim gönderimi sırasında hata oluştu.");
        }

        _logger.LogInformation("E-posta bildirim gönderimi tamamlandı. {Sayi} kullanıcıya e-posta gönderildi.", gonderimSayisi);
        return gonderimSayisi;
    }

    public async Task<bool> TestEpostaGonderAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var ayar = await GetKullaniciAyarAsync(kullaniciId);
            if (ayar == null || string.IsNullOrEmpty(ayar.EpostaAdresi))
            {
                _logger.LogWarning("Kullanıcı {KullaniciId} için e-posta ayarı veya adresi bulunamadı.", kullaniciId);
                return false;
            }

            // Test uyarısı oluştur
            var testUyarilar = new List<BelgeUyariEmail>
            {
                new() { SahipAdi = "Test Araç: 34 ABC 123", BelgeAdi = "Trafik Sigortası", BitisTarihi = DateTime.Today.AddDays(5), GunKaldi = 5 },
                new() { SahipAdi = "Test Şoför: Ahmet Yılmaz", BelgeAdi = "Ehliyet", BitisTarihi = DateTime.Today.AddDays(10), GunKaldi = 10 },
                new() { SahipAdi = "Test Fatura: FTR-2025-001", BelgeAdi = "Ödenmemiş Fatura", BitisTarihi = DateTime.Today.AddDays(-2), GunKaldi = -2 }
            };

            return await _emailService.SendBelgeUyariEmailAsync(ayar.EpostaAdresi, testUyarilar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test e-postası gönderilirken hata oluştu.");
            return false;
        }
    }

    private static string GetBelgeTipiAdi(BildirimTipi tip) => tip switch
    {
        BildirimTipi.EhliyetBitis => "Ehliyet",
        BildirimTipi.SrcBelgesi => "SRC Belgesi",
        BildirimTipi.Psikoteknik => "Psikoteknik",
        BildirimTipi.SaglikRaporu => "Sağlık Raporu",
        BildirimTipi.TrafikSigorta => "Trafik Sigortası",
        BildirimTipi.Kasko => "Kasko",
        BildirimTipi.Muayene => "Araç Muayenesi",
        _ => tip.ToString()
    };

    #endregion
}


