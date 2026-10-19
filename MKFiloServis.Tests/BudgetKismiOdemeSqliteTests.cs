using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data.Migrations;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

public sealed class BudgetKismiOdemeSqliteTests
{
    private static readonly DateTime Ekim = DateTime.SpecifyKind(new DateTime(2026, 10, 1), DateTimeKind.Utc);

    [Fact]
    public async Task Kismi_odeme_kalani_sonraki_doneme_aktarir_ve_temizlik_devir_kaydini_silmez()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);

        var sonuc = await service.KismiOdemeYapAsync(odemeId, AktarimliRequest(hesap, 1000));

        Assert.Equal(OdemeDurum.Odendi, sonuc.Durum);
        Assert.Equal(1000, sonuc.Miktar);
        Assert.NotNull(sonuc.SonrakiDonemOdemeId);

        // Bütçe Analiz her yenilemede bu rutini çalıştırır; aktarılan kalan tutarı silmemelidir.
        await service.TemizleMukerrerKrediKartiBorclariAsync();

        await using var db = fixture.Factory.CreateDbContext();
        var devir = await db.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == sonuc.SonrakiDonemOdemeId);
        Assert.False(devir.IsDeleted);
        Assert.Equal(500, devir.Miktar);
        Assert.Equal(OdemeDurum.Bekliyor, devir.Durum);
        Assert.Equal(odemeId, devir.OncekiDonemOdemeId);
        Assert.Equal(11, devir.OdemeAy);
    }

    [Fact]
    public async Task Kismi_odeme_geri_alma_hareketi_devri_ve_tutari_birlikte_geri_alir_ve_yeniden_yapilabilir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        var ilk = await service.KismiOdemeYapAsync(odemeId, AktarimliRequest(hesap, 1000));
        var ilkDevirId = ilk.SonrakiDonemOdemeId!.Value;
        await service.TemizleMukerrerKrediKartiBorclariAsync();

        var geriAlinan = await service.OdemeGeriAlAsync(odemeId);

        Assert.Equal(OdemeDurum.Bekliyor, geriAlinan.Durum);
        Assert.Equal(1500, geriAlinan.Miktar);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var ana = await db.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
            Assert.Equal(OdemeDurum.Bekliyor, ana.Durum);
            Assert.Equal(1500, ana.Miktar);
            Assert.Equal(0, ana.ToplamKismiOdenen);
            Assert.False(ana.KismiOdemeMi);
            Assert.False(ana.KalanSonrakiDonemeAktarilsin);
            Assert.Null(ana.SonrakiDonemOdemeId);
            Assert.Null(ana.OdenenTutar);
            Assert.Null(ana.BankaKasaHareketId);
            Assert.Null(ana.GercekOdemeTarihi);
            Assert.Null(ana.OdemeYapildigiHesapId);

            var devir = await db.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == ilkDevirId);
            Assert.True(devir.IsDeleted);
            Assert.NotNull(devir.DeletedAt);
            Assert.Null(devir.OncekiDonemOdemeId);
            Assert.Empty(await db.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync());
        }

        // Aynı ödeme yeniden kısmi ödenip aktarılabilmeli (devir bağlantıları benzersiz indekslidir).
        var ikinci = await service.KismiOdemeYapAsync(odemeId, AktarimliRequest(hesap, 600));
        Assert.NotEqual(ilkDevirId, ikinci.SonrakiDonemOdemeId);
        await using var dogrulama = fixture.Factory.CreateDbContext();
        var yeniDevir = await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == ikinci.SonrakiDonemOdemeId);
        Assert.Equal(900, yeniDevir.Miktar);
        Assert.Equal(600, (await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId)).Miktar);
    }

    [Fact]
    public async Task Devir_kaydi_eski_hatayla_silinmis_olsa_da_geri_alma_tam_tutari_geri_getirir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        var sonuc = await service.KismiOdemeYapAsync(odemeId, AktarimliRequest(hesap, 1000));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            // Temizlik rutinindeki eski hatanın bıraktığı durum: devir silinmiş ama bağlantıları duruyor.
            var devir = await db.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == sonuc.SonrakiDonemOdemeId);
            devir.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var geriAlinan = await service.OdemeGeriAlAsync(odemeId);

        Assert.Equal(1500, geriAlinan.Miktar);
        await using var dogrulama = fixture.Factory.CreateDbContext();
        var ana = await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
        Assert.Equal(OdemeDurum.Bekliyor, ana.Durum);
        Assert.Equal(1500, ana.Miktar);
        Assert.Null(ana.SonrakiDonemOdemeId);
        Assert.Null((await dogrulama.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == sonuc.SonrakiDonemOdemeId)).OncekiDonemOdemeId);
        Assert.Empty(await dogrulama.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Aktarimsiz_birden_fazla_kismi_odeme_tek_islemle_geri_alinir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        await service.KismiOdemeYapAsync(odemeId, new KismiOdemeRequest { OdenecekTutar = 300, OdemeTarihi = Ekim, BankaHesapId = hesap });
        var ikinci = await service.KismiOdemeYapAsync(odemeId, new KismiOdemeRequest { OdenecekTutar = 400, OdemeTarihi = Ekim, BankaHesapId = hesap });
        Assert.Equal(OdemeDurum.KismiOdendi, ikinci.Durum);
        await using (var db = fixture.Factory.CreateDbContext())
            Assert.Equal(2, await db.BankaKasaHareketleri.CountAsync());

        await service.OdemeGeriAlAsync(odemeId);

        await using var dogrulama = fixture.Factory.CreateDbContext();
        var ana = await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
        Assert.Equal(OdemeDurum.Bekliyor, ana.Durum);
        Assert.Equal(1500, ana.Miktar);
        Assert.Equal(0, ana.ToplamKismiOdenen);
        Assert.False(ana.KismiOdemeMi);
        Assert.Empty(await dogrulama.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Odenmis_devir_kaydi_geri_almayi_engeller_ve_hicbir_sey_degistirmez()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        var sonuc = await service.KismiOdemeYapAsync(odemeId, AktarimliRequest(hesap, 1000));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var devir = await db.BudgetOdemeler.SingleAsync(o => o.Id == sonuc.SonrakiDonemOdemeId);
            devir.Durum = OdemeDurum.Odendi;
            devir.OdenenTutar = 500;
            await db.SaveChangesAsync();
        }

        var hata = await Assert.ThrowsAsync<InvalidOperationException>(() => service.OdemeGeriAlAsync(odemeId));

        Assert.Contains("ödenmiş veya işlem görmüş", hata.Message);
        await using var dogrulama = fixture.Factory.CreateDbContext();
        var ana = await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
        Assert.Equal(OdemeDurum.Odendi, ana.Durum);
        Assert.Equal(1000, ana.Miktar);
        Assert.Equal(sonuc.SonrakiDonemOdemeId, ana.SonrakiDonemOdemeId);
        Assert.False((await dogrulama.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == sonuc.SonrakiDonemOdemeId)).IsDeleted);
        Assert.Single(await dogrulama.BankaKasaHareketleri.ToListAsync());
    }

    [Fact]
    public async Task Kredi_karti_borc_kaydi_geri_almayla_kaldirilir_odenmisse_engeller()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        await service.KismiOdemeYapAsync(odemeId, new KismiOdemeRequest { OdenecekTutar = 300, OdemeTarihi = Ekim, BankaHesapId = hesap });
        int borcId;
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var borc = new BudgetOdeme
            {
                OdemeTarihi = Ekim, OdemeAy = 10, OdemeYil = 2026, MasrafKalemi = "Kredi Kartı", Aciklama = "[Kart] test",
                Miktar = 300, FirmaId = 1, Durum = OdemeDurum.Bekliyor, Notlar = $"[KaynakOdemeId:{odemeId}]"
            };
            db.BudgetOdemeler.Add(borc);
            await db.SaveChangesAsync();
            borcId = borc.Id;
        }

        // Ödenmiş ekstre borcu varken geri alma reddedilir ve hiçbir şey değişmez.
        await using (var db = fixture.Factory.CreateDbContext())
        {
            (await db.BudgetOdemeler.SingleAsync(o => o.Id == borcId)).Durum = OdemeDurum.Odendi;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OdemeGeriAlAsync(odemeId));
        await using (var db = fixture.Factory.CreateDbContext())
        {
            Assert.Equal(OdemeDurum.KismiOdendi, (await db.BudgetOdemeler.SingleAsync(o => o.Id == odemeId)).Durum);
            Assert.Single(await db.BankaKasaHareketleri.ToListAsync());
            (await db.BudgetOdemeler.SingleAsync(o => o.Id == borcId)).Durum = OdemeDurum.Bekliyor;
            await db.SaveChangesAsync();
        }

        await service.OdemeGeriAlAsync(odemeId);

        await using var dogrulama = fixture.Factory.CreateDbContext();
        Assert.True((await dogrulama.BudgetOdemeler.IgnoreQueryFilters().SingleAsync(o => o.Id == borcId)).IsDeleted);
        Assert.Equal(OdemeDurum.Bekliyor, (await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId)).Durum);
        Assert.Empty(await dogrulama.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Kismi_olmayan_odeme_geri_alma_eskisi_gibi_calisir()
    {
        await using var fixture = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var (service, hesap) = await CreateServiceAsync(fixture);
        var odemeId = await SeedOdemeAsync(fixture, 1500);
        await using (var db = fixture.Factory.CreateDbContext())
        {
            var hareket = new BankaKasaHareket
            {
                FirmaId = 1, BankaHesapId = hesap, IslemNo = "BORC-TEST", IslemTarihi = Ekim, Tutar = 1500,
                HareketTipi = HareketTipi.Cikis, IslemKaynak = IslemKaynak.Butce
            };
            db.BankaKasaHareketleri.Add(hareket);
            await db.SaveChangesAsync();
            var odeme = await db.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
            odeme.Durum = OdemeDurum.Odendi;
            odeme.OdenenTutar = 1500;
            odeme.BankaKasaHareketId = hareket.Id;
            odeme.OdemeYapildigiHesapId = hesap;
            await db.SaveChangesAsync();
        }

        var geriAlinan = await service.OdemeGeriAlAsync(odemeId);

        Assert.Equal(OdemeDurum.Bekliyor, geriAlinan.Durum);
        Assert.Equal(1500, geriAlinan.Miktar);
        await using var dogrulama = fixture.Factory.CreateDbContext();
        var ana = await dogrulama.BudgetOdemeler.SingleAsync(o => o.Id == odemeId);
        Assert.Equal(OdemeDurum.Bekliyor, ana.Durum);
        Assert.Null(ana.BankaKasaHareketId);
        Assert.Null(ana.OdenenTutar);
        Assert.Empty(await dogrulama.BankaKasaHareketleri.IgnoreQueryFilters().ToListAsync());
    }

    private static KismiOdemeRequest AktarimliRequest(int hesap, decimal tutar) => new()
    {
        OdenecekTutar = tutar, OdemeTarihi = Ekim, BankaHesapId = hesap,
        KalanSonrakiDonemeAktarilsin = true, HedefAy = 11, HedefYil = 2026
    };

    private static async Task<int> SeedOdemeAsync(PayrollWriteSqliteTests.Fixture fixture, decimal miktar)
    {
        await using var db = fixture.Factory.CreateDbContext();
        var odeme = new BudgetOdeme
        {
            OdemeTarihi = Ekim, OdemeAy = 10, OdemeYil = 2026, MasrafKalemi = "Banka Kredisi", Aciklama = "test taksit",
            Miktar = miktar, FirmaId = 1, Durum = OdemeDurum.Bekliyor
        };
        db.BudgetOdemeler.Add(odeme);
        await db.SaveChangesAsync();
        return odeme.Id;
    }

    private static async Task<(BudgetService service, int hesap)> CreateServiceAsync(PayrollWriteSqliteTests.Fixture fixture)
    {
        await using var db = fixture.Factory.CreateDbContext();
        // Başlangıç şeması yardımcısı bu tabloyu sahiplenir; EF varlığı değildir.
        await SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(db);
        var hesap = new BankaHesap { HesapKodu = "KASA", HesapAdi = "Kasa", FirmaId = 1, HesapTipi = HesapTipi.Kasa, AcilisBakiye = 10000, VarsayilanMuhasebeKodu = "100" };
        db.BankaHesaplari.Add(hesap);
        foreach (var kod in new[] { "100", "102", "120", "320" })
            db.MuhasebeHesaplari.Add(new MuhasebeHesap { HesapKodu = kod, HesapAdi = kod });
        await db.SaveChangesAsync();
        var numbers = new NumaraSerisiService(fixture.Factory);
        var accounts = new BankaHesapService(fixture.Factory, numbers, fixture.Guard, fixture.ActiveFirm);
        var accounting = new MuhasebeService(fixture.Factory, fixture.Guard);
        var movements = new BankaKasaHareketService(fixture.Factory, accounting, accounts, numbers, fixture.ActiveFirm, fixture.Guard);
        return (new BudgetService(fixture.Factory, movements, accounting, NullLogger<BudgetService>.Instance), hesap.Id);
    }
}
