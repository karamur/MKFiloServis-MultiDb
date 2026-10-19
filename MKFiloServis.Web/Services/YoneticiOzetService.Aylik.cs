using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Shared.Time;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Aylık grafik raporu (dashboard): yönetici özetiyle aynı kaynak ve kurallar, ama 12 aylık seri olarak.
/// <para>
/// Her bölüm kendi bağlamıyla ve kendi try/catch'iyle çalışır; kayıtlar geçen yılın başından seçilen yılın sonuna
/// kadar tek sorguda çekilir (geçen yılın aynı dönemiyle karşılaştırma için) ve bellekte aylara dağıtılır.
/// Gerçekleşen işler bugüne kadar sayılır; yalnız planlı sefer ve bütçe ödemeleri ileri aylara da uzanır.
/// </para>
/// </summary>
public sealed partial class YoneticiOzetService
{
    #region Kart tanımları

    private static readonly AylikKartTanimi ServisKarti = new(
        "servis", "Servis Operasyonu", "Aylık yapılan ve planlı sefer", "bi-truck", "primary", "/operasyon/puantaj",
        AylikBirim.Adet, "Yapılan sefer", "Planlı sefer", GrafikRengi.Mavi, GrafikRengi.Mavi, true, false, "sefer", Yigili: true);

    private static readonly AylikKartTanimi HakedisKarti = new(
        "hakedis", "Hakediş", "Üretilen hakedişin gelir ve gider tutarı", "bi-receipt-cutoff", "info", "/operasyonel-hakedis",
        AylikBirim.Para, "Gelir", "Gider", GrafikRengi.Yesil, GrafikRengi.Kirmizi, true, false);

    private static readonly AylikKartTanimi FaturaKarti = new(
        "fatura", "Faturalar", "Kesilen ve alınan fatura tutarı (iade ve iptal hariç)", "bi-receipt", "success", "/faturalar",
        AylikBirim.Para, "Kesilen", "Alınan", GrafikRengi.Yesil, GrafikRengi.Turuncu, true, false);

    private static readonly AylikKartTanimi KasaKarti = new(
        "kasa", "Banka / Kasa", "Nakit giriş ve çıkışları", "bi-bank", "warning", "/banka-hareketleri",
        AylikBirim.Para, "Tahsilat", "Ödeme", GrafikRengi.Yesil, GrafikRengi.Kirmizi, true, false);

    private static readonly AylikKartTanimi ButceKarti = new(
        "butce", "Bütçe ve Ödemeler", "Ödeme tarihine göre planlanan ve ödenen", "bi-wallet2", "danger", "/butce/odemeler",
        AylikBirim.Para, "Planlanan", "Ödenen", GrafikRengi.Gri, GrafikRengi.Mavi, false, false);

    private static readonly AylikKartTanimi PersonelKarti = new(
        "personel", "Personel ve Maaş", "Maaş dönem kaydı (ödenecek) ve verilen avans", "bi-people", "secondary", "/personel/maas",
        AylikBirim.Para, "Maaş dönem kaydı", "Avans", GrafikRengi.Mavi, GrafikRengi.Turuncu, false, false);

    private static readonly AylikKartTanimi AracKarti = new(
        "arac", "Araç Masrafları", "İşlenen araç masraf tutarı", "bi-car-front", "primary", "/araclar",
        AylikBirim.Para, "Masraf", string.Empty, GrafikRengi.Turuncu, GrafikRengi.Gri, false, false);

    private static readonly AylikKartTanimi RentACarKarti = new(
        "rentacar", "Rent a Car", "Tamamlanan kiralamaların cirosu", "bi-key", "info", "/rent-a-car",
        AylikBirim.Para, "Tamamlanan ciro", string.Empty, GrafikRengi.Mor, GrafikRengi.Gri, true, false);

    private static readonly AylikKartTanimi EvrakKarti = new(
        "evrak", "Evrak Trafiği", "Kayıt tarihine göre gelen ve giden evrak", "bi-inbox", "secondary", "/ebys/takip",
        AylikBirim.Adet, "Gelen evrak", "Giden evrak", GrafikRengi.Mavi, GrafikRengi.Turuncu, false, false, "evrak");

    private static readonly AylikKartTanimi EkipKarti = new(
        "ekip", "Ekip Hareketleri", "Kullanıcıların yaptığı kayıt işlemleri", "bi-activity", "primary", "/aktivite-log",
        AylikBirim.Adet, "Kayıt işlemi", string.Empty, GrafikRengi.Mor, GrafikRengi.Gri, false, false, "işlem");

