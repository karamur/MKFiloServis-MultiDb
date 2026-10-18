using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>
/// Kapsamlı hakediş: araç sahibi, güzergâh, eşleştirme, cari, gün aralığı filtreleri ile
/// önizleme, kapsamlı üretim ve çifte faturalama koruması.
/// </summary>
public sealed class OperasyonelHakedisKapsamSqliteTests
{
    private sealed record Veri(
        int CariA, int CariB, int KiralikCari, int KomisyoncuCari, int Tedarikci,
        int Guzergah1, int Guzergah2,
        int AracOzmal, int AracKiralik, int AracKomisyon, int AracTedarikci,
        int Sofor1, int Sofor2, int Eslestirme1, int Eslestirme2);

    [Fact]
    public async Task Onizleme_tum_ay_toplamlari_ve_sahip_secenekleri()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var onizleme = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));

        Assert.Equal(4, onizleme.Satirlar.Count);
        Assert.Equal(8m, onizleme.Satirlar.Sum(s => s.Sefer));
        Assert.Equal(1050m, onizleme.Satirlar.Sum(s => s.Gelir));
        Assert.Equal(520m, onizleme.Satirlar.Sum(s => s.Gider));
        Assert.Equal(40m, onizleme.Satirlar.Sum(s => s.Maliyet));
        Assert.All(onizleme.Satirlar, s => Assert.False(s.HakedisteMi));
        Assert.Equal(2, onizleme.OlusacakHakedisSayisi);

        // Filtre kutuları yalnızca dönemde gerçekten kullanılan değerleri gösterir.
        var sahipler = onizleme.Secenekler.Sahipler.Select(s => s.Anahtar).ToHashSet();
        Assert.Equal(new[] { "O", $"K:{d.KiralikCari}", $"M:{d.KomisyoncuCari}", $"T:{d.Tedarikci}" }.ToHashSet(), sahipler);
        Assert.Equal(new[] { "0", d.Eslestirme1.ToString(), d.Eslestirme2.ToString() }.ToHashSet(),
            onizleme.Secenekler.Eslestirmeler.Select(s => s.Anahtar).ToHashSet());
        Assert.Equal(2, onizleme.Secenekler.Cariler.Count);
    }

    [Fact]
    public async Task Onizleme_filtreler_arac_sahibi_guzergah_eslestirme_cari_ve_gun_araligina_gore_daraltir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        async Task<(decimal Sefer, decimal Gelir)> Topla(Action<HakedisKapsami> ayarla)
        {
            var kapsam = HakedisKapsami.Donem(2026, 10);
            ayarla(kapsam);
            var o = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, kapsam);
            return (o.Satirlar.Sum(s => s.Sefer), o.Satirlar.Sum(s => s.Gelir));
        }

        // Araç sahibi: belirli tedarikçi
        Assert.Equal((4m, 600m), await Topla(k => k.SahipAnahtarlari.Add($"T:{d.Tedarikci}")));
        // Araç sahibi: sahiplik tipi
        Assert.Equal((1m, 100m), await Topla(k => k.SahiplikTipleri.Add(AracSahiplikTipi.Kiralik)));
        Assert.Equal((3m, 350m), await Topla(k => k.SahiplikTipleri.AddRange(new[] { AracSahiplikTipi.Ozmal, AracSahiplikTipi.Komisyon })));
        // Güzergâh
        Assert.Equal((3m, 300m), await Topla(k => k.GuzergahIdleri.Add(d.Guzergah1)));
        // Eşleştirme (0 = elle girilen)
        Assert.Equal((2m, 200m), await Topla(k => k.EslestirmeIdleri.Add(d.Eslestirme1)));
        Assert.Equal((2m, 250m), await Topla(k => k.EslestirmeIdleri.Add(0)));
        // Cari
        Assert.Equal((5m, 750m), await Topla(k => k.CariIdleri.Add(d.CariB)));
        // Araç + şoför
        Assert.Equal((4m, 600m), await Topla(k => { k.AracIdleri.Add(d.AracTedarikci); k.SoforIdleri.Add(d.Sofor2); }));
        // Servis türü
        Assert.Equal((1m, 150m), await Topla(k => k.ServisTurleri.Add(ServisTuru.Aksam)));
        // Gün aralığı: 16 Ekim'deki puantaj dışarıda kalır
        Assert.Equal((6m, 750m), await Topla(k =>
        {
            k.BaslangicTarihi = new DateTime(2026, 10, 1);
            k.BitisTarihi = new DateTime(2026, 10, 15);
        }));
        // Birden fazla filtre birlikte (VE)
        Assert.Equal((2m, 300m), await Topla(k =>
        {
            k.CariIdleri.Add(d.CariB);
            k.SahipAnahtarlari.Add($"T:{d.Tedarikci}");
            k.BitisTarihi = new DateTime(2026, 10, 15);
        }));
    }

    [Fact]
    public async Task Tedarikci_turu_yalniz_tedarikci_araclarini_alir_digerlerini_tur_disi_sayar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var onizleme = await servis.KapsamOnizleAsync(HakedisTipi.Tedarikci, HakedisKapsami.Donem(2026, 10));

        var satir = Assert.Single(onizleme.Satirlar);
        Assert.Equal(d.Tedarikci, satir.ReferansId);
        Assert.Equal("Tedarikçi X", satir.ReferansAd);
        Assert.Equal(4m, satir.Sefer);
        Assert.Equal(400m, satir.HakedisTutari);
        Assert.Equal(4, onizleme.TurDisiPuantajSayisi);
        Assert.Equal(4m, onizleme.TurDisiSefer);
    }

    [Fact]
    public async Task Uretim_tum_kapsamda_cari_basina_taslak_uretir_ve_tekrar_calisinca_ayni_taslagi_yeniler()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var sonuc = await servis.KapsamliUretAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));
        Assert.Equal(2, sonuc.UretilenAdet);
        Assert.Equal(0, sonuc.HataliAdet);

        await using (var db = f.Factory.CreateDbContext())
        {
            var hakedisler = await db.Hakedisler.Include(h => h.Detaylar).OrderBy(h => h.ReferansId).ToListAsync();
            Assert.Equal(2, hakedisler.Count);

            var a = hakedisler.Single(h => h.ReferansId == d.CariA);
            Assert.Equal(300m, a.Tutar);
            Assert.Equal(3m, a.ToplamSeferSayisi);
            Assert.Equal(3, a.Detaylar.Count);
            Assert.Equal(360m, a.GenelToplam); // %20 KDV
            Assert.Null(a.GenerationParams);   // filtresiz taslak eski kayıtlarla aynı biçimde

            var b = hakedisler.Single(h => h.ReferansId == d.CariB);
            Assert.Equal(750m, b.Tutar);
            Assert.Equal(5m, b.ToplamSeferSayisi);
        }

        await servis.KapsamliUretAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));

        await using var kontrol = f.Factory.CreateDbContext();
        Assert.Equal(2, await kontrol.Hakedisler.CountAsync());
        Assert.Equal(6, await kontrol.HakedisDetaylari.CountAsync());
    }

    [Fact]
    public async Task Farkli_kapsamdaki_taslaklar_birbirinin_uzerine_yazilmaz_ayni_kapsam_yerinde_yenilenir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var tamAy = HakedisKapsami.Donem(2026, 10);
        var yalnizRota1 = HakedisKapsami.Donem(2026, 10);
        yalnizRota1.GuzergahIdleri.Add(d.Guzergah1);
        var ilkYari = HakedisKapsami.Donem(2026, 10);
        ilkYari.BitisTarihi = new DateTime(2026, 10, 15);

        await servis.KapsamliUretAsync(HakedisTipi.Kurum, tamAy);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, yalnizRota1);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, ilkYari);
        // Aynı kapsam tekrar: yeni kayıt açılmaz.
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, yalnizRota1);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, ilkYari);

        await using var db = f.Factory.CreateDbContext();
        var hakedisler = await db.Hakedisler.ToListAsync();

        // tam ay: A + B, güzergâh-1: yalnız A, ilk yarı: A + B  → 2 + 1 + 2
        Assert.Equal(5, hakedisler.Count);

        var rota1 = hakedisler.Single(h => h.ReferansId == d.CariA && h.GenerationParams != null
                                           && HakedisUretimParametreleri.Coz(h.GenerationParams)!.Kapsam!.GuzergahIdleri.Contains(d.Guzergah1));
        var parametre = HakedisUretimParametreleri.Coz(rota1.GenerationParams)!;
        Assert.Equal(yalnizRota1.Imza(), parametre.Imza);
        Assert.Contains("Rota Bir", parametre.Ozet);
        Assert.Equal(300m, rota1.Tutar);

        var ilkYariB = hakedisler.Single(h => h.ReferansId == d.CariB
                                              && HakedisUretimParametreleri.Coz(h.GenerationParams)?.Imza == ilkYari.Imza());
        Assert.Equal(450m, ilkYariB.Tutar); // 16 Ekim'deki 300 TL dışarıda
        Assert.Contains("01.10", HakedisUretimParametreleri.Coz(ilkYariB.GenerationParams)!.Ozet);
    }

    [Fact]
    public async Task Onayli_hakedisteki_puantajlar_yeni_uretimde_disarida_kalir_ve_onizlemede_isaretlenir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var yalnizRota1 = HakedisKapsami.Donem(2026, 10);
        yalnizRota1.GuzergahIdleri.Add(d.Guzergah1);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, yalnizRota1);

        int rota1Id;
        await using (var db = f.Factory.CreateDbContext())
            rota1Id = (await db.Hakedisler.SingleAsync()).Id;
        await servis.OnaylaAsync(rota1Id, "test");

        // Cari A'nın tüm satırları güzergâh-1'de olduğundan tam ay üretiminde A atlanır, yalnız B üretilir.
        var sonuc = await servis.KapsamliUretAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));
        Assert.Equal(1, sonuc.UretilenAdet);
        Assert.Contains(sonuc.Satirlar, s => s.ReferansId == d.CariA && s.Sonuc == "Atlandı");

        var onizleme = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));
        var cariASatirlari = onizleme.Satirlar.Where(s => s.CariId == d.CariA).ToList();
        var cariBSatirlari = onizleme.Satirlar.Where(s => s.CariId == d.CariB).ToList();
        Assert.NotEmpty(cariASatirlari);
        Assert.NotEmpty(cariBSatirlari);
        Assert.All(cariASatirlari, s => Assert.True(s.HakedisteMi));
        Assert.All(cariBSatirlari, s => Assert.False(s.HakedisteMi));
        Assert.Equal(1, onizleme.OlusacakHakedisSayisi);
    }

    [Fact]
    public async Task Cakisan_taslak_onaylanamaz_ve_toplu_onayda_yalniz_ilki_onaylanir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        // Aynı satırları içeren iki taslak: tüm ay ve yalnız güzergâh-1 (cari A)
        var yalnizRota1 = HakedisKapsami.Donem(2026, 10);
        yalnizRota1.GuzergahIdleri.Add(d.Guzergah1);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, HakedisKapsami.Donem(2026, 10));
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, yalnizRota1);

        int tamA, rota1;
        await using (var db = f.Factory.CreateDbContext())
        {
            var a = await db.Hakedisler.Where(h => h.ReferansId == d.CariA).OrderBy(h => h.Id).ToListAsync();
            Assert.Equal(2, a.Count);
            (tamA, rota1) = (a[0].Id, a[1].Id);
        }

        var toplu = await servis.TopluOnaylaAsync(new[] { tamA, rota1 }, "test");
        Assert.Equal(1, toplu.BasariliAdet);
        Assert.Equal(1, toplu.AtlananAdet);
        Assert.Contains(toplu.Satirlar, s => s.HakedisId == rota1 && s.Sonuc == "Atlandı" && s.Mesaj!.Contains($"#{tamA}"));

        // Tekil onay da aynı kuralla reddedilir.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => servis.OnaylaAsync(rota1, "test"));
        Assert.Contains($"#{tamA}", ex.Message);

        // Onaylı hakediş iptal edilince çakışma kalkar.
        await servis.IptalEtAsync(tamA);
        var onay = await servis.OnaylaAsync(rota1, "test");
        Assert.Equal(HakedisDurum.Onaylandi, onay.Durum);
    }

    [Fact]
    public async Task Taslak_yenileme_kendi_kapsamiyla_calisir_ve_yeni_puantaji_ekler()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var yalnizRota1 = HakedisKapsami.Donem(2026, 10);
        yalnizRota1.GuzergahIdleri.Add(d.Guzergah1);
        await servis.KapsamliUretAsync(HakedisTipi.Kurum, yalnizRota1);

        int hakedisId;
        await using (var db = f.Factory.CreateDbContext())
        {
            hakedisId = (await db.Hakedisler.SingleAsync()).Id;
            // Kapsam içine yeni bir puantaj girilir; kapsam dışına bir başkası.
            db.FiloGunlukPuantajlar.AddRange(
                Puantaj(new DateTime(2026, 10, 20), d.CariA, d.Guzergah1, d.AracOzmal, d.Sofor1, d.Eslestirme1, 1m, 100m, 0m, 10m),
                Puantaj(new DateTime(2026, 10, 21), d.CariB, d.Guzergah2, d.AracTedarikci, d.Sofor2, d.Eslestirme2, 1m, 150m, 100m));
            await db.SaveChangesAsync();
        }

        var sonuc = await servis.TaslaklariYenileAsync(new[] { hakedisId });
        Assert.Equal(1, sonuc.BasariliAdet);

        await using var kontrol = f.Factory.CreateDbContext();
        var hakedis = await kontrol.Hakedisler.Include(h => h.Detaylar).SingleAsync(h => h.Id == hakedisId);
        Assert.Equal(400m, hakedis.Tutar);   // 300 + yeni 100; güzergâh-2'deki puantaj kapsam dışı
        Assert.Equal(4, hakedis.Detaylar.Count);
        Assert.Equal(d.Guzergah1, HakedisUretimParametreleri.Coz(hakedis.GenerationParams)!.Kapsam!.GuzergahIdleri.Single());
    }

    [Fact]
    public async Task Arac_turu_ic_maliyeti_arac_bazinda_uretir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        var kapsam = HakedisKapsami.Donem(2026, 10);
        kapsam.SahiplikTipleri.AddRange(new[] { AracSahiplikTipi.Ozmal, AracSahiplikTipi.Kiralik });
        var sonuc = await servis.KapsamliUretAsync(HakedisTipi.Arac, kapsam);

        Assert.Equal(2, sonuc.UretilenAdet);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(20m, (await db.Hakedisler.SingleAsync(h => h.ReferansId == d.AracOzmal)).Tutar);
        Assert.Equal(20m, (await db.Hakedisler.SingleAsync(h => h.ReferansId == d.AracKiralik)).Tutar);
    }

    [Fact]
    public void Kapsam_imzasi_kararlidir_ve_tum_ay_filtresiz_sayilir()
    {
        var bos = HakedisKapsami.Donem(2026, 10);
        Assert.Equal(string.Empty, bos.Imza());
        Assert.False(bos.FiltreVarMi);

        // Ayın tamamını kapsayan gün aralığı filtre sayılmaz.
        bos.BaslangicTarihi = new DateTime(2026, 9, 25);
        bos.BitisTarihi = new DateTime(2026, 11, 3);
        Assert.True(bos.TumAyMi);
        Assert.Equal(string.Empty, bos.Imza());

        var a = HakedisKapsami.Donem(2026, 10);
        a.GuzergahIdleri.AddRange(new[] { 5, 2 });
        a.SahipAnahtarlari.Add("T:3");
        var b = HakedisKapsami.Donem(2026, 10);
        b.SahipAnahtarlari.Add("T:3");
        b.GuzergahIdleri.AddRange(new[] { 2, 5, 2 });
        Assert.Equal(a.Imza(), b.Imza());
        Assert.NotEqual(string.Empty, a.Imza());

        // Ters girilen gün aralığı yer değiştirir.
        var ters = HakedisKapsami.Donem(2026, 10);
        ters.BaslangicTarihi = new DateTime(2026, 10, 20);
        ters.BitisTarihi = new DateTime(2026, 10, 10);
        Assert.Equal((new DateTime(2026, 10, 10), new DateTime(2026, 10, 20)), ters.GunAraligi());
    }

    [Fact]
    public async Task Kurum_hakedisi_musteri_kurumu_eslestirmeden_alir_ve_eslestirme_kaynakli_hakedis_dogrudan_cariye_faturalanir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        // Eşleştirme-1'in müşteri kurumu B olarak değiştirilir; güzergâh-1'in kartında hâlâ A yazar.
        await using (var db = f.Factory.CreateDbContext())
        {
            (await db.FiloGuzergahEslestirmeleri.SingleAsync(e => e.Id == d.Eslestirme1)).KurumFirmaId = d.CariB;
            await db.SaveChangesAsync();
        }

        var eslestirmeKapsami = HakedisKapsami.Donem(2026, 10);
        eslestirmeKapsami.MusteriKaynagi = HakedisMusteriKaynagi.Eslestirme;
        var onizleme = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, eslestirmeKapsami);

        // A: yalnız eşleştirmesiz kiralık sefer (puantajın kendi müşterisi); B: eşleştirmeli 5 + elle girilen komisyon seferi.
        var a = onizleme.Satirlar.Where(s => s.CariId == d.CariA).ToList();
        var b = onizleme.Satirlar.Where(s => s.CariId == d.CariB).ToList();
        Assert.Equal(1m, a.Sum(s => s.Sefer));
        Assert.Equal(7m, b.Sum(s => s.Sefer));
        Assert.Equal(950m, b.Sum(s => s.HakedisTutari));

        // "Cari / kurum" filtresi de aynı kaynağa göre çalışır.
        var yalnizB = eslestirmeKapsami.Kopyala();
        yalnizB.CariIdleri.Add(d.CariB);
        var filtreli = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, yalnizB);
        Assert.Equal(7m, filtreli.Satirlar.Sum(s => s.Sefer));

        // Güzergâh kartı kaynağında önceki davranış korunur.
        var guzergahKapsami = HakedisKapsami.Donem(2026, 10);
        guzergahKapsami.MusteriKaynagi = HakedisMusteriKaynagi.Guzergah;
        var rota = await servis.KapsamOnizleAsync(HakedisTipi.Kurum, guzergahKapsami);
        Assert.Equal(3m, rota.Satirlar.Where(s => s.CariId == d.CariA).Sum(s => s.Sefer));
        Assert.Equal(5m, rota.Satirlar.Where(s => s.CariId == d.CariB).Sum(s => s.Sefer));

        await servis.KapsamliUretAsync(HakedisTipi.Kurum, eslestirmeKapsami);

        int hakedisId;
        await using (var db = f.Factory.CreateDbContext())
        {
            var hakedis = await db.Hakedisler.Include(h => h.Detaylar).SingleAsync(h => h.ReferansId == d.CariB);
            hakedisId = hakedis.Id;
            Assert.Equal(950m, hakedis.Tutar);
            Assert.True(HakedisUretimParametreleri.MusteriEslestirmedenMi(hakedis.GenerationParams));
            // Detaylar iki farklı güzergâh kartından gelir; eski kural bu hakedişi faturalayamazdı.
            Assert.Equal(2, hakedis.Detaylar.Select(x => x.GuzergahId).Distinct().Count());
        }

        await servis.OnaylaAsync(hakedisId, "test");
        var fatura = await servis.FaturayaDonustureAsync(hakedisId, new DateTime(2026, 10, 31));

        await using var kontrol = f.Factory.CreateDbContext();
        var kayit = await kontrol.Faturalar.SingleAsync(x => x.Id == fatura.Id);
        Assert.Equal(d.CariB, kayit.CariId);
        Assert.Equal(950m, kayit.AraToplam);
        Assert.Equal(FaturaTipi.SatisFaturasi, kayit.FaturaTipi);
    }

    [Fact]
    public async Task Arac_hakedisi_birim_fiyati_eslestirmeden_alir_ve_kaynak_secilebilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        await using (var db = f.Factory.CreateDbContext())
        {
            var e1 = await db.FiloGuzergahEslestirmeleri.SingleAsync(e => e.Id == d.Eslestirme1);
            (e1.KurumaKesilecekUcret, e1.TaseronaOdenenUcret) = (120m, 0m);      // özmal araç: ödenen yok
            var e2 = await db.FiloGuzergahEslestirmeleri.SingleAsync(e => e.Id == d.Eslestirme2);
            (e2.KurumaKesilecekUcret, e2.TaseronaOdenenUcret) = (150m, 100m);    // tedarikçi aracı
            await db.SaveChangesAsync();
        }

        async Task<Dictionary<int, decimal>> Tutarlar(HakedisBirimFiyatKaynagi kaynak)
        {
            var kapsam = HakedisKapsami.Donem(2026, 10);
            kapsam.BirimFiyatKaynagi = kaynak;
            var o = await servis.KapsamOnizleAsync(HakedisTipi.Arac, kapsam);
            return o.Satirlar.GroupBy(s => s.ReferansId).ToDictionary(g => g.Key, g => g.Sum(s => s.HakedisTutari));
        }

        // Varsayılan: eşleştirmedeki "taşerona / şoföre ödenen"; yoksa puantaj taşeron tahakkuku, o da yoksa maliyet kaydı.
        var odenen = await Tutarlar(HakedisBirimFiyatKaynagi.EslestirmeOdenen);
        Assert.Equal(400m, odenen[d.AracTedarikci]);   // 100 × 2 sefer × 2 gün
        Assert.Equal(20m, odenen[d.AracOzmal]);        // ödenen 0 → maliyet kaydı (10 × 2)
        Assert.Equal(20m, odenen[d.AracKiralik]);      // eşleştirmesiz → maliyet kaydı
        Assert.Equal(120m, odenen[d.AracKomisyon]);    // eşleştirmesiz → puantaj taşeron tahakkuku

        var kesilecek = await Tutarlar(HakedisBirimFiyatKaynagi.EslestirmeKesilecek);
        Assert.Equal(600m, kesilecek[d.AracTedarikci]); // 150 × 4 sefer
        Assert.Equal(240m, kesilecek[d.AracOzmal]);     // 120 × 2 sefer
        Assert.Equal(100m, kesilecek[d.AracKiralik]);   // eşleştirmesiz → puantaj kurum tahakkuku
        Assert.Equal(150m, kesilecek[d.AracKomisyon]);

        var maliyet = await Tutarlar(HakedisBirimFiyatKaynagi.MaliyetKaydi);
        Assert.Equal(0m, maliyet[d.AracTedarikci]);
        Assert.Equal(20m, maliyet[d.AracOzmal]);

        // Üretilen hakedişte birim fiyat eşleştirmeden gelir ve kaynak taslakta saklanır.
        var uretim = HakedisKapsami.Donem(2026, 10);
        uretim.AracIdleri.Add(d.AracTedarikci);
        uretim.BirimFiyatKaynagi = HakedisBirimFiyatKaynagi.EslestirmeKesilecek;
        await servis.KapsamliUretAsync(HakedisTipi.Arac, uretim);

        await using var kontrol = f.Factory.CreateDbContext();
        var hakedis = await kontrol.Hakedisler.Include(h => h.Detaylar).SingleAsync();
        Assert.Equal(600m, hakedis.Tutar);
        Assert.Equal(150m, hakedis.BirimFiyat);
        Assert.All(hakedis.Detaylar, x => Assert.Equal(150m, x.BirimFiyat));
        Assert.Equal(HakedisBirimFiyatKaynagi.EslestirmeKesilecek,
            HakedisUretimParametreleri.Coz(hakedis.GenerationParams)!.Kapsam!.BirimFiyatKaynagi);
    }

    [Fact]
    public async Task Arac_sahibi_hakedisi_komisyon_ve_kiralik_sahiplere_eslestirme_ucretiyle_uretilir_ve_faturalanir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var d = await TohumlaAsync(f.Factory);
        var servis = new OperasyonelHakedisService(f.Factory);

        int sahipsizAracId;
        await using (var db = f.Factory.CreateDbContext())
        {
            // Kiralık (2. sefer günü) ve komisyon araçları için eşleştirme; kiralık sahibi tanımsız bir araç da eklenir.
            var kiralikEsl = new FiloGuzergahEslestirme
            {
                FirmaId = 1, KurumFirmaId = d.CariA, GuzergahId = d.Guzergah1, AracId = d.AracKiralik, SoforId = d.Sofor1,
                KurumaKesilecekUcret = 100m, TaseronaOdenenUcret = 80m
            };
            var komisyonEsl = new FiloGuzergahEslestirme
            {
                FirmaId = 1, KurumFirmaId = d.CariB, GuzergahId = d.Guzergah2, AracId = d.AracKomisyon, SoforId = d.Sofor2,
                KurumaKesilecekUcret = 150m, TaseronaOdenenUcret = 90m
            };
            var sahipsiz = new Arac { SaseNo = "S9", AktifPlaka = "34 SHP 009", SahiplikTipi = AracSahiplikTipi.Kiralik, FirmaId = 1 };
            db.AddRange(kiralikEsl, komisyonEsl, sahipsiz);
            await db.SaveChangesAsync();

            // 5 Ekim (kiralık) ve 6 Ekim (komisyon) puantajları eşleştirmelere bağlanır.
            (await db.FiloGunlukPuantajlar.SingleAsync(p => p.Tarih == new DateTime(2026, 10, 5))).FiloGuzergahEslestirmeId = kiralikEsl.Id;
            (await db.FiloGunlukPuantajlar.SingleAsync(p => p.Tarih == new DateTime(2026, 10, 6))).FiloGuzergahEslestirmeId = komisyonEsl.Id;
            db.FiloGunlukPuantajlar.Add(Puantaj(new DateTime(2026, 10, 9), d.CariA, d.Guzergah1, sahipsiz.Id, d.Sofor1, null, 1m, 100m, 0m));
            await db.SaveChangesAsync();
            sahipsizAracId = sahipsiz.Id;
        }

        var onizleme = await servis.KapsamOnizleAsync(HakedisTipi.AracSahibi, HakedisKapsami.Donem(2026, 10));

        // Özmal (2) + tedarikçi (2) puantajı bu türe girmez.
        Assert.Equal(4, onizleme.TurDisiPuantajSayisi);
        Assert.Equal(2, onizleme.OlusacakHakedisSayisi);

        var kiralik = Assert.Single(onizleme.Satirlar, s => s.ReferansId == d.KiralikCari);
        Assert.Equal(80m, kiralik.HakedisTutari);   // eşleştirmedeki ödenen ücret
        Assert.Equal("Kiralık Sahibi", kiralik.ReferansAd);
        var komisyon = Assert.Single(onizleme.Satirlar, s => s.ReferansId == d.KomisyoncuCari);
        Assert.Equal(90m, komisyon.HakedisTutari);  // eşleştirme, puantajdaki 120 TL'lik tahakkukun önüne geçer

        var sahipsizSatir = Assert.Single(onizleme.Satirlar, s => s.AracId == sahipsizAracId);
        Assert.False(sahipsizSatir.Uygun);
        Assert.Contains("kiralık plaka sahibi", sahipsizSatir.UygunsuzlukNedeni);

        var sonuc = await servis.KapsamliUretAsync(HakedisTipi.AracSahibi, HakedisKapsami.Donem(2026, 10));
        Assert.Equal(2, sonuc.UretilenAdet);
        Assert.Contains(sonuc.Satirlar, s => s.Sonuc == "Atlandı" && s.ReferansId == 0);

        int kiralikHakedisId;
        await using (var db = f.Factory.CreateDbContext())
        {
            var hakedisler = await db.Hakedisler.OrderBy(h => h.ReferansId).ToListAsync();
            Assert.Equal(2, hakedisler.Count);
            Assert.All(hakedisler, h => Assert.Equal(HakedisTipi.AracSahibi, h.Tip));
            kiralikHakedisId = hakedisler.Single(h => h.ReferansId == d.KiralikCari).Id;
        }

        // Onay + fatura: sahibin carisine alış faturası kesilir.
        await servis.OnaylaAsync(kiralikHakedisId, "test");
        var fatura = await servis.FaturayaDonustureAsync(kiralikHakedisId, new DateTime(2026, 10, 31));
        await using var kontrol = f.Factory.CreateDbContext();
        var kayit = await kontrol.Faturalar.SingleAsync(x => x.Id == fatura.Id);
        Assert.Equal(d.KiralikCari, kayit.CariId);
        Assert.Equal(FaturaTipi.AlisFaturasi, kayit.FaturaTipi);
        Assert.Equal(FaturaYonu.Gelen, kayit.FaturaYonu);
        Assert.Equal(80m, kayit.AraToplam);

        // Aynı hakedişi tekrar üretmek onaylı satırı değiştirmez; kalan sahip için taslak yenilenir.
        var tekrar = await servis.KapsamliUretAsync(HakedisTipi.AracSahibi, HakedisKapsami.Donem(2026, 10));
        Assert.Equal(1, tekrar.UretilenAdet);
        Assert.Contains(tekrar.Satirlar, s => s.ReferansId == d.KiralikCari && s.Sonuc == "Atlandı");
    }

    // ── Veri ────────────────────────────────────────────────────────────────

    private static FiloGunlukPuantaj Puantaj(
        DateTime tarih, int cariId, int guzergahId, int aracId, int soforId, int? eslestirmeId,
        decimal sefer, decimal kurum, decimal taseron, decimal? maliyet = null,
        OperasyonDurumu durum = OperasyonDurumu.Gitti, ServisTuru servis = ServisTuru.SabahAksam) => new()
    {
        FirmaId = 1,
        Tarih = tarih,
        KurumFirmaId = cariId,
        GuzergahId = guzergahId,
        AracId = aracId,
        SoforId = soforId,
        FiloGuzergahEslestirmeId = eslestirmeId,
        SeferSayisi = sefer,
        PuantajCarpani = 1m,
        TahakkukEdenKurumUcreti = kurum,
        TahakkukEdenTaseronUcreti = taseron,
        MaliyetOzmalKiralik = maliyet,
        Durum = durum,
        ServisTuru = servis
    };

    private static async Task<Veri> TohumlaAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = factory.CreateDbContext();

        Cari Cari(string kod, string unvan) => new() { CariKodu = kod, Unvan = unvan, CariTipi = CariTipi.Musteri, FirmaId = 1 };
        var cariA = Cari("A", "Kurum A");
        var cariB = Cari("B", "Kurum B");
        var kiralikCari = Cari("K", "Kiralık Sahibi");
        var komisyoncuCari = Cari("M", "Komisyoncu");
        var tedarikci = new TasimaTedarikci { TedarikciKodu = "T1", Unvan = "Tedarikçi X" };
        db.AddRange(cariA, cariB, kiralikCari, komisyoncuCari, tedarikci);
        await db.SaveChangesAsync();

        Guzergah Guzergah(string kod, string ad, int cariId) => new() { GuzergahKodu = kod, GuzergahAdi = ad, CariId = cariId, FirmaId = 1 };
        var g1 = Guzergah("G1", "Rota Bir", cariA.Id);
        var g2 = Guzergah("G2", "Rota Iki", cariB.Id);

        Arac Arac(string sase, string plaka, AracSahiplikTipi tip) => new()
        {
            SaseNo = sase, AktifPlaka = plaka, SahiplikTipi = tip, FirmaId = 1
        };
        var ozmal = Arac("S1", "34 OZM 001", AracSahiplikTipi.Ozmal);
        var kiralik = Arac("S2", "34 KRL 002", AracSahiplikTipi.Kiralik);
        kiralik.KiralikCariId = kiralikCari.Id;
        var komisyon = Arac("S3", "34 KOM 003", AracSahiplikTipi.Komisyon);
        komisyon.KomisyoncuCariId = komisyoncuCari.Id;
        var tedarikAraci = Arac("S4", "34 TED 004", AracSahiplikTipi.Tedarikci);
        tedarikAraci.TasimaTedarikciId = tedarikci.Id;

        var sofor1 = new Sofor { SoforKodu = "SF1", Ad = "Ali", Soyad = "Yilmaz", FirmaId = 1 };
        var sofor2 = new Sofor { SoforKodu = "SF2", Ad = "Veli", Soyad = "Kaya", FirmaId = 1 };

        db.AddRange(g1, g2, ozmal, kiralik, komisyon, tedarikAraci, sofor1, sofor2);
        await db.SaveChangesAsync();

        var e1 = new FiloGuzergahEslestirme
        {
            FirmaId = 1, KurumFirmaId = cariA.Id, GuzergahId = g1.Id, AracId = ozmal.Id, SoforId = sofor1.Id
        };
        var e2 = new FiloGuzergahEslestirme
        {
            FirmaId = 1, KurumFirmaId = cariB.Id, GuzergahId = g2.Id, AracId = tedarikAraci.Id, SoforId = sofor2.Id
        };
        db.AddRange(e1, e2);
        await db.SaveChangesAsync();

        db.FiloGunlukPuantajlar.AddRange(
            Puantaj(new DateTime(2026, 10, 1), cariA.Id, g1.Id, ozmal.Id, sofor1.Id, e1.Id, 1m, 100m, 0m, 10m),
            Puantaj(new DateTime(2026, 10, 2), cariA.Id, g1.Id, ozmal.Id, sofor1.Id, e1.Id, 1m, 100m, 0m, 10m),
            Puantaj(new DateTime(2026, 10, 3), cariB.Id, g2.Id, tedarikAraci.Id, sofor2.Id, e2.Id, 2m, 300m, 200m),
            Puantaj(new DateTime(2026, 10, 16), cariB.Id, g2.Id, tedarikAraci.Id, sofor2.Id, e2.Id, 2m, 300m, 200m),
            Puantaj(new DateTime(2026, 10, 5), cariA.Id, g1.Id, kiralik.Id, sofor1.Id, null, 1m, 100m, 0m, 20m),
            Puantaj(new DateTime(2026, 10, 6), cariB.Id, g2.Id, komisyon.Id, sofor2.Id, null, 1m, 150m, 120m, servis: ServisTuru.Aksam),
            // Gitmedi → hakedişe girmez
            Puantaj(new DateTime(2026, 10, 7), cariA.Id, g1.Id, ozmal.Id, sofor1.Id, e1.Id, 1m, 100m, 0m, 10m, OperasyonDurumu.Gitmedi_Mazeretli),
            // Başka dönem
            Puantaj(new DateTime(2026, 11, 1), cariA.Id, g1.Id, ozmal.Id, sofor1.Id, e1.Id, 1m, 100m, 0m, 10m));
        await db.SaveChangesAsync();

        return new Veri(cariA.Id, cariB.Id, kiralikCari.Id, komisyoncuCari.Id, tedarikci.Id,
            g1.Id, g2.Id, ozmal.Id, kiralik.Id, komisyon.Id, tedarikAraci.Id,
            sofor1.Id, sofor2.Id, e1.Id, e2.Id);
    }
}
