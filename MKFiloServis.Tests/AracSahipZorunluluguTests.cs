using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Controllers;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>Kiralık araçlarda araç (plaka) sahibi cari seçimi zorunludur: kural, kayıt ve Excel aktarımı.</summary>
public sealed class AracSahipZorunluluguTests
{
    private sealed record SahipCarileri(int Kiralayan, int OzKaynak, int Musteri, int IkizBir, int IkizIki);

    // ── Saf kural ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AracSahiplikTipi.Kiralik, null, true)]
    [InlineData(AracSahiplikTipi.Kiralik, 0, true)]
    [InlineData(AracSahiplikTipi.Kiralik, -3, true)]
    [InlineData(AracSahiplikTipi.Kiralik, 7, false)]
    [InlineData(AracSahiplikTipi.Ozmal, null, false)]
    [InlineData(AracSahiplikTipi.Komisyon, null, false)]
    [InlineData(AracSahiplikTipi.Tedarikci, null, false)]
    [InlineData(AracSahiplikTipi.Diger, null, false)]
    public void Kiralik_arac_icin_pozitif_cari_gerekir(AracSahiplikTipi tip, int? cariId, bool eksikMi)
        => Assert.Equal(eksikMi, AracKurallari.KiralikSahipEksik(tip, cariId));

    [Theory]
    // Eski sahipsiz kiralığa dokunmayan güncelleme (km, şase düzeltmesi) engellenmez.
    [InlineData(AracSahiplikTipi.Kiralik, null, AracSahiplikTipi.Kiralik, null, false)]
    [InlineData(AracSahiplikTipi.Kiralik, 0, AracSahiplikTipi.Kiralik, null, false)]
    // Sahipsiz kiralığa dönüştüren ya da sahibi boşaltan güncelleme engellenir.
    [InlineData(AracSahiplikTipi.Ozmal, null, AracSahiplikTipi.Kiralik, null, true)]
    [InlineData(AracSahiplikTipi.Komisyon, 4, AracSahiplikTipi.Kiralik, null, true)]
    [InlineData(AracSahiplikTipi.Kiralik, 5, AracSahiplikTipi.Kiralik, null, true)]
    [InlineData(AracSahiplikTipi.Kiralik, 5, AracSahiplikTipi.Kiralik, 0, true)]
    // Sahip seçilen, değiştirilen ya da kiralıktan çıkan güncellemeler serbesttir.
    [InlineData(AracSahiplikTipi.Kiralik, null, AracSahiplikTipi.Kiralik, 7, false)]
    [InlineData(AracSahiplikTipi.Kiralik, 5, AracSahiplikTipi.Kiralik, 6, false)]
    [InlineData(AracSahiplikTipi.Kiralik, 5, AracSahiplikTipi.Ozmal, null, false)]
    public void Guncelleme_yalniz_yeni_sahipsiz_kiralik_olusturuyorsa_engellenir(
        AracSahiplikTipi eskiTip, int? eskiCari, AracSahiplikTipi yeniTip, int? yeniCari, bool engellenirMi)
        => Assert.Equal(engellenirMi, AracKurallari.GuncellemeSahipsizKiralikBirakiyor(eskiTip, eskiCari, yeniTip, yeniCari));

    // ── Kayıt ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Olusturma_sahipsiz_kiralik_araci_reddeder_ve_kayit_birakmaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await ServisKurAsync(f);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servis.CreateAsync(YeniArac("SASE-KRL-0001", AracSahiplikTipi.Kiralik, null), "34 KRL 001"));

        Assert.Equal(AracKurallari.KiralikSahipZorunluMesaji, ex.Message);
        await using var db = f.Factory.CreateDbContext();
        Assert.Empty(await db.Araclar.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await db.AracPlakalar.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task Olusturma_sahipli_kiralik_ve_sahipsiz_ozmal_araci_kaydeder()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);

        await servis.CreateAsync(YeniArac("SASE-KRL-0002", AracSahiplikTipi.Kiralik, c.Kiralayan), "34 KRL 002");
        await servis.CreateAsync(YeniArac("SASE-OZM-0003", AracSahiplikTipi.Ozmal, null), "34 OZM 003");

