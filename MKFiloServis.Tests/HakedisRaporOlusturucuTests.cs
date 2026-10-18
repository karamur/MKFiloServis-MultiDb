using ClosedXML.Excel;
using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Services;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Tests;

/// <summary>Hakediş önizlemesinin gruplanması ve Excel / PDF çıktısı aynı satırlardan üretilir.</summary>
public sealed class HakedisRaporOlusturucuTests
{
    private static HakedisKapsamSatiri Satir(
        int cariId, string cari, string guzergah, string plaka, AracSahiplikTipi tip, string sahipAnahtari, string sahip,
        decimal sefer, decimal gelir, decimal gider, decimal maliyet = 0m, bool hakedistemi = false) => new()
    {
        ReferansId = cariId,
        ReferansAd = cari,
        CariId = cariId,
        CariAd = cari,
        GuzergahId = guzergah.GetHashCode(),
        GuzergahAd = guzergah,
        AracId = plaka.GetHashCode(),
        Plaka = plaka,
        SahiplikTipi = tip,
        SahipAnahtari = sahipAnahtari,
        SahipAd = sahip,
        SoforId = 1,
        SoforAd = "Ali Yılmaz",
        PuantajSayisi = (int)sefer,
        Sefer = sefer,
        Gelir = gelir,
        Gider = gider,
        Maliyet = maliyet,
        HakedisTutari = gelir, // Kurum hakedişi görünümü
        HakedisteMi = hakedistemi
    };

    private static readonly HakedisKapsamSatiri[] Ornek =
    {
        Satir(1, "Kurum A", "Rota 1", "34 AAA 001", AracSahiplikTipi.Ozmal, "O", "Özmal (firma aracı)", 2m, 200m, 0m, 20m),
        Satir(1, "Kurum A", "Rota 1", "34 BBB 002", AracSahiplikTipi.Kiralik, "K:7", "Kiralık Sahibi", 1m, 100m, 0m, 20m),
        Satir(2, "Kurum B", "Rota 2", "34 CCC 003", AracSahiplikTipi.Tedarikci, "T:3", "Tedarikçi X", 4m, 600m, 400m)
    };

    [Fact]
    public void Gruplama_secilen_boyuta_gore_baslik_ve_toplam_uretir()
    {
        var cariyeGore = HakedisRaporOlusturucu.Grupla(Ornek, HakedisGruplama.Cari);
        Assert.Equal(new[] { "Kurum A", "Kurum B" }, cariyeGore.Select(g => g.Baslik).ToArray());
        Assert.Equal(3m, cariyeGore[0].Sefer);
        Assert.Equal(300m, cariyeGore[0].Gelir);
        Assert.Equal(300m, cariyeGore[0].HakedisTutari);

        var sahibeGore = HakedisRaporOlusturucu.Grupla(Ornek, HakedisGruplama.AracSahibi);
        Assert.Equal(3, sahibeGore.Count);
        var tedarikci = Assert.Single(sahibeGore, g => g.Anahtar == "T:3");
        Assert.Equal("Tedarikçi X", tedarikci.Baslik);
        Assert.Equal("Tedarikçi", tedarikci.Alt);
        Assert.Equal(400m, tedarikci.Gider);

        var gruplamasiz = HakedisRaporOlusturucu.Grupla(Ornek, HakedisGruplama.Yok);
        Assert.Single(gruplamasiz);
        Assert.Equal(3, gruplamasiz[0].Satirlar.Count);
    }

    [Fact]
    public void Birlestir_eslestirmeli_ve_elle_girilen_ayni_satirlari_tek_satirda_toplar()
    {
        var eslestirmeli = Satir(1, "Kurum A", "Rota 1", "34 AAA 001", AracSahiplikTipi.Ozmal, "O", "Özmal", 21m, 2100m, 0m) with { EslestirmeId = 5, EslestirmeAd = "A • Rota 1" };
        var elleGirilen = Satir(1, "Kurum A", "Rota 1", "34 AAA 001", AracSahiplikTipi.Ozmal, "O", "Özmal", 4m, 400m, 0m) with { EslestirmeId = 0, EslestirmeAd = "Eşleştirmesiz" };
        // Aynı cari ve güzergâh ama farklı araç: ayrı kalmalı
        var baskaArac = Satir(1, "Kurum A", "Rota 1", "34 ZZZ 009", AracSahiplikTipi.Ozmal, "O", "Özmal", 1m, 100m, 0m);

        var birlesik = HakedisRaporOlusturucu.Birlestir(new[] { eslestirmeli, elleGirilen, baskaArac }, eslestirmeyiAyir: false);
        Assert.Equal(2, birlesik.Count);
        var toplanan = Assert.Single(birlesik, s => s.Plaka == "34 AAA 001");
        Assert.Equal(25m, toplanan.Sefer);
        Assert.Equal(2500m, toplanan.Gelir);
        Assert.Equal(25, toplanan.PuantajSayisi); // 21 + 4 puantaj kaydı birleşti
        Assert.Equal("Birden fazla eşleştirme", toplanan.EslestirmeAd);

        // Eşleştirmeye göre gruplanırken eşleştirmeler ayrı tutulur.
        var ayri = HakedisRaporOlusturucu.Birlestir(new[] { eslestirmeli, elleGirilen, baskaArac }, eslestirmeyiAyir: true);
        Assert.Equal(3, ayri.Count);
    }

