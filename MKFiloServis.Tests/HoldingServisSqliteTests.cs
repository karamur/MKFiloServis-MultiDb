using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>
/// Holding toplayıcısı: aktif firma seçiminden bağımsız tüm firmaların toplanması, doğru dönem alanları,
/// iptal/taslak kayıtların dışarıda bırakılması, ödenmemiş fatura, bütçe tanımı ve canlı vade verisi.
/// </summary>
public sealed class HoldingServisSqliteTests
{
    private static HoldingService Servis(IDbContextFactory<ApplicationDbContext> fabrika)
        => new(fabrika, NullLogger<HoldingService>.Instance);

    private static async Task<HoldingVeri> SatirAsync(IDbContextFactory<ApplicationDbContext> fabrika, int firmaId, int ay, string kategori = "KARZARAR")
    {
        await using var db = fabrika.CreateDbContext();
        return await db.HoldingVeriler.AsNoTracking()
            .SingleAsync(v => v.FirmaId == firmaId && v.Yil == 2026 && v.Ay == ay && v.Kategori == kategori);
    }

    [Fact]
    public async Task Toplama_aktif_firma_baska_firma_secili_olsa_bile_tum_firmalari_toplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        // Fixture'da aktif firma 1; firma 2'nin kayıtları firma filtresi yüzünden eskiden 0 çıkıyordu.
        Assert.Equal(1, f.ActiveFirm.AktifFirmaId);
        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 4);

        var bir = await SatirAsync(f.Factory, 1, 4);
        var iki = await SatirAsync(f.Factory, 2, 4);

        Assert.Equal(1500m, bir.ToplamGelir);
        Assert.Equal(3000m, iki.ToplamGelir);
        Assert.Equal(1000m, iki.ToplamGider);
        Assert.Equal(2000m, iki.Kar);
        Assert.Equal(100m, iki.AraclarMaliyet);
        Assert.Equal(200m, iki.PersonelMaliyet);
        Assert.Equal(2, iki.AktifAracSayisi);
        Assert.Equal(1, iki.PersonelSayisi);
        Assert.Equal("Diğer Firma", iki.FirmaAdi);
    }

    [Fact]
    public async Task Toplama_oturumsuz_calisan_aylik_is_icin_de_tum_firmalari_toplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        // Quartz işi bir kullanıcı oturumu olmadan çalışır: aktif firma sağlayıcısı yoktur ve firma filtresi hiçbir kaydı göstermez.
        await using var tohum = f.Factory.CreateDbContext();
        var secenekler = (DbContextOptions<ApplicationDbContext>)tohum.GetService<IDbContextOptions>();
        ApplicationDbContext.AmbientServiceProvider.Value = null;
        var oturumsuz = new OturumsuzFabrika(secenekler);

        await using (var kontrol = oturumsuz.CreateDbContext())
            Assert.Equal(0, await kontrol.Faturalar.CountAsync()); // eski davranışın nedeni: filtre her şeyi gizler

        await Servis(oturumsuz).ToplaVeKaydetAsync(2026, 4);

        Assert.Equal(1500m, (await SatirAsync(f.Factory, 1, 4)).ToplamGelir);
        Assert.Equal(3000m, (await SatirAsync(f.Factory, 2, 4)).ToplamGelir);
    }

    [Fact]
    public async Task Toplama_iptal_ve_iade_faturalari_saymaz_odenmemis_alis_tutarini_hesaplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 4);
        var s = await SatirAsync(f.Factory, 1, 4);

        // Satış: 1000 + 500 (iptal 9999, iade 100, mart/mayıs faturaları hariç). Alış: 600 + 400 + 300 (iptal 777 hariç).
        Assert.Equal(1500m, s.ToplamGelir);
        Assert.Equal(1300m, s.ToplamGider);
        Assert.Equal(200m, s.Kar);
        // Ödenmemiş: 500 (kısmi) + 0 (ödendi) + 300 (bekleyen).
        Assert.Equal(800m, s.OdenmemisFaturaToplam);
    }

    [Fact]
    public async Task Toplama_arac_masrafini_masraf_tarihine_maasi_ve_hakedisi_donem_alanina_gore_aylandirir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 4);
        var s = await SatirAsync(f.Factory, 1, 4);

        // Kayıt tarihi (CreatedAt) bugündür; ay masraf tarihiyle belirlenir: 250 + 150 (mart/mayıs hariç).
        Assert.Equal(400m, s.AraclarMaliyet);
        // Nisan maaşı 700 (mart 900 ve fixture'ın ekim maaşı hariç).
        Assert.Equal(700m, s.PersonelMaliyet);
        // Aktif araç: biri pasif; aktif personel: fixture'ın personeli + Ali (Veli pasif).
        Assert.Equal(1, s.AktifAracSayisi);
        Assert.Equal(2, s.PersonelSayisi);

        // Mart ayı toplanınca aynı kayıtlar o aya düşer.
        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 3);
        var mart = await SatirAsync(f.Factory, 1, 3);
        Assert.Equal(400m, mart.AraclarMaliyet);
        Assert.Equal(900m, mart.PersonelMaliyet);
    }

    [Fact]
    public async Task Toplama_hakedisi_gelir_ve_gider_olarak_ayirir_taslak_iptal_ve_ic_raporu_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 4);
        var s = await SatirAsync(f.Factory, 1, 4);

        // Gelir (kurum): 800 onaylı + 200 faturalı; taslak 5000, iptal 7000 ve mart 6000 hariç.
        Assert.Equal(1000m, s.HakedisToplam);
        // Gider: tedarikçi 300 + araç sahibi 150; araç (iç rapor) 9999 hariç.
        Assert.Equal(450m, HoldingDetay.Oku(s.JsonDetay)!.HakedisGider);
    }

    [Fact]
    public async Task Toplama_butceyi_butce_modulundeki_tanimla_toplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        await Servis(f.Factory).ToplaVeKaydetAsync(2026, 4);

        // Firma 1: yıllık hedef (Ay = 0) 12000; aynı yılın aylık 1000'lik hedefi ikinci kez eklenmez.
        var bir = await SatirAsync(f.Factory, 1, 4, "BUTCE");
        Assert.Equal(12000m, bir.ButceHedef);
        // Nisan: 450 (ödenen tutar) + 300 (tutar) + 200 (faturayla kapatıldı); bekleyen 999 hariç.
        Assert.Equal(950m, bir.ButceGerceklesen);
        // Yıl başından: nisan 950 + mart 1000.
        Assert.Equal(1950m, bir.ToplamGelir);

        // Firma 2: yıllık hedef yok → aylık hedeflerin toplamı.
        var iki = await SatirAsync(f.Factory, 2, 4, "BUTCE");
        Assert.Equal(1200m, iki.ButceHedef);
        Assert.Equal(0m, iki.ButceGerceklesen);
    }

    [Fact]
    public async Task Toplama_ikinci_kez_calisinca_ayni_satiri_gunceller()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);
        var servis = Servis(f.Factory);

        await servis.ToplaVeKaydetAsync(2026, 4);
        await using (var db = f.Factory.CreateDbContext())
        {
            db.Faturalar.Add(YeniFatura("S-YENI", new DateTime(2026, 4, 29), FaturaTipi.SatisFaturasi, 250m, 0m, FaturaDurum.Beklemede, firma: 1, cari: 1));
            await db.SaveChangesAsync();
        }

        await servis.ToplaVeKaydetAsync(2026, 4);

        await using var kontrol = f.Factory.CreateDbContext();
        Assert.Equal(1, await kontrol.HoldingVeriler.CountAsync(v => v.FirmaId == 1 && v.Ay == 4 && v.Kategori == "KARZARAR"));
        Assert.Equal(1750m, (await SatirAsync(f.Factory, 1, 4)).ToplamGelir);
    }

    [Fact]
    public async Task Toplama_hicbir_firma_toplanamazsa_basarili_gibi_gorunmez_hata_verir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        // İlk bağlantı (firma listesi) çalışır, firma başına açılan bağlantılar patlar: hiçbir firma toplanamaz.
        var servis = Servis(new IlkCagridanSonraPatlayanFabrika(f.Factory));

        var hata = await Assert.ThrowsAsync<InvalidOperationException>(() => servis.ToplaVeKaydetAsync(2026, 4));
        Assert.Contains("hiçbir firmanın verisi toplanamadı", hata.Message);

        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(0, await db.HoldingVeriler.CountAsync());                  // yarım/boş satır da yazılmaz
    }

    [Fact]
    public async Task Son_veri_donemi_en_yeni_yil_ve_ayi_verir_veri_yoksa_null()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = Servis(f.Factory);
        Assert.Null(await servis.GetSonVeriDonemiAsync());

        await servis.ToplaVeKaydetAsync(2025, 12);
        await servis.ToplaVeKaydetAsync(2026, 2);
        await servis.ToplaVeKaydetAsync(2026, 1);

        Assert.Equal(new HoldingDonemSecimi(2026, 2), await servis.GetSonVeriDonemiAsync());
    }

    [Fact]
    public async Task Acik_faturalar_tum_aktif_firmalarin_kalan_tutarini_verir_iptal_odenen_ve_pasif_firmayi_saymaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f.Factory);

        var acik = await Servis(f.Factory).GetAcikFaturalarAsync();

        // Firma 1: S1 satış kalan 600, S5 mart satış kalan 2000, A1 alış kalan 500, A3 alış kalan 300 (iptal/ödenmiş/iade hariç).
        var bir = acik.Where(k => k.FirmaId == 1).ToList();
        Assert.Equal(4, bir.Count);
        Assert.Equal(2600m, bir.Where(k => !k.Borc).Sum(k => k.Kalan));
        Assert.Equal(800m, bir.Where(k => k.Borc).Sum(k => k.Kalan));
        Assert.Equal(new DateTime(2026, 5, 10), bir.Single(k => k.Borc && k.Kalan == 500m).Vade);

        // Firma 2 de görünür (aktif firma 1 seçili olsa bile); pasif firma 3 görünmez.
        var iki = acik.Where(k => k.FirmaId == 2).ToList();
        Assert.Equal(2, iki.Count);
        Assert.Equal("Diğer Firma", iki[0].FirmaAdi);
        Assert.DoesNotContain(acik, k => k.FirmaId == 3);
    }

    [Fact]
    public void Detay_bozuk_ya_da_bos_metni_sessizce_yok_sayar_ve_gidis_donus_yapar()
    {
        Assert.Null(HoldingDetay.Oku(null));
        Assert.Null(HoldingDetay.Oku("  "));
        Assert.Null(HoldingDetay.Oku("{bozuk"));
        Assert.Equal(12.5m, HoldingDetay.Oku(HoldingDetay.Yaz(new HoldingDetay { HakedisGider = 12.5m }))!.HakedisGider);
    }

    // ---- tohum ----

    private static Fatura YeniFatura(string no, DateTime tarih, FaturaTipi tip, decimal genel, decimal odenen, FaturaDurum durum, int firma, int cari, DateTime? vade = null) => new()
    {
        FaturaNo = no, FaturaTarihi = tarih, FaturaTipi = tip, GenelToplam = genel, OdenenTutar = odenen, Durum = durum,
        VadeTarihi = vade, FirmaId = firma, CariId = cari,
        FaturaYonu = tip == FaturaTipi.AlisFaturasi ? FaturaYonu.Gelen : FaturaYonu.Giden
    };

    private static async Task TohumlaAsync(IDbContextFactory<ApplicationDbContext> fabrika)
    {
        await using var db = fabrika.CreateDbContext();

        db.Firmalar.AddRange(
            new Firma { Id = 2, FirmaKodu = "F2", FirmaAdi = "Diğer Firma", Aktif = true },
            new Firma { Id = 3, FirmaKodu = "F3", FirmaAdi = "Pasif Firma", Aktif = false });

        Cari Cari(string kod, int firma) => new() { CariKodu = kod, Unvan = "Cari " + kod, CariTipi = CariTipi.Musteri, FirmaId = firma };
        var cari1 = Cari("C1", 1);
        var cari2 = Cari("C2", 2);
        var cari3 = Cari("C3", 3);
        db.Cariler.AddRange(cari1, cari2, cari3);

        Arac Arac(string sase, int firma, bool aktif) => new() { SaseNo = sase, AktifPlaka = "34 " + sase, SahiplikTipi = AracSahiplikTipi.Ozmal, FirmaId = firma, Aktif = aktif };
        var arac1 = Arac("S1", 1, true);
        var arac1Pasif = Arac("S2", 1, false);
        var arac2a = Arac("S3", 2, true);
        var arac2b = Arac("S4", 2, true);
        var kalem1 = new MasrafKalemi { MasrafKodu = "YKT1", MasrafAdi = "Yakıt", Kategori = MasrafKategori.Yakit, FirmaId = 1 };
        var kalem2 = new MasrafKalemi { MasrafKodu = "YKT2", MasrafAdi = "Yakıt", Kategori = MasrafKategori.Yakit, FirmaId = 2 };
        Sofor Sofor(string kod, string ad, int firma, bool aktif) => new() { SoforKodu = kod, Ad = ad, Soyad = "Test", FirmaId = firma, Aktif = aktif };
        var ali = Sofor("P1", "Ali", 1, true);
        var veli = Sofor("P2", "Veli", 1, false);
        var can = Sofor("P3", "Can", 2, true);
        db.AddRange(arac1, arac1Pasif, arac2a, arac2b, kalem1, kalem2, ali, veli, can);
        await db.SaveChangesAsync();

        // --- firma 1 faturaları ---
        db.Faturalar.AddRange(
            YeniFatura("S1", new DateTime(2026, 4, 10), FaturaTipi.SatisFaturasi, 1000, 400, FaturaDurum.KismiOdendi, 1, cari1.Id, new DateTime(2026, 5, 5)),
            YeniFatura("S2", new DateTime(2026, 4, 15), FaturaTipi.SatisFaturasi, 500, 500, FaturaDurum.Odendi, 1, cari1.Id),
            YeniFatura("S3", new DateTime(2026, 4, 20), FaturaTipi.SatisFaturasi, 9999, 0, FaturaDurum.IptalEdildi, 1, cari1.Id),
            YeniFatura("S4", new DateTime(2026, 4, 21), FaturaTipi.SatisIadeFaturasi, 100, 0, FaturaDurum.Beklemede, 1, cari1.Id),
            YeniFatura("S5", new DateTime(2026, 3, 31), FaturaTipi.SatisFaturasi, 2000, 0, FaturaDurum.Beklemede, 1, cari1.Id, new DateTime(2026, 4, 30)),
            YeniFatura("S6", new DateTime(2026, 5, 1), FaturaTipi.SatisFaturasi, 1000, 1000, FaturaDurum.Odendi, 1, cari1.Id),
            YeniFatura("A1", new DateTime(2026, 4, 5), FaturaTipi.AlisFaturasi, 600, 100, FaturaDurum.KismiOdendi, 1, cari1.Id, new DateTime(2026, 5, 10)),
            YeniFatura("A2", new DateTime(2026, 4, 6), FaturaTipi.AlisFaturasi, 400, 400, FaturaDurum.Odendi, 1, cari1.Id),
            YeniFatura("A3", new DateTime(2026, 4, 28), FaturaTipi.AlisFaturasi, 300, 0, FaturaDurum.Beklemede, 1, cari1.Id),
            YeniFatura("A4", new DateTime(2026, 4, 12), FaturaTipi.AlisFaturasi, 777, 0, FaturaDurum.IptalEdildi, 1, cari1.Id));

        // --- firma 2 / pasif firma 3 ---
        db.Faturalar.AddRange(
            YeniFatura("F2S1", new DateTime(2026, 4, 8), FaturaTipi.SatisFaturasi, 3000, 3000, FaturaDurum.Odendi, 2, cari2.Id),
            YeniFatura("F2A1", new DateTime(2026, 4, 9), FaturaTipi.AlisFaturasi, 1000, 0, FaturaDurum.Beklemede, 2, cari2.Id, new DateTime(2026, 4, 1)),
            YeniFatura("F2A2", new DateTime(2026, 5, 9), FaturaTipi.AlisFaturasi, 250, 0, FaturaDurum.Beklemede, 2, cari2.Id),
            YeniFatura("F3A1", new DateTime(2026, 4, 9), FaturaTipi.AlisFaturasi, 5000, 0, FaturaDurum.Beklemede, 3, cari3.Id));

        // --- araç masrafı: ay masraf tarihinden gelir, CreatedAt bugündür ---
        AracMasraf M(DateTime tarih, decimal tutar, int arac, int kalem, int firma) => new()
        {
            FirmaId = firma, AracId = arac, MasrafKalemiId = kalem, MasrafTarihi = tarih, Tutar = tutar
        };
        db.AracMasraflari.AddRange(
            M(new DateTime(2026, 4, 12), 250, arac1.Id, kalem1.Id, 1),
            M(new DateTime(2026, 4, 30), 150, arac1.Id, kalem1.Id, 1),
            M(new DateTime(2026, 5, 1), 999, arac1.Id, kalem1.Id, 1),
            M(new DateTime(2026, 3, 31), 400, arac1.Id, kalem1.Id, 1),
            M(new DateTime(2026, 4, 3), 100, arac2a.Id, kalem2.Id, 2));

        // --- maaş: dönem (Yıl/Ay) alanından ---
        PersonelMaas Maas(int sofor, int ay, decimal net, int firma) => new() { SoforId = sofor, FirmaId = firma, Yil = 2026, Ay = ay, NetMaas = net };
        db.PersonelMaaslari.AddRange(
            Maas(ali.Id, 4, 700, 1),
            Maas(ali.Id, 3, 900, 1),
            Maas(can.Id, 4, 200, 2));

        // --- hakediş ---
        var referans = 100;
        Hakedis H(HakedisTipi tip, HakedisDurum durum, decimal genel, int ay = 4) => new()
        {
            FirmaId = 1, Yil = 2026, Ay = ay, Tip = tip, Durum = durum, ReferansId = referans++, GenelToplam = genel, Tutar = genel
        };
        db.Hakedisler.AddRange(
            H(HakedisTipi.Kurum, HakedisDurum.Onaylandi, 800),
            H(HakedisTipi.Kurum, HakedisDurum.Faturalandi, 200),
            H(HakedisTipi.Kurum, HakedisDurum.Taslak, 5000),
            H(HakedisTipi.Kurum, HakedisDurum.Iptal, 7000),
            H(HakedisTipi.Tedarikci, HakedisDurum.Odendi, 300),
            H(HakedisTipi.AracSahibi, HakedisDurum.Onaylandi, 150),
            H(HakedisTipi.Arac, HakedisDurum.Onaylandi, 9999),
            H(HakedisTipi.Kurum, HakedisDurum.Onaylandi, 6000, ay: 3));

        // --- bütçe ---
        BudgetHedef Hedef(int firma, int ay, decimal tutar, string kalem) => new() { Yil = 2026, Ay = ay, MasrafKalemi = kalem, HedefTutar = tutar, FirmaId = firma };
        db.BudgetHedefler.AddRange(
            Hedef(1, 0, 12000, "Kira"),
            Hedef(1, 4, 1000, "Kira"),
            Hedef(2, 3, 500, "Kira"),
            Hedef(2, 4, 700, "Kira"));
        BudgetOdeme O(int ay, decimal miktar, OdemeDurum durum, decimal? odenen = null, bool faturaIleKapatildi = false) => new()
        {
            OdemeTarihi = new DateTime(2026, ay, 10), OdemeAy = ay, OdemeYil = 2026, MasrafKalemi = "Kira", Miktar = miktar,
            Durum = durum, OdenenTutar = odenen, FaturaIleKapatildi = faturaIleKapatildi, FirmaId = 1
        };
        db.BudgetOdemeler.AddRange(
            O(4, 500, OdemeDurum.Odendi, odenen: 450),
            O(4, 300, OdemeDurum.Odendi),
            O(4, 200, OdemeDurum.Bekliyor, faturaIleKapatildi: true),
            O(4, 999, OdemeDurum.Bekliyor),
            O(3, 1000, OdemeDurum.Odendi));

        await db.SaveChangesAsync();
    }

    /// <summary>İlk bağlantıyı verir, sonrakilerde hata fırlatır (firma başına toplamanın tümünün düşmesini taklit eder).</summary>
    private sealed class IlkCagridanSonraPatlayanFabrika(IDbContextFactory<ApplicationDbContext> ic) : IDbContextFactory<ApplicationDbContext>
    {
        private int _sayac;

        public ApplicationDbContext CreateDbContext()
            => Interlocked.Increment(ref _sayac) == 1 ? ic.CreateDbContext() : throw new InvalidOperationException("test: bağlantı yok");

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    /// <summary>Kullanıcı oturumu (aktif firma sağlayıcısı) olmayan bağlam: örneğin Quartz işi.</summary>
    private sealed class OturumsuzFabrika(DbContextOptions<ApplicationDbContext> secenekler) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(secenekler);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