        await using var db = f.Factory.CreateDbContext();
        var araclar = await db.Araclar.ToDictionaryAsync(a => a.SaseNo);
        Assert.Equal(c.Kiralayan, araclar["SASE-KRL-0002"].KiralikCariId);
        Assert.Null(araclar["SASE-OZM-0003"].KiralikCariId);
    }

    [Fact]
    public async Task Guncelleme_ozmal_araci_sahipsiz_kiraliga_cevirmeyi_reddeder_sahipliye_izin_verir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);
        var kayit = await servis.CreateAsync(YeniArac("SASE-OZM-0004", AracSahiplikTipi.Ozmal, null), "34 OZM 004");

        var duzenlenen = (await servis.GetByIdAsync(kayit.Id))!;
        duzenlenen.SahiplikTipi = AracSahiplikTipi.Kiralik;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => servis.UpdateAsync(duzenlenen));
        Assert.Equal(AracKurallari.KiralikSahipZorunluMesaji, ex.Message);
        Assert.Equal(AracSahiplikTipi.Ozmal, (await servis.GetByIdAsync(kayit.Id))!.SahiplikTipi);

        duzenlenen.KiralikCariId = c.Kiralayan;
        await servis.UpdateAsync(duzenlenen);
        var kaydedilen = (await servis.GetByIdAsync(kayit.Id))!;
        Assert.Equal(AracSahiplikTipi.Kiralik, kaydedilen.SahiplikTipi);
        Assert.Equal(c.Kiralayan, kaydedilen.KiralikCariId);
    }

    [Fact]
    public async Task Guncelleme_eski_sahipsiz_kiralik_arac_sahibe_dokunulmadan_guncellenir_sahibi_bosaltilamaz()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);

        // Kural öncesinden kalma, sahibi olmayan kiralık araç.
        var eski = await servis.CreateAsync(YeniArac("SASE-KRL-0005", AracSahiplikTipi.Ozmal, null), "34 KRL 005");
        await using (var db = f.Factory.CreateDbContext())
        {
            await db.Araclar.IgnoreQueryFilters().Where(a => a.Id == eski.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.SahiplikTipi, AracSahiplikTipi.Kiralik));
        }

        // Km güncellemesi (API / şase ekranı gibi) sahipliğe dokunmadığı için geçer.
        var duzenlenen = (await servis.GetByIdAsync(eski.Id))!;
        Assert.Null(duzenlenen.KiralikCariId);
        duzenlenen.KmDurumu = 12345;
        await servis.UpdateAsync(duzenlenen);
        Assert.Equal(12345, (await servis.GetByIdAsync(eski.Id))!.KmDurumu);

        // Sahibi seçildikten sonra boşaltılamaz.
        duzenlenen.KiralikCariId = c.Kiralayan;
        await servis.UpdateAsync(duzenlenen);
        duzenlenen.KiralikCariId = null;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => servis.UpdateAsync(duzenlenen));
        Assert.Equal(AracKurallari.KiralikSahipZorunluMesaji, ex.Message);
        Assert.Equal(c.Kiralayan, (await servis.GetByIdAsync(eski.Id))!.KiralikCariId);
    }

    // ── Excel aktarımı ──────────────────────────────────────────────────────

    [Fact]
    public async Task Excel_yeni_kiralik_arac_sahipsizse_satir_hata_verir_sahipliyse_unvan_veya_kodla_aktarilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);

        var sonuc = await servis.ImportFromExcelAsync(ExcelOlustur(
            ["Şase No *", "Plaka", "Sahiplik Tipi", "Araç Sahibi (Cari)"],
            ["EXCEL-0001", "34 EXC 001", "Kiralık", ""],                       // sahipsiz → hata
            ["EXCEL-0002", "34 EXC 002", "Kiralık", "kiralık sahibi"],         // unvan, büyük/küçük harf farkı
            ["EXCEL-0003", "34 EXC 003", "Kiralık", "oz kaynak insaat"],       // Türkçe karakter farkı
            ["EXCEL-0004", "34 EXC 004", "Kiralık", "OZK"],                    // cari kodu
            ["EXCEL-0005", "34 EXC 005", "Kiralık", "Bilinmeyen Ltd."],        // bulunamadı → hata
            ["EXCEL-0006", "34 EXC 006", "Kiralık", "Müşteri A.Ş."],           // tedarikçi değil → hata
            ["EXCEL-0007", "34 EXC 007", "Kiralık", "Aynı Ad"],                // iki cariyle eşleşir → hata
            ["EXCEL-0008", "34 EXC 008", "Özmal", "Kiralık Sahibi"]));         // özmalda sahip yok sayılır

        Assert.Equal(4, sonuc.ImportedCount);
        Assert.Equal(4, sonuc.ErrorCount);
        Assert.Contains(sonuc.Errors, e => e.Contains("EXCEL-0001") && e.Contains(AracKurallari.KiralikSahipZorunluMesaji));
        Assert.Contains(sonuc.Errors, e => e.Contains("EXCEL-0005") && e.Contains("bulunamadı"));
        Assert.Contains(sonuc.Errors, e => e.Contains("EXCEL-0006") && e.Contains("bulunamadı"));
        Assert.Contains(sonuc.Errors, e => e.Contains("EXCEL-0007") && e.Contains("birden fazla"));

        await using var db = f.Factory.CreateDbContext();
        var araclar = await db.Araclar.ToDictionaryAsync(a => a.SaseNo);
        Assert.Equal(new[] { "EXCEL-0002", "EXCEL-0003", "EXCEL-0004", "EXCEL-0008" }.Order(), araclar.Keys.Order());
        Assert.Equal(c.Kiralayan, araclar["EXCEL-0002"].KiralikCariId);
        Assert.Equal(c.OzKaynak, araclar["EXCEL-0003"].KiralikCariId);
        Assert.Equal(c.OzKaynak, araclar["EXCEL-0004"].KiralikCariId);
        Assert.Null(araclar["EXCEL-0008"].KiralikCariId);
    }

    [Fact]
    public async Task Excel_sahip_sutunu_olmayan_eski_sablonda_kiralik_satir_hata_verir_diger_satirlar_aktarilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await ServisKurAsync(f);

        var sonuc = await servis.ImportFromExcelAsync(ExcelOlustur(
            ["Şase No *", "Plaka", "Sahiplik Tipi"],
            ["ESKI-0001", "34 ESK 001", "Kiralık"],
            ["ESKI-0002", "34 ESK 002", "Özmal"]));

        Assert.Equal(1, sonuc.ImportedCount);
        Assert.Equal(1, sonuc.ErrorCount);
        await using var db = f.Factory.CreateDbContext();
        Assert.Equal("ESKI-0002", Assert.Single(await db.Araclar.ToListAsync()).SaseNo);
    }

    [Fact]
    public async Task Excel_mevcut_arac_guncellemesi_eski_sahipsiz_kiraliga_dokunmuyorsa_gecer_kiralik_yapiyorsa_sahip_ister()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);
        var eskiKiralik = await servis.CreateAsync(YeniArac("MEVCUT-0001", AracSahiplikTipi.Ozmal, null), "34 MVC 001");
        var ozmal = await servis.CreateAsync(YeniArac("MEVCUT-0002", AracSahiplikTipi.Ozmal, null), "34 MVC 002");
        await using (var db = f.Factory.CreateDbContext())
        {
            await db.Araclar.IgnoreQueryFilters().Where(a => a.Id == eskiKiralik.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.SahiplikTipi, AracSahiplikTipi.Kiralik));
        }

        var sonuc = await servis.ImportFromExcelAsync(ExcelOlustur(
            ["Şase No *", "Sahiplik Tipi", "KM", "Araç Sahibi (Cari)"],
            ["MEVCUT-0001", "", "5000", ""],            // sahiplik boş: eski sahipsiz kiralık km ile güncellenir
            ["MEVCUT-0002", "Kiralık", "6000", ""]));   // özmal → sahipsiz kiralık: reddedilir

        Assert.Equal(1, sonuc.UpdatedCount);
        Assert.Equal(1, sonuc.ErrorCount);
        Assert.Contains(sonuc.Errors, e => e.Contains("MEVCUT-0002") && e.Contains(AracKurallari.KiralikSahipZorunluMesaji));

        var sahipli = await servis.ImportFromExcelAsync(ExcelOlustur(
            ["Şase No *", "Sahiplik Tipi", "Araç Sahibi (Cari)"],
            ["MEVCUT-0002", "Kiralık", "Kiralık Sahibi"],
            ["MEVCUT-0001", "", "OZK"]));               // sahipsiz eski kiralık, sahip sütunuyla tamamlanır
        Assert.Equal(2, sahipli.UpdatedCount);
        Assert.Equal(0, sahipli.ErrorCount);

        await using var kontrol = f.Factory.CreateDbContext();
        var araclar = await kontrol.Araclar.ToDictionaryAsync(a => a.SaseNo);
        Assert.Equal(5000, araclar["MEVCUT-0001"].KmDurumu);
        Assert.Equal(c.OzKaynak, araclar["MEVCUT-0001"].KiralikCariId);
        Assert.Equal(AracSahiplikTipi.Kiralik, araclar["MEVCUT-0002"].SahiplikTipi);
        Assert.Equal(c.Kiralayan, araclar["MEVCUT-0002"].KiralikCariId);
    }

    [Fact]
    public async Task Excel_sablonu_arac_sahibi_sutununu_icerir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = await ServisKurAsync(f);

        using var akis = new MemoryStream(await servis.GetExcelSablonAsync());
        using var kitap = new XLWorkbook(akis);
        var sayfa = kitap.Worksheet(1);
        var basliklar = sayfa.Row(1).CellsUsed().Select(h => h.GetString()).ToList();

        Assert.Contains("Araç Sahibi (Cari)", basliklar);
        Assert.Contains("Sahiplik Tipi", basliklar);
    }

    // ── API ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Api_olusturma_sahipsiz_kiraliga_400_verir_sahipli_kiralik_araci_kaydeder()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var kontrol = new AraclarController(await ServisKurAsync(f), f.Guard);

        var red = await kontrol.Create(new AracCreateDto { AktifPlaka = "34 API 001", SaseNo = "API-0001", SahiplikTipi = "Kiralik" });
        var hata = Assert.IsType<BadRequestObjectResult>(red);
        Assert.Contains(AracKurallari.KiralikSahipZorunluMesaji, hata.Value!.ToString());

        var kabul = await kontrol.Create(new AracCreateDto
        {
            AktifPlaka = "34 API 002", SaseNo = "API-0002", SahiplikTipi = "Kiralik", KiralikCariId = c.Kiralayan
        });
        Assert.IsType<CreatedAtActionResult>(kabul);

        await using var db = f.Factory.CreateDbContext();
        var kayit = Assert.Single(await db.Araclar.ToListAsync());
        Assert.Equal("API-0002", kayit.SaseNo);
        Assert.Equal(c.Kiralayan, kayit.KiralikCariId);
    }

    [Fact]
    public async Task Api_guncelleme_sahipsiz_kiraliga_cevirmeyi_reddeder_eski_sahipsiz_kiralikta_km_guncellenir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var c = await CarileriTohumlaAsync(f.Factory);
        var servis = await ServisKurAsync(f);
        var kontrol = new AraclarController(servis, f.Guard);
        var ozmal = await servis.CreateAsync(YeniArac("API-0003", AracSahiplikTipi.Ozmal, null), "34 API 003");

        Assert.IsType<BadRequestObjectResult>(await kontrol.Update(ozmal.Id, new AracUpdateDto { SahiplikTipi = "Kiralik" }));
        Assert.Equal(AracSahiplikTipi.Ozmal, (await servis.GetByIdAsync(ozmal.Id))!.SahiplikTipi);

        Assert.IsType<OkObjectResult>(await kontrol.Update(ozmal.Id, new AracUpdateDto { SahiplikTipi = "Kiralik", KiralikCariId = c.Kiralayan }));
        var kiralik = (await servis.GetByIdAsync(ozmal.Id))!;
        Assert.Equal(AracSahiplikTipi.Kiralik, kiralik.SahiplikTipi);
        Assert.Equal(c.Kiralayan, kiralik.KiralikCariId);

        // Kural öncesinden kalma sahipsiz kiralık araçta km güncellemesi sahipliğe dokunmadığı için sürer.
        var eski = await servis.CreateAsync(YeniArac("API-0004", AracSahiplikTipi.Ozmal, null), "34 API 004");
        await using (var db = f.Factory.CreateDbContext())
        {
            await db.Araclar.IgnoreQueryFilters().Where(a => a.Id == eski.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.SahiplikTipi, AracSahiplikTipi.Kiralik));
        }

        Assert.IsType<OkObjectResult>(await kontrol.UpdateKm(eski.Id, new KmUpdateDto { KmDurumu = 4321 }));
        Assert.Equal(4321, (await servis.GetByIdAsync(eski.Id))!.KmDurumu);
    }

    // ── Yardımcılar ─────────────────────────────────────────────────────────

    private static Arac YeniArac(string saseNo, AracSahiplikTipi tip, int? kiralikCariId) => new()
    {
        FirmaId = 1,
        SaseNo = saseNo,
        AracTipi = AracTipi.Minibus,
        SahiplikTipi = tip,
        KiralikCariId = kiralikCariId
    };

    private static async Task<AracService> ServisKurAsync(PayrollWriteSqliteTests.Fixture f)
    {
        int kullaniciId;
        await using (var db = f.Factory.CreateDbContext())
            kullaniciId = await db.Kullanicilar.IgnoreQueryFilters().Select(k => k.Id).FirstAsync();

        var http = new SabitHttpErisimi
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, kullaniciId.ToString()) }, "Test"))
            }
        };
        return new AracService(f.Factory, null!, null!, f.ActiveFirm, NullLogger<AracService>.Instance, http, null!);
    }

    private static async Task<SahipCarileri> CarileriTohumlaAsync(IDbContextFactory<ApplicationDbContext> factory)
    {
        await using var db = factory.CreateDbContext();
        Cari Cari(string kod, string unvan, CariTipi tip) => new() { CariKodu = kod, Unvan = unvan, CariTipi = tip, FirmaId = 1 };
        var kiralayan = Cari("KRL", "Kiralık Sahibi", CariTipi.Tedarikci);
        var ozKaynak = Cari("OZK", "Öz Kaynak İnşaat", CariTipi.MusteriTedarikci);
        var musteri = Cari("MST", "Müşteri A.Ş.", CariTipi.Musteri);
        var ikizBir = Cari("IK1", "Aynı Ad", CariTipi.Tedarikci);
        var ikizIki = Cari("IK2", "Aynı Ad", CariTipi.Tedarikci);
        db.AddRange(kiralayan, ozKaynak, musteri, ikizBir, ikizIki);
        await db.SaveChangesAsync();
        return new SahipCarileri(kiralayan.Id, ozKaynak.Id, musteri.Id, ikizBir.Id, ikizIki.Id);
    }

    private static byte[] ExcelOlustur(string[] basliklar, params string[][] satirlar)
    {
        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Araclar");
        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];
        for (var r = 0; r < satirlar.Length; r++)
            for (var i = 0; i < satirlar[r].Length; i++)
                sayfa.Cell(r + 2, i + 1).Value = satirlar[r][i];

        using var akis = new MemoryStream();
        kitap.SaveAs(akis);
        return akis.ToArray();
    }

    /// <summary>HttpContextAccessor değeri AsyncLocal'de tuttuğundan, async yardımcıda atanan kullanıcı çağırana taşınmaz.</summary>
    private sealed class SabitHttpErisimi : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}