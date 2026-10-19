using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

internal static class PuantajTestVerisi
{
    public static GunlukPuantaj Gun(DateTime tarih, decimal saat = 0, bool izin = false, bool mazeret = false)
    {
        var (normal, fazlaMesai) = PuantajKartKontrolu.Bol(saat);
        return new GunlukPuantaj
        {
            Gun = tarih.Day,
            Tarih = tarih,
            Calisti = saat > 0,
            Durum = saat > 0 ? 1 : izin ? 2 : mazeret ? 3 : 0,
            CalismaSaati = normal,
            FazlaMesaiSaat = fazlaMesai,
            Izinli = izin,
            Mazeret = mazeret
        };
    }

    /// <summary>Verilen ayın hafta içi (Pzt–Cum) günlerini, her gün için aynı saatle üretir; diğer günler boş kayıttır.</summary>
    public static List<GunlukPuantaj> HaftaIci(int yil, int ay, decimal saat)
    {
        var liste = new List<GunlukPuantaj>();
        for (var gun = 1; gun <= DateTime.DaysInMonth(yil, ay); gun++)
        {
            var tarih = new DateTime(yil, ay, gun);
            var haftaIci = tarih.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
            liste.Add(Gun(tarih, haftaIci ? saat : 0));
        }

        return liste;
    }

    public static Sofor SaatlikPersonel(decimal birimUcret = 100m, decimal kartSaati = 195m, decimal net = 15000m) => new()
    {
        BrutMaasHesaplamaTipi = BrutMaasHesaplamaTipi.Saatlik,
        BirimUcret = birimUcret,
        CalismaMiktari = kartSaati,
        BrutMaas = birimUcret * kartSaati,
        NetMaas = net
    };
}

public sealed class PuantajUcretHesabiTests
{
    [Fact]
    public void Brut_toplam_saat_carpi_saatlik_ucrettir_net_kart_oraniyla_olceklenir()
    {
        // 22 gün × 7,5 saat = 165 saat; kartta 195 saat × 100 ₺ = 19.500 ₺ brüt ve 15.000 ₺ net yazıyor.
        var gunler = PuantajTestVerisi.HaftaIci(2026, 10, 7.5m);

        var sonuc = PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(), gunler);

        Assert.True(sonuc.Hesaplandi);
        Assert.Equal(165m, sonuc.ToplamSaat);
        Assert.Equal(16500m, sonuc.Brut);
        Assert.Equal(12692.31m, sonuc.Net);
    }

    [Fact]
    public void Fazla_mesai_toplam_saata_dahildir()
    {
        // 8 saatlik günler: 7,5 normal + 0,5 fazla mesai → gün başına 8 saat ücretlenir.
        var gunler = PuantajTestVerisi.HaftaIci(2026, 10, 8m);

        var sonuc = PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(), gunler);

        Assert.Equal(176m, sonuc.ToplamSaat);
        Assert.Equal(17600m, sonuc.Brut);
        Assert.Equal(13538.46m, sonuc.Net);
    }

    [Fact]
    public void Kartta_aylik_saat_ya_da_net_yoksa_net_hesaplanmaz()
    {
        var gunler = PuantajTestVerisi.HaftaIci(2026, 10, 7.5m);

        var saatsiz = PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(kartSaati: 0m), gunler);
        var netsiz = PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(net: 0m), gunler);

        Assert.True(saatsiz.Hesaplandi);
        Assert.Equal(16500m, saatsiz.Brut);
        Assert.Null(saatsiz.Net);
        Assert.Equal(16500m, netsiz.Brut);
        Assert.Null(netsiz.Net);
    }

    [Theory]
    [InlineData(BrutMaasHesaplamaTipi.Aylik)]
    [InlineData(BrutMaasHesaplamaTipi.Gunluk)]
    [InlineData(BrutMaasHesaplamaTipi.Manuel)]
    public void Saatlik_olmayan_personelde_tutar_puantajdan_hesaplanmaz(BrutMaasHesaplamaTipi tip)
    {
        var personel = PuantajTestVerisi.SaatlikPersonel();
        personel.BrutMaasHesaplamaTipi = tip;

        Assert.False(PuantajUcretHesabi.Hesapla(personel, PuantajTestVerisi.HaftaIci(2026, 10, 7.5m)).Hesaplandi);
    }

    [Fact]
    public void Birim_ucret_girilmemisse_ya_da_personel_yoksa_hesaplanmaz()
    {
        Assert.False(PuantajUcretHesabi.Hesapla(null, []).Hesaplandi);
        Assert.False(PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(birimUcret: 0m), PuantajTestVerisi.HaftaIci(2026, 10, 7.5m)).Hesaplandi);
    }

    [Fact]
    public void Izinli_ve_mazeretli_gunler_ucretlendirilmez()
    {
        var gunler = new List<GunlukPuantaj>
        {
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 5), 7.5m),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 6), izin: true),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 7), mazeret: true)
        };

        var sonuc = PuantajUcretHesabi.Hesapla(PuantajTestVerisi.SaatlikPersonel(), gunler);

        Assert.Equal(7.5m, sonuc.ToplamSaat);
        Assert.Equal(750m, sonuc.Brut);
    }
}

