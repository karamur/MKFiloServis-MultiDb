using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Data;

/// <summary>
/// Demo ve test amaçlı örnek veri oluşturma servisi
/// </summary>
public class TestDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestDataSeeder> _logger;
    private readonly Random _random = new();

    // Türkçe isimler
    private readonly string[] _erkekAdlari = { "Ahmet", "Mehmet", "Ali", "Mustafa", "Hüseyin", "İbrahim", "Hasan", "Osman", "Yusuf", "Kemal", "Murat", "Emre", "Burak", "Serkan", "Fatih" };
    private readonly string[] _kadinAdlari = { "Fatma", "Ayşe", "Zeynep", "Emine", "Hatice", "Elif", "Merve", "Büşra", "Seda", "Esra" };
    private readonly string[] _soyadlari = { "Yılmaz", "Kaya", "Demir", "Şahin", "Çelik", "Yıldız", "Arslan", "Koç", "Aydın", "Özdemir", "Kurt", "Aslan", "Erdoğan", "Kılıç", "Polat" };

    // İstanbul ilçeleri
    private readonly string[] _ilceler = { "Kadıköy", "Beşiktaş", "Şişli", "Bakırköy", "Ataşehir", "Üsküdar", "Maltepe", "Kartal", "Pendik", "Tuzla", "Beylikdüzü", "Esenyurt", "Başakşehir", "Sarıyer", "Beykoz" };

    public TestDataSeeder(ApplicationDbContext context, ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tüm örnek verileri oluşturur
    /// </summary>
    public async Task<TestDataResult> SeedAllAsync(bool silinenleriTemizle = false)
    {
        var result = new TestDataResult();

        try
        {
            if (silinenleriTemizle)
            {
                await TemizleAsync();
                result.Mesajlar.Add("Mevcut test verileri temizlendi");
            }

            // Sırayla oluştur (bağımlılıklar nedeniyle)
            result.CariSayisi = await SeedCarilerAsync();
            result.SoforSayisi = await SeedSoforlerAsync();
            result.AracSayisi = await SeedAraclarAsync();
            result.GuzergahSayisi = await SeedGuzergahlarAsync();
            result.FaturaSayisi = await SeedFaturalarAsync();
            result.ServisCalismasiSayisi = await SeedServisCalismalarıAsync();
            result.IhaleHazirlikSayisi = await SeedIhaleHazirlikAsync();
            result.ProformaFaturaSayisi = await SeedProformaFaturalarAsync();
            result.PuantajKayitSayisi = await SeedPuantajKayitlarAsync();

            result.Basarili = true;
            result.Mesajlar.Add($"Toplam {result.ToplamKayit} örnek kayıt oluşturuldu");

            _logger.LogInformation("Test verileri oluşturuldu: {ToplamKayit} kayıt", result.ToplamKayit);
        }
        catch (Exception ex)
        {
            result.Basarili = false;
            result.Mesajlar.Add($"Hata: {ex.Message}");
            _logger.LogError(ex, "Test verileri oluşturulurken hata");
        }

        return result;
    }

    /// <summary>
    /// Test verilerini temizler (sadece test verisi olarak işaretlenenler)
    /// </summary>
    public async Task TemizleAsync()
    {
        // Puantaj Kayıtları
        var puantajlar = await _context.PuantajKayitlar
            .Where(x => x.GuzergahAdi != null && x.GuzergahAdi.Contains("[TEST]"))
            .ToListAsync();
        _context.PuantajKayitlar.RemoveRange(puantajlar);

        // Proforma Faturalar ve kalemleri
        var proformalari = await _context.ProformaFaturalar
            .Include(x => x.Kalemler)
            .Where(x => x.Aciklama != null && x.Aciklama.Contains("[TEST]"))
            .ToListAsync();
        foreach (var pf in proformalari)
        {
            _context.ProformaFaturaKalemler.RemoveRange(pf.Kalemler);
        }
        _context.ProformaFaturalar.RemoveRange(proformalari);

        // İhale projeleri ve kalemleri
        var ihaleler = await _context.IhaleProjeleri
            .Include(x => x.Kalemler)
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        foreach (var ihale in ihaleler)
        {
            _context.IhaleGuzergahKalemleri.RemoveRange(ihale.Kalemler);
        }
        _context.IhaleProjeleri.RemoveRange(ihaleler);

        // Servis çalışmaları
        var servisler = await _context.ServisCalismalari
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        _context.ServisCalismalari.RemoveRange(servisler);

        // Faturalar ve kalemleri
        var faturalar = await _context.Faturalar
            .Include(x => x.FaturaKalemleri)
            .Where(x => x.Aciklama != null && x.Aciklama.Contains("[TEST]"))
            .ToListAsync();
        foreach (var fatura in faturalar)
        {
            _context.FaturaKalemleri.RemoveRange(fatura.FaturaKalemleri);
        }
        _context.Faturalar.RemoveRange(faturalar);

        // Güzergahlar
        var guzergahlar = await _context.Guzergahlar
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        _context.Guzergahlar.RemoveRange(guzergahlar);

        // Araçlar
        var araclar = await _context.Araclar
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        _context.Araclar.RemoveRange(araclar);

        // Şoförler
        var soforler = await _context.Soforler
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        _context.Soforler.RemoveRange(soforler);

        // Cariler
        var cariler = await _context.Cariler
            .Where(x => x.Notlar != null && x.Notlar.Contains("[TEST]"))
            .ToListAsync();
        _context.Cariler.RemoveRange(cariler);

        await _context.SaveChangesAsync();
        _logger.LogInformation("Test verileri temizlendi");
    }

    #region Cari Seed

    private async Task<int> SeedCarilerAsync()
    {
        if (await _context.Cariler.AnyAsync(c => c.Notlar != null && c.Notlar.Contains("[TEST]")))
            return 0;

        var firma = await _context.Firmalar.FirstOrDefaultAsync();
        var cariler = new List<Cari>();

        // 10 Müşteri
        var musteriAdlari = new[] { "ABC Lojistik", "XYZ Taşımacılık", "Mega Transport", "Hızlı Kargo", "Güven Nakliyat", 
            "Star Ulaşım", "Yıldız Servis", "İstanbul Filo", "Anadolu Transit", "Marmara Taşıma" };

        for (int i = 0; i < musteriAdlari.Length; i++)
        {
            cariler.Add(new Cari
            {
                FirmaId = firma?.Id,
                CariKodu = $"MUS{(i + 1):D4}",
                Unvan = musteriAdlari[i] + " A.Ş.",
                CariTipi = CariTipi.Musteri,
                VergiDairesi = "Kadıköy VD",
                VergiNo = $"{_random.Next(100, 999)}{_random.Next(1000000, 9999999)}",
                Adres = $"{_ilceler[i % _ilceler.Length]} / İstanbul",
                Telefon = $"0216 {_random.Next(100, 999)} {_random.Next(10, 99)} {_random.Next(10, 99)}",
                Email = $"info@{musteriAdlari[i].ToLower().Replace(" ", "")}.com.tr",
                YetkiliKisi = RastgeleIsim(),
                Aktif = true,
                Notlar = "[TEST] Demo müşteri verisi",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 365))
            });
        }

        // 5 Tedarikçi
        var tedarikciAdlari = new[] { "Petrol Ofisi", "BP Akaryakıt", "Shell Türkiye", "Oto Yedek Parça", "Lastik Dünyası" };

        for (int i = 0; i < tedarikciAdlari.Length; i++)
        {
            cariler.Add(new Cari
            {
                FirmaId = firma?.Id,
                CariKodu = $"TED{(i + 1):D4}",
                Unvan = tedarikciAdlari[i] + " Ltd. Şti.",
                CariTipi = CariTipi.Tedarikci,
                VergiDairesi = "Beşiktaş VD",
                VergiNo = $"{_random.Next(100, 999)}{_random.Next(1000000, 9999999)}",
                Adres = $"{_ilceler[(i + 5) % _ilceler.Length]} / İstanbul",
                Telefon = $"0212 {_random.Next(100, 999)} {_random.Next(10, 99)} {_random.Next(10, 99)}",
                Email = $"satis@{tedarikciAdlari[i].ToLower().Replace(" ", "")}.com.tr",
                YetkiliKisi = RastgeleIsim(),
                Aktif = true,
                Notlar = "[TEST] Demo tedarikçi verisi",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 365))
            });
        }

        _context.Cariler.AddRange(cariler);
        await _context.SaveChangesAsync();
        return cariler.Count;
    }

    #endregion

    #region Şoför Seed

    private async Task<int> SeedSoforlerAsync()
    {
        if (await _context.Soforler.AnyAsync(s => s.Notlar != null && s.Notlar.Contains("[TEST]")))
            return 0;

        var soforler = new List<Sofor>();

        for (int i = 0; i < 15; i++)
        {
            var erkek = _random.Next(100) < 90; // %90 erkek
            var ad = erkek ? _erkekAdlari[_random.Next(_erkekAdlari.Length)] : _kadinAdlari[_random.Next(_kadinAdlari.Length)];
            var soyad = _soyadlari[_random.Next(_soyadlari.Length)];

            soforler.Add(new Sofor
            {
                SoforKodu = $"SFR{(i + 1):D3}",
                Ad = ad,
                Soyad = soyad,
                TcKimlikNo = $"{_random.Next(10000, 99999)}{_random.Next(100000, 999999)}",
                Telefon = $"05{_random.Next(30, 59)} {_random.Next(100, 999)} {_random.Next(10, 99)} {_random.Next(10, 99)}",
                Email = $"{ad.ToLower()}.{soyad.ToLower()}@email.com",
                Adres = $"{_ilceler[_random.Next(_ilceler.Length)]} / İstanbul",
                Gorev = PersonelGorev.Sofor,
                EhliyetNo = $"{_random.Next(10, 99)}{ad.Substring(0, 2).ToUpper()}{_random.Next(10000, 99999)}",
                EhliyetGecerlilikTarihi = DateTime.Today.AddMonths(_random.Next(6, 60)),
                SrcBelgesiGecerlilikTarihi = _random.Next(100) < 80 ? DateTime.Today.AddMonths(_random.Next(6, 36)) : null,
                PsikoteknikGecerlilikTarihi = DateTime.Today.AddMonths(_random.Next(6, 24)),
                SaglikRaporuGecerlilikTarihi = DateTime.Today.AddMonths(_random.Next(6, 12)),
                IseBaslamaTarihi = DateTime.Today.AddDays(-_random.Next(30, 1825)), // Son 5 yıl
                NetMaas = _random.Next(25, 45) * 1000m,
                Aktif = _random.Next(100) < 90, // %90 aktif
                Notlar = "[TEST] Demo şoför verisi",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 365))
            });
        }

        _context.Soforler.AddRange(soforler);
        await _context.SaveChangesAsync();
        return soforler.Count;
    }

    #endregion

    #region Araç Seed

    private async Task<int> SeedAraclarAsync()
    {
        if (await _context.Araclar.AnyAsync(a => a.Notlar != null && a.Notlar.Contains("[TEST]")))
            return 0;

        var araclar = new List<Arac>();
        var plakaHarfler = new[] { "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AJ", "AK" };
        var markalar = new[] { "Mercedes-Benz", "Ford", "Volkswagen", "Fiat", "Hyundai", "Iveco" };
        var modeller = new[] { "Sprinter", "Transit", "Crafter", "Ducato", "H350", "Daily" };

        for (int i = 0; i < 12; i++)
        {
            var yil = _random.Next(2018, 2025);
            var sahiplikTipi = (AracSahiplikTipi)(_random.Next(1, 4)); // 1-3

            var arac = new Arac
            {
                SaseNo = $"WDB{_random.Next(100000000, 999999999)}{_random.Next(100000, 999999)}",
                AktifPlaka = $"34 {plakaHarfler[i % plakaHarfler.Length]} {_random.Next(100, 999)}",
                Marka = markalar[_random.Next(markalar.Length)],
                Model = modeller[_random.Next(modeller.Length)],
                ModelYili = yil,
                AracTipi = (AracTipi)_random.Next(1, 5),
                SahiplikTipi = sahiplikTipi,
                KoltukSayisi = new[] { 9, 14, 16, 20, 27, 46 }[_random.Next(6)],
                KmDurumu = _random.Next(10000, 350000),
                TrafikSigortaBitisTarihi = DateTime.Today.AddMonths(_random.Next(-1, 12)),
                KaskoBitisTarihi = _random.Next(100) < 70 ? DateTime.Today.AddMonths(_random.Next(-1, 12)) : null,
                MuayeneBitisTarihi = DateTime.Today.AddMonths(_random.Next(-1, 24)),
                Aktif = _random.Next(100) < 90,
                Notlar = "[TEST] Demo araç verisi",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 365))
            };

            // Kiralık araçlar için kira bilgisi
            if (sahiplikTipi == AracSahiplikTipi.Kiralik)
            {
                arac.AylikKiraBedeli = _random.Next(15, 40) * 1000m;
            }

            araclar.Add(arac);
        }

        _context.Araclar.AddRange(araclar);
        await _context.SaveChangesAsync();
        return araclar.Count;
    }

    #endregion

    #region Güzergah Seed

    private async Task<int> SeedGuzergahlarAsync()
    {
        if (await _context.Guzergahlar.AnyAsync(g => g.Notlar != null && g.Notlar.Contains("[TEST]")))
            return 0;

        var musteriler = await _context.Cariler.Where(c => c.CariTipi == CariTipi.Musteri && c.Aktif).ToListAsync();
        if (!musteriler.Any())
        {
            _logger.LogWarning("Müşteri bulunamadı, güzergah seed atlanıyor");
            return 0;
        }

        var guzergahlar = new List<Guzergah>();

        // İstanbul'daki önemli noktalar (lat, lng)
        var noktalar = new (string Ad, double Lat, double Lng)[]
        {
            ("Kadıköy", 40.9908, 29.0259),
            ("Beşiktaş", 41.0422, 29.0067),
            ("Taksim", 41.0370, 28.9850),
            ("Şişli", 41.0602, 28.9877),
            ("Levent", 41.0819, 29.0131),
            ("Maslak", 41.1086, 29.0200),
            ("Ataşehir", 40.9923, 29.1244),
            ("Kartal", 40.8893, 29.1856),
            ("Pendik", 40.8756, 29.2333),
            ("Bakırköy", 40.9798, 28.8717),
            ("Beylikdüzü", 41.0022, 28.6444),
            ("Esenyurt", 41.0333, 28.6778)
        };

        var renkler = new[] { "#3498db", "#e74c3c", "#2ecc71", "#f39c12", "#9b59b6", "#1abc9c", "#e67e22", "#34495e" };

        for (int i = 0; i < 8; i++)
        {
            var baslangic = noktalar[_random.Next(noktalar.Length)];
            var bitis = noktalar.Where(n => n.Ad != baslangic.Ad).ElementAt(_random.Next(noktalar.Length - 1));
            var mesafe = Math.Round(5 + _random.NextDouble() * 35, 1); // 5-40 km
            var musteri = musteriler[_random.Next(musteriler.Count)];

            guzergahlar.Add(new Guzergah
            {
                GuzergahKodu = $"GZR{(i + 1):D3}",
                GuzergahAdi = $"{baslangic.Ad} - {bitis.Ad}",
                BaslangicNoktasi = baslangic.Ad,
                BitisNoktasi = bitis.Ad,
                Mesafe = (decimal)mesafe,
                TahminiSure = (int)(mesafe * 2.5), // Ortalama 25 km/saat
                BaslangicLatitude = baslangic.Lat,
                BaslangicLongitude = baslangic.Lng,
                BitisLatitude = bitis.Lat,
                BitisLongitude = bitis.Lng,
                RotaRengi = renkler[i % renkler.Length],
                BirimFiyat = _random.Next(300, 1000),
                CariId = musteri.Id,
                Aktif = true,
                Notlar = "[TEST] Demo güzergah verisi",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30, 180))
            });
        }

        _context.Guzergahlar.AddRange(guzergahlar);
        await _context.SaveChangesAsync();
        return guzergahlar.Count;
    }

    #endregion

    #region Fatura Seed

    private async Task<int> SeedFaturalarAsync()
    {
        if (await _context.Faturalar.AnyAsync(f => f.Aciklama != null && f.Aciklama.Contains("[TEST]")))
            return 0;

        var musteriler = await _context.Cariler.Where(c => c.CariTipi == CariTipi.Musteri && c.Aktif).ToListAsync();
        var tedarikciler = await _context.Cariler.Where(c => c.CariTipi == CariTipi.Tedarikci && c.Aktif).ToListAsync();
        var guzergahlar = await _context.Guzergahlar.Where(g => g.Aktif).ToListAsync();

        if (!musteriler.Any())
        {
            _logger.LogWarning("Müşteri bulunamadı, fatura seed atlanıyor");
            return 0;
        }

        var faturalar = new List<Fatura>();

        // Son 6 aylık satış faturaları
        for (int i = 0; i < 30; i++)
        {
            var musteri = musteriler[_random.Next(musteriler.Count)];
            var tarih = DateTime.Today.AddDays(-_random.Next(0, 180));
            var kalemSayisi = _random.Next(1, 5);

            var fatura = new Fatura
            {
                FaturaNo = $"SF{tarih:yyyyMM}-{(i + 1):D4}",
                FaturaTarihi = tarih,
                VadeTarihi = tarih.AddDays(30),
                CariId = musteri.Id,
                FaturaYonu = FaturaYonu.Giden,
                FaturaTipi = FaturaTipi.SatisFaturasi,
                EFaturaTipi = EFaturaTipi.EFatura,
                Durum = tarih < DateTime.Today.AddDays(-45) ? FaturaDurum.Odendi : 
                        tarih < DateTime.Today.AddDays(-15) ? FaturaDurum.KismiOdendi : FaturaDurum.Beklemede,
                Aciklama = "[TEST] Demo satış faturası",
                CreatedAt = tarih
            };

            // Fatura kalemleri
            for (int k = 0; k < kalemSayisi; k++)
            {
                var guzergah = guzergahlar.Any() ? guzergahlar[_random.Next(guzergahlar.Count)] : null;
                var birimFiyat = _random.Next(500, 3000);
                var miktar = _random.Next(1, 20);
                var netTutar = birimFiyat * miktar;
                var kdvTutar = netTutar * 0.20m;

                var kalem = new FaturaKalem
                {
                    SiraNo = k + 1,
                    Aciklama = guzergah != null ? $"Personel Servis - {guzergah.GuzergahAdi}" : "Personel Servis Hizmeti",
                    Miktar = miktar,
                    BirimFiyat = birimFiyat,
                    KdvOrani = 20,
                    KdvTutar = kdvTutar,
                    ToplamTutar = netTutar + kdvTutar,
                    CreatedAt = tarih
                };

                fatura.FaturaKalemleri.Add(kalem);
            }

            fatura.AraToplam = fatura.FaturaKalemleri.Sum(k => k.Miktar * k.BirimFiyat);
            fatura.KdvTutar = fatura.FaturaKalemleri.Sum(k => k.KdvTutar);
            fatura.GenelToplam = fatura.FaturaKalemleri.Sum(k => k.ToplamTutar);

            if (fatura.Durum == FaturaDurum.Odendi)
                fatura.OdenenTutar = fatura.GenelToplam;
            else if (fatura.Durum == FaturaDurum.KismiOdendi)
                fatura.OdenenTutar = Math.Round(fatura.GenelToplam * (decimal)(_random.Next(30, 70) / 100.0), 2);

            faturalar.Add(fatura);
        }

        // Son 6 aylık alış faturaları (tedarikçilerden)
        if (tedarikciler.Any())
        {
            for (int i = 0; i < 15; i++)
            {
                var tedarikci = tedarikciler[_random.Next(tedarikciler.Count)];
                var tarih = DateTime.Today.AddDays(-_random.Next(0, 180));

                var fatura = new Fatura
                {
                    FaturaNo = $"AF{tarih:yyyyMM}-{(i + 1):D4}",
                    FaturaTarihi = tarih,
                    VadeTarihi = tarih.AddDays(30),
                    CariId = tedarikci.Id,
                    FaturaYonu = FaturaYonu.Gelen,
                    FaturaTipi = FaturaTipi.AlisFaturasi,
                    EFaturaTipi = EFaturaTipi.EFatura,
                    Durum = tarih < DateTime.Today.AddDays(-30) ? FaturaDurum.Odendi : FaturaDurum.Beklemede,
                    Aciklama = "[TEST] Demo alış faturası",
                    CreatedAt = tarih
                };

                // Tek kalem (yakıt, bakım vb.)
                var aciklamalar = new[] { "Yakıt Alımı", "Araç Bakım", "Lastik Değişimi", "Yedek Parça", "Sigorta Primi" };
                var birimFiyat = _random.Next(2000, 15000);
                var kdvTutar = birimFiyat * 0.20m;

                var kalem = new FaturaKalem
                {
                    SiraNo = 1,
                    Aciklama = aciklamalar[_random.Next(aciklamalar.Length)],
                    Miktar = 1,
                    BirimFiyat = birimFiyat,
                    KdvOrani = 20,
                    KdvTutar = kdvTutar,
                    ToplamTutar = birimFiyat + kdvTutar,
                    CreatedAt = tarih
                };

                fatura.FaturaKalemleri.Add(kalem);
                fatura.AraToplam = birimFiyat;
                fatura.KdvTutar = kdvTutar;
                fatura.GenelToplam = birimFiyat + kdvTutar;

                if (fatura.Durum == FaturaDurum.Odendi)
                    fatura.OdenenTutar = fatura.GenelToplam;

                faturalar.Add(fatura);
            }
        }

        _context.Faturalar.AddRange(faturalar);
        await _context.SaveChangesAsync();
        return faturalar.Count;
    }

    #endregion

    #region Servis Çalışması Seed

    private async Task<int> SeedServisCalismalarıAsync()
    {
        if (await _context.ServisCalismalari.AnyAsync(s => s.Notlar != null && s.Notlar.Contains("[TEST]")))
            return 0;

        var araclar = await _context.Araclar.Where(a => a.Aktif).ToListAsync();
        var soforler = await _context.Soforler.Where(s => s.Aktif && s.Gorev == PersonelGorev.Sofor).ToListAsync();
        var guzergahlar = await _context.Guzergahlar.Where(g => g.Aktif).ToListAsync();

        if (!araclar.Any() || !guzergahlar.Any() || !soforler.Any())
        {
            _logger.LogWarning("Araç, güzergah veya şoför bulunamadı, servis çalışması seed atlanıyor");
            return 0;
        }

        var calismalar = new List<ServisCalisma>();

        // Son 30 günlük servis çalışmaları
        for (int gun = 0; gun < 30; gun++)
        {
            var tarih = DateTime.Today.AddDays(-gun);
            if (tarih.DayOfWeek == DayOfWeek.Sunday) continue; // Pazar hariç

            // Her gün için 5-10 sefer
            var seferSayisi = _random.Next(5, 11);
            for (int s = 0; s < seferSayisi; s++)
            {
                var arac = araclar[_random.Next(araclar.Count)];
                var sofor = soforler[_random.Next(soforler.Count)];
                var guzergah = guzergahlar[_random.Next(guzergahlar.Count)];

                calismalar.Add(new ServisCalisma
                {
                    CalismaTarihi = tarih,
                    AracId = arac.Id,
                    SoforId = sofor.Id,
                    GuzergahId = guzergah.Id,
                    ServisTuru = (ServisTuru)_random.Next(1, 4),
                    Fiyat = guzergah.BirimFiyat,
                    Durum = CalismaDurum.Tamamlandi,
                    Notlar = "[TEST] Demo servis çalışması",
                    CreatedAt = tarih
                });
            }
        }

        _context.ServisCalismalari.AddRange(calismalar);
        await _context.SaveChangesAsync();
        return calismalar.Count;
    }

    #endregion

    #region İhale Hazırlık Seed

    private async Task<int> SeedIhaleHazirlikAsync()
    {
        if (await _context.IhaleProjeleri.AnyAsync(p => p.Notlar != null && p.Notlar.Contains("[TEST]")))
            return 0;

        var firma = await _context.Firmalar.FirstOrDefaultAsync();
        var musteriler = await _context.Cariler.Where(c => c.CariTipi == CariTipi.Musteri && c.Aktif).Take(5).ToListAsync();
        var guzergahlar = await _context.Guzergahlar.Where(g => g.Aktif).ToListAsync();

        var projeler = new List<IhaleProje>();

        // Proje 1: Büyük kurumsal taşımacılık
        var proje1 = new IhaleProje
        {
            ProjeKodu = "IHL-2025-001",
            ProjeAdi = "ABC Holding Personel Servis İhalesi",
            Aciklama = "[TEST] ABC Holding merkez ve 3 fabrika lokasyonu için personel servis hizmeti",
            FirmaId = firma?.Id,
            CariId = musteriler.FirstOrDefault()?.Id,
            BaslangicTarihi = new DateTime(2025, 9, 1),
            BitisTarihi = new DateTime(2027, 8, 31),
            SozlesmeSuresiAy = 24,
            Durum = IhaleProjeDurum.Hazirlaniyor,
            EnflasyonOrani = 30,
            YakitZamOrani = 35,
            AylikCalismGunu = 22,
            GunlukCalismaSaati = 8,
            Notlar = "[TEST] Demo ihale projesi",
            CreatedAt = DateTime.Now.AddDays(-15)
        };

        // Hat 1: Kadıköy - Levent
        proje1.Kalemler.Add(new IhaleGuzergahKalem
        {
            GuzergahId = guzergahlar.FirstOrDefault()?.Id,
            HatAdi = "Kadıköy - Levent (Sabah/Akşam)",
            BaslangicNoktasi = "Kadıköy",
            BitisNoktasi = "Levent",
            MesafeKm = 28.5m,
            TahminiSureDakika = 65,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 2,
            AylikSeferGunu = 22,
            PersonelSayisi = 35,
            SahiplikDurumu = AracSahiplikKalem.Ozmal,
            AracModelBilgi = "2023 Mercedes Sprinter 516",
            AracKoltukSayisi = 22,
            YakitTuketimi = 18,
            YakitFiyati = 44.50m,
            GunlukYakitMaliyeti = 456.30m,
            AylikYakitMaliyeti = 10038.60m,
            AylikBakimMasrafi = 3500,
            AylikLastikMasrafi = 1200,
            AylikSigortaMasrafi = 2800,
            AylikKaskoMasrafi = 1500,
            AylikMuayeneMasrafi = 200,
            AylikYedekParcaMasrafi = 1800,
            AylikDigerMasraf = 500,
            SoforBrutMaas = 35000,
            SoforNetMaas = 28500,
            SoforSGKIsverenPay = 8750,
            SoforToplamMaliyet = 43750,
            AracDegeri = 2800000,
            AmortismanYili = 5,
            AylikAmortisman = 46666.67m,
            AylikMaliyet = 122455.27m,
            ToplamMaliyet = 2938926.48m,
            SeferBasiMaliyet = 2782.62m,
            SaatlikMaliyet = 696.00m,
            KmBasiMaliyet = 97.58m,
            KarMarjiOrani = 15,
            AylikKarTutari = 18368.29m,
            AylikTeklifFiyati = 140823.56m,
            SeferBasiTeklifFiyati = 3200.01m,
            SaatlikTeklifFiyati = 800.40m,
            CreatedAt = DateTime.Now.AddDays(-15)
        });

        // Hat 2: Pendik - Maslak
        proje1.Kalemler.Add(new IhaleGuzergahKalem
        {
            GuzergahId = guzergahlar.Skip(1).FirstOrDefault()?.Id,
            HatAdi = "Pendik - Maslak (Sabah/Akşam)",
            BaslangicNoktasi = "Pendik",
            BitisNoktasi = "Maslak",
            MesafeKm = 52.0m,
            TahminiSureDakika = 95,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 2,
            AylikSeferGunu = 22,
            PersonelSayisi = 42,
            SahiplikDurumu = AracSahiplikKalem.Ozmal,
            AracModelBilgi = "2022 Ford Transit 19+1",
            AracKoltukSayisi = 19,
            YakitTuketimi = 16,
            YakitFiyati = 44.50m,
            GunlukYakitMaliyeti = 742.40m,
            AylikYakitMaliyeti = 16332.80m,
            AylikBakimMasrafi = 4200,
            AylikLastikMasrafi = 1500,
            AylikSigortaMasrafi = 2500,
            AylikKaskoMasrafi = 1800,
            AylikMuayeneMasrafi = 200,
            AylikYedekParcaMasrafi = 2200,
            AylikDigerMasraf = 600,
            SoforBrutMaas = 38000,
            SoforNetMaas = 30500,
            SoforSGKIsverenPay = 9500,
            SoforToplamMaliyet = 47500,
            AracDegeri = 2200000,
            AmortismanYili = 5,
            AylikAmortisman = 36666.67m,
            AylikMaliyet = 113499.47m,
            ToplamMaliyet = 2723987.28m,
            SeferBasiMaliyet = 2579.53m,
            SaatlikMaliyet = 644.88m,
            KmBasiMaliyet = 49.57m,
            KarMarjiOrani = 15,
            AylikKarTutari = 17024.92m,
            AylikTeklifFiyati = 130524.39m,
            SeferBasiTeklifFiyati = 2966.46m,
            SaatlikTeklifFiyati = 741.62m,
            CreatedAt = DateTime.Now.AddDays(-15)
        });

        // Hat 3: Bakırköy - Ataşehir (Kira araç)
        proje1.Kalemler.Add(new IhaleGuzergahKalem
        {
            HatAdi = "Bakırköy - Ataşehir (Sabah/Akşam)",
            BaslangicNoktasi = "Bakırköy",
            BitisNoktasi = "Ataşehir",
            MesafeKm = 32.0m,
            TahminiSureDakika = 70,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 2,
            AylikSeferGunu = 22,
            PersonelSayisi = 28,
            SahiplikDurumu = AracSahiplikKalem.Kiralik,
            AracModelBilgi = "2024 Iveco Daily 16+1",
            AracKoltukSayisi = 16,
            YakitTuketimi = 14,
            YakitFiyati = 44.50m,
            GunlukYakitMaliyeti = 399.84m,
            AylikYakitMaliyeti = 8796.48m,
            AylikBakimMasrafi = 0,
            AylikLastikMasrafi = 0,
            AylikSigortaMasrafi = 0,
            AylikKaskoMasrafi = 0,
            AylikKiraBedeli = 45000,
            SoforBrutMaas = 32000,
            SoforNetMaas = 26000,
            SoforSGKIsverenPay = 8000,
            SoforToplamMaliyet = 40000,
            AylikMaliyet = 93796.48m,
            ToplamMaliyet = 2251115.52m,
            SeferBasiMaliyet = 2131.74m,
            SaatlikMaliyet = 532.93m,
            KmBasiMaliyet = 66.62m,
            KarMarjiOrani = 18,
            AylikKarTutari = 16883.37m,
            AylikTeklifFiyati = 110679.85m,
            SeferBasiTeklifFiyati = 2515.45m,
            SaatlikTeklifFiyati = 628.86m,
            CreatedAt = DateTime.Now.AddDays(-15)
        });

        projeler.Add(proje1);

        // Proje 2: Küçük ölçekli okul servisi
        var proje2 = new IhaleProje
        {
            ProjeKodu = "IHL-2025-002",
            ProjeAdi = "Marmara Üniversitesi Kampüs Servisi",
            Aciklama = "[TEST] Göztepe ve Maltepe kampüsleri arası öğrenci/personel ulaşım",
            FirmaId = firma?.Id,
            CariId = musteriler.Skip(1).FirstOrDefault()?.Id,
            BaslangicTarihi = new DateTime(2025, 10, 1),
            BitisTarihi = new DateTime(2026, 6, 30),
            SozlesmeSuresiAy = 9,
            Durum = IhaleProjeDurum.TeklifVerildi,
            EnflasyonOrani = 25,
            YakitZamOrani = 30,
            AylikCalismGunu = 20,
            GunlukCalismaSaati = 10,
            Notlar = "[TEST] Demo ihale projesi",
            CreatedAt = DateTime.Now.AddDays(-30)
        };

        proje2.Kalemler.Add(new IhaleGuzergahKalem
        {
            HatAdi = "Göztepe Kampüs - Maltepe Kampüs",
            BaslangicNoktasi = "Göztepe",
            BitisNoktasi = "Maltepe",
            MesafeKm = 12.0m,
            TahminiSureDakika = 35,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 4,
            AylikSeferGunu = 20,
            PersonelSayisi = 60,
            SahiplikDurumu = AracSahiplikKalem.Ozmal,
            AracModelBilgi = "2023 Mercedes Sprinter 22+1",
            AracKoltukSayisi = 22,
            YakitTuketimi = 18,
            YakitFiyati = 44.50m,
            GunlukYakitMaliyeti = 384.48m,
            AylikYakitMaliyeti = 7689.60m,
            AylikBakimMasrafi = 2800,
            AylikLastikMasrafi = 900,
            AylikSigortaMasrafi = 2200,
            AylikKaskoMasrafi = 1200,
            SoforBrutMaas = 34000,
            SoforNetMaas = 27500,
            SoforSGKIsverenPay = 8500,
            SoforToplamMaliyet = 42500,
            AracDegeri = 2800000,
            AmortismanYili = 5,
            AylikAmortisman = 46666.67m,
            AylikMaliyet = 103956.27m,
            ToplamMaliyet = 935606.43m,
            SeferBasiMaliyet = 1299.45m,
            SaatlikMaliyet = 519.78m,
            KmBasiMaliyet = 108.29m,
            KarMarjiOrani = 20,
            AylikKarTutari = 20791.25m,
            AylikTeklifFiyati = 124747.52m,
            SeferBasiTeklifFiyati = 1559.34m,
            SaatlikTeklifFiyati = 623.74m,
            CreatedAt = DateTime.Now.AddDays(-30)
        });

        projeler.Add(proje2);

        // Proje 3: Kazanılmış proje
        var proje3 = new IhaleProje
        {
            ProjeKodu = "IHL-2024-015",
            ProjeAdi = "Tuzla OSB Personel Taşıma",
            Aciklama = "[TEST] Tuzla Organize Sanayi Bölgesi fabrikalar arası personel taşıma",
            FirmaId = firma?.Id,
            CariId = musteriler.Skip(2).FirstOrDefault()?.Id,
            BaslangicTarihi = new DateTime(2025, 1, 1),
            BitisTarihi = new DateTime(2025, 12, 31),
            SozlesmeSuresiAy = 12,
            Durum = IhaleProjeDurum.Kazanildi,
            EnflasyonOrani = 28,
            YakitZamOrani = 32,
            Notlar = "[TEST] Demo ihale projesi",
            CreatedAt = DateTime.Now.AddDays(-90)
        };

        proje3.Kalemler.Add(new IhaleGuzergahKalem
        {
            HatAdi = "Pendik Metro - Tuzla OSB",
            BaslangicNoktasi = "Pendik",
            BitisNoktasi = "Tuzla OSB",
            MesafeKm = 18.0m,
            TahminiSureDakika = 40,
            SeferTipi = SeferTipi.SabahAksam,
            GunlukSeferSayisi = 6,
            AylikSeferGunu = 26,
            PersonelSayisi = 120,
            SahiplikDurumu = AracSahiplikKalem.Ozmal,
            AracModelBilgi = "2021 Mercedes Sprinter 22+1",
            AracKoltukSayisi = 22,
            YakitTuketimi = 19,
            YakitFiyati = 44.50m,
            GunlukYakitMaliyeti = 912.60m,
            AylikYakitMaliyeti = 23727.60m,
            AylikBakimMasrafi = 5000,
            AylikLastikMasrafi = 2000,
            AylikSigortaMasrafi = 3000,
            AylikKaskoMasrafi = 1800,
            AylikMuayeneMasrafi = 250,
            AylikYedekParcaMasrafi = 3000,
            AylikDigerMasraf = 800,
            SoforBrutMaas = 36000,
            SoforNetMaas = 29000,
            SoforSGKIsverenPay = 9000,
            SoforToplamMaliyet = 45000,
            AracDegeri = 2200000,
            AmortismanYili = 5,
            AylikAmortisman = 36666.67m,
            AylikMaliyet = 121244.27m,
            ToplamMaliyet = 1454931.24m,
            SeferBasiMaliyet = 777.21m,
            SaatlikMaliyet = 689.00m,
            KmBasiMaliyet = 43.18m,
            KarMarjiOrani = 12,
            AylikKarTutari = 14549.31m,
            AylikTeklifFiyati = 135793.58m,
            SeferBasiTeklifFiyati = 870.47m,
            SaatlikTeklifFiyati = 771.68m,
            CreatedAt = DateTime.Now.AddDays(-90)
        });

        projeler.Add(proje3);

        _context.IhaleProjeleri.AddRange(projeler);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Count} ihale projesi ve güzergah kalemi oluşturuldu", projeler.Count);
        return projeler.Count + projeler.Sum(p => p.Kalemler.Count);
    }

    #endregion

    #region Proforma Fatura Seed

    private async Task<int> SeedProformaFaturalarAsync()
    {
        if (await _context.ProformaFaturalar.AnyAsync(p => p.Aciklama != null && p.Aciklama.Contains("[TEST]")))
            return 0;

        var firma = await _context.Firmalar.FirstOrDefaultAsync();
        var musteriler = await _context.Cariler
            .Where(c => c.CariTipi == CariTipi.Musteri && c.Aktif)
            .Take(5).ToListAsync();

        if (!musteriler.Any())
        {
            _logger.LogWarning("Müşteri bulunamadı, proforma fatura seed atlanıyor");
            return 0;
        }

        var proformalari = new List<ProformaFatura>();

        // Proforma 1: Taslak - Aylık personel servis teklifi
        var pf1 = new ProformaFatura
        {
            ProformaNo = "PF-2025-000001",
            ProformaTarihi = DateTime.Now.AddDays(-5),
            GecerlilikTarihi = DateTime.Now.AddDays(25),
            Durum = ProformaDurum.Taslak,
            CariId = musteriler[0].Id,
            FirmaId = firma?.Id,
            KdvOrani = 20,
            OdemeKosulu = "30 gün vadeli",
            TeslimKosulu = "Kapıda teslim",
            VadeGun = 30,
            IlgiliKisi = musteriler[0].YetkiliKisi,
            Email = musteriler[0].Email,
            Aciklama = "[TEST] Temmuz 2025 aylık personel servis hizmeti proforması",
            OzelNotlar = "Müşteri ile telefonda görüşüldü, fiyatlar onaya sunulacak",
            CreatedAt = DateTime.Now.AddDays(-5)
        };

        pf1.Kalemler.Add(new ProformaFaturaKalem
        {
            SiraNo = 1,
            UrunAdi = "Kadıköy - Levent Personel Servis (S/A)",
            UrunKodu = "SRV-001",
            Aciklama = "Sabah-akşam servis, 22 iş günü",
            Miktar = 44, Birim = "Sefer", BirimFiyat = 3200m,
            KdvOrani = 20,
            AraToplam = 140800m, IskontoOrani = 0, IskontoTutar = 0,
            NetTutar = 140800m, KdvTutar = 28160m, ToplamTutar = 168960m,
            CreatedAt = DateTime.Now.AddDays(-5)
        });
        pf1.Kalemler.Add(new ProformaFaturaKalem
        {
            SiraNo = 2,
            UrunAdi = "Pendik - Maslak Personel Servis (S/A)",
            UrunKodu = "SRV-002",
            Aciklama = "Sabah-akşam servis, 22 iş günü",
            Miktar = 44, Birim = "Sefer", BirimFiyat = 2966m,
            KdvOrani = 20,
            AraToplam = 130504m, IskontoOrani = 0, IskontoTutar = 0,
            NetTutar = 130504m, KdvTutar = 26100.80m, ToplamTutar = 156604.80m,
            CreatedAt = DateTime.Now.AddDays(-5)
        });

        pf1.AraToplam = 271304m;
        pf1.KdvTutar = 54260.80m;
        pf1.GenelToplam = 325564.80m;
        proformalari.Add(pf1);

        // Proforma 2: Gönderildi - Okul servisi teklifi
        var pf2 = new ProformaFatura
        {
            ProformaNo = "PF-2025-000002",
            ProformaTarihi = DateTime.Now.AddDays(-10),
            GecerlilikTarihi = DateTime.Now.AddDays(20),
            Durum = ProformaDurum.Gonderildi,
            CariId = musteriler.Count > 1 ? musteriler[1].Id : musteriler[0].Id,
            FirmaId = firma?.Id,
            KdvOrani = 20,
            OdemeKosulu = "Peşin",
            VadeGun = 0,
            Aciklama = "[TEST] Ekim-Haziran kampüs servis hizmeti proforması",
            CreatedAt = DateTime.Now.AddDays(-10)
        };

        pf2.Kalemler.Add(new ProformaFaturaKalem
        {
            SiraNo = 1,
            UrunAdi = "Göztepe - Maltepe Kampüs Servis",
            UrunKodu = "SRV-003",
            Aciklama = "Günde 4 sefer, 20 iş günü x 9 ay",
            Miktar = 720, Birim = "Sefer", BirimFiyat = 1559m,
            KdvOrani = 20,
            AraToplam = 1122480m, IskontoOrani = 5, IskontoTutar = 56124m,
            NetTutar = 1066356m, KdvTutar = 213271.20m, ToplamTutar = 1279627.20m,
            CreatedAt = DateTime.Now.AddDays(-10)
        });

        pf2.AraToplam = 1122480m;
        pf2.IskontoOrani = 5;
        pf2.IskontoTutar = 56124m;
        pf2.KdvTutar = 213271.20m;
        pf2.GenelToplam = 1279627.20m;
        proformalari.Add(pf2);

        // Proforma 3: Onaylandı (faturaya dönüştürülmeye hazır)
        var pf3 = new ProformaFatura
        {
            ProformaNo = "PF-2025-000003",
            ProformaTarihi = DateTime.Now.AddDays(-20),
            GecerlilikTarihi = DateTime.Now.AddDays(10),
            Durum = ProformaDurum.Onaylandi,
            CariId = musteriler.Count > 2 ? musteriler[2].Id : musteriler[0].Id,
            FirmaId = firma?.Id,
            KdvOrani = 20,
            OdemeKosulu = "15 gün vadeli",
            VadeGun = 15,
            IlgiliKisi = "Ahmet Bey",
            Aciklama = "[TEST] Tuzla OSB personel taşıma hizmeti - onaylanmış proforma",
            CreatedAt = DateTime.Now.AddDays(-20)
        };

        pf3.Kalemler.Add(new ProformaFaturaKalem
        {
            SiraNo = 1,
            UrunAdi = "Pendik Metro - Tuzla OSB Servis",
            UrunKodu = "SRV-004",
            Aciklama = "Günde 6 sefer, 26 iş günü",
            Miktar = 156, Birim = "Sefer", BirimFiyat = 870m,
            KdvOrani = 20,
            AraToplam = 135720m, IskontoOrani = 0, IskontoTutar = 0,
            NetTutar = 135720m, KdvTutar = 27144m, ToplamTutar = 162864m,
            CreatedAt = DateTime.Now.AddDays(-20)
        });

        pf3.AraToplam = 135720m;
        pf3.KdvTutar = 27144m;
        pf3.GenelToplam = 162864m;
        proformalari.Add(pf3);

        _context.ProformaFaturalar.AddRange(proformalari);
        await _context.SaveChangesAsync();

        var totalKalem = proformalari.Sum(p => p.Kalemler.Count);
        _logger.LogInformation("{Count} proforma fatura ve {KalemCount} kalem oluşturuldu", proformalari.Count, totalKalem);
        return proformalari.Count + totalKalem;
    }

    #endregion

    #region Puantaj Kayıt Seed (Toplu Fatura İçin)

    private async Task<int> SeedPuantajKayitlarAsync()
    {
        // Fatura kesilmemiş puantaj kayıtları (toplu fatura demo için)
        if (await _context.PuantajKayitlar.AnyAsync(p => p.GuzergahAdi != null && p.GuzergahAdi.Contains("[TEST]")))
            return 0;

        var musteriler = await _context.Cariler
            .Where(c => c.CariTipi == CariTipi.Musteri && c.Aktif)
            .Take(3).ToListAsync();
        var soforler = await _context.Soforler.Where(s => s.Aktif).Take(5).ToListAsync();
        var araclar = await _context.Araclar.Where(a => a.Aktif).Take(5).ToListAsync();
        var guzergahlar = await _context.Guzergahlar.Where(g => g.Aktif).Take(4).ToListAsync();

        if (!musteriler.Any())
        {
            _logger.LogWarning("Müşteri bulunamadı, puantaj seed atlanıyor");
            return 0;
        }

        var yil = DateTime.Now.Year;
        var ay = DateTime.Now.Month;
        var kayitlar = new List<PuantajKayit>();

        // Müşteri 1: 2 güzergah, fatura kesilmemiş
        for (int g = 0; g < Math.Min(2, guzergahlar.Count); g++)
        {
            var guzergah = guzergahlar[g];
            var musteri = musteriler[0];
            var sofor = soforler.Count > g ? soforler[g] : null;
            var arac = araclar.Count > g ? araclar[g] : null;

            var kayit = new PuantajKayit
            {
                Yil = yil,
                Ay = ay,
                Bolge = "Anadolu",
                SiraNo = g + 1,
                KurumCariId = musteri.Id,
                KurumAdi = musteri.Unvan,
                GuzergahId = guzergah.Id,
                GuzergahAdi = $"[TEST] {guzergah.GuzergahAdi}",
                Yon = PuantajYon.SabahAksam,
                AracId = arac?.Id,
                Plaka = arac?.AktifPlaka ?? "34 AA 001",
                SoforId = sofor?.Id,
                SoforAdi = sofor != null ? $"{sofor.Ad} {sofor.Soyad}" : "Test Şoför",
                SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                Gun = 22,
                SeferSayisi = 2,
                BirimGelir = 3200m + (g * 500m),
                ToplamGelir = (3200m + (g * 500m)) * 22,
                GelirKdvOrani = 20,
                GelirKdvTutari = (3200m + (g * 500m)) * 22 * 0.20m,
                GelirToplam = (3200m + (g * 500m)) * 22 * 1.20m,
                Alinacak = (3200m + (g * 500m)) * 22 * 1.20m,
                BirimGider = 2400m + (g * 300m),
                ToplamGider = (2400m + (g * 300m)) * 22,
                Odenecek = (2400m + (g * 300m)) * 22,
                GelirFaturaKesildi = false,
                GiderFaturaAlindi = false,
                OnayDurum = PuantajOnayDurum.Onaylandi,
                Kaynak = PuantajKaynak.Manuel,
                CreatedAt = DateTime.Now.AddDays(-5)
            };

            // 22 iş günü puantaj doldur (hafta içi)
            int gunSayac = 0;
            for (int d = 1; d <= 28 && gunSayac < 22; d++)
            {
                var tarih = new DateTime(yil, ay, d);
                if (tarih.DayOfWeek != DayOfWeek.Saturday && tarih.DayOfWeek != DayOfWeek.Sunday)
                {
                    kayit.SetGunDeger(d, 2); // 2 = S+A
                    gunSayac++;
                }
            }

            kayitlar.Add(kayit);
        }

        // Müşteri 2: 1 güzergah, fatura kesilmemiş
        if (musteriler.Count > 1 && guzergahlar.Count > 2)
        {
            var musteri2 = musteriler[1];
            var guz = guzergahlar[2];
            var sofor = soforler.Count > 2 ? soforler[2] : null;
            var arac = araclar.Count > 2 ? araclar[2] : null;

            var kayit2 = new PuantajKayit
            {
                Yil = yil,
                Ay = ay,
                Bolge = "Avrupa",
                SiraNo = 1,
                KurumCariId = musteri2.Id,
                KurumAdi = musteri2.Unvan,
                GuzergahId = guz.Id,
                GuzergahAdi = $"[TEST] {guz.GuzergahAdi}",
                Yon = PuantajYon.SabahAksam,
                AracId = arac?.Id,
                Plaka = arac?.AktifPlaka ?? "34 BB 002",
                SoforId = sofor?.Id,
                SoforAdi = sofor != null ? $"{sofor.Ad} {sofor.Soyad}" : "Test Şoför 2",
                SoforOdemeTipi = SoforOdemeTipi.Ozmal,
                Gun = 20,
                SeferSayisi = 4,
                BirimGelir = 1560m,
                ToplamGelir = 1560m * 20,
                GelirKdvOrani = 20,
                GelirKdvTutari = 1560m * 20 * 0.20m,
                GelirToplam = 1560m * 20 * 1.20m,
                Alinacak = 1560m * 20 * 1.20m,
                BirimGider = 1100m,
                ToplamGider = 1100m * 20,
                Odenecek = 1100m * 20,
                GelirFaturaKesildi = false,
                GiderFaturaAlindi = false,
                OnayDurum = PuantajOnayDurum.Onaylandi,
                Kaynak = PuantajKaynak.Manuel,
                CreatedAt = DateTime.Now.AddDays(-3)
            };

            int gunSayac2 = 0;
            for (int d = 1; d <= 28 && gunSayac2 < 20; d++)
            {
                var tarih = new DateTime(yil, ay, d);
                if (tarih.DayOfWeek != DayOfWeek.Saturday && tarih.DayOfWeek != DayOfWeek.Sunday)
                {
                    kayit2.SetGunDeger(d, 2);
                    gunSayac2++;
                }
            }

            kayitlar.Add(kayit2);
        }

        _context.PuantajKayitlar.AddRange(kayitlar);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Count} puantaj kayıtı oluşturuldu (toplu fatura demo için)", kayitlar.Count);
        return kayitlar.Count;
    }

    #endregion

    #region Yardımcı Metodlar

    private string RastgeleIsim()
    {
        var erkek = _random.Next(100) < 50;
        var ad = erkek ? _erkekAdlari[_random.Next(_erkekAdlari.Length)] : _kadinAdlari[_random.Next(_kadinAdlari.Length)];
        var soyad = _soyadlari[_random.Next(_soyadlari.Length)];
        return $"{ad} {soyad}";
    }

    #endregion
}

/// <summary>
/// Test veri oluşturma sonucu
/// </summary>
public class TestDataResult
{
    public bool Basarili { get; set; }
    public List<string> Mesajlar { get; set; } = new();
    public int CariSayisi { get; set; }
    public int SoforSayisi { get; set; }
    public int AracSayisi { get; set; }
    public int GuzergahSayisi { get; set; }
    public int FaturaSayisi { get; set; }
    public int ServisCalismasiSayisi { get; set; }
    public int IhaleHazirlikSayisi { get; set; }
    public int ProformaFaturaSayisi { get; set; }
    public int PuantajKayitSayisi { get; set; }

    public int ToplamKayit => CariSayisi + SoforSayisi + AracSayisi + GuzergahSayisi + FaturaSayisi + ServisCalismasiSayisi + IhaleHazirlikSayisi + ProformaFaturaSayisi + PuantajKayitSayisi;
}


