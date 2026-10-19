using Microsoft.EntityFrameworkCore;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;

namespace MKFiloServis.Tests;

/// <summary>
/// Genel arama Türkçe harf duyarsız olmalı: PostgreSQL lower() 'İ'yi küçültmediği, tr-TR'de ToLower() ise 'I'yı 'ı'
/// yaptığı için "ali", "filo", "demir" gibi aramalar ALİ / FİLO / DEMİREZ kayıtlarını bulamıyordu.
/// </summary>
public sealed class GlobalSearchTests
{
    // ---- saf yardımcılar ----

    [Theory]
    [InlineData("ALİ COŞAR", "ali cosar")]
    [InlineData("IŞIK ÇINAR", "isik cinar")]
    [InlineData("ısparta", "isparta")]
    [InlineData("ÜSTÜN FİLO TURİZM", "ustun filo turizm")]
    [InlineData("ŞÖFÖR İĞNE", "sofor igne")]
    [InlineData("Mercedes Sprinter 06FMU312", "mercedes sprinter 06fmu312")]
    [InlineData("", "")]
    public void Katla_turkce_harfleri_ve_buyuk_kucuk_harfi_ortaklastirir(string girdi, string beklenen)
        => Assert.Equal(beklenen, AramaMetni.Katla(girdi));

    [Fact]
    public void Katla_birlesik_nokta_isaretini_atar_ve_null_icin_bos_doner()
    {
        Assert.Equal("ali", AramaMetni.Katla("ali̇"));
        Assert.Equal("i", AramaMetni.Katla("i̇"));
        Assert.Equal(string.Empty, AramaMetni.Katla(null));
    }

    [Fact]
    public void Kelimeler_ayirir_katlar_ve_tekrarlari_atar()
    {
        Assert.Equal(new[] { "ali", "aydin", "boduroglu" }, AramaMetni.Kelimeler("  ALİ  aydın,Boduroğlu ; ALI").ToArray());
        Assert.Empty(AramaMetni.Kelimeler("   "));
        Assert.Empty(AramaMetni.Kelimeler(null));
    }

    [Theory]
    [InlineData("filo", "%f_l_%")]       // i ve o ailesi joker
    [InlineData("aydin", "%ayd_n%")]
    [InlineData("cosar", "%___ar%")]    // c, o, s ailesi joker
    [InlineData("06fmu312", "%06fm_312%")]
    [InlineData("50%", "%50\\%%")]       // kullanıcının yazdığı % ve _ kaçırılır
    [InlineData("a_b", "%a\\_b%")]
    public void KabaDesen_duyarsiz_harfleri_joker_yapar_ve_joker_karakterleri_kacirir(string kelime, string beklenen)
        => Assert.Equal(beklenen, AramaMetni.KabaDesen(AramaMetni.Katla(kelime)));

    [Fact]
    public void HepsiEslesir_kelimeleri_birlesik_alanlarda_ve_bosluksuz_yazimda_arar()
    {
        var ali = AramaMetni.Kelimeler("ali aydin");
        Assert.True(AramaMetni.HepsiEslesir(ali, "ALİ", "AYDIN", "PRS-0001"));
        Assert.False(AramaMetni.HepsiEslesir(AramaMetni.Kelimeler("ali kaya"), "ALİ", "AYDIN"));
        Assert.True(AramaMetni.HepsiEslesir(AramaMetni.Kelimeler("34abc123"), "34 ABC 123"));
        Assert.False(AramaMetni.HepsiEslesir(Array.Empty<string>(), "herhangi"));
    }

    [Fact]
    public void Puan_tam_eslesmeyi_baslangic_ve_icerikten_ustun_tutar()
    {
        var k = AramaMetni.Kelimeler("ali");
        var tam = AramaMetni.Puan(k, "ALİ", "C-1");
        var baslangic = AramaMetni.Puan(k, "ALİ COŞAR", "C-2");
        var icerik = AramaMetni.Puan(k, "MEHMET ALİ", "C-3");

        Assert.True(tam > baslangic);
        Assert.True(baslangic > icerik);
        Assert.True(icerik > 0);
    }

    // ---- veritabanı (SQLite: lower() PostgreSQL gibi yalnız ASCII harfleri küçültür) ----