public sealed class PuantajOzetTests
{
    [Fact]
    public void Gun_sayilari_ve_saatlik_tutar_gunluk_kayitlardan_yazilir()
    {
        var personel = PuantajTestVerisi.SaatlikPersonel();
        var puantaj = new PersonelPuantaj { BrutMaas = 19500m, NetOdeme = 15000m };
        var gunler = new List<GunlukPuantaj>
        {
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 1), 8m),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 2), 7.5m),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 5), izin: true),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 6), mazeret: true),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 7))
        };

        PuantajOzet.Uygula(puantaj, personel, gunler);

        Assert.Equal((2, 1, 1), (puantaj.CalisilanGun, puantaj.IzinGunu, puantaj.MazeretGunu));
        Assert.Equal(0.5m, puantaj.FazlaMesaiSaat);
        Assert.Equal(1550m, puantaj.BrutMaas); // 15,5 saat × 100
        Assert.Equal(1192.31m, puantaj.NetOdeme);
    }

    [Fact]
    public void Hic_gunluk_kayit_yoksa_tutar_degismez()
    {
        var puantaj = new PersonelPuantaj { BrutMaas = 19500m, NetOdeme = 15000m, CalisilanGun = 4 };

        PuantajOzet.Uygula(puantaj, PuantajTestVerisi.SaatlikPersonel(), []);

        Assert.Equal(0, puantaj.CalisilanGun);
        Assert.Equal((19500m, 15000m), (puantaj.BrutMaas, puantaj.NetOdeme));
    }

    [Fact]
    public void Saatlik_olmayan_personelde_tutar_degismez()
    {
        var personel = PuantajTestVerisi.SaatlikPersonel();
        personel.BrutMaasHesaplamaTipi = BrutMaasHesaplamaTipi.Aylik;
        var puantaj = new PersonelPuantaj { BrutMaas = 19500m, NetOdeme = 15000m };

        PuantajOzet.Uygula(puantaj, personel, PuantajTestVerisi.HaftaIci(2026, 10, 7.5m));

        Assert.Equal(22, puantaj.CalisilanGun);
        Assert.Equal((19500m, 15000m), (puantaj.BrutMaas, puantaj.NetOdeme));
    }
}

public sealed class PuantajKopyalamaTests
{
    private static PuantajKaynakAy Eylul(IReadOnlyList<GunlukPuantaj> gunler, params DateTime[] tatiller)
        => new(2026, 9, gunler, tatiller);

