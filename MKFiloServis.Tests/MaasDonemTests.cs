using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Calculation;

namespace MKFiloServis.Tests;

public sealed class MaasDonemHesaplayiciTests
{
    private static readonly IReadOnlyList<DonemMahsup> Mahsupsuz = [];

    // Yakup Avşar örneği: 29.08.2026 16.500 ₺ ve 15.09.2026 14.999,99 ₺ avans, hiçbiri mahsup edilmemiş.
    private static readonly DonemAvans[] YakupAvanslari =
    [
        new(new DateTime(2026, 8, 29), 16500m, Mahsupsuz),
        new(new DateTime(2026, 9, 15), 14999.99m, Mahsupsuz)
    ];

    [Fact]
    public void Avans_kendi_ayinda_gorunur_sonraki_aylarda_devreder()
    {
        var temmuz = MaasDonemHesaplayici.Hesapla(2026, 7, YakupAvanslari, null, null);
        var agustos = MaasDonemHesaplayici.Hesapla(2026, 8, YakupAvanslari, null, null);
        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, YakupAvanslari, null, null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, YakupAvanslari, null, null);

        Assert.Equal((0m, 0m, 0m), (temmuz.Avans, temmuz.DevirAvans, temmuz.GecenAydanKalan));
        Assert.Equal((16500m, 0m, 0m), (agustos.Avans, agustos.DevirAvans, agustos.GecenAydanKalan));
        Assert.Equal((14999.99m, 16500m, -16500m), (eylul.Avans, eylul.DevirAvans, eylul.GecenAydanKalan));
        Assert.Equal((0m, 31499.99m, -31499.99m), (ekim.Avans, ekim.DevirAvans, ekim.GecenAydanKalan));
    }

    [Fact]
    public void Maastan_kesinti_mahsubu_ayin_kesintisini_silmez_ama_sonraki_ayin_devrini_kapatir()
    {
        // Eylül avansı Eylül maaşından kesilerek 30.09'da mahsup edilmiş.
        var avans = new DonemAvans(new DateTime(2026, 9, 15), 1000m,
            [new DonemMahsup(new DateTime(2026, 9, 30), 1000m, MaastanKesinti: true)]);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, [avans], null, null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, [avans], null, null);

        Assert.Equal(1000m, eylul.Avans);
        Assert.Equal(0m, ekim.DevirAvans);
        Assert.Equal(0m, ekim.GecenAydanKalan);
    }

    [Fact]
    public void Personelin_ayiciinde_yaptigi_nakit_iade_ayin_avansini_dusurur()
    {
        var avans = new DonemAvans(new DateTime(2026, 9, 5), 1000m,
            [new DonemMahsup(new DateTime(2026, 9, 20), 400m, MaastanKesinti: false)]);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, [avans], null, null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, [avans], null, null);

        Assert.Equal(600m, eylul.Avans);
        Assert.Equal(600m, ekim.DevirAvans);
    }

    [Fact]
    public void Gecmis_ay_mahsubu_devri_kapatir_ay_sonrasi_mahsup_kapatmaz()
    {
        var avans = new DonemAvans(new DateTime(2026, 7, 10), 1000m,
        [
            new DonemMahsup(new DateTime(2026, 8, 31), 300m, MaastanKesinti: true),
            new DonemMahsup(new DateTime(2026, 10, 5), 700m, MaastanKesinti: true)
        ]);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, [avans], null, null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, [avans], null, null);
        var kasim = MaasDonemHesaplayici.Hesapla(2026, 11, [avans], null, null);

        Assert.Equal(700m, eylul.DevirAvans); // 300 Ağustos'ta kapandı
        Assert.Equal(700m, ekim.DevirAvans);  // 700 Ekim'de kapanıyor; ay başında hâlâ açıktı
        Assert.Equal(0m, kasim.DevirAvans);
    }

    [Fact]
    public void Ay_sonundan_sonraki_kayitlar_o_ayi_etkilemez()
    {
        var avans = new DonemAvans(new DateTime(2026, 11, 2), 500m, Mahsupsuz);
        var harcama = new DonemHarcama(new DateTime(2026, 11, 3), 80m, null);
        var borc = new DonemBorc(new DateTime(2026, 11, 4), 900m, []);

        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, [avans], [borc], [harcama]);

        Assert.Equal(0m, ekim.Avans);
        Assert.Equal(0m, ekim.Harcama);
        Assert.False(ekim.DevirVar);
    }

    [Fact]
    public void Harcama_bu_ay_gorunur_geri_odenene_kadar_devreder()
    {
        var odenmemis = new DonemHarcama(new DateTime(2026, 9, 10), 250m, null);
        var ekimdeOdenen = new DonemHarcama(new DateTime(2026, 9, 12), 100m, new DateTime(2026, 10, 5));
        var ayIcindeOdenen = new DonemHarcama(new DateTime(2026, 9, 14), 60m, new DateTime(2026, 9, 20));

        var kayitlar = new[] { odenmemis, ekimdeOdenen, ayIcindeOdenen };
        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, null, null, kayitlar);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, null, null, kayitlar);
        var kasim = MaasDonemHesaplayici.Hesapla(2026, 11, null, null, kayitlar);

        Assert.Equal(350m, eylul.Harcama);       // ay içinde ödenen 60 düşer; 250 + 100 ödenecek
        Assert.Equal(350m, ekim.DevirHarcama);   // 100, 5 Ekim'de ödendiği için ay başında hâlâ açıktı
        Assert.Equal(0m, ekim.Harcama);
        Assert.Equal(250m, kasim.DevirHarcama);  // yalnız ödenmemiş olan sürer
    }

    [Fact]
    public void Sirket_borcu_bu_ay_gorunur_odeme_dusulur_kalan_devreder()
    {
        var borc = new DonemBorc(new DateTime(2026, 9, 25), 1000m,
        [
            new DonemBorcOdeme(new DateTime(2026, 9, 28), 200m),
            new DonemBorcOdeme(new DateTime(2026, 10, 10), 300m)
        ]);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, null, [borc], null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, null, [borc], null);
        var kasim = MaasDonemHesaplayici.Hesapla(2026, 11, null, [borc], null);

        Assert.Equal(800m, eylul.Harcama);
        Assert.Equal(800m, ekim.DevirBorc);
        Assert.Equal(500m, kasim.DevirBorc);
        Assert.Equal(500m, kasim.GecenAydanKalan);
    }

    [Fact]
    public void Gecen_aydan_kalan_net_bakiyedir_alacak_pozitif_borc_negatif()
    {
        var avans = new DonemAvans(new DateTime(2026, 8, 1), 300m, Mahsupsuz);
        var borc = new DonemBorc(new DateTime(2026, 8, 2), 500m, []);
        var harcama = new DonemHarcama(new DateTime(2026, 8, 3), 200m, null);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, [avans], [borc], [harcama]);

        Assert.Equal(300m, eylul.DevirAvans);
        Assert.Equal(500m, eylul.DevirBorc);
        Assert.Equal(200m, eylul.DevirHarcama);
        Assert.Equal(400m, eylul.GecenAydanKalan);
        Assert.True(eylul.DevirVar);
    }

    [Fact]
    public void Gecersiz_tutarli_kayitlar_ve_zaman_bilesenli_tarihler_gunluk_karsilastirilir()
    {
        var sifirAvans = new DonemAvans(new DateTime(2026, 9, 1), 0m, Mahsupsuz);
        var ayinSonSaatindeAvans = new DonemAvans(new DateTime(2026, 9, 30, 23, 59, 0), 100m, Mahsupsuz);
        var yeniAyinIlkSaatindeAvans = new DonemAvans(new DateTime(2026, 10, 1, 0, 0, 1), 50m, Mahsupsuz);

        var eylul = MaasDonemHesaplayici.Hesapla(2026, 9, [sifirAvans, ayinSonSaatindeAvans, yeniAyinIlkSaatindeAvans], null, null);
        var ekim = MaasDonemHesaplayici.Hesapla(2026, 10, [sifirAvans, ayinSonSaatindeAvans, yeniAyinIlkSaatindeAvans], null, null);

        Assert.Equal(100m, eylul.Avans);
        Assert.Equal(50m, ekim.Avans);
        Assert.Equal(100m, ekim.DevirAvans);
    }

    [Fact]
    public void Motor_gecen_aydan_kalani_odenecege_ekler_bu_ay_tutari_ayri_tutar()
    {
        var sonuc = MaasHesaplamaEngine.Hesapla(new MaasInput
        {
            GercekMaas = 33000,
            BankayaYatan = 0,
            Avans = 14999.99m,
            Kesinti = 0,
            Harcama = 0,
            GecenAydanKalan = -16500
        });

        Assert.Equal(18000.01m, sonuc.BuAyOdenecek);
        Assert.Equal(1500.01m, sonuc.Odenecek);
        Assert.Equal(-16500m, sonuc.GecenAydanKalan);
    }

    [Fact]
    public void Motor_gecen_aydan_kalan_verilmezse_eski_sonucu_verir()
    {
        var sonuc = MaasHesaplamaEngine.Hesapla(new MaasInput { GercekMaas = 5000, BankayaYatan = 4000, Avans = 100, Kesinti = 2100, Harcama = 500 });

        Assert.Equal(-700m, sonuc.Odenecek);
        Assert.Equal(sonuc.Odenecek, sonuc.BuAyOdenecek);
        Assert.Equal(0m, sonuc.GecenAydanKalan);
    }
}