    [Fact]
    public async Task Cari_aramasi_ali_ve_filo_gibi_kucuk_harfli_yazimlari_ALİ_ve_FİLO_kayitlarina_esler()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        var ali = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("ali"), 10);
        Assert.Contains(ali, x => x.Baslik == "ALİ COŞAR");
        Assert.Contains(ali, x => x.Baslik == "ALI YILMAZ");
        Assert.DoesNotContain(ali, x => x.Baslik == "Mehmet Kaya");

        foreach (var yazim in new[] { "filo", "FİLO", "FILO", "Filo" })
        {
            var filo = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler(yazim), 10);
            Assert.Single(filo);
            Assert.Equal("ÜSTÜN FİLO TURİZM", filo[0].Baslik);
        }
    }

    [Fact]
    public async Task Buyuk_I_ile_yazilan_ve_turkce_harfsiz_aramalar_calisir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        foreach (var yazim in new[] { "ISPARTA", "ısparta", "isparta", "İsparta" })
        {
            var sonuc = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler(yazim), 10);
            Assert.Equal("ISPARTA TAŞIMACILIK", Assert.Single(sonuc).Baslik);
        }

        // Türkçe klavye olmadan: cosar → COŞAR, ustun → ÜSTÜN
        Assert.Contains(await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("cosar"), 10), x => x.Baslik == "ALİ COŞAR");
        Assert.Contains(await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("ustun"), 10), x => x.Baslik == "ÜSTÜN FİLO TURİZM");
    }

    [Fact]
    public async Task Cok_kelimeli_arama_ad_ve_soyadi_birlikte_arar()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        var ad = await servis.SearchPersonellerAsync(db, AramaMetni.Kelimeler("ali aydin"), 10);
        Assert.Equal("ALİ AYDIN", Assert.Single(ad).Baslik);

        var ayse = await servis.SearchPersonellerAsync(db, AramaMetni.Kelimeler("ayse demirez"), 10);
        Assert.Equal("AYŞE DEMİREZ", Assert.Single(ayse).Baslik);

        // Yalnız soyadın bir kısmı: DEMİREZ → "demir"
        var soyad = await servis.SearchPersonellerAsync(db, AramaMetni.Kelimeler("demir"), 10);
        Assert.Equal("AYŞE DEMİREZ", Assert.Single(soyad).Baslik);

        // Ad aynı, soyad farklı → hiçbiri
        Assert.Empty(await servis.SearchPersonellerAsync(db, AramaMetni.Kelimeler("ali kaya"), 10));
    }

    [Fact]
    public async Task Joker_karakterler_kullanici_girdisinde_duz_metin_sayilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        var yuzde = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("100%"), 10);
        Assert.Equal("100% Net_Ticaret", Assert.Single(yuzde).Baslik);

        // "net_t" alt çizgisi tek karakter jokeri değil: NETXTICARET'i getirmemeli
        var altCizgi = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("net_t"), 10);
        Assert.Equal("100% Net_Ticaret", Assert.Single(altCizgi).Baslik);
    }

    [Fact]
    public async Task Telefon_vergi_no_ve_cari_koduyla_arama_yapilir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        Assert.Equal("Mehmet Kaya", Assert.Single(await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("0532 123"), 10)).Baslik);
        Assert.Equal("Mehmet Kaya", Assert.Single(await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("1234567890"), 10)).Baslik);
        Assert.Equal("ÜSTÜN FİLO TURİZM", Assert.Single(await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("c-2"), 10)).Baslik);
    }

    [Fact]
    public async Task Arac_plakasi_bosluklu_ve_bitisik_yazimla_bulunur()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        foreach (var yazim in new[] { "06fmu312", "06 FMU 312", "fmu312", "sprinter" })
            Assert.Equal("06FMU312", Assert.Single(await servis.SearchAraclarAsync(db, AramaMetni.Kelimeler(yazim), 10)).Baslik);

        foreach (var yazim in new[] { "34abc123", "34 ABC 123", "34 abc" })
            Assert.Equal("34 ABC 123", Assert.Single(await servis.SearchAraclarAsync(db, AramaMetni.Kelimeler(yazim), 10)).Baslik);
    }

    [Fact]
    public async Task Fatura_ve_guzergah_aramasi_cari_unvanini_turkce_duyarsiz_arar_ve_tam_eslesme_basa_gelir()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        await TohumlaAsync(f);
        var servis = Servis(f);
        await using var db = f.Factory.CreateDbContext();

        var fatura = await servis.SearchFaturalarAsync(db, AramaMetni.Kelimeler("cosar"), 10);
        Assert.Equal("SF-2026-000001", Assert.Single(fatura).Baslik);

        var guzergah = await servis.SearchGuzergahlarAsync(db, AramaMetni.Kelimeler("ali"), 10);
        Assert.Equal("Gebze Sabah", Assert.Single(guzergah).Baslik);

        // Sıralama: "ali" tam eşleşen kayıt (ALİ) içerenlerden önce gelir
        db.Cariler.Add(new Cari { CariKodu = "C-9", Unvan = "ALİ", CariTipi = CariTipi.Musteri, FirmaId = 1 });
        await db.SaveChangesAsync();
        var sirali = await servis.SearchCarilerAsync(db, AramaMetni.Kelimeler("ali"), 10);
        Assert.Equal("ALİ", sirali[0].Baslik);
    }

    [Fact]
    public async Task Cok_kisa_veya_bos_arama_veritabanina_gitmeden_bos_doner()
    {
        await using var f = await PayrollWriteSqliteTests.Fixture.CreateAsync();
        var servis = Servis(f);

        Assert.Equal(0, (await servis.SearchAsync("a")).ToplamSonuc);
        Assert.Equal(0, (await servis.SearchAsync("   ")).ToplamSonuc);
        Assert.Equal(0, (await servis.SearchAsync("")).ToplamSonuc);
    }

    // ---- yardımcılar ----

    // İzin/lisans bağımlılıkları yalnızca SearchAsync'te kullanılır; sorgu yöntemleri doğrudan test edilir.
    private static GlobalSearchService Servis(PayrollWriteSqliteTests.Fixture f)
        => new(f.Factory, null!, null!);

    private static async Task TohumlaAsync(PayrollWriteSqliteTests.Fixture f)
    {
        await using var db = f.Factory.CreateDbContext();

        Cari C(string kod, string unvan, string? vergi = null, string? telefon = null) => new()
        {
            CariKodu = kod, Unvan = unvan, CariTipi = CariTipi.Musteri, FirmaId = 1, VergiNo = vergi, Telefon = telefon
        };
        var ali = C("C-1", "ALİ COŞAR");
        db.Cariler.AddRange(
            ali,
            C("C-2", "ÜSTÜN FİLO TURİZM"),
            C("C-3", "ALI YILMAZ"),
            C("C-4", "Mehmet Kaya", "1234567890", "05321234567"),
            C("C-5", "100% Net_Ticaret"),
            C("C-6", "NETXTICARET"),
            C("C-7", "ISPARTA TAŞIMACILIK"));
        await db.SaveChangesAsync();

        Sofor S(string kod, string ad, string soyad) => new() { SoforKodu = kod, Ad = ad, Soyad = soyad, FirmaId = 1, Aktif = true };
        db.Soforler.AddRange(S("PRS-0002", "AYŞE", "DEMİREZ"), S("PRS-0003", "ALİ", "AYDIN"), S("PRS-0004", "Ahmet", "Şahin"));

        Arac A(string sase, string plaka, string marka, string model) => new()
        {
            SaseNo = sase, AktifPlaka = plaka, Marka = marka, Model = model, SahiplikTipi = AracSahiplikTipi.Ozmal, FirmaId = 1
        };
        db.Araclar.AddRange(A("S1", "06FMU312", "Mercedes", "Sprinter"), A("S2", "34 ABC 123", "Ford", "Transit"));

        db.Guzergahlar.Add(new Guzergah { GuzergahKodu = "G-1", GuzergahAdi = "Gebze Sabah", CariId = ali.Id, FirmaId = 1 });
        db.Faturalar.Add(new Fatura
        {
            FaturaNo = "SF-2026-000001", FaturaTarihi = new DateTime(2026, 10, 3), FaturaYonu = FaturaYonu.Giden,
            FaturaTipi = FaturaTipi.SatisFaturasi, CariId = ali.Id, FirmaId = 1, GenelToplam = 1200
        });
        await db.SaveChangesAsync();
    }
}