    [Fact]
    public void Tablo_gruplanan_boyutun_kolonunu_gizler_ve_toplamlari_dogru_yazar()
    {
        var tablo = HakedisRaporOlusturucu.TabloOlustur(
            Ornek, HakedisTipi.Kurum, HakedisGruplama.Cari, karlilikKolonlari: false, 2026, 10, "Tüm dönem");

        var basliklar = tablo.Kolonlar.Select(k => k.Baslik).ToList();
        Assert.DoesNotContain("Cari", basliklar);
        Assert.Contains("Güzergâh", basliklar);
        Assert.Contains("Kesilecek Fatura", basliklar);
        Assert.Equal(2, tablo.Gruplar.Count);
        Assert.StartsWith("CARİ: Kurum A", tablo.Gruplar[0].Baslik);

        var tutarKolonu = basliklar.IndexOf("Kesilecek Fatura");
        Assert.Equal(300m, tablo.Gruplar[0].AraToplam![tutarKolonu]);
        Assert.Equal(900m, tablo.GenelToplam[tutarKolonu]);
        Assert.Equal(7m, tablo.GenelToplam[basliklar.IndexOf("Sefer")]);
    }

    [Fact]
    public void Karlilik_kolonlari_gelir_gider_maliyet_ve_neti_ayri_yazar()
    {
        var tablo = HakedisRaporOlusturucu.TabloOlustur(
            Ornek, HakedisTipi.Kurum, HakedisGruplama.Yok, karlilikKolonlari: true, 2026, 10, "Tüm dönem");

        var basliklar = tablo.Kolonlar.Select(k => k.Baslik).ToList();
        Assert.Equal(new[] { "Gelir", "Gider", "İç Maliyet", "Net" }, basliklar.Skip(basliklar.IndexOf("Gelir")).Take(4).ToArray());
        Assert.Equal(900m, tablo.GenelToplam[basliklar.IndexOf("Gelir")]);
        Assert.Equal(400m, tablo.GenelToplam[basliklar.IndexOf("Gider")]);
        Assert.Equal(40m, tablo.GenelToplam[basliklar.IndexOf("İç Maliyet")]);
        Assert.Equal(460m, tablo.GenelToplam[basliklar.IndexOf("Net")]);
        Assert.Null(tablo.Gruplar[0].Baslik);
        Assert.Null(tablo.Gruplar[0].AraToplam);
    }

    [Fact]
    public void Hakedistekiler_dahilse_durum_kolonu_eklenir()
    {
        var satirlar = Ornek.Append(Satir(2, "Kurum B", "Rota 2", "34 DDD 004", AracSahiplikTipi.Ozmal, "O", "Özmal", 1m, 50m, 0m, hakedistemi: true)).ToList();
        var tablo = HakedisRaporOlusturucu.TabloOlustur(
            satirlar, HakedisTipi.Kurum, HakedisGruplama.Yok, false, 2026, 10, "Tüm dönem");

        Assert.Contains("Durum", tablo.Kolonlar.Select(k => k.Baslik));
        var tum = tablo.Gruplar.SelectMany(g => g.Satirlar).ToList();
        Assert.Contains(tum, s => (string?)s[^1] == "Hakedişte");
        Assert.Contains(tum, s => (string?)s[^1] == "Yeni");
    }

    [Fact]
    public void Excel_ayni_tablodan_sayisal_hucrelerle_uretilir()
    {
        var tablo = HakedisRaporOlusturucu.TabloOlustur(
            Ornek, HakedisTipi.Kurum, HakedisGruplama.Cari, false, 2026, 10, "Güzergâh: Rota 1");

        var bytes = HakedisRaporOlusturucu.ExcelOlustur(tablo);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);
        Assert.Equal("HAKEDİŞ RAPORU", ws.Cell(1, 1).GetString());
        Assert.Contains("10/2026", ws.Cell(2, 1).GetString());
        Assert.Contains("Güzergâh: Rota 1", ws.Cell(3, 1).GetString());

        var sonSatir = ws.LastRowUsed()!.RowNumber();
        Assert.Equal("GENEL TOPLAM", ws.Cell(sonSatir, 1).GetString());
        var tutarKolonu = tablo.Kolonlar.FindIndex(k => k.Baslik == "Kesilecek Fatura") + 1;
        Assert.Equal(900d, ws.Cell(sonSatir, tutarKolonu).GetDouble());
    }

    [Fact]
    public void Pdf_gecerli_bir_pdf_belgesi_uretir()
    {
        var tablo = HakedisRaporOlusturucu.TabloOlustur(
            Ornek, HakedisTipi.Kurum, HakedisGruplama.AracSahibi, true, 2026, 10, "Tüm dönem");

        var bytes = new PdfService().GenerateHakedisRaporTablosuPdf(tablo);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
