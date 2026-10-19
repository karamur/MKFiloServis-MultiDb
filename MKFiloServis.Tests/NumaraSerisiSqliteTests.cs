using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Data.Migrations;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>
/// Belge numaraları yalnız yılı içerdiği için sayaç yıllık olmalı. Eskiden aylık sayaç her ay 1'den başlayıp
/// önceki aylarda verilmiş numaraları yeniden üretiyor ve benzersiz indeksli proforma/fatura kaydı düşüyordu.
/// </summary>
public sealed class NumaraSerisiSqliteTests
{
    private static int Yil => DateTime.UtcNow.Year;

    [Fact]
    public async Task Yillik_sayac_ayni_yilin_aylik_sayaclarinin_en_buyugunden_devam_eder()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await HazirlaAsync(f);
        // Gerçek veride görülen durum: Ağustos'ta 1, Eylül'de 1, Ekim'de 4'e kadar numara verilmiş.
        await SayacEkleAsync(f, "PRF", 0, $"{Yil}08", 1);
        await SayacEkleAsync(f, "PRF", 0, $"{Yil}09", 1);
        await SayacEkleAsync(f, "PRF", 0, $"{Yil}10", 4);

        var ilk = await servis.GenerateFormattedAsync("PRF", 0, 5);
        var ikinci = await servis.GenerateFormattedAsync("PRF", 0, 5);