    private static AylikKartTanimi KayitKarti(bool cari, bool personel) => new(
        "kayit", "Yeni Kayıtlar", cari && personel ? "Açılan cari ve personel kartları" : cari ? "Açılan cari kartları" : "Açılan personel kartları",
        "bi-journal-plus", "success", cari ? "/cariler" : "/soforler",
        AylikBirim.Adet, cari ? "Yeni cari" : "Yeni personel", cari && personel ? "Yeni personel" : string.Empty,
        GrafikRengi.Yesil, GrafikRengi.Mavi, true, false, "kayıt");

    #endregion

    public async Task<AylikGrafikRaporu> GetAylikGrafikAsync(AylikGrafikIstegi istek, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var bugun = (istek.Bugun ?? BusinessTime.Today).Date;
        var (enErken, enGec) = AylikGrafikIstegi.YilSiniri(bugun);
        var yil = Math.Clamp(istek.Yil ?? bugun.Year, enErken, enGec);
        var kapsam = istek.Kapsam ?? YoneticiOzetKapsami.Hicbiri;
        var b = new AylikBaglam(yil, bugun, _aktifFirma.TumFirmalar ? null : _aktifFirma.AktifFirmaId);
        var hatalar = new List<string>();
        var kartlar = new List<AylikGrafikKarti>();

        async Task EkleAsync(bool izinli, string ad, Func<Task<AylikGrafikKarti>> uret)
        {
            if (!izinli)
                return;

            var kart = await BolumAsync(ad, uret, hatalar, cancellationToken);
            if (kart != null)
                kartlar.Add(kart);
        }

        await EkleAsync(kapsam.Servis, "Servis operasyonu", async () =>
            AylikSeriHesaplayici.KartKur(ServisKarti, yil, bugun, await ServisAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Hakedis, "Hakediş", async () =>
            AylikSeriHesaplayici.KartKur(HakedisKarti, yil, bugun, await HakedisAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Fatura, "Faturalar", async () =>
            AylikSeriHesaplayici.KartKur(FaturaKarti, yil, bugun, await FaturaAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Finans, "Banka/Kasa", async () =>
            AylikSeriHesaplayici.KartKur(KasaKarti, yil, bugun, await KasaAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Finans, "Bütçe", async () =>
            AylikSeriHesaplayici.KartKur(ButceKarti, yil, bugun, await ButceAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Personel, "Personel ve maaş", async () =>
            AylikSeriHesaplayici.KartKur(PersonelKarti, yil, bugun, await PersonelAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Arac, "Araç masrafları", async () =>
            AylikSeriHesaplayici.KartKur(AracKarti, yil, bugun, await AracAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.RentACar, "Rent a Car", async () =>
            AylikSeriHesaplayici.KartKur(RentACarKarti, yil, bugun, await RentACarAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Cari || kapsam.Personel, "Yeni kayıtlar", async () =>
            AylikSeriHesaplayici.KartKur(KayitKarti(kapsam.Cari, kapsam.Personel), yil, bugun, await KayitAylikAsync(b, kapsam, cancellationToken)));
        await EkleAsync(kapsam.Evrak, "Evrak", async () =>
            AylikSeriHesaplayici.KartKur(EvrakKarti, yil, bugun, await EvrakAylikAsync(b, cancellationToken)));
        await EkleAsync(kapsam.Ekip, "Ekip hareketleri", async () =>
            AylikSeriHesaplayici.KartKur(EkipKarti, yil, bugun, await EkipAylikAsync(b, cancellationToken)));

        var kesim = AylikSeriHesaplayici.KesimGunu(yil, bugun);
        return new AylikGrafikRaporu
        {
            Yil = yil,
            Bugun = bugun,
            KesimGunu = kesim,
            KarsilastirmaMetni = kesim.HasValue
                ? YoneticiOzetDonemi.AralikEtiketi(new DateTime(yil - 1, 1, 1), AylikSeriHesaplayici.OncekiYilKesimi(yil, bugun))
                : string.Empty,
            UretimZamani = BusinessTime.Now,
            KapsamAdi = KapsamAdi(),
            Kartlar = kartlar,
            Hatalar = hatalar
        };
    }

    /// <summary>
    /// Aylık rapor bağlamı. <see cref="Bas"/> karşılaştırma için geçen yılın ilk günüdür;
    /// <see cref="Tum"/> planlı değerleri, <see cref="Gerceklesen"/> yalnız bugüne kadarkini kapsar.
    /// </summary>
    private sealed record AylikBaglam(int Yil, DateTime Bugun, int? FirmaId)
    {
        public DateTime Bas => new(Yil - 1, 1, 1);

        public DateTime Bit => new(Yil, 12, 31);

        public Aralik Tum => Aralik.Gunler(Bas, Bit);

        public Aralik Gerceklesen => Aralik.Gunler(Bas, Kucuk(Bit, Bugun));
    }

    private static decimal HakedisTutari(decimal genelToplam, decimal tutar) => genelToplam > 0 ? genelToplam : tutar;

    #region Aylık kaynaklar (her biri: tarih + iki değer)

    private async Task<List<AylikSatir>> ServisAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var satirlar = await SeferSatirlariAsync(b.Tum, ct);
        return satirlar
            .Where(s => !YapilamadiMi(s.Durum))
            .Select(s => s.Tarih <= b.Bugun
                ? new AylikSatir(s.Tarih, s.SeferSayisi)                // yapılan
                : new AylikSatir(s.Tarih, 0m, s.SeferSayisi))           // planlı (bugünden sonra)
            .ToList();
    }

    private async Task<List<AylikSatir>> HakedisAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var utc = b.Gerceklesen.Utc();
        if (utc.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.Hakedisler.AsNoTracking()
            .Where(h => !h.IsDeleted && h.Durum != HakedisDurum.Iptal && h.CreatedAt >= utc.Bas && h.CreatedAt < utc.Sonra)
            .Select(h => new { h.CreatedAt, h.Tip, h.GenelToplam, h.Tutar })
            .ToListAsync(ct);

        // Araç bazlı iç maliyet hakedişleri faturalanmaz; gelir ya da gider sayılmaz.
        return ham
            .Where(h => h.Tip.GelirMi() || h.Tip.GiderMi())
            .Select(h =>
            {
                var tutar = HakedisTutari(h.GenelToplam, h.Tutar);
                return new AylikSatir(BusinessTime.DateAt(h.CreatedAt), h.Tip.GelirMi() ? tutar : 0m, h.Tip.GiderMi() ? tutar : 0m);
            })
            .ToList();
    }

    private async Task<List<AylikSatir>> FaturaAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var aralik = b.Gerceklesen;
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.Faturalar.AsNoTracking()
            .Where(f => !f.IsDeleted && f.Durum != FaturaDurum.IptalEdildi && f.FaturaTarihi >= aralik.Bas && f.FaturaTarihi < aralik.Sonra)
            .Select(f => new { f.FaturaTarihi, f.FaturaYonu, f.FaturaTipi, f.GenelToplam })
            .ToListAsync(ct);

        return ham
            .Where(f => !IadeMi(f.FaturaTipi))
            .Select(f => new AylikSatir(
                f.FaturaTarihi.Date,
                f.FaturaYonu == FaturaYonu.Giden ? f.GenelToplam : 0m,
                f.FaturaYonu == FaturaYonu.Gelen ? f.GenelToplam : 0m))
            .ToList();
    }

    private async Task<List<AylikSatir>> KasaAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var satirlar = await HareketSatirlariAsync(b.Gerceklesen, ct);
        return satirlar
            .Select(h => new AylikSatir(
                h.Tarih,
                h.Tip == HareketTipi.Giris ? h.Tutar : 0m,
                h.Tip == HareketTipi.Cikis ? h.Tutar : 0m))
            .ToList();
    }

    private async Task<List<AylikSatir>> ButceAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var aralik = b.Tum;
        await using var db = await _factory.CreateDbContextAsync(ct);

        var ham = await ButceSorgusu(db, b.FirmaId)
            .Where(o => o.OdemeTarihi >= aralik.Bas && o.OdemeTarihi < aralik.Sonra)
            .Select(o => new
            {
                o.OdemeTarihi,
                o.Miktar,
                o.Durum,
                o.FaturaIleKapatildi,
                o.OdenenTutar,
                o.MasrafKesintisi,
                o.CezaKesintisi,
                o.DigerKesinti
            })
            .ToListAsync(ct);

        return ham
            .Select(o => new AylikSatir(
                o.OdemeTarihi.Date,
                o.Miktar,
                ButceOdenenMi(o.Durum, o.FaturaIleKapatildi)
                    ? ButceOdenenTutar(o.Miktar, o.Durum, o.OdenenTutar, o.MasrafKesintisi + o.CezaKesintisi + o.DigerKesinti)
                    : 0m))
            .ToList();
    }

    private async Task<List<AylikSatir>> PersonelAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var avans = b.Gerceklesen;
        await using var db = await _factory.CreateDbContextAsync(ct);

        var maaslar = await db.MaasOdemeSnapshotlar.AsNoTracking()
            .Where(m => !m.IsDeleted && m.Yil >= b.Yil - 1 && m.Yil <= b.Yil && m.Ay >= 1 && m.Ay <= 12)
            .Select(m => new { m.Yil, m.Ay, m.Odenecek })
            .ToListAsync(ct);

        var avanslar = avans.Bos
            ? []
            : await db.PersonelAvanslar.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Durum != AvansDurum.IptalEdildi && a.AvansTarihi >= avans.Bas && a.AvansTarihi < avans.Sonra)
                .Select(a => new { a.AvansTarihi, a.Tutar })
                .ToListAsync(ct);

        // Maaş dönem kaydı ayın ilk gününe yazılır; avans verildiği güne.
        return maaslar.Select(m => new AylikSatir(new DateTime(m.Yil, m.Ay, 1), m.Odenecek))
            .Concat(avanslar.Select(a => new AylikSatir(a.AvansTarihi.Date, 0m, a.Tutar)))
            .ToList();
    }

    private async Task<List<AylikSatir>> AracAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var aralik = b.Gerceklesen;
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.AracMasraflari.AsNoTracking()
            .Where(m => !m.IsDeleted && m.MasrafTarihi >= aralik.Bas && m.MasrafTarihi < aralik.Sonra)
            .Select(m => new { m.MasrafTarihi, m.Tutar })
            .ToListAsync(ct);

        return ham.Select(m => new AylikSatir(m.MasrafTarihi.Date, m.Tutar)).ToList();
    }

    private async Task<List<AylikSatir>> RentACarAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var aralik = b.Gerceklesen;
        if (aralik.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.MusteriKiralamalar.AsNoTracking()
            .Where(k => !k.IsDeleted && k.Durum == KiralamaDurumu.Tamamlandi
                        && k.GercekBitisTarihi != null && k.GercekBitisTarihi >= aralik.Bas && k.GercekBitisTarihi < aralik.Sonra)
            .Select(k => new { k.GercekBitisTarihi, k.ToplamTutar })
            .ToListAsync(ct);

        return ham.Select(k => new AylikSatir(k.GercekBitisTarihi!.Value.Date, k.ToplamTutar)).ToList();
    }

    private async Task<List<AylikSatir>> KayitAylikAsync(AylikBaglam b, YoneticiOzetKapsami kapsam, CancellationToken ct)
    {
        var utc = b.Gerceklesen.Utc();
        if (utc.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);
        var satirlar = new List<AylikSatir>();

        // Cari yetkisi yoksa personel ilk seri olur (kart tanımı da buna göre kurulur).
        if (kapsam.Cari)
        {
            var cariler = await db.Cariler.AsNoTracking()
                .Where(c => !c.IsDeleted && c.CreatedAt >= utc.Bas && c.CreatedAt < utc.Sonra)
                .Select(c => c.CreatedAt)
                .ToListAsync(ct);
            satirlar.AddRange(cariler.Select(z => new AylikSatir(BusinessTime.DateAt(z), 1m)));
        }

        if (kapsam.Personel)
        {
            var personeller = await db.Soforler.AsNoTracking()
                .Where(s => !s.IsDeleted && s.CreatedAt >= utc.Bas && s.CreatedAt < utc.Sonra)
                .Select(s => s.CreatedAt)
                .ToListAsync(ct);
            satirlar.AddRange(personeller.Select(z => kapsam.Cari
                ? new AylikSatir(BusinessTime.DateAt(z), 0m, 1m)
                : new AylikSatir(BusinessTime.DateAt(z), 1m)));
        }

        return satirlar;
    }

    private async Task<List<AylikSatir>> EvrakAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var utc = b.Gerceklesen.Utc();
        if (utc.Bos)
            return [];

        // EBYS evrakları firmaya bağlı değildir (modül genelinde ortak).
        await using var db = await _factory.CreateDbContextAsync(ct);
        var ham = await db.EbysEvraklar.AsNoTracking()
            .Where(e => !e.IsDeleted && e.KayitTarihi != null && e.KayitTarihi >= utc.Bas && e.KayitTarihi < utc.Sonra)
            .Select(e => new { e.KayitTarihi, e.Yon })
            .ToListAsync(ct);

        return ham
            .Select(e => new AylikSatir(
                BusinessTime.DateAt(e.KayitTarihi!.Value),
                e.Yon == EvrakYonu.Gelen ? 1m : 0m,
                e.Yon == EvrakYonu.Giden ? 1m : 0m))
            .ToList();
    }

    private async Task<List<AylikSatir>> EkipAylikAsync(AylikBaglam b, CancellationToken ct)
    {
        var utc = b.Gerceklesen.Utc();
        if (utc.Bos)
            return [];

        await using var db = await _factory.CreateDbContextAsync(ct);

        // Yönetici özetiyle aynı süzgeç: adı bilinen kullanıcıların ana kayıt işlemleri.
        var zamanlar = await db.AktiviteLoglar.AsNoTracking()
            .Where(l => l.IslemZamani >= utc.Bas && l.IslemZamani < utc.Sonra
                        && l.KullaniciAdi != null && l.KullaniciAdi != "" && l.KullaniciAdi != "Sistem"
                        && l.EntityTipi != null && AnaKayitTurleri.Contains(l.EntityTipi)
                        && IslemTipleri.Contains(l.IslemTipi))
            .Select(l => l.IslemZamani)
            .ToListAsync(ct);

        return zamanlar.Select(z => new AylikSatir(BusinessTime.DateAt(z), 1m)).ToList();
    }

    #endregion
}