    [Fact]
    public void Haftanin_gunu_hizalamasi_her_hedef_gunu_ayni_haftanin_gunune_esler()
    {
        for (var gun = 1; gun <= 31; gun++)
        {
            var kaynak = PuantajKopyalama.KaynakGun(2026, 10, gun, 2026, 9, PuantajKopyaHizalama.HaftaninGunu);

            Assert.NotNull(kaynak);
            Assert.InRange(kaynak!.Value, 1, 30);
            Assert.Equal(new DateTime(2026, 10, gun).DayOfWeek, new DateTime(2026, 9, kaynak.Value).DayOfWeek);
        }

        // 1 Ekim 2026 Perşembe → 3 Eylül 2026 Perşembe.
        Assert.Equal(3, PuantajKopyalama.KaynakGun(2026, 10, 1, 2026, 9, PuantajKopyaHizalama.HaftaninGunu));
        // 29 Ekim karşılığı Eylül'de yok (31), bir hafta geriye sarılır: 24 Eylül Perşembe.
        Assert.Equal(24, PuantajKopyalama.KaynakGun(2026, 10, 29, 2026, 9, PuantajKopyaHizalama.HaftaninGunu));
    }

    [Fact]
    public void Ayin_gunu_hizalamasi_gun_numarasini_korur_ve_olmayan_gunu_bos_birakir()
    {
        Assert.Equal(15, PuantajKopyalama.KaynakGun(2026, 10, 15, 2026, 9, PuantajKopyaHizalama.AyinGunu));
        Assert.Null(PuantajKopyalama.KaynakGun(2026, 10, 31, 2026, 9, PuantajKopyaHizalama.AyinGunu));
    }

    [Fact]
    public void Calisilan_gunler_saat_ve_fazla_mesaiyle_tasinir()
    {
        var sonuc = PuantajKopyalama.Uret(2026, 10, Eylul(PuantajTestVerisi.HaftaIci(2026, 9, 8m)), new PuantajKopyaSecenekleri());

        Assert.Equal(31, sonuc.Count);
        Assert.Equal(22, sonuc.Count(g => g.Calisti));
        Assert.Equal(176m, PuantajKartKontrolu.ToplamSaat(sonuc));
        Assert.Equal(11m, PuantajKartKontrolu.ToplamFazlaMesai(sonuc));
        Assert.All(sonuc.Where(g => g.Tarih.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday), g => Assert.False(g.Calisti));
        Assert.All(sonuc.Where(g => g.Calisti), g => Assert.Equal(1, g.Durum));
    }

    [Fact]
    public void Fazla_mesai_kopyalanmayabilir()
    {
        var sonuc = PuantajKopyalama.Uret(2026, 10, Eylul(PuantajTestVerisi.HaftaIci(2026, 9, 8m)),
            new PuantajKopyaSecenekleri(FazlaMesaiKopyala: false));

        Assert.Equal(0m, PuantajKartKontrolu.ToplamFazlaMesai(sonuc));
        Assert.Equal(165m, PuantajKartKontrolu.ToplamSaat(sonuc));
    }

    [Fact]
    public void Izin_ve_mazeret_istisna_sayilir_ayni_haftanin_gununun_baska_haftadaki_normal_gunu_esas_alinir()
    {
        // Eylül'de 14–18 arası izinli; bu günler Ekim'in 12–16'sına denk gelir.
        var kaynak = PuantajTestVerisi.HaftaIci(2026, 9, 7.5m);
        for (var gun = 14; gun <= 18; gun++)
            kaynak[gun - 1] = PuantajTestVerisi.Gun(new DateTime(2026, 9, gun), izin: true);

        var kopya = PuantajKopyalama.Uret(2026, 10, Eylul(kaynak), new PuantajKopyaSecenekleri());
        Assert.Equal(22, kopya.Count(g => g.Calisti));
        Assert.Equal(0, kopya.Count(g => g.Izinli));

        var izinliKopya = PuantajKopyalama.Uret(2026, 10, Eylul(kaynak), new PuantajKopyaSecenekleri(IzinMazeretKopyala: true));
        Assert.Equal(17, izinliKopya.Count(g => g.Calisti));
        Assert.Equal(5, izinliKopya.Count(g => g.Izinli && g.Durum == 2));
        Assert.All(izinliKopya.Where(g => g.Izinli), g => Assert.InRange(g.Gun, 12, 16));
    }

