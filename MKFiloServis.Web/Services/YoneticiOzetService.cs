using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Yönetici özeti: seçilen dönemde yapılan işleri modül modül toplar.
/// <para>
/// Kurallar:
/// <list type="bullet">
/// <item>Her bölüm kendi bağlamıyla ve kendi try/catch'iyle çalışır; biri düşerse rapor yine döner.</item>
/// <item>Tutar toplamları sağlayıcıdan bağımsız olsun diye (SQLite decimal toplamı) dar projeksiyonla çekilip bellekte toplanır.</item>
/// <item>Firma izolasyonu <see cref="IFirmaTenant"/> sorgu filtresiyle gelir; bütçe tablosu muaf olduğundan firma elle süzülür.</item>
/// <item>Gerçekleşen işler bugüne kadar sayılır (<see cref="YoneticiOzetDonemi.EtkinBitis"/>); bütçe ve planlı sefer dönem sonuna kadar bakar.</item>
/// </list>
/// </para>
/// </summary>
public sealed partial class YoneticiOzetService : IYoneticiOzetService
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private static readonly TimeZoneInfo IsGunuDilimi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    /// <summary>Aktivite günlüğünde "iş" sayılan ana kayıt türleri ve görünen adları (detay/teknik satırlar sayılmaz).</summary>
    private static readonly IReadOnlyDictionary<string, string> AnaKayitEtiketleri = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Fatura"] = "Fatura",
        ["ProformaFatura"] = "Proforma fatura",
        ["BankaKasaHareket"] = "Banka/Kasa hareketi",
        ["Hakedis"] = "Hakediş",
        ["BudgetOdeme"] = "Bütçe ödemesi",
        ["Cari"] = "Cari",
        ["Arac"] = "Araç",
        ["AracEvrak"] = "Araç evrakı",
        ["AracMasraf"] = "Araç masrafı",
        ["Sofor"] = "Personel",
        ["PersonelPuantaj"] = "Personel puantajı",
        ["FiloGunlukPuantaj"] = "Servis puantajı",
        ["PersonelAvans"] = "Personel avansı",
        ["PersonelMaas"] = "Maaş kaydı",
        ["MuhasebeFis"] = "Muhasebe fişi",
        ["EbysEvrak"] = "Evrak",
        ["MusteriKiralama"] = "Kiralama",
        ["Guzergah"] = "Güzergah"
    };

    private static readonly string[] AnaKayitTurleri = AnaKayitEtiketleri.Keys.ToArray();

    // Aktivite günlüğüne iki yazar var: SaveChanges denetimi (Added/Modified/Deleted) ve servisler (Ekleme/Güncelleme/Silme).
    private static readonly string[] EklemeTipleri = ["Added", "Ekleme"];
    private static readonly string[] GuncellemeTipleri = ["Modified", "Güncelleme"];
    private static readonly string[] SilmeTipleri = ["Deleted", "Silme"];
    private static readonly string[] IslemTipleri = [.. EklemeTipleri, .. GuncellemeTipleri, .. SilmeTipleri];

    private const int SonIslemSayisi = 8;
    private const int EnAktifKullaniciSayisi = 5;
    private const int IsTuruSayisi = 6;
    private const int KritikGecikmeGunu = 30;
    private const int KritikDevirGunu = 180;

    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly IAktifFirmaProvider _aktifFirma;
    private readonly ILogger<YoneticiOzetService> _logger;

    public YoneticiOzetService(
        IDbContextFactory<ApplicationDbContext> factory,
        IAktifFirmaProvider aktifFirma,
        ILogger<YoneticiOzetService> logger)
    {
        _factory = factory;
        _aktifFirma = aktifFirma;
        _logger = logger;
    }

    public async Task<YoneticiOzetRapor> GetRaporAsync(YoneticiOzetIstegi istek, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var bugun = (istek.Bugun ?? BusinessTime.Today).Date;
        var donem = YoneticiOzetDonemi.Hesapla(istek.DonemTuru, bugun, istek.OzelBaslangic, istek.OzelBitis);
        var kapsam = istek.Kapsam ?? YoneticiOzetKapsami.Hicbiri;
        var baglam = new Baglam(donem, _aktifFirma.TumFirmalar ? null : _aktifFirma.AktifFirmaId);
        var hatalar = new List<string>();

        var servis = kapsam.Servis ? await BolumAsync("Servis operasyonu", () => ServisAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var hakedis = kapsam.Hakedis ? await BolumAsync("Hakediş", () => HakedisAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var fatura = kapsam.Fatura ? await BolumAsync("Faturalar", () => FaturaAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var kasa = kapsam.Finans ? await BolumAsync("Banka/Kasa", () => KasaAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var butce = kapsam.Finans ? await BolumAsync("Bütçe", () => ButceAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var personel = kapsam.Personel ? await BolumAsync("Personel ve maaş", () => PersonelAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var arac = kapsam.Arac ? await BolumAsync("Araç", () => AracAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var kayitlar = kapsam.Cari || kapsam.Personel || kapsam.Evrak
            ? await BolumAsync("Yeni kayıtlar", () => KayitAsync(baglam, kapsam, cancellationToken), hatalar, cancellationToken)
            : null;
        var rentACar = kapsam.RentACar ? await BolumAsync("Rent a Car", () => RentACarAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var ekip = kapsam.Ekip ? await BolumAsync("Ekip hareketleri", () => EkipAsync(baglam, cancellationToken), hatalar, cancellationToken) : null;
        var dikkat = await BolumAsync("Dikkat gerektirenler", () => DikkatAsync(baglam, kapsam, istek.KritikBelgeSayisi, cancellationToken), hatalar, cancellationToken)
                     ?? new List<DikkatKalemi>();

        return new YoneticiOzetRapor
        {
            Donem = donem,
            UretimZamani = BusinessTime.Now,
            KapsamAdi = KapsamAdi(),
            Servis = servis,
            Hakedis = hakedis,
            Fatura = fatura,
            Kasa = kasa,
            Butce = butce,
            Personel = personel,
            Arac = arac,
            Kayitlar = kayitlar,
            RentACar = rentACar,
            Ekip = ekip,
            Dikkat = dikkat
                .OrderByDescending(d => d.Seviye)
                .ToList(),
            Hatalar = hatalar
        };
    }

    private string KapsamAdi()
    {
        if (_aktifFirma.TumFirmalar)
            return "Tüm Firmalar";
        var ad = _aktifFirma.Mevcut?.FirmaAdi;
        return string.IsNullOrWhiteSpace(ad) ? "Aktif firma" : ad;
    }

    private async Task<T?> BolumAsync<T>(string ad, Func<Task<T>> uret, List<string> hatalar, CancellationToken ct) where T : class
    {
        try
        {
            return await uret();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yönetici özeti bölümü yüklenemedi: {Bolum}", ad);
            hatalar.Add(ad);
            return null;
        }
    }

    #region Servis (günlük servis puantajı = sefer)

    private sealed record SeferSatiri(
        DateTime Tarih,
        OperasyonDurumu Durum,
        decimal SeferSayisi,
        int AracId,
        decimal Kurum,
        decimal Taseron,
        bool Onaylandi);

    private static bool YapilamadiMi(OperasyonDurumu durum)
        => durum is OperasyonDurumu.Gitmedi_Mazeretli
            or OperasyonDurumu.Gitmedi_Mazeretsiz
            or OperasyonDurumu.Iptal_KurumTarafindan;

    private async Task<List<SeferSatiri>> SeferSatirlariAsync(Aralik aralik, CancellationToken ct)
    {
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.FiloGunlukPuantajlar.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Tarih >= aralik.Bas && p.Tarih < aralik.Sonra)
            .Select(p => new
            {
                p.Tarih,
                p.Durum,
                p.SeferSayisi,
                p.AracId,
                p.TahakkukEdenKurumUcreti,
                p.TahakkukEdenTaseronUcreti,
                p.Onaylandi
            })
            .ToListAsync(ct);

        return ham
            .Select(p => new SeferSatiri(p.Tarih.Date, p.Durum, p.SeferSayisi, p.AracId, p.TahakkukEdenKurumUcreti, p.TahakkukEdenTaseronUcreti, p.Onaylandi))
            .ToList();
    }

    private async Task<ServisOzeti> ServisAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        // Grafik penceresi dönemden geniş olabilir ("Bugün" son 14 günü gösterir); tek sorguda ikisi de çekilir.
        var sorgu = Aralik.Gunler(Kucuk(d.Baslangic, d.GrafikBaslangic), Buyuk(d.Bitis, d.GrafikBitis));
        var satirlar = await SeferSatirlariAsync(sorgu, ct);

        var donemSatirlari = satirlar.Where(s => s.Tarih >= d.Baslangic && s.Tarih <= d.Bitis).ToList();
        var gerceklesen = donemSatirlari.Where(s => s.Tarih <= d.Bugun).ToList();
        var yapilan = gerceklesen.Where(s => !YapilamadiMi(s.Durum)).ToList();
        var planli = donemSatirlari.Where(s => s.Tarih > d.Bugun && !YapilamadiMi(s.Durum)).ToList();

        var onceki = (await SeferSatirlariAsync(Aralik.Gunler(d.OncekiBaslangic, d.OncekiBitis), ct))
            .Where(s => s.Tarih <= d.Bugun && !YapilamadiMi(s.Durum))
            .ToList();

        var seri = Seri(
            d,
            satirlar.Where(s => !YapilamadiMi(s.Durum))
                .Select(s => (s.Tarih, V1: s.Tarih <= d.Bugun ? s.SeferSayisi : 0m, V2: s.Tarih > d.Bugun ? s.SeferSayisi : 0m)),
            "Yapılan sefer",
            "Planlı sefer");

        return new ServisOzeti
        {
            YapilanSefer = yapilan.Sum(s => s.SeferSayisi),
            OncekiYapilanSefer = onceki.Sum(s => s.SeferSayisi),
            CalisanAracSayisi = yapilan.Select(s => s.AracId).Distinct().Count(),
            CalisilanGunSayisi = yapilan.Select(s => s.Tarih).Distinct().Count(),
            YapilamayanKayit = gerceklesen.Count(s => YapilamadiMi(s.Durum)),
            ArizaKaydi = gerceklesen.Count(s => s.Durum == OperasyonDurumu.Arizalandi_YoldaKaldi),
            PlanliSefer = planli.Sum(s => s.SeferSayisi),
            KurumTahakkuku = yapilan.Sum(s => s.Kurum),
            OncekiKurumTahakkuku = onceki.Sum(s => s.Kurum),
            TaseronTahakkuku = yapilan.Sum(s => s.Taseron),
            KayitSayisi = yapilan.Count,
            OnayliKayitSayisi = yapilan.Count(s => s.Onaylandi),
            Seri = seri
        };
    }

    #endregion

    #region Hakediş

    private async Task<HakedisOzeti> HakedisAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var simdi = Aralik.Gunler(d.Baslangic, d.EtkinBitis).Utc();
        var oncekiAralik = Aralik.Gunler(d.OncekiBaslangic, d.OncekiBitis).Utc();

        await using var db = await _factory.CreateDbContextAsync(ct);

        var uretilen = simdi.Bos
            ? []
            : await db.Hakedisler.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Durum != HakedisDurum.Iptal && h.CreatedAt >= simdi.Bas && h.CreatedAt < simdi.Sonra)
                .Select(h => new { h.Tip, h.Durum, h.GenelToplam, h.Tutar })
                .ToListAsync(ct);

        var onaylanan = simdi.Bos
            ? 0
            : await db.Hakedisler.AsNoTracking()
                .CountAsync(h => !h.IsDeleted && h.Durum != HakedisDurum.Iptal
                                 && h.OnayTarihi != null && h.OnayTarihi >= simdi.Bas && h.OnayTarihi < simdi.Sonra, ct);

        var onceki = oncekiAralik.Bos
            ? []
            : await db.Hakedisler.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Durum != HakedisDurum.Iptal && h.CreatedAt >= oncekiAralik.Bas && h.CreatedAt < oncekiAralik.Sonra)
                .Select(h => new { h.Tip, h.GenelToplam, h.Tutar })
                .ToListAsync(ct);

        static decimal Tutar(decimal genelToplam, decimal tutar) => genelToplam > 0 ? genelToplam : tutar;

        var gelir = uretilen.Where(h => h.Tip.GelirMi()).ToList();
        var gider = uretilen.Where(h => h.Tip.GiderMi()).ToList();
        var ic = uretilen.Where(h => h.Tip == HakedisTipi.Arac).ToList();

        return new HakedisOzeti
        {
            UretilenAdet = uretilen.Count,
            OncekiUretilenAdet = onceki.Count,
            GelirAdet = gelir.Count,
            GelirTutari = gelir.Sum(h => Tutar(h.GenelToplam, h.Tutar)),
            OncekiGelirTutari = onceki.Where(h => h.Tip.GelirMi()).Sum(h => Tutar(h.GenelToplam, h.Tutar)),
            GiderAdet = gider.Count,
            GiderTutari = gider.Sum(h => Tutar(h.GenelToplam, h.Tutar)),
            IcAdet = ic.Count,
            IcTutar = ic.Sum(h => Tutar(h.GenelToplam, h.Tutar)),
            OnaylananAdet = onaylanan,
            TaslakAdet = uretilen.Count(h => h.Durum == HakedisDurum.Taslak),
            OnayliAdet = uretilen.Count(h => h.Durum == HakedisDurum.Onaylandi),
            FaturaliAdet = uretilen.Count(h => h.Durum == HakedisDurum.Faturalandi),
            TamamlananAdet = uretilen.Count(h => h.Durum is HakedisDurum.TahsilEdildi or HakedisDurum.Odendi)
        };
    }

    #endregion

    #region Fatura

    private sealed record FaturaSatiri(FaturaYonu Yon, FaturaTipi Tip, decimal GenelToplam, decimal Odenen);

    private async Task<List<FaturaSatiri>> FaturaSatirlariAsync(Aralik aralik, CancellationToken ct)
    {
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.Faturalar.AsNoTracking()
            .Where(f => !f.IsDeleted && f.Durum != FaturaDurum.IptalEdildi && f.FaturaTarihi >= aralik.Bas && f.FaturaTarihi < aralik.Sonra)
            .Select(f => new { f.FaturaYonu, f.FaturaTipi, f.GenelToplam, f.OdenenTutar })
            .ToListAsync(ct);
        return ham.Select(f => new FaturaSatiri(f.FaturaYonu, f.FaturaTipi, f.GenelToplam, f.OdenenTutar)).ToList();
    }

    private static bool IadeMi(FaturaTipi tip) => tip is FaturaTipi.SatisIadeFaturasi or FaturaTipi.AlisIadeFaturasi;

    private async Task<FaturaOzeti> FaturaAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var simdi = await FaturaSatirlariAsync(Aralik.Gunler(d.Baslangic, d.EtkinBitis), ct);
        var onceki = await FaturaSatirlariAsync(Aralik.Gunler(d.OncekiBaslangic, d.OncekiBitis), ct);

        var kesilen = simdi.Where(f => f.Yon == FaturaYonu.Giden && !IadeMi(f.Tip)).ToList();
        var alinan = simdi.Where(f => f.Yon == FaturaYonu.Gelen && !IadeMi(f.Tip)).ToList();
        var oncekiKesilen = onceki.Where(f => f.Yon == FaturaYonu.Giden && !IadeMi(f.Tip)).ToList();

        return new FaturaOzeti
        {
            KesilenAdet = kesilen.Count,
            KesilenTutar = kesilen.Sum(f => f.GenelToplam),
            KesilenTahsilEdilen = kesilen.Sum(f => f.Odenen),
            OncekiKesilenAdet = oncekiKesilen.Count,
            OncekiKesilenTutar = oncekiKesilen.Sum(f => f.GenelToplam),
            AlinanAdet = alinan.Count,
            AlinanTutar = alinan.Sum(f => f.GenelToplam),
            AlinanOdenen = alinan.Sum(f => f.Odenen),
            IadeAdet = simdi.Count(f => IadeMi(f.Tip))
        };
    }

    #endregion

    #region Banka / Kasa

    private sealed record HareketSatiri(DateTime Tarih, HareketTipi Tip, decimal Tutar);

    private async Task<List<HareketSatiri>> HareketSatirlariAsync(Aralik aralik, CancellationToken ct)
    {
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        // Hesaplar arası aktarım (Mahsup), cari mahsup ve personel cebinden harcama nakit hareketi değildir.
        var ham = await db.BankaKasaHareketleri.AsNoTracking()
            .Where(h => !h.IsDeleted
                        && h.IslemTarihi >= aralik.Bas && h.IslemTarihi < aralik.Sonra
                        && h.IslemKaynak != IslemKaynak.Mahsup
                        && h.IslemKaynak != IslemKaynak.CariMahsup
                        && h.IslemKaynak != IslemKaynak.PersonelCebinden)
            .Select(h => new { h.IslemTarihi, h.HareketTipi, h.Tutar })
            .ToListAsync(ct);
        return ham.Select(h => new HareketSatiri(h.IslemTarihi.Date, h.HareketTipi, h.Tutar)).ToList();
    }

    private async Task<KasaOzeti> KasaAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var sorgu = Aralik.Gunler(Kucuk(d.Baslangic, d.GrafikBaslangic), Kucuk(Buyuk(d.EtkinBitis, d.GrafikBitis), d.Bugun));
        var satirlar = await HareketSatirlariAsync(sorgu, ct);
        var onceki = await HareketSatirlariAsync(Aralik.Gunler(d.OncekiBaslangic, d.OncekiBitis), ct);

        var donem = satirlar.Where(h => h.Tarih >= d.Baslangic && h.Tarih <= d.EtkinBitis).ToList();
        var tahsilat = donem.Where(h => h.Tip == HareketTipi.Giris).ToList();
        var odeme = donem.Where(h => h.Tip == HareketTipi.Cikis).ToList();

        var seri = Seri(
            d,
            satirlar.Select(h => (h.Tarih,
                V1: h.Tip == HareketTipi.Giris ? h.Tutar : 0m,
                V2: h.Tip == HareketTipi.Cikis ? h.Tutar : 0m)),
            "Tahsilat",
            "Ödeme");

        return new KasaOzeti
        {
            TahsilatAdet = tahsilat.Count,
            TahsilatTutari = tahsilat.Sum(h => h.Tutar),
            OncekiTahsilat = onceki.Where(h => h.Tip == HareketTipi.Giris).Sum(h => h.Tutar),
            OdemeAdet = odeme.Count,
            OdemeTutari = odeme.Sum(h => h.Tutar),
            OncekiOdeme = onceki.Where(h => h.Tip == HareketTipi.Cikis).Sum(h => h.Tutar),
            Seri = seri
        };
    }

    #endregion

    #region Bütçe

    private IQueryable<BudgetOdeme> ButceSorgusu(ApplicationDbContext db, int? firmaId)
    {
        // Bütçe tablosu tenant filtresinden muaftır; firma seçiliyse "firması boş" genel kayıtlar da dahil (BudgetService ile aynı).
        var sorgu = db.BudgetOdemeler.AsNoTracking().Where(o => !o.IsDeleted);
        return firmaId.HasValue ? sorgu.Where(o => o.FirmaId == firmaId.Value || o.FirmaId == null) : sorgu;
    }

    private async Task<ButceOzeti> ButceAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var aralik = Aralik.Gunler(d.Baslangic, d.Bitis);
        await using var db = await _factory.CreateDbContextAsync(ct);

        var ham = await ButceSorgusu(db, b.FirmaId)
            .Where(o => o.OdemeTarihi >= aralik.Bas && o.OdemeTarihi < aralik.Sonra)
            .Select(o => new
            {
                o.Miktar,
                o.Durum,
                o.FaturaIleKapatildi,
                o.OdenenTutar,
                o.ToplamKismiOdenen,
                o.MasrafKesintisi,
                o.CezaKesintisi,
                o.DigerKesinti
            })
            .ToListAsync(ct);

        decimal planlanan = 0, odenenToplam = 0, bekleyenToplam = 0;
        int odenenKalem = 0, bekleyenKalem = 0;
        foreach (var o in ham)
        {
            planlanan += o.Miktar;
            var kismi = o.Durum == OdemeDurum.KismiOdendi;

            if (ButceOdenenMi(o.Durum, o.FaturaIleKapatildi))
            {
                odenenKalem++;
                odenenToplam += ButceOdenenTutar(o.Miktar, o.Durum, o.OdenenTutar, o.MasrafKesintisi + o.CezaKesintisi + o.DigerKesinti);
            }

            if (o.Durum == OdemeDurum.Bekliyor || kismi)
            {
                bekleyenKalem++;
                bekleyenToplam += kismi ? o.Miktar - o.ToplamKismiOdenen : o.Miktar;
            }
        }

        return new ButceOzeti
        {
            Planlanan = planlanan,
            Odenen = odenenToplam,
            Bekleyen = bekleyenToplam,
            KalemSayisi = ham.Count,
            OdenenKalem = odenenKalem,
            BekleyenKalem = bekleyenKalem
        };
    }

    // Anlamlar BudgetService.GetAylikOzetAsync ile aynı tutulur (gerçekleşen tanımı ve kısmi ödeme).
    private static bool ButceOdenenMi(OdemeDurum durum, bool faturaIleKapatildi)
        => durum is OdemeDurum.Odendi or OdemeDurum.KismiOdendi || faturaIleKapatildi;

    private static decimal ButceOdenenTutar(decimal miktar, OdemeDurum durum, decimal? odenenTutar, decimal ek)
    {
        var net = (odenenTutar ?? miktar) + ek;
        return durum == OdemeDurum.KismiOdendi ? net : odenenTutar ?? net;
    }

    #endregion

    #region Personel ve maaş

    private async Task<PersonelOzeti> PersonelAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var basAy = d.Baslangic.Year * 100 + d.Baslangic.Month;
        var bitisTarihi = d.EtkinBitis >= d.Baslangic ? d.EtkinBitis : d.Baslangic;
        var bitAy = bitisTarihi.Year * 100 + bitisTarihi.Month;
        var avans = Aralik.Gunler(d.Baslangic, d.EtkinBitis);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var aktif = await db.Soforler.AsNoTracking().CountAsync(s => !s.IsDeleted && s.Aktif, ct);

        var puantajlar = await db.PersonelPuantajlar.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Yil * 100 + p.Ay >= basAy && p.Yil * 100 + p.Ay <= bitAy)
            .Select(p => p.OnayDurumu)
            .ToListAsync(ct);

        var maaslar = await db.MaasOdemeSnapshotlar.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Yil * 100 + m.Ay >= basAy && m.Yil * 100 + m.Ay <= bitAy)
            .Select(m => new { m.PersonelId, m.Odenecek })
            .ToListAsync(ct);

        var avanslar = avans.Bos
            ? []
            : await db.PersonelAvanslar.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Durum != AvansDurum.IptalEdildi && a.AvansTarihi >= avans.Bas && a.AvansTarihi < avans.Sonra)
                .Select(a => a.Tutar)
                .ToListAsync(ct);

        return new PersonelOzeti
        {
            AktifPersonel = aktif,
            PuantajKaydi = puantajlar.Count,
            PuantajOnayli = puantajlar.Count(p => p == PersonelPuantajOnayDurumu.Onaylandi),
            PuantajOnayBekleyen = puantajlar.Count(p => p == PersonelPuantajOnayDurumu.OnayBekliyor),
            PuantajTaslak = puantajlar.Count(p => p == PersonelPuantajOnayDurumu.Taslak),
            PuantajReddedilen = puantajlar.Count(p => p == PersonelPuantajOnayDurumu.Reddedildi),
            MaasKaydiPersonel = maaslar.Select(m => m.PersonelId).Distinct().Count(),
            MaasOdenecek = maaslar.Sum(m => m.Odenecek),
            AvansAdet = avanslar.Count,
            AvansTutar = avanslar.Sum()
        };
    }

    #endregion

    #region Araç

    private async Task<AracOzeti> AracAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var yerel = Aralik.Gunler(d.Baslangic, d.EtkinBitis);
        var utc = yerel.Utc();
        var onceki = Aralik.Gunler(d.OncekiBaslangic, d.OncekiBitis);

        await using var db = await _factory.CreateDbContextAsync(ct);

        var aktif = await db.Araclar.AsNoTracking().CountAsync(a => !a.IsDeleted && a.Aktif, ct);
        var yeni = utc.Bos ? 0 : await db.Araclar.AsNoTracking().CountAsync(a => !a.IsDeleted && a.CreatedAt >= utc.Bas && a.CreatedAt < utc.Sonra, ct);

        var masraflar = yerel.Bos
            ? []
            : await db.AracMasraflari.AsNoTracking()
                .Where(m => !m.IsDeleted && m.MasrafTarihi >= yerel.Bas && m.MasrafTarihi < yerel.Sonra)
                .Select(m => m.Tutar)
                .ToListAsync(ct);

        var oncekiMasraflar = onceki.Bos
            ? []
            : await db.AracMasraflari.AsNoTracking()
                .Where(m => !m.IsDeleted && m.MasrafTarihi >= onceki.Bas && m.MasrafTarihi < onceki.Sonra)
                .Select(m => m.Tutar)
                .ToListAsync(ct);

        return new AracOzeti
        {
            AktifArac = aktif,
            YeniArac = yeni,
            MasrafAdet = masraflar.Count,
            MasrafTutari = masraflar.Sum(),
            OncekiMasrafTutari = oncekiMasraflar.Sum()
        };
    }

    #endregion

    #region Yeni kayıtlar (cari, personel, evrak)

    private async Task<KayitOzeti> KayitAsync(Baglam b, YoneticiOzetKapsami kapsam, CancellationToken ct)
    {
        var utc = Aralik.Gunler(b.Donem.Baslangic, b.Donem.EtkinBitis).Utc();
        await using var db = await _factory.CreateDbContextAsync(ct);

        int? yeniCari = null, yeniPersonel = null, gelen = null, giden = null;

        if (kapsam.Cari)
            yeniCari = utc.Bos ? 0 : await db.Cariler.AsNoTracking().CountAsync(c => !c.IsDeleted && c.CreatedAt >= utc.Bas && c.CreatedAt < utc.Sonra, ct);

        if (kapsam.Personel)
            yeniPersonel = utc.Bos ? 0 : await db.Soforler.AsNoTracking().CountAsync(s => !s.IsDeleted && s.CreatedAt >= utc.Bas && s.CreatedAt < utc.Sonra, ct);

        if (kapsam.Evrak)
        {
            // EBYS evrakları firmaya bağlı değildir (modül genelinde ortak).
            if (utc.Bos)
            {
                gelen = 0;
                giden = 0;
            }
            else
            {
                var evraklar = await db.EbysEvraklar.AsNoTracking()
                    .Where(e => !e.IsDeleted && e.KayitTarihi != null && e.KayitTarihi >= utc.Bas && e.KayitTarihi < utc.Sonra)
                    .Select(e => e.Yon)
                    .ToListAsync(ct);
                gelen = evraklar.Count(y => y == EvrakYonu.Gelen);
                giden = evraklar.Count(y => y == EvrakYonu.Giden);
            }
        }

        return new KayitOzeti { YeniCari = yeniCari, YeniPersonel = yeniPersonel, GelenEvrak = gelen, GidenEvrak = giden };
    }

    #endregion

    #region Rent a Car

    private async Task<RentACarOzeti> RentACarAsync(Baglam b, CancellationToken ct)
    {
        var d = b.Donem;
        var yerel = Aralik.Gunler(d.Baslangic, d.EtkinBitis);
        var utc = yerel.Utc();

        await using var db = await _factory.CreateDbContextAsync(ct);

        var yeni = utc.Bos
            ? 0
            : await db.MusteriKiralamalar.AsNoTracking()
                .CountAsync(k => !k.IsDeleted && k.Durum != KiralamaDurumu.IptalEdildi && k.CreatedAt >= utc.Bas && k.CreatedAt < utc.Sonra, ct);

        var tamamlanan = yerel.Bos
            ? []
            : await db.MusteriKiralamalar.AsNoTracking()
                .Where(k => !k.IsDeleted && k.Durum == KiralamaDurumu.Tamamlandi
                            && k.GercekBitisTarihi != null && k.GercekBitisTarihi >= yerel.Bas && k.GercekBitisTarihi < yerel.Sonra)
                .Select(k => k.ToplamTutar)
                .ToListAsync(ct);

        var aktif = await db.MusteriKiralamalar.AsNoTracking().CountAsync(k => !k.IsDeleted && k.Durum == KiralamaDurumu.Aktif, ct);

        return new RentACarOzeti
        {
            YeniKiralama = yeni,
            TamamlananKiralama = tamamlanan.Count,
            TamamlananCiro = tamamlanan.Sum(),
            AktifKiralama = aktif
        };
    }

    #endregion

    #region Ekip (aktivite günlüğü)

    private async Task<EkipOzeti> EkipAsync(Baglam b, CancellationToken ct)
    {
        var utc = Aralik.Gunler(b.Donem.Baslangic, b.Donem.EtkinBitis).Utc();
        if (utc.Bos)
            return new EkipOzeti();

        await using var db = await _factory.CreateDbContextAsync(ct);

        // Yalnızca adı bilinen kullanıcıların ana kayıt işlemleri sayılır: sistem/arka plan işleri ve
        // detay satırları (fatura kalemi, günlük puantaj satırı vb.) "iş" gürültüsü yaratır.
        IQueryable<AktiviteLog> Kaynak() => db.AktiviteLoglar.AsNoTracking()
            .Where(l => l.IslemZamani >= utc.Bas && l.IslemZamani < utc.Sonra
                        && l.KullaniciAdi != null && l.KullaniciAdi != "" && l.KullaniciAdi != "Sistem"
                        && l.EntityTipi != null && AnaKayitTurleri.Contains(l.EntityTipi)
                        && IslemTipleri.Contains(l.IslemTipi));

        var gruplar = await Kaynak()
            .GroupBy(l => new { l.KullaniciAdi, l.EntityTipi, l.IslemTipi })
            .Select(g => new
            {
                g.Key.KullaniciAdi,
                g.Key.EntityTipi,
                g.Key.IslemTipi,
                Adet = g.Count(),
                Son = g.Max(x => x.IslemZamani)
            })
            .ToListAsync(ct);

        var sonlar = await Kaynak()
            .OrderByDescending(l => l.IslemZamani)
            .Take(SonIslemSayisi)
            .Select(l => new { l.IslemZamani, l.KullaniciAdi, l.EntityTipi, l.IslemTipi, l.EntityAdi })
            .ToListAsync(ct);

        var kullanicilar = gruplar
            .GroupBy(g => g.KullaniciAdi!)
            .Select(g => new EkipKullanici
            {
                KullaniciAdi = g.Key,
                Toplam = g.Sum(x => x.Adet),
                Eklenen = g.Where(x => EklemeTipleri.Contains(x.IslemTipi)).Sum(x => x.Adet),
                Guncellenen = g.Where(x => GuncellemeTipleri.Contains(x.IslemTipi)).Sum(x => x.Adet),
                Silinen = g.Where(x => SilmeTipleri.Contains(x.IslemTipi)).Sum(x => x.Adet),
                SonIslem = BusinessTime.LocalAt(g.Max(x => x.Son))
            })
            .OrderByDescending(k => k.Toplam)
            .ThenBy(k => k.KullaniciAdi, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var isTurleri = gruplar
            .GroupBy(g => AnaKayitEtiketleri[g.EntityTipi!])
            .Select(g => new EkipIsTuru { Etiket = g.Key, Adet = g.Sum(x => x.Adet) })
            .OrderByDescending(t => t.Adet)
            .ThenBy(t => t.Etiket, StringComparer.CurrentCultureIgnoreCase)
            .Take(IsTuruSayisi)
            .ToList();

        return new EkipOzeti
        {
            ToplamIslem = gruplar.Sum(g => g.Adet),
            AktifKullanici = kullanicilar.Count,
            Kullanicilar = kullanicilar.Take(EnAktifKullaniciSayisi).ToList(),
            IsTurleri = isTurleri,
            SonIslemler = sonlar.Select(l => new EkipIslem
            {
                Zaman = BusinessTime.LocalAt(l.IslemZamani),
                Kullanici = l.KullaniciAdi ?? string.Empty,
                Etiket = AnaKayitEtiketleri[l.EntityTipi!],
                Tur = IslemTuru(l.IslemTipi),
                Ayrinti = string.IsNullOrWhiteSpace(l.EntityAdi) ? null : l.EntityAdi
            }).ToList()
        };
    }

    private static EkipIslemTuru IslemTuru(string islemTipi)
        => SilmeTipleri.Contains(islemTipi) ? EkipIslemTuru.Silindi
            : EklemeTipleri.Contains(islemTipi) ? EkipIslemTuru.Eklendi
            : EkipIslemTuru.Guncellendi;

    #endregion

    #region Dikkat gerektirenler

    private async Task<List<DikkatKalemi>> DikkatAsync(Baglam b, YoneticiOzetKapsami kapsam, int? kritikBelge, CancellationToken ct)
    {
        var bugun = b.Donem.Bugun;
        var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);
        var liste = new List<DikkatKalemi>();

        await using var db = await _factory.CreateDbContextAsync(ct);

        if (kapsam.Hakedis)
        {
            var taslaklar = await db.Hakedisler.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Durum == HakedisDurum.Taslak)
                .Select(h => new { h.GenelToplam, h.Tutar })
                .ToListAsync(ct);
            if (taslaklar.Count > 0)
                liste.Add(Kalem("taslak-hakedis", "Onay bekleyen taslak hakediş", taslaklar.Count,
                    taslaklar.Sum(h => h.GenelToplam > 0 ? h.GenelToplam : h.Tutar),
                    DikkatSeviyesi.Uyari, "bi-hourglass-split", "/operasyonel-hakedis"));

            var faturasizlar = await db.Hakedisler.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Durum == HakedisDurum.Onaylandi && h.Tip != HakedisTipi.Arac)
                .Select(h => new { h.GenelToplam, h.Tutar })
                .ToListAsync(ct);
            if (faturasizlar.Count > 0)
                liste.Add(Kalem("faturasiz-hakedis", "Faturalanmayı bekleyen onaylı hakediş", faturasizlar.Count,
                    faturasizlar.Sum(h => h.GenelToplam > 0 ? h.GenelToplam : h.Tutar),
                    DikkatSeviyesi.Uyari, "bi-receipt-cutoff", "/operasyonel-hakedis"));
        }

        if (kapsam.Servis)
        {
            var onaysiz = await db.FiloGunlukPuantajlar.AsNoTracking()
                .CountAsync(p => !p.IsDeleted && !p.Onaylandi && p.Tarih < bugun.AddDays(1), ct);
            if (onaysiz > 0)
                liste.Add(Kalem("onaysiz-servis-puantaji", "Onaylanmamış servis puantajı", onaysiz, null,
                    DikkatSeviyesi.Uyari, "bi-clipboard-check", "/operasyon/puantaj",
                    "bugüne kadarki günlük kayıt"));
        }

        if (kapsam.Personel)
        {
            var bekleyen = await db.PersonelPuantajlar.AsNoTracking()
                .CountAsync(p => !p.IsDeleted && p.OnayDurumu == PersonelPuantajOnayDurumu.OnayBekliyor, ct);
            if (bekleyen > 0)
                liste.Add(Kalem("onay-bekleyen-personel-puantaji", "Onay bekleyen personel puantajı", bekleyen, null,
                    DikkatSeviyesi.Uyari, "bi-person-check", "/personel/puantaj"));
        }

        if (kapsam.Fatura)
        {
            // Ödendi/iptal faturalar sunucuda elenir; kalan tutar kontrolü (eski tutarsız kayıtlar için) bellekte yapılır.
            var gecikenler = await db.Faturalar.AsNoTracking()
                .Where(f => !f.IsDeleted && f.Durum != FaturaDurum.IptalEdildi && f.Durum != FaturaDurum.Odendi
                            && f.VadeTarihi != null && f.VadeTarihi < bugun)
                .Select(f => new { f.FaturaYonu, f.FaturaTipi, f.GenelToplam, f.OdenenTutar, f.VadeTarihi })
                .ToListAsync(ct);

            var tahsilat = gecikenler
                .Where(f => f.FaturaYonu == FaturaYonu.Giden && !IadeMi(f.FaturaTipi) && f.GenelToplam - f.OdenenTutar > 0)
                .ToList();
            if (tahsilat.Count > 0)
                liste.Add(Kalem("geciken-tahsilat", "Vadesi geçmiş kesilen fatura (tahsil edilecek)", tahsilat.Count,
                    tahsilat.Sum(f => f.GenelToplam - f.OdenenTutar),
                    tahsilat.Any(f => (bugun - f.VadeTarihi!.Value.Date).Days >= KritikGecikmeGunu) ? DikkatSeviyesi.Kritik : DikkatSeviyesi.Uyari,
                    "bi-cash-coin", "/efatura/giden"));

            var odeme = gecikenler
                .Where(f => f.FaturaYonu == FaturaYonu.Gelen && !IadeMi(f.FaturaTipi) && f.GenelToplam - f.OdenenTutar > 0)
                .ToList();
            if (odeme.Count > 0)
                liste.Add(Kalem("geciken-odeme", "Vadesi geçmiş alınan fatura (ödenecek)", odeme.Count,
                    odeme.Sum(f => f.GenelToplam - f.OdenenTutar),
                    odeme.Any(f => (bugun - f.VadeTarihi!.Value.Date).Days >= KritikGecikmeGunu) ? DikkatSeviyesi.Kritik : DikkatSeviyesi.Uyari,
                    "bi-credit-card-2-front", "/efatura/gelen"));
        }

        if (kapsam.Finans)
        {
            // Dashboard'daki "Toplam Devir Borç" ile aynı tanım: ay başından önceki, ödenmemiş (kalan aktarılmamış) kayıtlar.
            var devirSorgu = db.BudgetOdemeler.AsNoTracking()
                .Where(o => !o.IsDeleted
                            && (o.Durum == OdemeDurum.Bekliyor || o.Durum == OdemeDurum.KismiOdendi)
                            && o.OdemeTarihi < ayBasi
                            && !o.KalanSonrakiDonemeAktarilsin);
            if (b.FirmaId.HasValue)
                devirSorgu = devirSorgu.Where(o => o.FirmaId == b.FirmaId.Value);

            var devirler = await devirSorgu.Select(o => new { o.Miktar, o.OdemeTarihi }).ToListAsync(ct);
            if (devirler.Count > 0)
            {
                var kritik = devirler.Count(o => (bugun - o.OdemeTarihi.Date).Days >= KritikDevirGunu);
                liste.Add(Kalem("devir-borc", "Geçmiş aylardan devreden ödenmemiş borç", devirler.Count,
                    devirler.Sum(o => o.Miktar),
                    kritik > 0 ? DikkatSeviyesi.Kritik : DikkatSeviyesi.Uyari,
                    "bi-arrow-repeat", "/butce/odemeler",
                    kritik > 0 ? $"{kritik} kayıt {KritikDevirGunu} günü aştı" : null));
            }
        }

        if (kapsam.Evrak)
        {
            var bekleyenEvrak = await db.EbysEvraklar.AsNoTracking()
                .CountAsync(e => !e.IsDeleted
                                 && (e.Durum == EbysEvrakDurum.Beklemede || e.Durum == EbysEvrakDurum.Isleniyor
                                     || e.Durum == EbysEvrakDurum.AtamaBekliyor || e.Durum == EbysEvrakDurum.CevapBekliyor), ct);
            if (bekleyenEvrak > 0)
                liste.Add(Kalem("bekleyen-evrak", "İşlem bekleyen evrak", bekleyenEvrak, null,
                    DikkatSeviyesi.Bilgi, "bi-inbox", "/ebys/takip"));

            if (kritikBelge is > 0)
                liste.Add(Kalem("kritik-belge", "Süresi dolmuş veya dolmak üzere olan belge", kritikBelge.Value, null,
                    DikkatSeviyesi.Kritik, "bi-file-earmark-medical", "/belge-uyarilari"));
        }

        return liste;
    }

    private static DikkatKalemi Kalem(
        string anahtar, string baslik, int adet, decimal? tutar, DikkatSeviyesi seviye, string ikon, string link, string? aciklama = null)
        => new()
        {
            Anahtar = anahtar,
            Baslik = baslik,
            Adet = adet,
            Tutar = tutar,
            Seviye = seviye,
            Ikon = ikon,
            Link = link,
            Aciklama = aciklama
        };

    #endregion

    #region Yardımcılar

    private sealed record Baglam(YoneticiOzetDonemi Donem, int? FirmaId);

    /// <summary>Yarı açık gün aralığı [Bas, Sonra) — iş günü saatiyle.</summary>
    private readonly record struct Aralik(DateTime Bas, DateTime Sonra)
    {
        public bool Bos => Sonra <= Bas;

        public static Aralik Gunler(DateTime ilk, DateTime son) => new(ilk.Date, son.Date.AddDays(1));

        /// <summary>UTC damgalı kolonlar (CreatedAt, IslemZamani, OnayTarihi) için aynı aralığın UTC karşılığı.</summary>
        public Aralik Utc() => new(YerelGunBasiUtc(Bas), YerelGunBasiUtc(Sonra));

        private static DateTime YerelGunBasiUtc(DateTime yerel)
            => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(yerel, DateTimeKind.Unspecified), IsGunuDilimi);
    }

    private static DateTime Kucuk(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime Buyuk(DateTime a, DateTime b) => a > b ? a : b;

    /// <summary>Günlük (≤ 62 gün) veya aylık kovalara dağıtılmış iki değerli zaman serisi üretir.</summary>
    private static ZamanSerisi Seri(
        YoneticiOzetDonemi d,
        IEnumerable<(DateTime Tarih, decimal V1, decimal V2)> noktalar,
        string etiket1,
        string etiket2)
    {
        var kovalar = new List<SeriNoktasi>();

        if (!d.GrafikAylik)
        {
            var toplamlar = noktalar
                .GroupBy(n => n.Tarih.Date)
                .ToDictionary(g => g.Key, g => (V1: g.Sum(x => x.V1), V2: g.Sum(x => x.V2)));

            // Pencere iki aya yayılıyorsa yalnız gün numarası belirsiz kalır ("1" Eylül mü Ekim mi?): gün.ay yazılır.
            var cokAy = d.GrafikBaslangic.Year != d.GrafikBitis.Year || d.GrafikBaslangic.Month != d.GrafikBitis.Month;

            for (var gun = d.GrafikBaslangic.Date; gun <= d.GrafikBitis.Date; gun = gun.AddDays(1))
            {
                toplamlar.TryGetValue(gun, out var v);
                var etiket = cokAy ? $"{gun.Day}.{gun.Month}" : gun.Day.ToString(Tr);
                kovalar.Add(new SeriNoktasi(gun, etiket, gun.ToString("d MMM", Tr), v.V1, v.V2, gun > d.Bugun));
            }
        }
        else
        {
            var toplamlar = noktalar
                .GroupBy(n => new DateTime(n.Tarih.Year, n.Tarih.Month, 1))
                .ToDictionary(g => g.Key, g => (V1: g.Sum(x => x.V1), V2: g.Sum(x => x.V2)));
            var buAy = new DateTime(d.Bugun.Year, d.Bugun.Month, 1);

            for (var ay = new DateTime(d.GrafikBaslangic.Year, d.GrafikBaslangic.Month, 1); ay <= d.GrafikBitis; ay = ay.AddMonths(1))
            {
                toplamlar.TryGetValue(ay, out var v);
                kovalar.Add(new SeriNoktasi(ay, ay.ToString("MMM", Tr), ay.ToString("MMMM yyyy", Tr), v.V1, v.V2, ay > buAy));
            }
        }

        return new ZamanSerisi { Noktalar = kovalar, Aylik = d.GrafikAylik, Etiket1 = etiket1, Etiket2 = etiket2 };
    }

    #endregion
}