public sealed class MaasDonemServiceSqliteTests
{
    private static async Task<int> PersonelIdAsync(PayrollWriteSqliteTests.Fixture fixture)
    {
        await using var db = fixture.Factory.CreateDbContext();
        return (await db.PersonelMaaslari.SingleAsync(m => m.Id == fixture.SalaryId)).SoforId;
    }

    [Fact]
    public async Task Avans_kayitlari_aya_boluner_iptal_ve_silinenler_sayilmaz()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var personelId = await PersonelIdAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var mahsuplu = new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 8, 29), Tutar = 16500, MahsupEdilen = 6500 };
            mahsuplu.Mahsuplasmalar.Add(new PersonelAvansMahsup { MahsupTarihi = new DateTime(2026, 9, 30), MahsupTutari = 6500, MahsupSekli = MahsupSekli.MaastanKesinti });
            db.PersonelAvanslar.AddRange(
                mahsuplu,
                new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 9, 15), Tutar = 14999.99m },
                new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 9, 16), Tutar = 777, Durum = AvansDurum.IptalEdildi },
                new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 9, 17), Tutar = 888, IsDeleted = true });
            await db.SaveChangesAsync();
        }

        var servis = new MaasDonemService(fixture.Factory);
        var eylul = (await servis.GetDonemFinansAsync([personelId], 2026, 9))[personelId];
        var ekim = (await servis.GetDonemFinansAsync([personelId], 2026, 10))[personelId];

        Assert.Equal(14999.99m, eylul.Avans);
        Assert.Equal(16500m, eylul.DevirAvans);       // Ağustos avansı Eylül ayı başında açıktı
        Assert.Equal(0m, ekim.Avans);
        Assert.Equal(14999.99m + 10000m, ekim.DevirAvans); // 6.500'lük mahsup 30.09'da kapandı
        Assert.Equal(-(14999.99m + 10000m), ekim.GecenAydanKalan);
    }

    [Fact]
    public async Task Cebinden_harcama_ve_sirket_borcu_aya_bolunur_kayitsiz_personel_sifir_doner()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var personelId = await PersonelIdAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var kasa = new BankaHesap { FirmaId = 1, HesapAdi = "Kasa", HesapTipi = HesapTipi.Kasa };
            db.BankaHesaplari.Add(kasa);
            await db.SaveChangesAsync();

            // Test verisinde araç ve masraf kalemi üretmek yerine yabancı anahtar denetimini kapat.
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
            db.AracMasraflari.AddRange(
                new AracMasraf { FirmaId = 1, AracId = 1, MasrafKalemiId = 1, PersonelCebindenId = personelId, MasrafTarihi = new DateTime(2026, 9, 10), Tutar = 250 },
                new AracMasraf { FirmaId = 1, AracId = 1, MasrafKalemiId = 1, PersonelCebindenId = personelId, MasrafTarihi = new DateTime(2026, 9, 12), Tutar = 100, PersoneleOdendi = true, PersonelOdemeTarihi = new DateTime(2026, 10, 5) });
            db.BankaKasaHareketleri.Add(new BankaKasaHareket { FirmaId = 1, BankaHesapId = kasa.Id, PersonelCebindenId = personelId, IslemTarihi = new DateTime(2026, 9, 20), Tutar = 40, HareketTipi = HareketTipi.Cikis });
            var borc = new PersonelBorc { PersonelId = personelId, FirmaId = 1, BorcTarihi = new DateTime(2026, 9, 25), Tutar = 1000, BorcNedeni = "Maaş alacağı" };
            borc.Odemeler.Add(new PersonelBorcOdeme { OdemeTarihi = new DateTime(2026, 10, 10), OdemeTutari = 300 });
            db.PersonelBorclar.Add(borc);
            await db.SaveChangesAsync();
        }

        var servis = new MaasDonemService(fixture.Factory);
        var sonuc = await servis.GetDonemFinansAsync([personelId, 9999], 2026, 9);
        var ekim = (await servis.GetDonemFinansAsync([personelId], 2026, 10))[personelId];

        Assert.Equal(250m + 100m + 40m + 1000m, sonuc[personelId].Harcama);
        Assert.Equal(0m, sonuc[9999].Harcama);
        Assert.False(sonuc[9999].DevirVar);
        Assert.Equal(250m + 100m + 40m, ekim.DevirHarcama);
        Assert.Equal(1000m, ekim.DevirBorc); // 300'lük ödeme 10.10'da; ay başında borç tamamen açıktı
        Assert.Equal(0m, ekim.Harcama);
    }

    [Fact]
    public async Task Mahsup_ve_odeme_satiri_olmayan_eski_kapanis_toplam_alanindan_okunur()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var personelId = await PersonelIdAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            // Aktarılmış eski kayıtlar: toplam alan dolu, satır yok.
            db.PersonelAvanslar.Add(new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 8, 20), Tutar = 1000, MahsupEdilen = 400, MahsupTarihi = new DateTime(2026, 9, 10) });
            db.PersonelBorclar.Add(new PersonelBorc
            {
                PersonelId = personelId, FirmaId = 1, BorcTarihi = new DateTime(2026, 8, 25), Tutar = 500, OdenenTutar = 200,
                GerceklesenOdemeTarihi = new DateTime(2026, 10, 3), BorcNedeni = "Eski kayıt"
            });
            await db.SaveChangesAsync();
        }

        var servis = new MaasDonemService(fixture.Factory);
        var eylul = (await servis.GetDonemFinansAsync([personelId], 2026, 9))[personelId];
        var ekim = (await servis.GetDonemFinansAsync([personelId], 2026, 10))[personelId];
        var kasim = (await servis.GetDonemFinansAsync([personelId], 2026, 11))[personelId];

        Assert.Equal(1000m, eylul.DevirAvans);  // 400 ₺ 10 Eylül'de kapandı; Eylül başında hâlâ açıktı
        Assert.Equal(600m, ekim.DevirAvans);
        Assert.Equal(500m, ekim.DevirBorc);     // 200 ₺ 3 Ekim'de ödendi; Ekim başında borç tamamen açıktı
        Assert.Equal(300m, kasim.DevirBorc);
        Assert.Equal(300m - 600m, kasim.GecenAydanKalan);
    }

    [Fact]
    public async Task Tum_firmalar_kapsami_yalniz_verilen_firmalarin_kayitlarini_okur()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var personelId = await PersonelIdAsync(fixture);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
            db.PersonelAvanslar.AddRange(
                new PersonelAvans { PersonelId = personelId, FirmaId = 1, AvansTarihi = new DateTime(2026, 9, 5), Tutar = 100 },
                new PersonelAvans { PersonelId = personelId, FirmaId = 2, AvansTarihi = new DateTime(2026, 9, 6), Tutar = 200 },
                new PersonelAvans { PersonelId = personelId, FirmaId = 3, AvansTarihi = new DateTime(2026, 9, 7), Tutar = 400 },
                new PersonelAvans { PersonelId = personelId, FirmaId = 2, AvansTarihi = new DateTime(2026, 9, 8), Tutar = 800, IsDeleted = true });
            await db.SaveChangesAsync();
        }

        var servis = new MaasDonemService(fixture.Factory);
        var aktifFirma = (await servis.GetDonemFinansAsync([personelId], 2026, 9))[personelId];
        var firma12 = (await servis.GetDonemFinansAsync([personelId], 2026, 9, tumFirmalar: true, firmaIds: [1, 2]))[personelId];

        Assert.Equal(100m, aktifFirma.Avans);   // aktif firma filtresi (firma 1)
        Assert.Equal(300m, firma12.Avans);      // 1 + 2; firma 3 ve silinmiş kayıt dışarıda
        await Assert.ThrowsAsync<ArgumentException>(() => servis.GetDonemFinansAsync([personelId], 2026, 9, tumFirmalar: true));
    }
}