        Assert.Equal($"PRF-{Yil}-00005", ilk);   // 1, 3, 4 gibi verilmiş numaralar tekrarlanmaz
        Assert.Equal($"PRF-{Yil}-00006", ikinci);
    }

    [Fact]
    public async Task Yeni_ayda_numara_sifirlanmaz_yil_boyunca_artar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await HazirlaAsync(f);

        var numaralar = new List<string>();
        for (var i = 0; i < 4; i++)
            numaralar.Add(await servis.GenerateFormattedAsync("SF", 3, 6));

        Assert.Equal(
            new[] { $"SF-{Yil}-000001", $"SF-{Yil}-000002", $"SF-{Yil}-000003", $"SF-{Yil}-000004" },
            numaralar.ToArray());
        Assert.Equal(numaralar.Count, numaralar.Distinct().Count());
    }

    [Fact]
    public async Task Onceki_yilin_baska_firmanin_ve_baska_onekin_sayaclari_seriyi_etkilemez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await HazirlaAsync(f);
        await SayacEkleAsync(f, "PRF", 0, $"{Yil - 1}12", 9);   // geçen yıl: yeni yıl 1'den başlar
        await SayacEkleAsync(f, "PRF", 7, $"{Yil}03", 12);      // başka firma
        await SayacEkleAsync(f, "SF", 0, $"{Yil}03", 30);       // başka önek
        await SayacEkleAsync(f, "PRF", 0, "GLOBAL", 50);        // yıl/aya bağlı olmayan anahtar

        var numara = await servis.GenerateFormattedAsync("PRF", 0, 5);

        Assert.Equal($"PRF-{Yil}-00001", numara);
    }

    [Fact]
    public async Task Yillik_satir_olusur_aylik_sayaclar_oldugu_gibi_kalir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await HazirlaAsync(f);
        await SayacEkleAsync(f, "HRK", 0, $"{Yil}08", 5);
        await SayacEkleAsync(f, "HRK", 0, $"{Yil}10", 2);

        await servis.GenerateFormattedAsync("HRK", 0, 4);
        await servis.GenerateFormattedAsync("HRK", 0, 4);

        await using var db = f.Factory.CreateDbContext();
        var satirlar = await SayaclariOkuAsync(db, "HRK");
        Assert.Equal(5, satirlar[$"{Yil}08"]);
        Assert.Equal(2, satirlar[$"{Yil}10"]);
        Assert.Equal(7, satirlar[$"{Yil}"]);                  // 5'ten devam: 6, 7
    }

    [Fact]
    public async Task Eski_aylik_aralik_ayni_yilda_ayni_numarayi_iki_kez_vermez()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await HazirlaAsync(f);
        // Önceki davranışta Ağustos'ta 1..3, Eylül'de 1..2 verilmişti (1 ve 2 iki kez); şimdi hiçbiri tekrar etmemeli.
        await SayacEkleAsync(f, "SF_GIDEN", 3, $"{Yil}08", 3);
        await SayacEkleAsync(f, "SF_GIDEN", 3, $"{Yil}09", 2);

        var yeni = await servis.GenerateFormattedAsync("SF_GIDEN", 3, 6);

        var no = int.Parse(yeni[(yeni.LastIndexOf('-') + 1)..]);
        Assert.True(no > 3, $"Verilmiş numaraların (≤3) üstünde olmalı: {yeni}");
    }

    [Fact]
    public async Task Proforma_kaydi_kullanilmis_numarayi_bir_sonraki_bos_numarayla_degistirir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var numaraSerisi = await HazirlaAsync(f);
        var servis = new ProformaFaturaService(f.Factory, null!, numaraSerisi, NullLogger<ProformaFaturaService>.Instance);
        var cariId = await CariEkleAsync(f);
        await ProformaEkleAsync(f, $"PRF-{Yil}-00001", cariId);   // Ağustos'ta kesilmiş proforma

        // Form, sayaç gerideyken aynı numarayı üretmişti: PRF-xxxx-00001
        var yeni = await servis.CreateAsync(YeniProforma($"PRF-{Yil}-00001", cariId));

        Assert.NotEqual($"PRF-{Yil}-00001", yeni.ProformaNo);
        Assert.Equal($"PRF-{Yil}-00002", yeni.ProformaNo);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal(2, await db.ProformaFaturalar.IgnoreQueryFilters().CountAsync());
        Assert.Equal(2, await db.ProformaFaturalar.IgnoreQueryFilters().Select(p => p.ProformaNo).Distinct().CountAsync());
    }

    [Fact]
    public async Task Silinmis_proformanin_numarasi_da_kullanimda_sayilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var numaraSerisi = await HazirlaAsync(f);
        var servis = new ProformaFaturaService(f.Factory, null!, numaraSerisi, NullLogger<ProformaFaturaService>.Instance);
        var cariId = await CariEkleAsync(f);
        await ProformaEkleAsync(f, $"PRF-{Yil}-00001", cariId, silinmis: true);

        var yeni = await servis.CreateAsync(YeniProforma($"PRF-{Yil}-00001", cariId));

        Assert.Equal($"PRF-{Yil}-00002", yeni.ProformaNo);   // benzersiz indeks silinmiş satırları da kapsar
    }

    [Fact]
    public async Task Proforma_kaydi_bos_numarada_yeni_numara_uretir_ve_kalemlerle_toplami_hesaplar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var numaraSerisi = await HazirlaAsync(f);
        var servis = new ProformaFaturaService(f.Factory, null!, numaraSerisi, NullLogger<ProformaFaturaService>.Instance);
        var cariId = await CariEkleAsync(f);

        var proforma = YeniProforma(string.Empty, cariId);
        proforma.Kalemler.Add(new ProformaFaturaKalem
        {
            SiraNo = 1, UrunAdi = "personel taşıma", Miktar = 12, Birim = "Adet", BirimFiyat = 12000, KdvOrani = 20,
            AraToplam = 144000, NetTutar = 144000, KdvTutar = 28800, ToplamTutar = 172800
        });
        var kayit = await servis.CreateAsync(proforma);

        Assert.Equal($"PRF-{Yil}-00001", kayit.ProformaNo);
        Assert.Equal(144000m, kayit.AraToplam);
        Assert.Equal(28800m, kayit.KdvTutar);
        Assert.Equal(172800m, kayit.GenelToplam);   // ekran görüntüsündeki kayıt
        await using var db = f.Factory.CreateDbContext();
        Assert.Single(await db.ProformaFaturaKalemler.ToListAsync());
    }

    // ---- yardımcılar ----

    private static async Task<NumaraSerisiService> HazirlaAsync(PayrollWriteSqliteTests.Fixture f)
    {
        await using var db = f.Factory.CreateDbContext();
        await SchemaSyncHelper.EnsureFisNoCountersSchemaAsync(db);
        return new NumaraSerisiService(f.Factory);
    }

    private static async Task SayacEkleAsync(PayrollWriteSqliteTests.Fixture f, string onek, int firmaId, string yilAy, int sonNo)
    {
        await using var db = f.Factory.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO ""FisNoCounters"" (""Prefix"", ""FirmaId"", ""YilAy"", ""SonNo"") VALUES ({onek}, {firmaId}, {yilAy}, {sonNo})");
    }

    private static async Task<Dictionary<string, int>> SayaclariOkuAsync(ApplicationDbContext db, string onek)
    {
        var sonuc = new Dictionary<string, int>();
        var baglanti = db.Database.GetDbConnection();
        if (baglanti.State != System.Data.ConnectionState.Open)
            await baglanti.OpenAsync();
        await using var komut = baglanti.CreateCommand();
        komut.CommandText = @"SELECT ""YilAy"", ""SonNo"" FROM ""FisNoCounters"" WHERE ""Prefix"" = $onek";
        var parametre = komut.CreateParameter();
        parametre.ParameterName = "$onek";
        parametre.Value = onek;
        komut.Parameters.Add(parametre);
        await using var okuyucu = await komut.ExecuteReaderAsync();
        while (await okuyucu.ReadAsync())
            sonuc[okuyucu.GetString(0)] = okuyucu.GetInt32(1);
        return sonuc;
    }

    private static async Task<int> CariEkleAsync(PayrollWriteSqliteTests.Fixture f)
    {
        await using var db = f.Factory.CreateDbContext();
        var cari = new Cari { CariKodu = "C1", Unvan = "ALİ COŞAR", CariTipi = CariTipi.Musteri, FirmaId = 1 };
        db.Cariler.Add(cari);
        await db.SaveChangesAsync();
        return cari.Id;
    }

    private static async Task ProformaEkleAsync(PayrollWriteSqliteTests.Fixture f, string no, int cariId, bool silinmis = false)
    {
        await using var db = f.Factory.CreateDbContext();
        var proforma = YeniProforma(no, cariId);
        proforma.IsDeleted = silinmis;
        db.ProformaFaturalar.Add(proforma);
        await db.SaveChangesAsync();
    }

    private static ProformaFatura YeniProforma(string no, int cariId) => new()
    {
        ProformaNo = no,
        CariId = cariId,
        ProformaTarihi = new DateTime(Yil, 8, 29),
        GecerlilikTarihi = new DateTime(Yil, 9, 13),
        KdvOrani = 20,
        VadeGun = 30,
        OdemeKosulu = "Peşin"
    };
}