    [Fact]
    public void Kaynaktaki_resmi_tatil_bos_gunu_hedefe_bos_olarak_tasinmaz()
    {
        // 7 Eylül Pazartesi resmi tatil olduğu için çalışılmamış; 5 Ekim Pazartesi bu güne denk gelir ama başka bir Pazartesi esas alınır.
        var kaynak = PuantajTestVerisi.HaftaIci(2026, 9, 7.5m);
        kaynak[6] = PuantajTestVerisi.Gun(new DateTime(2026, 9, 7));

        var tatilBilgisiyle = PuantajKopyalama.Uret(2026, 10, Eylul(kaynak, new DateTime(2026, 9, 7)), new PuantajKopyaSecenekleri());
        var tatilBilgisiz = PuantajKopyalama.Uret(2026, 10, Eylul(kaynak), new PuantajKopyaSecenekleri());

        Assert.True(tatilBilgisiyle[4].Calisti);   // 5 Ekim
        Assert.False(tatilBilgisiz[4].Calisti);
    }

    [Fact]
    public void Hedef_aydaki_resmi_tatiller_bos_birakilir()
    {
        var tatil = new[] { new DateTime(2026, 10, 29) };
        var kaynak = Eylul(PuantajTestVerisi.HaftaIci(2026, 9, 7.5m));

        var atlayarak = PuantajKopyalama.Uret(2026, 10, kaynak, new PuantajKopyaSecenekleri(ResmiTatilleriAtla: true), tatil);
        var atlamadan = PuantajKopyalama.Uret(2026, 10, kaynak, new PuantajKopyaSecenekleri(ResmiTatilleriAtla: false), tatil);

        Assert.False(atlayarak[28].Calisti);
        Assert.True(atlamadan[28].Calisti);
        Assert.Equal(21, atlayarak.Count(g => g.Calisti));
    }

    [Fact]
    public void Ayin_gunu_hizalamasinda_istisna_gun_bos_kalir_ve_eksik_gun_atlanir()
    {
        var kaynak = PuantajTestVerisi.HaftaIci(2026, 9, 7.5m);
        kaynak[4] = PuantajTestVerisi.Gun(new DateTime(2026, 9, 5), izin: true);

        var sonuc = PuantajKopyalama.Uret(2026, 10, Eylul(kaynak), new PuantajKopyaSecenekleri(PuantajKopyaHizalama.AyinGunu));

        Assert.False(sonuc[4].Calisti); // 5'i izinliydi, alternatif aranmaz
        Assert.False(sonuc[30].Calisti); // 31 Ekim'in Eylül karşılığı yok
        Assert.Equal(new DateTime(2026, 10, 2), sonuc[1].Tarih);
        Assert.Equal(sonuc[1].Calisti, kaynak[1].Calisti);
    }
}

public sealed class PuantajServiceSqliteTests
{
    private static async Task<(int PuantajId, int PersonelId)> SeedAsync(PayrollWriteSqliteTests.Fixture fixture)
    {
        await using var db = fixture.Factory.CreateDbContext();
        var personel = PuantajTestVerisi.SaatlikPersonel();
        personel.SoforKodu = "SAAT-1";
        personel.Ad = "Saatlik";
        personel.Soyad = "Calisan";
        personel.FirmaId = 1;
        db.Soforler.Add(personel);
        await db.SaveChangesAsync();

        var puantaj = new PersonelPuantaj { FirmaId = 1, PersonelId = personel.Id, Yil = 2026, Ay = 10, BrutMaas = 19500m, NetOdeme = 15000m };
        db.PersonelPuantajlar.Add(puantaj);
        await db.SaveChangesAsync();
        return (puantaj.Id, personel.Id);
    }

