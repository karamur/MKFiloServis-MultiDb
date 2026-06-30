using System.Diagnostics;
using System.Text.Json;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// EBYS Gelişmiş Belge Arama Servisi Implementasyonu
/// </summary>
public class EbysBelgeAramaService : IEbysBelgeAramaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<EbysBelgeAramaService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EbysBelgeAramaService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<EbysBelgeAramaService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    #region Ana Arama

    public async Task<EbysAramaSonuc> AraAsync(EbysGelismisAramaFiltre filtre)
    {
        var stopwatch = Stopwatch.StartNew();
        var sonuclar = new List<EbysAramaSonucItem>();

        try
        {
            // Kaynak seçimi - boşsa hepsinde ara
            var kaynaklar = filtre.Kaynaklar.Count > 0 
                ? filtre.Kaynaklar 
                : [EbysAramaKaynak.PersonelOzluk, EbysAramaKaynak.AracEvrak, EbysAramaKaynak.GelenEvrak, EbysAramaKaynak.GidenEvrak];

            // Her kaynakta paralel arama - her task için ayrı DbContext
            var tasks = new List<Task<List<EbysAramaSonucItem>>>();

            if (kaynaklar.Contains(EbysAramaKaynak.PersonelOzluk))
                tasks.Add(Task.Run(async () =>
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    return await AraPersonelOzlukAsync(context, filtre);
                }));

            if (kaynaklar.Contains(EbysAramaKaynak.AracEvrak))
                tasks.Add(Task.Run(async () =>
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    return await AraAracEvrakAsync(context, filtre);
                }));

            if (kaynaklar.Contains(EbysAramaKaynak.GelenEvrak))
                tasks.Add(Task.Run(async () =>
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    return await AraGelenEvrakAsync(context, filtre);
                }));

            if (kaynaklar.Contains(EbysAramaKaynak.GidenEvrak))
                tasks.Add(Task.Run(async () =>
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    return await AraGidenEvrakAsync(context, filtre);
                }));

            var tümSonuclar = await Task.WhenAll(tasks);
            foreach (var kaynak in tümSonuclar)
            {
                sonuclar.AddRange(kaynak);
            }

            // Alaka skoruna göre sıralama
            if (!string.IsNullOrWhiteSpace(filtre.AramaMetni))
            {
                HesaplaAlakaSkorlari(sonuclar, filtre.AramaMetni);
            }

            // Sıralama
            sonuclar = SiralaAsync(sonuclar, filtre.Siralama);

            // İstatistikler
            var istatistikler = HesaplaIstatistikler(sonuclar);

            // Sayfalama
            var toplamSonuc = sonuclar.Count;
            var sayfaliSonuclar = sonuclar
                .Skip((filtre.Sayfa - 1) * filtre.SayfaBoyutu)
                .Take(filtre.SayfaBoyutu)
                .ToList();

            stopwatch.Stop();

            return new EbysAramaSonuc
            {
                Sonuclar = sayfaliSonuclar,
                ToplamSonuc = toplamSonuc,
                Sayfa = filtre.Sayfa,
                SayfaBoyutu = filtre.SayfaBoyutu,
                AramaSuresi = stopwatch.Elapsed,
                Istatistikler = istatistikler
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EBYS arama hatası: {Filtre}", JsonSerializer.Serialize(filtre));
            throw;
        }
    }

    public async Task<EbysAramaSonuc> HizliAraAsync(string aramaMetni, int maxSonuc = 10)
    {
        var filtre = new EbysGelismisAramaFiltre
        {
            AramaMetni = aramaMetni,
            SayfaBoyutu = maxSonuc,
            Siralama = EbysAramaSiralama.Alaka
        };

        return await AraAsync(filtre);
    }

    public async Task<EbysAramaSonuc> KaynaktaAraAsync(EbysAramaKaynak kaynak, EbysGelismisAramaFiltre filtre)
    {
        filtre.Kaynaklar = [kaynak];
        return await AraAsync(filtre);
    }

    #endregion

    #region Kaynak Bazlı Arama

    private async Task<List<EbysAramaSonucItem>> AraPersonelOzlukAsync(ApplicationDbContext context, EbysGelismisAramaFiltre filtre)
    {
        var query = context.PersonelOzlukEvraklar
            .AsNoTracking()
            .Include(x => x.Sofor)
            .Include(x => x.EvrakTanim)
            .Where(x => !x.IsDeleted && !x.Sofor.IsDeleted);

        // Personel filtresi
        if (filtre.PersonelId.HasValue)
            query = query.Where(x => x.SoforId == filtre.PersonelId.Value);

        // Tarih filtreleri
        query = UygulaTarihFiltresi(query, filtre);

        // Durum filtreleri
        if (filtre.Durumlar.Count > 0)
        {
            var tamamlandi = filtre.Durumlar.Contains("Tamamlandı");
            var eksik = filtre.Durumlar.Contains("Eksik");
            if (tamamlandi && !eksik)
                query = query.Where(x => x.Tamamlandi);
            else if (eksik && !tamamlandi)
                query = query.Where(x => !x.Tamamlandi);
        }

        // Dosya filtresi
        if (filtre.SadeceDosyasiOlanlar == true)
            query = query.Where(x => !string.IsNullOrEmpty(x.DosyaYolu));

        var veriler = await query.ToListAsync();

        // Metin araması
        if (!string.IsNullOrWhiteSpace(filtre.AramaMetni))
        {
            var arama = filtre.AramaMetni.ToLower();
            veriler = veriler.Where(x => MetinEslesiyorMu(x, arama, filtre.AramaTipi)).ToList();
        }

        // Kategori filtresi
        if (filtre.Kategoriler.Count > 0)
        {
            veriler = veriler.Where(x => 
                filtre.Kategoriler.Any(k => 
                    GetOzlukKategoriAdi(x.EvrakTanim.Kategori).Equals(k, StringComparison.OrdinalIgnoreCase)
                )
            ).ToList();
        }

        return veriler.Select(x => new EbysAramaSonucItem
        {
            Kaynak = EbysAramaKaynak.PersonelOzluk,
            KayitId = x.Id,
            BelgeAdi = x.EvrakTanim.EvrakAdi,
            Kategori = GetOzlukKategoriAdi(x.EvrakTanim.Kategori),
            IlgiliKayitAdi = x.Sofor.TamAd,
            IlgiliKayitKodu = x.Sofor.SoforKodu,
            Aciklama = x.Aciklama,
            DosyaAdi = x.DosyaAdi,
            DosyaTipi = x.DosyaTipi,
            DosyaBoyutu = x.DosyaBoyutu,
            OlusturmaTarihi = x.CreatedAt,
            GuncellemeTarihi = x.UpdatedAt,
            Durum = x.Tamamlandi ? "Tamamlandı" : "Eksik",
            RiskDurumu = string.IsNullOrWhiteSpace(x.DosyaYolu) ? "Dosya Eksik" : "Normal",
            DosyaVar = !string.IsNullOrWhiteSpace(x.DosyaYolu),
            DetayUrl = "/personel/ozluk-evrak",
            EslesenMetin = BulEslesenMetin(x, filtre.AramaMetni)
        }).ToList();
    }

    private async Task<List<EbysAramaSonucItem>> AraAracEvrakAsync(ApplicationDbContext context, EbysGelismisAramaFiltre filtre)
    {
        var query = context.AracEvraklari
            .AsNoTracking()
            .Include(x => x.Arac)
            .Include(x => x.Dosyalar.Where(d => !d.IsDeleted))
            .Where(x => !x.IsDeleted && x.Arac != null && !x.Arac.IsDeleted);

        // Araç filtresi
        if (filtre.AracId.HasValue)
            query = query.Where(x => x.AracId == filtre.AracId.Value);

        // Tarih filtreleri
        if (filtre.BaslangicTarihi.HasValue && filtre.TarihAlani == EbysTarihAlani.BitisTarihi)
            query = query.Where(x => x.BitisTarihi >= filtre.BaslangicTarihi.Value);
        if (filtre.BitisTarihi.HasValue && filtre.TarihAlani == EbysTarihAlani.BitisTarihi)
            query = query.Where(x => x.BitisTarihi <= filtre.BitisTarihi.Value);

        // Süresi dolmuş filtresi
        if (filtre.SadeceSuresiDolmuslar == true)
            query = query.Where(x => x.BitisTarihi.HasValue && x.BitisTarihi.Value.Date < DateTime.Today);

        // Yaklaşan filtresi
        if (filtre.SadeceYaklasanlar == true)
        {
            var sinirTarih = DateTime.Today.AddDays(filtre.YaklasanGunSayisi ?? 30);
            query = query.Where(x => x.BitisTarihi.HasValue && 
                x.BitisTarihi.Value.Date >= DateTime.Today && 
                x.BitisTarihi.Value.Date <= sinirTarih);
        }

        var veriler = await query.ToListAsync();

        // Metin araması
        if (!string.IsNullOrWhiteSpace(filtre.AramaMetni))
        {
            var arama = filtre.AramaMetni.ToLower();
            veriler = veriler.Where(x => MetinEslesiyorMu(x, arama, filtre.AramaTipi)).ToList();
        }

        // Kategori filtresi
        if (filtre.Kategoriler.Count > 0)
        {
            veriler = veriler.Where(x => 
                filtre.Kategoriler.Any(k => 
                    x.EvrakKategorisi.Equals(k, StringComparison.OrdinalIgnoreCase)
                )
            ).ToList();
        }

        // Dosya filtresi
        if (filtre.SadeceDosyasiOlanlar == true)
            veriler = veriler.Where(x => x.Dosyalar.Any()).ToList();

        return veriler.Select(x =>
        {
            var aktifDosya = x.Dosyalar.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            var suresiDolmus = x.BitisTarihi.HasValue && x.BitisTarihi.Value.Date < DateTime.Today;
            var yaklasan = !suresiDolmus && x.BitisTarihi.HasValue && 
                x.BitisTarihi.Value.Date <= DateTime.Today.AddDays(filtre.YaklasanGunSayisi ?? 30);

            return new EbysAramaSonucItem
            {
                Kaynak = EbysAramaKaynak.AracEvrak,
                KayitId = x.Id,
                DosyaId = aktifDosya?.Id,
                BelgeAdi = string.IsNullOrWhiteSpace(x.EvrakAdi) ? x.EvrakKategorisi : x.EvrakAdi,
                Kategori = x.EvrakKategorisi,
                IlgiliKayitAdi = x.Arac?.AktifPlaka ?? x.Arac?.SaseNo ?? "Araç",
                IlgiliKayitKodu = x.Arac?.SaseNo ?? string.Empty,
                Aciklama = x.Aciklama,
                DosyaAdi = aktifDosya?.DosyaAdi,
                DosyaTipi = aktifDosya?.DosyaTipi,
                DosyaBoyutu = aktifDosya?.DosyaBoyutu,
                OlusturmaTarihi = x.CreatedAt,
                GuncellemeTarihi = x.UpdatedAt,
                BitisTarihi = x.BitisTarihi,
                Durum = GetAracDurumAdi(x.Durum),
                RiskDurumu = aktifDosya == null ? "Dosya Eksik" : suresiDolmus ? "Süresi Dolmuş" : yaklasan ? "Yaklaşan" : "Normal",
                YaklasanMi = yaklasan,
                SuresiDolmusMu = suresiDolmus,
                DosyaVar = aktifDosya != null,
                DetayUrl = x.AracId > 0 ? $"/araclar/{x.AracId}/evraklar" : "/araclar",
                EslesenMetin = BulEslesenMetin(x, filtre.AramaMetni)
            };
        }).ToList();
    }

    private async Task<List<EbysAramaSonucItem>> AraGelenEvrakAsync(ApplicationDbContext context, EbysGelismisAramaFiltre filtre)
    {
        return await AraEbysEvrakAsync(context, filtre, EvrakYonu.Gelen);
    }

    private async Task<List<EbysAramaSonucItem>> AraGidenEvrakAsync(ApplicationDbContext context, EbysGelismisAramaFiltre filtre)
    {
        return await AraEbysEvrakAsync(context, filtre, EvrakYonu.Giden);
    }

    private async Task<List<EbysAramaSonucItem>> AraEbysEvrakAsync(ApplicationDbContext context, EbysGelismisAramaFiltre filtre, EvrakYonu yon)
    {
        var query = context.EbysEvraklar
            .AsNoTracking()
            .Include(e => e.Kategori)
            .Include(e => e.AtananKullanici)
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Where(e => e.Yon == yon && !e.IsDeleted);

        // Tarih filtreleri
        if (filtre.BaslangicTarihi.HasValue)
            query = query.Where(e => e.EvrakTarihi >= filtre.BaslangicTarihi.Value);
        if (filtre.BitisTarihi.HasValue)
            query = query.Where(e => e.EvrakTarihi <= filtre.BitisTarihi.Value);

        // Kategori filtresi
        if (filtre.KategoriIdler.Count > 0)
            query = query.Where(e => e.KategoriId.HasValue && filtre.KategoriIdler.Contains(e.KategoriId.Value));

        // Kullanıcı filtresi
        if (filtre.KullaniciId.HasValue)
            query = query.Where(e => e.AtananKullaniciId == filtre.KullaniciId.Value);

        // Öncelik filtresi
        if (filtre.Oncelik.HasValue)
            query = query.Where(e => e.Oncelik == filtre.Oncelik.Value);

        // Gizlilik filtresi
        if (filtre.Gizlilik.HasValue)
            query = query.Where(e => e.Gizlilik == filtre.Gizlilik.Value);

        // Metin araması (veritabanı seviyesinde)
        if (!string.IsNullOrWhiteSpace(filtre.AramaMetni))
        {
            var arama = filtre.AramaMetni.ToLower();
            query = query.Where(e =>
                e.EvrakNo.ToLower().Contains(arama) ||
                e.Konu.ToLower().Contains(arama) ||
                (e.Ozet != null && e.Ozet.ToLower().Contains(arama)) ||
                (e.GonderenKurum != null && e.GonderenKurum.ToLower().Contains(arama)) ||
                (e.AliciKurum != null && e.AliciKurum.ToLower().Contains(arama)) ||
                (e.Aciklama != null && e.Aciklama.ToLower().Contains(arama))
            );
        }

        // Dosya filtresi
        if (filtre.SadeceDosyasiOlanlar == true)
            query = query.Where(e => e.Dosyalar.Any());

        var veriler = await query.ToListAsync();

        // Kategori filtresi (metin bazlı)
        if (filtre.Kategoriler.Count > 0)
        {
            veriler = veriler.Where(x =>
                x.Kategori != null && 
                filtre.Kategoriler.Any(k => x.Kategori.KategoriAdi.Equals(k, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        return veriler.Select(x =>
        {
            var aktifDosya = x.Dosyalar.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            var cevapGecikmis = x.CevapGerekli && x.CevapSuresi.HasValue && 
                x.CevapSuresi.Value < DateTime.Today &&
                x.Durum != EbysEvrakDurum.Cevaplandi && x.Durum != EbysEvrakDurum.Tamamlandi;

            return new EbysAramaSonucItem
            {
                Kaynak = yon == EvrakYonu.Gelen ? EbysAramaKaynak.GelenEvrak : EbysAramaKaynak.GidenEvrak,
                KayitId = x.Id,
                DosyaId = aktifDosya?.Id,
                BelgeAdi = x.Konu,
                Kategori = x.Kategori?.KategoriAdi ?? "Genel",
                IlgiliKayitAdi = yon == EvrakYonu.Gelen ? (x.GonderenKurum ?? "-") : (x.AliciKurum ?? "-"),
                IlgiliKayitKodu = x.EvrakNo,
                Aciklama = x.Aciklama,
                Ozet = x.Ozet,
                DosyaAdi = aktifDosya?.DosyaAdi,
                DosyaTipi = aktifDosya?.DosyaTipi,
                DosyaBoyutu = aktifDosya?.DosyaBoyutu,
                OlusturmaTarihi = x.CreatedAt,
                GuncellemeTarihi = x.UpdatedAt,
                BitisTarihi = x.CevapGerekli && x.CevapSuresi.HasValue 
                    ? x.CevapSuresi.Value 
                    : null,
                Durum = GetEbysEvrakDurumAdi(x.Durum),
                RiskDurumu = cevapGecikmis ? "Cevap Gecikmis" : "Normal",
                YaklasanMi = false,
                SuresiDolmusMu = cevapGecikmis,
                DosyaVar = aktifDosya != null,
                DetayUrl = $"/ebys/evrak/{x.Id}",
                EslesenMetin = BulEslesenMetin(x, filtre.AramaMetni)
            };
        }).ToList();
    }

    #endregion

    #region Arama Önerileri

    public async Task<List<EbysAramaOnerisi>> GetAramaOnerileriAsync(string metin, int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var oneriler = new List<EbysAramaOnerisi>();

        if (string.IsNullOrWhiteSpace(metin) || metin.Length < 2)
        {
            // Son aramalar
            var sonAramalar = await GetAramaGecmisiAsync(kullaniciId, 5);
            oneriler.AddRange(sonAramalar.Select(x => new EbysAramaOnerisi
            {
                Oneri = x.AramaMetni,
                Tip = EbysOneriTipi.SonArama,
                Skor = 100
            }));

            return oneriler;
        }

        var aramaLower = metin.ToLower();

        // Son aramalardan eşleşenler
        var gecmisOneriler = await context.Set<EbysAramaGecmisi>()
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId && x.AramaMetni.ToLower().Contains(aramaLower))
            .OrderByDescending(x => x.AramaTarihi)
            .Take(3)
            .Select(x => x.AramaMetni)
            .ToListAsync();

        oneriler.AddRange(gecmisOneriler.Select(x => new EbysAramaOnerisi
        {
            Oneri = x,
            Tip = EbysOneriTipi.SonArama,
            Skor = 90
        }));

        // Kategorilerden eşleşenler
        var kategoriler = await GetTumKategorilerAsync();
        var kategoriOnerileri = kategoriler
            .Where(k => k.ToLower().Contains(aramaLower))
            .Take(3)
            .Select(x => new EbysAramaOnerisi
            {
                Oneri = x,
                Tip = EbysOneriTipi.Kategori,
                Skor = 80
            });
        oneriler.AddRange(kategoriOnerileri);

        return oneriler.OrderByDescending(x => x.Skor).Take(10).ToList();
    }

    public async Task<List<string>> GetPopulerAramalarAsync(int adet = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<EbysAramaGecmisi>()
            .AsNoTracking()
            .Where(x => x.AramaTarihi > DateTime.Now.AddDays(-30))
            .GroupBy(x => x.AramaMetni.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(adet)
            .Select(g => g.First().AramaMetni)
            .ToListAsync();
    }

    public async Task<List<string>> GetIlgiliAramalarAsync(string aramaMetni)
    {
        if (string.IsNullOrWhiteSpace(aramaMetni))
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync();
        var kelimeler = aramaMetni.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return await context.Set<EbysAramaGecmisi>()
            .AsNoTracking()
            .Where(x => kelimeler.Any(k => x.AramaMetni.ToLower().Contains(k)))
            .GroupBy(x => x.AramaMetni)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToListAsync();
    }

    #endregion

    #region Arama Geçmişi

    public async Task<List<EbysAramaGecmisi>> GetAramaGecmisiAsync(int kullaniciId, int adet = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<EbysAramaGecmisi>()
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId && !x.IsDeleted)
            .OrderByDescending(x => x.AramaTarihi)
            .Take(adet)
            .ToListAsync();
    }

    public async Task KaydetAramaGecmisiAsync(int kullaniciId, EbysGelismisAramaFiltre filtre, int sonucSayisi)
    {
        if (string.IsNullOrWhiteSpace(filtre.AramaMetni))
            return;

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Aynı aramayı tekrar kaydetme
        var mevcutArama = await context.Set<EbysAramaGecmisi>()
            .Where(x => x.KullaniciId == kullaniciId && 
                x.AramaMetni.ToLower() == filtre.AramaMetni.ToLower() &&
                x.AramaTarihi > DateTime.Now.AddHours(-1))
            .FirstOrDefaultAsync();

        if (mevcutArama != null)
        {
            mevcutArama.AramaTarihi = DateTime.Now;
            mevcutArama.SonucSayisi = sonucSayisi;
        }
        else
        {
            var gecmis = new EbysAramaGecmisi
            {
                KullaniciId = kullaniciId,
                AramaMetni = filtre.AramaMetni.Trim(),
                FiltreJson = FiltreToJson(filtre),
                SonucSayisi = sonucSayisi,
                AramaTarihi = DateTime.Now
            };
            context.Set<EbysAramaGecmisi>().Add(gecmis);
        }

        await context.SaveChangesAsync();

        // Eski kayıtları temizle (max 100 kayıt)
        var eskiKayitlar = await context.Set<EbysAramaGecmisi>()
            .Where(x => x.KullaniciId == kullaniciId)
            .OrderByDescending(x => x.AramaTarihi)
            .Skip(100)
            .ToListAsync();

        if (eskiKayitlar.Count > 0)
        {
            context.Set<EbysAramaGecmisi>().RemoveRange(eskiKayitlar);
            await context.SaveChangesAsync();
        }
    }

    public async Task TemizleAramaGecmisiAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await context.Set<EbysAramaGecmisi>()
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        context.Set<EbysAramaGecmisi>().RemoveRange(kayitlar);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Kayıtlı Aramalar

    public async Task<List<EbysKayitliArama>> GetKayitliAramalarAsync(int kullaniciId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<EbysKayitliArama>()
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId && !x.IsDeleted)
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.AramaAdi)
            .ToListAsync();
    }

    public async Task<EbysKayitliArama> AramaKaydetAsync(int kullaniciId, string aramaAdi, EbysGelismisAramaFiltre filtre)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitli = new EbysKayitliArama
        {
            KullaniciId = kullaniciId,
            AramaAdi = aramaAdi,
            FiltreJson = FiltreToJson(filtre),
            SiraNo = await context.Set<EbysKayitliArama>()
                .Where(x => x.KullaniciId == kullaniciId)
                .CountAsync() + 1
        };

        context.Set<EbysKayitliArama>().Add(kayitli);
        await context.SaveChangesAsync();

        return kayitli;
    }

    public async Task SilKayitliAramaAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitli = await context.Set<EbysKayitliArama>().FindAsync(id);
        if (kayitli != null)
        {
            kayitli.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task GuncelleKayitliAramaAsync(EbysKayitliArama arama)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mevcut = await context.Set<EbysKayitliArama>().FindAsync(arama.Id);
        if (mevcut != null)
        {
            mevcut.AramaAdi = arama.AramaAdi;
            mevcut.Aciklama = arama.Aciklama;
            mevcut.FiltreJson = arama.FiltreJson;
            mevcut.BildirimAktif = arama.BildirimAktif;
            mevcut.SiraNo = arama.SiraNo;
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region İstatistikler

    public async Task<EbysAramaIstatistik> GetGenelIstatistiklerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var istatistik = new EbysAramaIstatistik();

        istatistik.PersonelOzlukSayisi = await context.PersonelOzlukEvraklar
            .CountAsync(x => !x.IsDeleted);

        istatistik.AracEvrakSayisi = await context.AracEvraklari
            .CountAsync(x => !x.IsDeleted);

        istatistik.GelenEvrakSayisi = await context.EbysEvraklar
            .CountAsync(x => !x.IsDeleted && x.Yon == EvrakYonu.Gelen);

        istatistik.GidenEvrakSayisi = await context.EbysEvraklar
            .CountAsync(x => !x.IsDeleted && x.Yon == EvrakYonu.Giden);

        istatistik.KategoriBazliSayilar = await GetKategoriBazliSayilarAsync();
        istatistik.RiskliSayisi = await GetRiskliBelgeSayisiAsync();

        return istatistik;
    }

    public async Task<Dictionary<string, int>> GetKategoriBazliSayilarAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuc = new Dictionary<string, int>();

        // Araç evrak kategorileri
        var aracKategoriler = await context.AracEvraklari
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.EvrakKategorisi)
            .Select(g => new { Kategori = g.Key, Sayi = g.Count() })
            .ToListAsync();

        foreach (var k in aracKategoriler)
        {
            if (!string.IsNullOrWhiteSpace(k.Kategori))
                sonuc[k.Kategori] = k.Sayi;
        }

        // EBYS evrak kategorileri
        var ebysKategoriler = await context.EbysEvraklar
            .Where(x => !x.IsDeleted && x.KategoriId.HasValue)
            .Include(x => x.Kategori)
            .GroupBy(x => x.Kategori!.KategoriAdi)
            .Select(g => new { Kategori = g.Key, Sayi = g.Count() })
            .ToListAsync();

        foreach (var k in ebysKategoriler)
        {
            if (!string.IsNullOrWhiteSpace(k.Kategori))
            {
                if (sonuc.ContainsKey(k.Kategori))
                    sonuc[k.Kategori] += k.Sayi;
                else
                    sonuc[k.Kategori] = k.Sayi;
            }
        }

        return sonuc;
    }

    public async Task<int> GetRiskliBelgeSayisiAsync(int yaklasanGunSayisi = 30)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sinirTarih = DateTime.Today.AddDays(yaklasanGunSayisi);

        var aracRiskli = await context.AracEvraklari
            .CountAsync(x => !x.IsDeleted && x.BitisTarihi.HasValue && 
                x.BitisTarihi.Value.Date <= sinirTarih);

        return aracRiskli;
    }

    #endregion

    #region Yardımcı Metodlar

    public async Task<List<string>> GetTumKategorilerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kategoriler = new List<string>();

        // Araç evrak kategorileri
        var aracKategoriler = await context.AracEvraklari
            .Where(x => !x.IsDeleted)
            .Select(x => x.EvrakKategorisi)
            .Distinct()
            .ToListAsync();
        kategoriler.AddRange(aracKategoriler.Where(k => !string.IsNullOrWhiteSpace(k))!);

        // EBYS evrak kategorileri
        var ebysKategoriler = await context.EbysEvrakKategoriler
            .Where(x => !x.IsDeleted && x.Aktif)
            .Select(x => x.KategoriAdi)
            .ToListAsync();
        kategoriler.AddRange(ebysKategoriler);

        // Personel özlük kategorileri
        kategoriler.AddRange(["Kimlik", "Ehliyet", "Sertifika", "Sağlık", "Sözleşme", "Diğer"]);

        return kategoriler.Distinct().OrderBy(x => x).ToList();
    }

    public EbysGelismisAramaFiltre? FiltreOlustur(string filtreJson)
    {
        if (string.IsNullOrWhiteSpace(filtreJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<EbysGelismisAramaFiltre>(filtreJson, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public string FiltreToJson(EbysGelismisAramaFiltre filtre)
    {
        return JsonSerializer.Serialize(filtre, _jsonOptions);
    }

    private static string GetOzlukKategoriAdi(OzlukEvrakKategori kategori)
    {
        return kategori switch
        {
            OzlukEvrakKategori.Genel => "Genel",
            OzlukEvrakKategori.KimlikBelgeleri => "Kimlik Belgeleri",
            OzlukEvrakKategori.EgitimBelgeleri => "Eğitim Belgeleri",
            OzlukEvrakKategori.SaglikBelgeleri => "Sağlık Belgeleri",
            OzlukEvrakKategori.SoforBelgeleri => "Şoför Belgeleri",
            OzlukEvrakKategori.SGKBelgeleri => "SGK Belgeleri",
            OzlukEvrakKategori.IseGirisBelgeleri => "İşe Giriş Belgeleri",
            OzlukEvrakKategori.Diger => "Diğer",
            _ => "Diğer"
        };
    }

    private static string GetAracDurumAdi(EvrakDurum durum)
    {
        return durum switch
        {
            EvrakDurum.Aktif => "Aktif",
            EvrakDurum.Pasif => "Pasif",
            EvrakDurum.SuresiDolmus => "Süresi Dolmuş",
            _ => "Aktif"
        };
    }

    private static string GetEbysEvrakDurumAdi(EbysEvrakDurum durum)
    {
        return durum switch
        {
            EbysEvrakDurum.Taslak => "Taslak",
            EbysEvrakDurum.Beklemede => "Beklemede",
            EbysEvrakDurum.Isleniyor => "İşleniyor",
            EbysEvrakDurum.AtamaBekliyor => "Atama Bekliyor",
            EbysEvrakDurum.CevapBekliyor => "Cevap Bekliyor",
            EbysEvrakDurum.Cevaplandi => "Cevaplandı",
            EbysEvrakDurum.Tamamlandi => "Tamamlandı",
            EbysEvrakDurum.Arsivlendi => "Arşivlendi",
            _ => "Beklemede"
        };
    }

    private bool MetinEslesiyorMu(PersonelOzlukEvrak evrak, string arama, EbysAramaTipi tip)
    {
        return tip switch
        {
            EbysAramaTipi.BelgeAdi => evrak.EvrakTanim.EvrakAdi.ToLower().Contains(arama),
            EbysAramaTipi.DosyaAdi => !string.IsNullOrWhiteSpace(evrak.DosyaAdi) && evrak.DosyaAdi.ToLower().Contains(arama),
            EbysAramaTipi.Aciklama => !string.IsNullOrWhiteSpace(evrak.Aciklama) && evrak.Aciklama.ToLower().Contains(arama),
            EbysAramaTipi.Kategori => GetOzlukKategoriAdi(evrak.EvrakTanim.Kategori).ToLower().Contains(arama),
            EbysAramaTipi.IlgiliKayit => evrak.Sofor.TamAd.ToLower().Contains(arama) || evrak.Sofor.SoforKodu.ToLower().Contains(arama),
            _ => evrak.EvrakTanim.EvrakAdi.ToLower().Contains(arama) ||
                 evrak.Sofor.TamAd.ToLower().Contains(arama) ||
                 (!string.IsNullOrWhiteSpace(evrak.DosyaAdi) && evrak.DosyaAdi.ToLower().Contains(arama)) ||
                 (!string.IsNullOrWhiteSpace(evrak.Aciklama) && evrak.Aciklama.ToLower().Contains(arama))
        };
    }

    private bool MetinEslesiyorMu(AracEvrak evrak, string arama, EbysAramaTipi tip)
    {
        var belgeAdi = string.IsNullOrWhiteSpace(evrak.EvrakAdi) ? evrak.EvrakKategorisi : evrak.EvrakAdi;
        
        return tip switch
        {
            EbysAramaTipi.BelgeAdi => belgeAdi.ToLower().Contains(arama),
            EbysAramaTipi.DosyaAdi => evrak.Dosyalar.Any(d => d.DosyaAdi.ToLower().Contains(arama)),
            EbysAramaTipi.Aciklama => !string.IsNullOrWhiteSpace(evrak.Aciklama) && evrak.Aciklama.ToLower().Contains(arama),
            EbysAramaTipi.Kategori => evrak.EvrakKategorisi.ToLower().Contains(arama),
            EbysAramaTipi.IlgiliKayit => (evrak.Arac?.AktifPlaka ?? "").ToLower().Contains(arama) || (evrak.Arac?.SaseNo ?? "").ToLower().Contains(arama),
            _ => belgeAdi.ToLower().Contains(arama) ||
                 evrak.EvrakKategorisi.ToLower().Contains(arama) ||
                 (evrak.Arac?.AktifPlaka ?? "").ToLower().Contains(arama) ||
                 (!string.IsNullOrWhiteSpace(evrak.Aciklama) && evrak.Aciklama.ToLower().Contains(arama)) ||
                 evrak.Dosyalar.Any(d => d.DosyaAdi.ToLower().Contains(arama))
        };
    }

    private static string? BulEslesenMetin(object evrak, string? aramaMetni)
    {
        if (string.IsNullOrWhiteSpace(aramaMetni))
            return null;

        // Basit eşleşme vurgulama - geliştirilecek
        return null;
    }

    private IQueryable<PersonelOzlukEvrak> UygulaTarihFiltresi(
        IQueryable<PersonelOzlukEvrak> query, 
        EbysGelismisAramaFiltre filtre)
    {
        if (filtre.BaslangicTarihi.HasValue)
        {
            query = filtre.TarihAlani switch
            {
                EbysTarihAlani.OlusturmaTarihi => query.Where(x => x.CreatedAt >= filtre.BaslangicTarihi.Value),
                EbysTarihAlani.GuncellemeTarihi => query.Where(x => x.UpdatedAt >= filtre.BaslangicTarihi.Value),
                _ => query
            };
        }

        if (filtre.BitisTarihi.HasValue)
        {
            query = filtre.TarihAlani switch
            {
                EbysTarihAlani.OlusturmaTarihi => query.Where(x => x.CreatedAt <= filtre.BitisTarihi.Value),
                EbysTarihAlani.GuncellemeTarihi => query.Where(x => x.UpdatedAt <= filtre.BitisTarihi.Value),
                _ => query
            };
        }

        return query;
    }

    private void HesaplaAlakaSkorlari(List<EbysAramaSonucItem> sonuclar, string aramaMetni)
    {
        var aramaLower = aramaMetni.ToLower();
        var kelimeler = aramaLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var sonuc in sonuclar)
        {
            double skor = 0;

            // Belge adı eşleşmesi (yüksek ağırlık)
            if (sonuc.BelgeAdi.ToLower().Contains(aramaLower))
                skor += 100;
            else if (kelimeler.Any(k => sonuc.BelgeAdi.ToLower().Contains(k)))
                skor += 50;

            // Kategori eşleşmesi
            if (sonuc.Kategori.ToLower().Contains(aramaLower))
                skor += 40;

            // İlgili kayıt eşleşmesi
            if (sonuc.IlgiliKayitAdi.ToLower().Contains(aramaLower))
                skor += 30;

            // Açıklama eşleşmesi
            if (!string.IsNullOrWhiteSpace(sonuc.Aciklama) && sonuc.Aciklama.ToLower().Contains(aramaLower))
                skor += 20;

            // Dosya adı eşleşmesi
            if (!string.IsNullOrWhiteSpace(sonuc.DosyaAdi) && sonuc.DosyaAdi.ToLower().Contains(aramaLower))
                skor += 10;

            sonuc.AlakaSkoru = skor;
        }
    }

    private static List<EbysAramaSonucItem> SiralaAsync(List<EbysAramaSonucItem> sonuclar, EbysAramaSiralama siralama)
    {
        return siralama switch
        {
            EbysAramaSiralama.TarihAzalan => sonuclar.OrderByDescending(x => x.OlusturmaTarihi ?? DateTime.MinValue).ToList(),
            EbysAramaSiralama.TarihArtan => sonuclar.OrderBy(x => x.OlusturmaTarihi ?? DateTime.MaxValue).ToList(),
            EbysAramaSiralama.AdAZ => sonuclar.OrderBy(x => x.BelgeAdi).ToList(),
            EbysAramaSiralama.AdZA => sonuclar.OrderByDescending(x => x.BelgeAdi).ToList(),
            EbysAramaSiralama.Alaka => sonuclar.OrderByDescending(x => x.AlakaSkoru).ThenByDescending(x => x.OlusturmaTarihi).ToList(),
            EbysAramaSiralama.KategoriAZ => sonuclar.OrderBy(x => x.Kategori).ThenBy(x => x.BelgeAdi).ToList(),
            _ => sonuclar
        };
    }

    private static EbysAramaIstatistik HesaplaIstatistikler(List<EbysAramaSonucItem> sonuclar)
    {
        return new EbysAramaIstatistik
        {
            PersonelOzlukSayisi = sonuclar.Count(x => x.Kaynak == EbysAramaKaynak.PersonelOzluk),
            AracEvrakSayisi = sonuclar.Count(x => x.Kaynak == EbysAramaKaynak.AracEvrak),
            GelenEvrakSayisi = sonuclar.Count(x => x.Kaynak == EbysAramaKaynak.GelenEvrak),
            GidenEvrakSayisi = sonuclar.Count(x => x.Kaynak == EbysAramaKaynak.GidenEvrak),
            KategoriBazliSayilar = sonuclar
                .GroupBy(x => x.Kategori)
                .ToDictionary(g => g.Key, g => g.Count()),
            DurumBazliSayilar = sonuclar
                .GroupBy(x => x.Durum)
                .ToDictionary(g => g.Key, g => g.Count()),
            DosyasiOlanSayisi = sonuclar.Count(x => x.DosyaVar),
            RiskliSayisi = sonuclar.Count(x => x.SuresiDolmusMu || x.YaklasanMi)
        };
    }

    #endregion
}