    [Fact]
    public async Task Gunlukleri_kaydet_olmayan_gunleri_ekler_olanlari_gunceller_ve_ozeti_hesaplar()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (puantajId, _) = await SeedAsync(fixture);
        var servis = new PuantajService(fixture.Factory);

        var ilk = await servis.GunlukleriKaydetAsync(puantajId,
        [
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 1), 8m),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 2), 8m),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 5), 8m)
        ]);

        Assert.Equal(3, ilk.CalisilanGun);
        Assert.Equal(1.5m, ilk.FazlaMesaiSaat);
        Assert.Equal(2400m, ilk.BrutMaas); // 24 saat × 100
        Assert.Equal(1846.15m, ilk.NetOdeme);

        // 2 Ekim izinli yapılır, 6 Ekim eklenir; 1 ve 5 Ekim'e dokunulmaz.
        var ikinci = await servis.GunlukleriKaydetAsync(puantajId,
        [
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 2), izin: true),
            PuantajTestVerisi.Gun(new DateTime(2026, 10, 6), 7.5m)
        ]);

        Assert.Equal((3, 1), (ikinci.CalisilanGun, ikinci.IzinGunu));
        Assert.Equal(2350m, ikinci.BrutMaas); // 8 + 8 + 7,5 = 23,5 saat

        var kayitlar = await servis.GetGunlukPuantajlarAsync(puantajId);
        Assert.Equal(4, kayitlar.Count);
        var izinli = kayitlar.Single(g => g.Tarih.Day == 2);
        Assert.True(izinli.Izinli);
        Assert.False(izinli.Calisti);
        Assert.Equal(0m, izinli.CalismaSaati + izinli.FazlaMesaiSaat);
        Assert.Equal(2, izinli.Durum);
    }

    [Fact]
    public async Task Onayli_puantajin_gunleri_degistirilemez()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (puantajId, _) = await SeedAsync(fixture);
        var servis = new PuantajService(fixture.Factory);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var puantaj = await db.PersonelPuantajlar.AsTracking().SingleAsync(p => p.Id == puantajId);
            puantaj.OnayDurumu = PersonelPuantajOnayDurumu.Onaylandi;
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servis.GunlukleriKaydetAsync(puantajId, [PuantajTestVerisi.Gun(new DateTime(2026, 10, 1), 8m)]));
    }

    [Fact]
    public async Task Aylik_gunlukler_puantaja_gore_gruplanir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (puantajId, _) = await SeedAsync(fixture);
        var servis = new PuantajService(fixture.Factory);
        await servis.GunlukleriKaydetAsync(puantajId, PuantajTestVerisi.HaftaIci(2026, 10, 7.5m));

        var harita = await servis.GetAylikGunlukleriAsync(1, 2026, 10);
        var bosHarita = await servis.GetAylikGunlukleriAsync(1, 2026, 9);

        Assert.Single(harita);
        Assert.Equal(31, harita[puantajId].Count);
        Assert.Equal(22, harita[puantajId].Count(g => g.Calisti));
        Assert.Empty(bosHarita);
    }

    [Fact]
    public async Task Saatlik_olmayan_personelde_gunlukleri_kaydetmek_tutari_degistirmez()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (puantajId, personelId) = await SeedAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var personel = await db.Soforler.AsTracking().SingleAsync(s => s.Id == personelId);
            personel.BrutMaasHesaplamaTipi = BrutMaasHesaplamaTipi.Aylik;
            await db.SaveChangesAsync();
        }

        var sonuc = await new PuantajService(fixture.Factory).GunlukleriKaydetAsync(puantajId, PuantajTestVerisi.HaftaIci(2026, 10, 7.5m));

        Assert.Equal(22, sonuc.CalisilanGun);
        Assert.Equal((19500m, 15000m), (sonuc.BrutMaas, sonuc.NetOdeme));
    }
}
